using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Game.Contracts;
using Game.Core;
using Game.Procedural;

using var game = new NaturalWorldGame();
game.Run();

internal sealed class NaturalWorldGame : Microsoft.Xna.Framework.Game
{
    private const int Tile = 10;
    private const int MapLeft = 24;
    private const int MapTop = 78;
    private readonly GraphicsDeviceManager _graphics;
    private readonly Dictionary<VisualKey, Texture2D> _visualCache = [];
    private readonly Dictionary<char, string[]> _glyphs = TinyFont.Glyphs;
    private SpriteBatch _batch = null!;
    private Texture2D _pixel = null!;
    private WorldSimulation _world = WorldSimulation.Create(1);
    private WorldSnapshot _snapshot = WorldSimulation.Create(1).Snapshot();
    private KeyboardState _previous;
    private double _tickAccumulator;
    private bool _paused;
    private int _overlayMode;
    private int _speedIndex = 2;
    private static readonly int[] Speeds = [1, 2, 6, 12, 24];
    private readonly string _savePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Game", "natural-world-save.json");

    public NaturalWorldGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1024,
            PreferredBackBufferHeight = 760,
            SynchronizeWithVerticalRetrace = true
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "Temperate Valley · flora L0";
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        _batch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _snapshot = _world.Snapshot();
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (Pressed(keyboard, Keys.Escape)) Exit();
        if (Pressed(keyboard, Keys.Space)) _paused = !_paused;
        if (Pressed(keyboard, Keys.D1)) _overlayMode = 0;
        if (Pressed(keyboard, Keys.D2)) _overlayMode = 1;
        if (Pressed(keyboard, Keys.D3)) _overlayMode = 2;
        if (Pressed(keyboard, Keys.OemPlus) || Pressed(keyboard, Keys.Add)) _speedIndex = Math.Min(Speeds.Length - 1, _speedIndex + 1);
        if (Pressed(keyboard, Keys.OemMinus) || Pressed(keyboard, Keys.Subtract)) _speedIndex = Math.Max(0, _speedIndex - 1);
        if (Pressed(keyboard, Keys.F5)) SaveWorld();
        if (Pressed(keyboard, Keys.F9)) LoadWorld();

        if (!_paused)
        {
            _tickAccumulator += gameTime.ElapsedGameTime.TotalSeconds * Speeds[_speedIndex];
            var steps = Math.Min(120, (int)_tickAccumulator);
            if (steps > 0)
            {
                _world.AdvanceTicks(steps);
                _tickAccumulator -= steps;
                _snapshot = _world.Snapshot();
            }
        }
        _previous = keyboard;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 27, 33));
        _batch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
        DrawMap();
        DrawHud();
        _batch.End();
        base.Draw(gameTime);
    }

    private void DrawMap()
    {
        for (var i = 0; i < _snapshot.Cells.Count; i++)
        {
            var cell = _snapshot.Cells[i];
            var rect = new Rectangle(MapLeft + cell.X * Tile, MapTop + cell.Y * Tile, Tile, Tile);
            _batch.Draw(_pixel, rect, TerrainColor(cell));
            var water = Math.Clamp(cell.SurfaceWaterMm / 8, 0, 0.72);
            if (water > 0.015)
                _batch.Draw(_pixel, rect, new Color(50, 133, 191) * (float)water);
            if (_overlayMode == 1)
            {
                var moisture = Math.Clamp((cell.SoilWaterMm - cell.SoilWiltMm) / (cell.SoilCapacityMm - cell.SoilWiltMm), 0, 1);
                _batch.Draw(_pixel, rect, new Color((byte)(95 - moisture * 55), (byte)(65 + moisture * 125), (byte)(40 + moisture * 145), (byte)120));
            }
            else if (_overlayMode == 2)
            {
                var biomass = Math.Clamp((cell.Grass.BiomassGPerM2 + cell.Woody.BiomassGPerM2) / 1200, 0, 1);
                _batch.Draw(_pixel, rect, new Color((byte)20, (byte)(80 + biomass * 155), (byte)45, (byte)145));
            }
        }

        // Presentation is a projection of confirmed plant stocks; all visual randomness has a separate salt.
        for (var i = 0; i < _snapshot.Cells.Count; i++)
        {
            var cell = _snapshot.Cells[i];
            var x = MapLeft + cell.X * Tile;
            var y = MapTop + cell.Y * Tile;
            var visualSeed = CounterRandom.VisualBits(_snapshot.Seed, cell.Index, 0x4752415353UL);
            if (RockVisible(cell, visualSeed))
                DrawSprite(TextureFor(new(VisualKind.Rock, (int)(visualSeed & 7), 0, _snapshot.Season)), x + 2, y + 3, 7, 5);
            if (cell.Grass.Present && cell.Grass.BiomassGPerM2 > 1)
            {
                var bucket = Math.Clamp((int)(cell.Grass.BiomassGPerM2 / 150), 1, 4);
                DrawSprite(TextureFor(new(VisualKind.Grass, (int)(visualSeed >> 8 & 7), bucket, _snapshot.Season)), x + 1, y + 1, 9, 9);
            }
            if (cell.Woody.Present && cell.Woody.BiomassGPerM2 > 1)
            {
                var bucket = Math.Clamp((int)(cell.Woody.BiomassGPerM2 / 450), 1, 4);
                var texture = TextureFor(new(VisualKind.Woody, (int)(visualSeed >> 16 & 7), bucket, _snapshot.Season));
                var size = 11 + bucket;
                DrawSprite(texture, x + Tile / 2 - size / 2, y + Tile / 2 - size / 2, size, size);
            }
        }
        _batch.Draw(_pixel, new Rectangle(MapLeft - 1, MapTop - 1, Tile * 64 + 2, 1), new Color(139, 161, 162));
        _batch.Draw(_pixel, new Rectangle(MapLeft - 1, MapTop + Tile * 64, Tile * 64 + 2, 1), new Color(139, 161, 162));
        _batch.Draw(_pixel, new Rectangle(MapLeft - 1, MapTop - 1, 1, Tile * 64 + 2), new Color(139, 161, 162));
        _batch.Draw(_pixel, new Rectangle(MapLeft + Tile * 64, MapTop - 1, 1, Tile * 64 + 2), new Color(139, 161, 162));
    }

    private void DrawHud()
    {
        var day = _snapshot.DayOfYear.ToString("D3");
        DrawText($"TEMPERATE VALLEY   DAY {day} {SeasonName(_snapshot.Season)}   TICK {_snapshot.TickIndex}", 24, 22, new Color(231, 226, 201), 2);
        var mode = _overlayMode switch { 1 => "SOIL WATER", 2 => "BIOMASS", _ => "TERRAIN" };
        DrawText($"VIEW {mode}   {( _paused ? "PAUSED" : $"RUN {Speeds[_speedIndex]} TICKS/S" )}", 24, 47, new Color(149, 203, 190), 1);

        DrawText("1 TERRAIN  2 SOIL  3 BIOMASS   SPACE PAUSE   +/- SPEED   F5 SAVE   F9 LOAD", 700, 90, new Color(207, 216, 207), 1);
        DrawText("PIXEL ART GENERATED FROM SNAPSHOTS", 700, 112, new Color(132, 155, 152), 1);
        DrawLegend(700, 146, new Color(103, 128, 76), "GRASS PATCH");
        DrawLegend(700, 166, new Color(103, 146, 82), "WOODY + LEAVES");
        DrawLegend(700, 186, new Color(76, 132, 178), "SURFACE WATER");
        DrawLegend(700, 206, new Color(126, 127, 121), "PROCEDURAL ROCK");
        DrawText("LEAF COLOR FOLLOWS THE CALENDAR", 700, 238, new Color(178, 171, 133), 1);

        var mouse = Mouse.GetState();
        var x = (mouse.X - MapLeft) / Tile;
        var y = (mouse.Y - MapTop) / Tile;
        if (mouse.X >= MapLeft && mouse.Y >= MapTop && x >= 0 && x < 64 && y >= 0 && y < 64)
        {
            var cell = _snapshot.Cells[y * 64 + x];
            var info = $"CELL {cell.Index}  Z {cell.ElevationM:F0}M  S {cell.SoilWaterMm:F1}MM  W {cell.SurfaceWaterMm:F1}MM";
            DrawText(info, 700, 292, new Color(228, 220, 196), 1);
            DrawText($"N {cell.MineralNitrogenGPerM2:F2}G/M2  GRASS {cell.Grass.BiomassGPerM2:F0}  WOODY {cell.Woody.BiomassGPerM2:F0}", 700, 310, new Color(184, 204, 178), 1);
            DrawText($"SEEDS {(cell.Grass.SeedBankGPerM2 + cell.Woody.SeedBankGPerM2):F2}G/M2  LITTER {cell.DetritusDryGPerM2:F1}G/M2", 700, 328, new Color(180, 188, 157), 1);
        }
        DrawText($"SEED {_snapshot.Seed}   CACHED VISUALS {_visualCache.Count}", 700, 704, new Color(125, 145, 143), 1);
        DrawText("NO FAUNA IN THIS BUILD", 24, 735, new Color(125, 145, 143), 1);
    }

    private void DrawLegend(int x, int y, Color color, string text)
    {
        _batch.Draw(_pixel, new Rectangle(x, y, 11, 11), color);
        DrawText(text, x + 19, y + 2, new Color(191, 201, 189), 1);
    }

    private void DrawSprite(Texture2D texture, int x, int y, int width, int height)
        => _batch.Draw(texture, new Rectangle(x, y, width, height), Color.White);

    private Texture2D TextureFor(VisualKey key)
    {
        if (_visualCache.TryGetValue(key, out var cached)) return cached;
        var (width, height) = key.Kind switch
        {
            VisualKind.Rock => (8, 6),
            VisualKind.Grass => (12, 12),
            _ => (20, 20)
        };
        var pixels = new Color[width * height];
        void Put(int x, int y, Color color)
        {
            if (x >= 0 && x < width && y >= 0 && y < height) pixels[y * width + x] = color;
        }

        if (key.Kind == VisualKind.Rock)
        {
            for (var y = 1; y < height; y++)
            for (var x = 1; x < width - 1; x++)
            {
                var edge = ((x * 13 + y * 7 + key.Variant * 5) % 9) > 1;
                if (edge) Put(x, y, (x + y + key.Variant) % 3 == 0 ? new Color(142, 148, 137) : new Color(88, 101, 98));
            }
            Put(2, 2, new Color(175, 172, 150));
        }
        else if (key.Kind == VisualKind.Grass)
        {
            var color = GrassColor(key.Season);
            for (var blade = 0; blade < key.Variant + 4; blade++)
            {
                var bx = 2 + (blade * 7 + key.Variant * 3) % (width - 4);
                var h = 3 + (blade * 5 + key.BiomassBucket * 3) % (5 + key.BiomassBucket);
                for (var dy = 0; dy < h; dy++) Put(bx + (dy / 3) * (blade % 2 == 0 ? -1 : 1), height - 1 - dy, color);
            }
        }
        else
        {
            var bark = new Color(91, 69, 49);
            var foliage = LeafColor(key.Season);
            Line(pixels, width, height, 10, 19, 10, 7, bark);
            var branches = new[] { (10, 14, 5, 9), (10, 13, 15, 8), (10, 10, 6, 5), (10, 9, 14, 4) };
            foreach (var (x1, y1, x2, y2) in branches) Line(pixels, width, height, x1, y1, x2, y2, bark);
            if (key.Season != Season.Winter)
            {
                foreach (var (cx, cy) in new[] { (5, 8), (14, 7), (7, 4), (13, 3), (10, 6) })
                {
                    var radius = 2 + (key.Variant + cx + key.BiomassBucket) % 2;
                    for (var dy = -radius; dy <= radius; dy++)
                    for (var dx = -radius; dx <= radius; dx++)
                        if (Math.Abs(dx) + Math.Abs(dy) <= radius + 1 && (dx * 3 + dy * 5 + key.Variant) % 7 != 0)
                            Put(cx + dx, cy + dy, ((dx + dy + key.Variant) % 5 == 0) ? Shade(foliage, 0.78f) : foliage);
                }
            }
        }

        var texture = new Texture2D(GraphicsDevice, width, height);
        texture.SetData(pixels);
        _visualCache.Add(key, texture);
        return texture;
    }

    private static void Line(Color[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color color)
    {
        var dx = Math.Abs(x1 - x0); var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0); var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;
        while (true)
        {
            if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height) pixels[y0 * width + x0] = color;
            if (x0 == x1 && y0 == y1) break;
            var twice = 2 * error;
            if (twice >= dy) { error += dy; x0 += sx; }
            if (twice <= dx) { error += dx; y0 += sy; }
        }
    }

    private static Color TerrainColor(WorldCell cell)
    {
        var normalized = Math.Clamp((cell.ElevationM - 25) / 120, 0, 1);
        return cell.SoilKind == SoilKind.Loam
            ? new Color((byte)(66 + normalized * 23), (byte)(100 + normalized * 15), (byte)(63 + normalized * 4))
            : new Color((byte)(94 + normalized * 21), (byte)(91 + normalized * 15), (byte)(72 + normalized * 10));
    }

    private static Color GrassColor(Season season) => season switch
    {
        Season.Spring => new Color(131, 176, 79),
        Season.Summer => new Color(79, 140, 63),
        Season.Autumn => new Color(174, 145, 67),
        _ => new Color(126, 126, 83)
    };

    private static Color LeafColor(Season season) => season switch
    {
        Season.Spring => new Color(116, 171, 76),
        Season.Summer => new Color(49, 117, 61),
        Season.Autumn => new Color(190, 117, 48),
        _ => new Color(0, 0, 0, 0)
    };

    private static Color Shade(Color color, float factor)
        => new((byte)(color.R * factor), (byte)(color.G * factor), (byte)(color.B * factor), color.A);

    private static bool RockVisible(WorldCell cell, ulong visualSeed)
    {
        var chance = cell.ElevationM > 100 ? 0.055 : cell.ElevationM > 75 ? 0.02 : 0.006;
        return CounterRandom.Unit(visualSeed, 0x524F434BUL, (ulong)cell.Index, 0, 0, 0) < chance;
    }

    private static string SeasonName(Season season) => season.ToString().ToUpperInvariant();

    private void DrawText(string text, int x, int y, Color color, int scale)
    {
        foreach (var ch in text)
        {
            if (!_glyphs.TryGetValue(ch, out var rows)) { x += 4 * scale; continue; }
            for (var py = 0; py < rows.Length; py++)
            for (var px = 0; px < rows[py].Length; px++)
                if (rows[py][px] == '1') _batch.Draw(_pixel, new Rectangle(x + px * scale, y + py * scale, scale, scale), color);
            x += 4 * scale;
        }
    }

    private bool Pressed(KeyboardState state, Keys key) => state.IsKeyDown(key) && !_previous.IsKeyDown(key);

    private void SaveWorld()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_savePath)!);
        File.WriteAllText(_savePath, _world.SaveToJson());
    }

    private void LoadWorld()
    {
        if (!File.Exists(_savePath)) return;
        _world = WorldSimulation.Load(File.ReadAllText(_savePath));
        _snapshot = _world.Snapshot();
        _tickAccumulator = 0;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var texture in _visualCache.Values) texture.Dispose();
            _pixel?.Dispose();
            _batch?.Dispose();
        }
        base.Dispose(disposing);
    }

    private enum VisualKind : byte { Rock, Grass, Woody }
    private readonly record struct VisualKey(VisualKind Kind, int Variant, int BiomassBucket, Season Season);
}

internal static class TinyFont
{
    public static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['A']=["010","101","111","101","101"], ['B']=["110","101","110","101","110"],
        ['C']=["011","100","100","100","011"], ['D']=["110","101","101","101","110"],
        ['E']=["111","100","110","100","111"], ['F']=["111","100","110","100","100"],
        ['G']=["011","100","101","101","011"], ['H']=["101","101","111","101","101"],
        ['I']=["111","010","010","010","111"], ['J']=["001","001","001","101","010"],
        ['K']=["101","101","110","101","101"], ['L']=["100","100","100","100","111"],
        ['M']=["101","111","111","101","101"], ['N']=["101","111","111","111","101"],
        ['O']=["010","101","101","101","010"], ['P']=["110","101","110","100","100"],
        ['Q']=["010","101","101","011","001"], ['R']=["110","101","110","101","101"],
        ['S']=["011","100","010","001","110"], ['T']=["111","010","010","010","010"],
        ['U']=["101","101","101","101","111"], ['V']=["101","101","101","101","010"],
        ['W']=["101","101","111","111","101"], ['X']=["101","101","010","101","101"],
        ['Y']=["101","101","010","010","010"], ['Z']=["111","001","010","100","111"],
        ['0']=["111","101","101","101","111"], ['1']=["010","110","010","010","111"],
        ['2']=["110","001","010","100","111"], ['3']=["110","001","010","001","110"],
        ['4']=["101","101","111","001","001"], ['5']=["111","100","110","001","110"],
        ['6']=["011","100","110","101","010"], ['7']=["111","001","010","010","010"],
        ['8']=["010","101","010","101","010"], ['9']=["010","101","011","001","110"],
        [':']=["000","010","000","010","000"], ['.']=["000","000","000","000","010"],
        ['-']=["000","000","111","000","000"]
    };
}
