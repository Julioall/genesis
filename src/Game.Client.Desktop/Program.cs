using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Game.Contracts;
using Game.Core;
using Game.Procedural;
using Game.Client.Desktop;

using var game = new NaturalWorldGame();
game.Run();

internal sealed class NaturalWorldGame : Microsoft.Xna.Framework.Game
{
    private const int Tile = 10;
    private const float GroundM11 = 2.10f;
    private const float GroundM12 = 0.035f;
    private const float GroundM21 = 0.18f;
    private const float GroundM22 = 1.11f;
    private float MapLeft => (GraphicsDevice.Viewport.Width
        - SimulationState.Width * Tile * (GroundM11 + GroundM21)) / 2f;
    private float MapTop => (GraphicsDevice.Viewport.Height
        - SimulationState.Height * Tile * (GroundM12 + GroundM22)) / 2f + 18f;
    private static readonly int[] DepthOrder = Enumerable.Range(0, SimulationState.Width * SimulationState.Height)
        .OrderBy(index => index / SimulationState.Width * GroundM22 + index % SimulationState.Width * GroundM12)
        .ToArray();
    private readonly GraphicsDeviceManager _graphics;
    private readonly Dictionary<VisualKey, Texture2D> _visualCache = [];
    private readonly Dictionary<char, string[]> _glyphs = TinyFont.Glyphs;
    private SpriteBatch _batch = null!;
    private Texture2D _pixel = null!;
    private Texture2D _terrainTexture = null!;
    private long _terrainRevision = -1;
    private int _terrainOverlay = -1;
    private double _minimumElevationM;
    private WorldSimulation _world = WorldSimulation.Create(1);
    private WorldSnapshot _snapshot = WorldSimulation.Create(1).Snapshot();
    private KeyboardState _previous;
    private double _tickAccumulator;
    private bool _paused;
    private bool _showHud = true;
    private int _overlayMode;
    private int _visualSeasonIndex = (int)Season.Summer;
    private int _speedIndex = 2;
    private static readonly int[] Speeds = [1, 2, 6, 12, 24];
    private readonly string _savePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Game", "natural-world-save.json");

    public NaturalWorldGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1600,
            PreferredBackBufferHeight = 900,
            SynchronizeWithVerticalRetrace = true
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "Genesis - Temperate Valley (flora L0)";
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        _batch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _snapshot = _world.Snapshot();
        _terrainTexture = new Texture2D(GraphicsDevice, SimulationState.Width * Tile, SimulationState.Height * Tile);
        RefreshTerrainTexture(force: true);
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
        if (Pressed(keyboard, Keys.H)) _showHud = !_showHud;
        if (Pressed(keyboard, Keys.V))
        {
            _visualSeasonIndex = (_visualSeasonIndex + 1) % 4;
            RefreshTerrainTexture(force: true);
        }
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
                RefreshTerrainTexture();
            }
        }
        RefreshTerrainTexture();
        _previous = keyboard;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(15, 23, 25));
        _batch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend,
            transformMatrix: GroundTransform());
        _batch.Draw(_terrainTexture, Vector2.Zero, Color.White);
        _batch.End();

        _batch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
        DrawGroundObjects();
        if (_showHud) DrawHud();
        _batch.End();
        base.Draw(gameTime);
    }

    private void DrawGroundObjects()
    {
        // Render back-to-front using the projected ground-plane Y coordinate.
        foreach (var index in DepthOrder)
        {
            var cell = _snapshot.Cells[index];
            var visualSeed = CounterRandom.VisualBits(_snapshot.Seed, cell.Index, 0x4752415353UL);
            var ground = GroundToScreen(cell.X * Tile + Tile / 2.0, cell.Y * Tile + Tile / 2.0, cell.ElevationM);

            if (cell.Woody.Present && cell.Woody.BiomassGPerM2 > 1)
            {
                var bucket = Math.Clamp((int)(cell.Woody.BiomassGPerM2 / 450), 1, 4);
                var size = 31 + bucket * 6;
                var shadowWidth = size + 12 + (int)(visualSeed & 5);
                DrawSprite(TextureFor(new(VisualKind.Shadow, (int)(visualSeed & 7), bucket, VisualSeason)),
                    (int)ground.X - shadowWidth / 2 + 7, (int)ground.Y - Math.Max(4, size / 5), shadowWidth,
                    Math.Max(8, size / 2));
            }

            if (RockVisible(cell, visualSeed))
            {
                var rockVariant = (int)(visualSeed & 7);
                var rockSize = 20 + rockVariant % 4 * 3;
                DrawSprite(TextureFor(new(VisualKind.Rock, rockVariant, 0, VisualSeason)),
                    (int)ground.X - rockSize / 2, (int)ground.Y - rockSize / 2 + 2, rockSize, rockSize);
            }

            if (cell.Grass.Present && cell.Grass.BiomassGPerM2 > 1)
            {
                var bucket = Math.Clamp((int)(cell.Grass.BiomassGPerM2 / 150), 1, 4);
                var mask = GrassMaskAt(_snapshot, cell.X, cell.Y);
                var neighborCount = 0;
                for (var bit = 0; bit < 8; bit++) neighborCount += (mask >> bit) & 1;
                var tuftCount = bucket >= 4 && neighborCount >= 4 ? 3 : bucket >= 3 || neighborCount >= 5 ? 2 : 1;
                for (var tuft = 0; tuft < tuftCount; tuft++)
                {
                    var tuftSeed = CounterRandom.VisualBits(_snapshot.Seed, cell.Index, 0x4752415353UL + (ulong)tuft);
                    var width = 12 + (int)(tuftSeed & 3) + bucket;
                    var height = 10 + (int)(tuftSeed >> 4 & 3) + bucket * 2;
                    var offsetX = tuftCount == 1 ? 0 : (tuft - (tuftCount - 1) / 2) * 6;
                    var offsetY = tuftCount == 1 ? 0 : tuft % 2 * 2;
                    DrawSprite(TextureFor(new(VisualKind.Grass, (int)(tuftSeed >> 8 & 15), bucket, VisualSeason)),
                        (int)ground.X - width / 2 + offsetX, (int)ground.Y - height + offsetY, width, height);
                }
            }

            if (cell.Woody.Present && cell.Woody.BiomassGPerM2 > 1)
            {
                var bucket = Math.Clamp((int)(cell.Woody.BiomassGPerM2 / 450), 1, 4);
                var texture = TextureFor(new(VisualKind.Woody, (int)(visualSeed >> 16 & 7), bucket, VisualSeason));
                var size = 31 + bucket * 6;
                DrawSprite(texture, (int)ground.X - size / 2, (int)ground.Y - size + 5, size, size);
            }
        }
    }

    private void DrawHud()
    {
        var viewport = GraphicsDevice.Viewport;
        var mode = _overlayMode switch { 1 => "SOIL WATER", 2 => "BIOMASS", _ => "LANDSCAPE" };
        const int cardX = 22;
        const int cardY = 20;
        const int cardWidth = 560;
        const int cardHeight = 90;
        _batch.Draw(_pixel, new Rectangle(cardX, cardY, cardWidth, cardHeight), new Color(10, 18, 20, 174));
        _batch.Draw(_pixel, new Rectangle(cardX, cardY, 3, cardHeight), new Color(157, 174, 112, 225));
        DrawText("GENESIS  /  TEMPERATE VALLEY", cardX + 20, cardY + 13, new Color(231, 226, 201), 2);
        DrawText($"DAY {_snapshot.DayOfYear:D3} {SeasonName(_snapshot.Season)}  ART {SeasonName(VisualSeason)}  {mode}",
            cardX + 21, cardY + 42, new Color(149, 203, 190), 1);
        DrawText($"{(_paused ? "PAUSED" : $"RUN {Speeds[_speedIndex]} TICKS/S")}  1 LAND  2 WATER  3 BIOMASS  SPACE PAUSE  H HIDE",
            cardX + 21, cardY + 61, new Color(207, 216, 207), 1);

        var mouse = Mouse.GetState();
        var screenX = mouse.X - MapLeft;
        var screenY = mouse.Y - MapTop;
        var determinant = GroundM11 * GroundM22 - GroundM12 * GroundM21;
        var worldX = (screenX * GroundM22 - screenY * GroundM21) / determinant;
        var worldY = (screenY * GroundM11 - screenX * GroundM12) / determinant;
        var x = (int)Math.Floor(worldX / Tile);
        var y = (int)Math.Floor(worldY / Tile);
        if (x >= 0 && x < SimulationState.Width && y >= 0 && y < SimulationState.Height)
        {
            var cell = _snapshot.Cells[y * SimulationState.Width + x];
            const int inspectWidth = 390;
            const int inspectHeight = 52;
            var inspectX = Math.Clamp(mouse.X + 18, 12, viewport.Width - inspectWidth - 12);
            var inspectY = Math.Clamp(mouse.Y + 18, 12, viewport.Height - inspectHeight - 12);
            _batch.Draw(_pixel, new Rectangle(inspectX, inspectY, inspectWidth, inspectHeight), new Color(10, 18, 20, 188));
            DrawText($"CELL {cell.Index}  Z {cell.ElevationM:F0}M  WATER {cell.SurfaceWaterMm:F1}MM",
                inspectX + 10, inspectY + 9, new Color(228, 220, 196), 1);
            DrawText($"GRASS {cell.Grass.BiomassGPerM2:F0}  WOODY {cell.Woody.BiomassGPerM2:F0}  SOIL {cell.SoilWaterMm:F0}MM",
                inspectX + 10, inspectY + 28, new Color(184, 204, 178), 1);
        }
        DrawText($"SEED {_snapshot.Seed}   NO FAUNA IN THIS BUILD", 26, viewport.Height - 24,
            new Color(220, 225, 210, 210), 1);
    }

    private void DrawLegend(int x, int y, Color color, string text)
    {
        _batch.Draw(_pixel, new Rectangle(x, y, 11, 11), color);
        DrawText(text, x + 19, y + 2, new Color(191, 201, 189), 1);
    }

    private void DrawSprite(Texture2D texture, int x, int y, int width, int height)
        => _batch.Draw(texture, new Rectangle(x, y, width, height), Color.White);

    private void RefreshTerrainTexture(bool force = false)
    {
        var cadence = _overlayMode == 1 ? SimulationState.TicksPerHour : SimulationState.TicksPerDay;
        var revision = _snapshot.TickIndex / cadence;
        if (!force && revision == _terrainRevision && _overlayMode == _terrainOverlay) return;
        _terrainTexture.SetData(PixelTerrainTexture.Generate(_snapshot, Tile, _overlayMode, VisualSeason));
        _minimumElevationM = _snapshot.Cells.Min(cell => cell.ElevationM);
        _terrainRevision = revision;
        _terrainOverlay = _overlayMode;
    }

    private Texture2D TextureFor(VisualKey key)
    {
        if (_visualCache.TryGetValue(key, out var cached)) return cached;
        var (width, height) = key.Kind switch
        {
            VisualKind.Rock => (24, 20),
            VisualKind.Grass => (18, 15),
            VisualKind.Shadow => (24, 12),
            _ => (64, 64)
        };
        var pixels = new Color[width * height];
        void Put(int x, int y, Color color)
        {
            if (x >= 0 && x < width && y >= 0 && y < height) pixels[y * width + x] = color;
        }

        if (key.Kind == VisualKind.Shadow)
        {
            var centerX = width / 2;
            var centerY = height / 2;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var dx = (x - centerX) / (double)(width / 2);
                var dy = (y - centerY) / (double)(height / 2);
                var distance = dx * dx + dy * dy;
                if (distance > 1.0 + ((x * 7 + y * 11 + key.Variant) % 5) * 0.055) continue;
                var alpha = (byte)Math.Clamp(30 + key.BiomassBucket * 5 - (int)(distance * 15), 12, 48);
                Put(x, y, new Color(28, 39, 31, (int)alpha));
            }
        }
        else if (key.Kind == VisualKind.Rock)
        {
            var baseRock = (key.Variant % 3) switch
            {
                0 => new Color(103, 112, 105),
                1 => new Color(112, 103, 91),
                _ => new Color(91, 107, 108)
            };
            var peaks = key.Variant % 3 == 0
                ? new[] { (6, 10, 5, 8), (13, 7, 6, 11), (19, 12, 4, 6) }
                : key.Variant % 3 == 1
                    ? new[] { (5, 12, 5, 6), (12, 8, 7, 10), (20, 13, 4, 5) }
                    : new[] { (4, 13, 4, 5), (10, 9, 6, 8), (17, 11, 6, 8) };
            foreach (var (cx, cy, rx, ry) in peaks)
            for (var y = Math.Max(2, cy - ry); y <= Math.Min(height - 2, cy + ry); y++)
            for (var x = Math.Max(1, cx - rx); x <= Math.Min(width - 2, cx + rx); x++)
            {
                var dx = (x - cx) / (double)rx;
                var dy = (y - cy) / (double)ry;
                var noise = (x * 11 + y * 17 + key.Variant * 7) % 13;
                if (dx * dx + dy * dy > 1.0 + (noise % 4) * 0.05) continue;
                var face = x < cx - rx / 4.0;
                var shade = face ? 1.23f : y > cy + 2 ? 0.69f : (x + y + noise) % 7 == 0 ? 1.15f : 0.93f;
                Put(x, y, Shade(baseRock, shade));
            }
            // Angular facets and a few loose stones make the mass read as a weathered outcrop.
            Line(pixels, width, height, 3, 15, 7, 17, Shade(baseRock, 0.65f));
            Line(pixels, width, height, 12, 2, 11, 7, Shade(baseRock, 1.32f));
            Line(pixels, width, height, 18, 11, 20, 14, Shade(baseRock, 0.62f));
            Put(5, 16, Shade(baseRock, 0.72f));
            Put(21, 16, Shade(baseRock, 0.76f));
            Put(13, 5, new Color(174, 165, 138));
        }
        else if (key.Kind == VisualKind.Grass)
        {
            var color = GrassColor(key.Season);
            var bladeCount = 5 + key.Variant % 4 + key.BiomassBucket;
            for (var blade = 0; blade < bladeCount; blade++)
            {
                var cluster = blade % 3;
                var baseX = 3 + cluster * 6 + (blade * 7 + key.Variant * 3) % 5 - 2;
                var baseY = height - 1 - cluster % 2;
                var bladeHeight = 4 + (blade * 5 + key.BiomassBucket * 2 + key.Variant) % (5 + key.BiomassBucket);
                var lean = (blade * 3 + key.Variant) % 5 - 2;
                var bladeColor = blade % 5 == 0 ? Shade(color, 1.24f) : blade % 4 == 0 ? Shade(color, 0.78f) : color;
                Line(pixels, width, height, baseX, baseY, baseX + lean / 2, baseY - bladeHeight / 2, bladeColor);
                Line(pixels, width, height, baseX + lean / 2, baseY - bladeHeight / 2,
                    baseX + lean, baseY - bladeHeight, bladeColor);
                if (blade % 3 != 1)
                    Line(pixels, width, height, baseX + 1, baseY, baseX + lean + (lean < 0 ? -1 : 1), baseY - bladeHeight + 3, Shade(color, 0.78f));
            }
            // Small rosettes and occasional blooms break the repeated blade rhythm.
            for (var cluster = 0; cluster < 2; cluster++)
            {
                var cx = 4 + cluster * 8 + key.Variant % 3;
                var cy = height - 2;
                for (var leaf = -1; leaf <= 1; leaf++)
                    Line(pixels, width, height, cx, cy, cx + leaf * 3, cy - Math.Abs(leaf) - 1, Shade(color, leaf == 0 ? 0.86f : 1.12f));
            }
            if ((key.Season is Season.Spring or Season.Summer) && (key.Variant % 4 == 0 || key.Variant % 7 == 0))
            {
                Put(7 + key.Variant % 5, 6 + key.Variant % 4, new Color(226, 207, 127));
                if (key.BiomassBucket > 2) Put(10 + key.Variant % 4, 8 + key.Variant % 3, new Color(235, 226, 175));
            }
        }
        else if (key.Kind == VisualKind.Woody)
        {
            DrawTreePixels(pixels, width, height, key);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported visual kind: {key.Kind}");
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

    private static void DrawTreePixels(Color[] pixels, int width, int height, VisualKey key)
    {
        var center = width / 2 + key.Variant % 5 - 2;
        var conifer = key.Variant % 8 >= 4;
        var bark = new Color(78, 55, 38);
        var barkLight = new Color(132, 96, 61);
        var trunkWidth = key.BiomassBucket >= 3 ? 3 : 2;

        double Roll(int index, ulong salt) => CounterRandom.Unit(
            (ulong)(key.Variant + 1), salt, (ulong)index, (ulong)key.BiomassBucket, 0, 0);
        void Put(int x, int y, Color color)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
                pixels[y * width + x] = color;
        }

        if (conifer)
        {
            var pine = new[]
            {
                new Color(34, 66, 43), new Color(43, 78, 49), new Color(49, 86, 51),
                new Color(40, 75, 48)
            }[key.Variant % 4];
            for (var tier = 0; tier < 12; tier++)
            {
                var progress = tier / 11.0;
                var y = 8 + tier * 4;
                var span = 4 + (int)(progress * 21);
                var drop = 3 + (int)((1 - progress) * 4) + (int)(Roll(tier, 0x50494E4554494552UL) * 3);
                var shade = tier % 3 == 0 ? 1.13f : tier % 3 == 1 ? 0.91f : 1.0f;
                var bough = Shade(pine, shade);
                Line(pixels, width, height, center, y, center - span, y + drop, Shade(bough, 0.78f));
                Line(pixels, width, height, center, y, center + span, y + drop, Shade(bough, 0.72f));

                for (var px = Math.Max(0, center - span); px <= Math.Min(width - 1, center + span); px++)
                {
                    var taper = 1 - Math.Abs(px - center) / (double)span;
                    var fringe = (int)(taper * (3 + progress * 4));
                    var top = y + Math.Max(0, (int)(Math.Abs(px - center) * 0.16) - 1);
                    for (var py = top; py <= y + drop + fringe; py++)
                    {
                        var grain = (px * 17 + py * 29 + key.Variant * 13 + tier * 7) % 19;
                        if (grain == 0 || grain == 11 && taper < 0.45) continue;
                        var light = px < center - span * 0.2 && py < y + drop ? 1.17f : grain % 7 == 0 ? 0.76f : 1.0f;
                        Put(px, py, Shade(bough, light));
                    }
                }
                if (tier % 2 == 0)
                    Put(center - span + 1, y + drop, Shade(pine, 1.36f));
            }

            // A dark central leader and a few gaps keep the crown from reading as a flat triangle.
            for (var x = center - 1; x <= center + 1; x++)
                Line(pixels, width, height, x, height - 2, center + (x - center) / 2, 5, x % 2 == 0 ? barkLight : bark);
        }
        else
        {
            var crown = LeafColor(key.Season, key.Variant);
            var branchCount = 18;
            for (var branch = 0; branch < branchCount; branch++)
            {
                var side = branch % 2 == 0 ? -1 : 1;
                var level = branch / 2;
                var anchorY = 47 - level * 3;
                var spread = 9 + (int)(Roll(branch, 0x4F414B4252414E43UL) * 17);
                var tipY = anchorY - 5 - (int)(Roll(branch, 0x4252414E43485249UL) * 11);
                Line(pixels, width, height, center, anchorY, center + side * spread, tipY,
                    branch % 3 == 0 ? barkLight : bark);
                if (branch % 3 == 0)
                    Line(pixels, width, height, center + side * (spread / 2), anchorY - 2,
                        center + side * (spread + 3), tipY - 4, bark);
            }

            if (key.Season != Season.Winter)
            {
                // Rounded, overlapping leaf masses use irregular silhouettes and a lit canopy edge.
                const int clusterCount = 11;
                for (var cluster = 0; cluster < clusterCount; cluster++)
                {
                    var angle = Math.PI * 2 * cluster / clusterCount - Math.PI / 2
                        + (Roll(cluster, 0x4C454146414E474CUL) - 0.5) * 0.42;
                    var radius = 11 + (int)(Roll(cluster, 0x4C45414652414449UL) * 7);
                    var cx = center + (int)Math.Round(Math.Cos(angle) * radius);
                    var cy = 28 + (int)Math.Round(Math.Sin(angle) * radius * 0.68);
                    var rx = 8 + (int)(Roll(cluster, 0x4C45414657494454UL) * 5);
                    var ry = 7 + (int)(Roll(cluster, 0x4C45414648454947UL) * 5);
                    var clusterColor = Shade(crown, (cluster % 4) switch
                    {
                        0 => 0.76f,
                        1 => 1.12f,
                        2 => 0.91f,
                        _ => 1.0f
                    });
                    for (var py = Math.Max(2, cy - ry); py <= Math.Min(height - 3, cy + ry); py++)
                    for (var px = Math.Max(2, cx - rx); px <= Math.Min(width - 3, cx + rx); px++)
                    {
                        var dx = (px - cx) / (double)rx;
                        var dy = (py - cy) / (double)ry;
                        var grain = (px * 13 + py * 7 + key.Variant * 11 + cluster * 17) % 23;
                        var edgeNoise = ((grain % 7) - 3) * 0.035;
                        if (dx * dx + dy * dy > 1.02 + edgeNoise || grain == 0 || grain == 15) continue;
                        var lit = px < cx - 2 && py < cy ? 1.16f : py > cy + 2 || px > cx ? 0.78f : grain % 6 == 0 ? 1.13f : 0.98f;
                        Put(px, py, Shade(clusterColor, lit));
                        if (grain % 9 == 0) Put(px + 1, py, Shade(clusterColor, 0.72f));
                    }
                }

                // Fine twigs and branch forks show through gaps in the leaf clusters.
                for (var branch = 0; branch < branchCount; branch++)
                {
                    var side = branch % 2 == 0 ? -1 : 1;
                    var level = branch / 2;
                    var anchorY = 47 - level * 3;
                    var spread = 9 + (int)(Roll(branch, 0x4F414B4252414E43UL) * 17);
                    var tipY = anchorY - 5 - (int)(Roll(branch, 0x4252414E43485249UL) * 11);
                    Line(pixels, width, height, center, anchorY, center + side * spread, tipY, bark);
                }
            }

            // A curved trunk stays visible below the crown and has a sunlit edge.
            for (var y = 24; y < height; y++)
            {
                var bend = (int)Math.Round(Math.Sin(y * 0.09 + key.Variant) * 1.5);
                for (var x = -trunkWidth / 2; x <= trunkWidth / 2; x++)
                    Put(center + bend + x, y, x < 0 ? barkLight : bark);
            }
            if (key.Season == Season.Winter)
            {
                for (var branch = 0; branch < 10; branch++)
                {
                    var side = branch % 2 == 0 ? -1 : 1;
                    var y = 44 - branch * 2;
                    var reach = 8 + (int)(Roll(branch, 0x57494E5445525457UL) * 15);
                    Line(pixels, width, height, center, y, center + side * reach, y - 9, barkLight);
                    Line(pixels, width, height, center + side * (reach / 2), y - 4,
                        center + side * (reach + 3), y - 11, bark);
                }
            }
        }
    }

    private static Color GrassColor(Season season) => season switch
    {
        Season.Spring => new Color(131, 176, 79),
        Season.Summer => new Color(79, 140, 63),
        Season.Autumn => new Color(174, 145, 67),
        _ => new Color(126, 126, 83)
    };

    private static Color LeafColor(Season season, int variant) => season switch
    {
        Season.Spring => new[] { new Color(137, 181, 82), new Color(108, 157, 70), new Color(162, 188, 88), new Color(94, 143, 68) }[variant % 4],
        Season.Summer => new[] { new Color(47, 103, 52), new Color(66, 125, 58), new Color(80, 133, 58), new Color(42, 91, 54) }[variant % 4],
        Season.Autumn => new[] { new Color(93, 131, 59), new Color(166, 150, 62), new Color(197, 119, 49), new Color(159, 87, 43) }[variant % 4],
        _ => new Color(0, 0, 0, 0)
    };

    private static Color Shade(Color color, float factor)
        => new((byte)(color.R * factor), (byte)(color.G * factor), (byte)(color.B * factor), color.A);

    private static bool RockVisible(WorldCell cell, ulong visualSeed)
    {
        var chance = cell.ElevationM > 100 ? 0.12 : cell.ElevationM > 75 ? 0.045 : 0.01;
        return CounterRandom.Unit(visualSeed, 0x524F434BUL, (ulong)cell.Index, 0, 0, 0) < chance;
    }

    private Matrix GroundTransform()
    {
        var matrix = Matrix.Identity;
        matrix.M11 = GroundM11;
        matrix.M12 = GroundM12;
        matrix.M21 = GroundM21;
        matrix.M22 = GroundM22;
        matrix.M41 = MapLeft;
        matrix.M42 = MapTop;
        return matrix;
    }

    private Vector2 GroundToScreen(double worldX, double worldY, double elevationM)
    {
        var x = MapLeft + worldX * GroundM11 + worldY * GroundM21;
        var y = MapTop + worldX * GroundM12 + worldY * GroundM22 - Math.Max(0, elevationM - _minimumElevationM) * 0.35;
        return new Vector2((float)x, (float)y);
    }

    private Season VisualSeason => (Season)_visualSeasonIndex;

    private static int GrassMaskAt(WorldSnapshot world, int x, int y)
    {
        bool At(int px, int py) => px >= 0 && py >= 0 && px < world.Width && py < world.Height
            && world.Cells[py * world.Width + px].Grass.Present
            && world.Cells[py * world.Width + px].Grass.BiomassGPerM2 > 1;

        var north = At(x, y - 1);
        var east = At(x + 1, y);
        var south = At(x, y + 1);
        var west = At(x - 1, y);
        return (north ? 1 : 0)
            | (north && east && At(x + 1, y - 1) ? 2 : 0)
            | (east ? 4 : 0)
            | (east && south && At(x + 1, y + 1) ? 8 : 0)
            | (south ? 16 : 0)
            | (south && west && At(x - 1, y + 1) ? 32 : 0)
            | (west ? 64 : 0)
            | (west && north && At(x - 1, y - 1) ? 128 : 0);
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
        RefreshTerrainTexture(force: true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var texture in _visualCache.Values) texture.Dispose();
            _pixel?.Dispose();
            _terrainTexture?.Dispose();
            _batch?.Dispose();
        }
        base.Dispose(disposing);
    }

    private enum VisualKind : byte { Rock, Grass, Woody, Shadow }
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
