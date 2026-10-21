using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using Game.Contracts;
using Game.Core;
using Game.Procedural;
using Game.Client.Desktop;
using Game.Modules.Geosphere;
using Game.Modules.Hydrosphere;

var capturePath = GetCapturePath(args);
var benchmarkSeconds = GetBenchmarkDurationSeconds(args);
using var game = new NaturalWorldGame(capturePath, benchmarkSeconds);
game.Run();

static string? GetCapturePath(string[] arguments)
{
    for (var index = 0; index < arguments.Length - 1; index++)
        if (arguments[index] == "--capture") return Path.GetFullPath(arguments[index + 1]);
    return null;
}

static int? GetBenchmarkDurationSeconds(string[] arguments)
{
    for (var index = 0; index < arguments.Length - 1; index++)
        if (arguments[index] == "--benchmark-seconds")
            return Math.Clamp(int.Parse(arguments[index + 1]), 1, 3600);
    return null;
}

internal sealed class NaturalWorldGame : Microsoft.Xna.Framework.Game
{
    private const int Tile = 10;
    private const int TerrainSubdivisions = 8;
    private const int TerrainChunkCells = 16;
    private const int WaterSubdivisions = 8;
    private const int VisibleRiverThresholdCells = 24;
    private const float TerrainExtentM = SimulationState.Width * Tile;
    private static readonly int[] DepthOrder = Enumerable.Range(0, SimulationState.Width * SimulationState.Height)
        .OrderBy(index => index / SimulationState.Width)
        .ThenBy(index => index % SimulationState.Width)
        .ToArray();
    private readonly GraphicsDeviceManager _graphics;
    private readonly string? _capturePath;
    private readonly int? _benchmarkDurationSeconds;
    private readonly List<double> _benchmarkFrameTimesMs = [];
    private readonly Dictionary<VisualKey, Texture2D> _visualCache = [];
    private readonly Dictionary<char, string[]> _glyphs = TinyFont.Glyphs;
    private SpriteBatch _batch = null!;
    private Texture2D _pixel = null!;
    private Texture2D _terrainTexture = null!;
    private BasicEffect _terrainEffect = null!;
    private BasicEffect _waterEffect = null!;
    private readonly List<TerrainRenderChunk> _terrainChunks = [];
    private VertexBuffer? _plateauVertexBuffer;
    private IndexBuffer? _plateauIndexBuffer;
    private int _plateauPrimitiveCount;
    private VertexBuffer? _cliffVertexBuffer;
    private int _cliffPrimitiveCount;
    private VertexBuffer? _waterVertexBuffer;
    private int _waterPrimitiveCount;
    private Matrix _view = Matrix.Identity;
    private Matrix _projection = Matrix.Identity;
    private Func<double, double, double> _terrainHeightSampler = null!;
    private Func<double, double, double> _underlyingHeightSampler = null!;
    private double _centerElevationM;
    private long _terrainRevision = -1;
    private long _waterRevision = -1;
    private int _terrainOverlay = -1;
    private WorldSimulation _world = WorldSimulation.Create(1);
    private WorldSnapshot _snapshot = null!;
    private KeyboardState _previous;
    private double _tickAccumulator;
    private double _fpsElapsedSeconds;
    private double _fps;
    private int _fpsFrameCount;
    private double _benchmarkWarmupSeconds;
    private double _benchmarkMeasuredSeconds;
    private long _benchmarkPreviousTimestamp;
    private TimeSpan _benchmarkCpuStart;
    private bool _paused;
    private bool _showHud = true;
    private int _overlayMode;
    private int _visualSeasonIndex = (int)Season.Summer;
    private int _speedIndex = 2;
    private static readonly int[] Speeds = [1, 2, 6, 12, 24];
    private readonly string _savePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Game", "natural-world-save.json");

    public NaturalWorldGame(string? capturePath, int? benchmarkDurationSeconds)
    {
        _capturePath = capturePath;
        _benchmarkDurationSeconds = benchmarkDurationSeconds;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = benchmarkDurationSeconds is null ? 1600 : 1280,
            PreferredBackBufferHeight = benchmarkDurationSeconds is null ? 900 : 720,
            SynchronizeWithVerticalRetrace = true
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "ASTERRA - Temperate Valley (L0)";
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
        _terrainEffect = new BasicEffect(GraphicsDevice)
        {
            TextureEnabled = true,
            VertexColorEnabled = false,
            LightingEnabled = true,
            AmbientLightColor = new Vector3(0.78f, 0.79f, 0.72f)
        };
        _terrainEffect.DirectionalLight0.Enabled = true;
        _terrainEffect.DirectionalLight0.DiffuseColor = new Vector3(0.22f, 0.22f, 0.19f);
        _terrainEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(0.45f, -1f, 0.35f));
        _terrainEffect.DirectionalLight0.SpecularColor = Vector3.Zero;
        _waterEffect = new BasicEffect(GraphicsDevice)
        {
            TextureEnabled = false,
            VertexColorEnabled = true,
            LightingEnabled = false
        };
        BuildTerrainGeometry();
        RefreshWaterGeometry(force: true);
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
        RefreshWaterGeometry();
        _previous = keyboard;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _fpsElapsedSeconds += gameTime.ElapsedGameTime.TotalSeconds;
        _fpsFrameCount++;
        if (_fpsElapsedSeconds >= 1)
        {
            _fps = _fpsFrameCount / _fpsElapsedSeconds;
            _fpsElapsedSeconds = 0;
            _fpsFrameCount = 0;
        }
        UpdateCamera();
        GraphicsDevice.Clear(new Color(21, 34, 34));
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

        _terrainEffect.World = Matrix.Identity;
        _terrainEffect.View = _view;
        _terrainEffect.Projection = _projection;
        _terrainEffect.Texture = _terrainTexture;
        _terrainEffect.TextureEnabled = true;
        _terrainEffect.VertexColorEnabled = false;
        _terrainEffect.LightingEnabled = true;
        var terrainFrustum = new BoundingFrustum(_view * _projection);
        foreach (var chunk in _terrainChunks)
        {
            if (!terrainFrustum.Intersects(chunk.Bounds)) continue;
            GraphicsDevice.SetVertexBuffer(chunk.Vertices);
            GraphicsDevice.Indices = chunk.Indices;
            foreach (var pass in _terrainEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, chunk.PrimitiveCount);
            }
        }

        DrawPlateauGeometry();
        DrawWaterSurface();

        GraphicsDevice.DepthStencilState = DepthStencilState.None;
        _batch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
        DrawGroundObjects();
        if (_showHud) DrawHud();
        _batch.End();
        if (_capturePath is not null)
        {
            CaptureFrameAndExit();
            base.Draw(gameTime);
            return;
        }
        RecordBenchmarkFrame();
        base.Draw(gameTime);
    }

    private void DrawWaterSurface()
    {
        if (_waterVertexBuffer is null || _waterPrimitiveCount == 0) return;
        _waterEffect.World = Matrix.Identity;
        _waterEffect.View = _view;
        _waterEffect.Projection = _projection;
        GraphicsDevice.BlendState = BlendState.AlphaBlend;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.SetVertexBuffer(_waterVertexBuffer);
        GraphicsDevice.Indices = null;
        foreach (var pass in _waterEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, _waterPrimitiveCount);
        }
        GraphicsDevice.BlendState = BlendState.Opaque;
    }

    private void CaptureFrameAndExit()
    {
        var viewport = GraphicsDevice.Viewport;
        var pixels = new Color[viewport.Width * viewport.Height];
        GraphicsDevice.GetBackBufferData(pixels);
        using var capturedFrame = new Texture2D(GraphicsDevice, viewport.Width, viewport.Height);
        capturedFrame.SetData(pixels);
        var directory = Path.GetDirectoryName(_capturePath!);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        using (var output = File.Create(_capturePath!)) capturedFrame.SaveAsPng(output, viewport.Width, viewport.Height);
        Exit();
    }

    private void RecordBenchmarkFrame()
    {
        if (_benchmarkDurationSeconds is null) return;
        var now = Stopwatch.GetTimestamp();
        if (_benchmarkPreviousTimestamp == 0)
        {
            _benchmarkPreviousTimestamp = now;
            return;
        }

        var frameSeconds = Stopwatch.GetElapsedTime(_benchmarkPreviousTimestamp, now).TotalSeconds;
        _benchmarkPreviousTimestamp = now;
        if (_benchmarkWarmupSeconds < 5)
        {
            _benchmarkWarmupSeconds += frameSeconds;
            return;
        }

        if (_benchmarkFrameTimesMs.Count == 0)
        {
            using var benchmarkProcess = Process.GetCurrentProcess();
            _benchmarkCpuStart = benchmarkProcess.TotalProcessorTime;
        }
        _benchmarkFrameTimesMs.Add(frameSeconds * 1000);
        _benchmarkMeasuredSeconds += frameSeconds;
        if (_benchmarkMeasuredSeconds < _benchmarkDurationSeconds.Value) return;

        var ordered = _benchmarkFrameTimesMs.Order().ToArray();
        var averageFps = ordered.Length / _benchmarkMeasuredSeconds;
        var p95Ms = Percentile(ordered, 0.95);
        var p99Ms = Percentile(ordered, 0.99);
        using var process = Process.GetCurrentProcess();
        var cpuSeconds = (process.TotalProcessorTime - _benchmarkCpuStart).TotalSeconds;
        var oneCorePercent = cpuSeconds / _benchmarkMeasuredSeconds * 100;
        var workingSetMiB = process.WorkingSet64 / (1024.0 * 1024.0);
        Console.WriteLine(FormattableString.Invariant(
            $"benchmark=desktop-l0 resolution={GraphicsDevice.Viewport.Width}x{GraphicsDevice.Viewport.Height} seed={_snapshot.Seed} ticksPerSecond={Speeds[_speedIndex]} warmupSeconds=5 measuredSeconds={_benchmarkMeasuredSeconds:F1} frames={ordered.Length} averageFps={averageFps:F2} p95FrameMs={p95Ms:F2} p99FrameMs={p99Ms:F2} cpuPercentOfOneCore={oneCorePercent:F1} workingSetMiB={workingSetMiB:F1}"));
        Exit();

        static double Percentile(double[] sorted, double percentile)
        {
            var index = Math.Clamp((int)Math.Ceiling(sorted.Length * percentile) - 1, 0, sorted.Length - 1);
            return sorted[index];
        }
    }

    private void DrawGroundObjects()
    {
        // Render back-to-front using the projected ground-plane Y coordinate.
        foreach (var index in DepthOrder)
        {
            var cell = _snapshot.Cells[index];
            var visualSeed = CounterRandom.VisualBits(_snapshot.Seed, cell.Index, 0x4752415353UL);
            var ground = GroundToScreen((cell.X + 0.5) * Tile, cell.ElevationM, (cell.Y + 0.5) * Tile);

            if (cell.Woody.Present && cell.Woody.BiomassGPerM2 > 1)
            {
                var bucket = Math.Clamp((int)(cell.Woody.BiomassGPerM2 / 450), 1, 4);
                var size = 35 + bucket * 8;
                var shadowWidth = size + 12 + (int)(visualSeed & 5);
                DrawSprite(TextureFor(new(VisualKind.Shadow, (int)(visualSeed & 7), bucket, VisualSeason)),
                    (int)ground.X - shadowWidth / 2 + 7, (int)ground.Y - Math.Max(4, size / 5), shadowWidth,
                    Math.Max(8, size / 2));
            }

            if (RockVisible(cell, visualSeed))
            {
                var rockVariant = (int)(visualSeed & 7);
                var rockWidth = 23 + rockVariant * 3;
                var rockHeight = 16 + rockVariant * 2;
                var rockOffsetX = (int)(visualSeed >> 9 & 7) - 3;
                DrawSprite(TextureFor(new(VisualKind.Rock, rockVariant, 1, VisualSeason)),
                    (int)ground.X - rockWidth / 2 + rockOffsetX, (int)ground.Y - rockHeight + 4,
                    rockWidth, rockHeight);
            }

            if (cell.Grass.Present && cell.Grass.BiomassGPerM2 > 1)
            {
                var bucket = Math.Clamp((int)(cell.Grass.BiomassGPerM2 / 150), 1, 4);
                var mask = GrassMaskAt(_snapshot, cell.X, cell.Y);
                var neighborCount = 0;
                for (var bit = 0; bit < 8; bit++) neighborCount += (mask >> bit) & 1;
                var patchNoise = CounterRandom.SpatialUnit(_snapshot.Seed, 0x475241535344454EUL,
                    cell.X, cell.Y, 3, 1, 0);
                var localNoise = CounterRandom.Unit(_snapshot.Seed, 0x4752415353444554UL,
                    (ulong)cell.Index, 0, 0, 0);
                var biomassCover = Math.Clamp(cell.Grass.BiomassGPerM2 / 500, 0, 1);
                var tuftDensity = Math.Clamp(0.12 + 0.43 * biomassCover + 0.29 * patchNoise
                    + 0.14 * neighborCount / 8.0, 0, 0.92);
                if (localNoise < tuftDensity)
                {
                    var tuftCount = bucket >= 3 && neighborCount >= 5 ? 2 : 1;
                    for (var tuft = 0; tuft < tuftCount; tuft++)
                    {
                        var tuftSeed = CounterRandom.VisualBits(_snapshot.Seed, cell.Index,
                            0x4752415353UL + (ulong)tuft);
                        var width = 12 + (int)(tuftSeed & 3) + bucket;
                        var height = 10 + (int)(tuftSeed >> 4 & 3) + bucket * 2;
                        var offsetX = (int)(tuftSeed >> 10 & 15) - 7 + (tuft == 1 ? 5 : 0);
                        var offsetY = (int)(tuftSeed >> 14 & 7) - 3 - (tuft == 1 ? 2 : 0);
                        DrawSprite(TextureFor(new(VisualKind.Grass, (int)(tuftSeed >> 8 & 15), bucket, VisualSeason)),
                            (int)ground.X - width / 2 + offsetX, (int)ground.Y - height + offsetY, width, height);
                    }
                }

                if (cell.SurfaceWaterMm < 100)
                {
                    var flowerChance = 0.08 + 0.18 * biomassCover + 0.20 * patchNoise;
                    var flowerSeed = CounterRandom.VisualBits(_snapshot.Seed, cell.Index, 0x464C4F57455253UL);
                    if (CounterRandom.Unit(_snapshot.Seed, 0x464C4F574552504CUL,
                            (ulong)cell.Index, 0, 0, 0) < flowerChance)
                    {
                        var flowerWidth = 7 + (int)(flowerSeed & 3);
                        var flowerHeight = 8 + (int)(flowerSeed >> 4 & 4);
                        var flowerOffset = (int)(flowerSeed >> 8 & 19) - 9;
                        DrawSprite(TextureFor(new(VisualKind.Wildflower, (int)(flowerSeed >> 16 & 7), bucket, VisualSeason)),
                            (int)ground.X - flowerWidth / 2 + flowerOffset,
                            (int)ground.Y - flowerHeight + 1, flowerWidth, flowerHeight);
                    }
                }
            }

            if (cell.Woody.Present && cell.Woody.BiomassGPerM2 > 1)
            {
                var bucket = Math.Clamp((int)(cell.Woody.BiomassGPerM2 / 450), 1, 4);
                var speciesRow = TreeSpeciesRow(_snapshot, cell, visualSeed, VisualSeason);
                var matureSize = 36 + bucket * 10 + (int)(visualSeed >> 20 & 7) - 3;
                var treeCount = 1 + bucket;
                for (var tree = 0; tree < treeCount; tree++)
                {
                    var treeSeed = CounterRandom.VisualBits(_snapshot.Seed, cell.Index,
                        0x47524F5645545245UL + (ulong)tree);
                    var scale = tree == 0 ? 1.0 : 0.62 + CounterRandom.Unit(_snapshot.Seed,
                        0x47524F564553495AUL, (ulong)cell.Index, (ulong)tree, 0, 0) * 0.14;
                    var size = (int)Math.Round(matureSize * scale);
                    var offsetX = tree == 0
                        ? (int)(visualSeed >> 28 & 7) - 3
                        : (int)(treeSeed & 15) - 7;
                    var offsetY = tree == 0 ? (int)(visualSeed >> 35 & 5) - 2 : 0;
                    var anchorY = (int)ground.Y + offsetY - (int)Math.Round(size * (184.0 / 192));
                    var artVariant = speciesRow * 4 + (int)(treeSeed >> 11 & 3);
                    DrawSprite(TextureFor(new(VisualKind.Woody, artVariant, bucket, VisualSeason)),
                        (int)ground.X - size / 2 + offsetX, anchorY, size, size);
                }
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
        DrawText("ASTERRA  /  TEMPERATE VALLEY", cardX + 20, cardY + 13, new Color(231, 226, 201), 2);
        DrawText($"DAY {_snapshot.DayOfYear:D3} {SeasonName(_snapshot.Season)}  ART {SeasonName(VisualSeason)}  {mode}",
            cardX + 21, cardY + 42, new Color(149, 203, 190), 1);
        DrawText($"{(_paused ? "PAUSED" : $"RUN {Speeds[_speedIndex]} TICKS/S")}  1 LAND  2 WATER  3 BIOMASS  SPACE PAUSE  H HIDE",
            cardX + 21, cardY + 61, new Color(207, 216, 207), 1);

        var mouse = Mouse.GetState();
        if (_capturePath is null && TryGetCellAtScreen(mouse.X, mouse.Y, out var cell))
        {
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
        DrawText($"SEED {_snapshot.Seed}   FPS {_fps:F0}   NO FAUNA IN THIS BUILD", 26, viewport.Height - 24,
            new Color(220, 225, 210, 210), 1);
    }

    private void DrawLegend(int x, int y, Color color, string text)
    {
        _batch.Draw(_pixel, new Rectangle(x, y, 11, 11), color);
        DrawText(text, x + 19, y + 2, new Color(191, 201, 189), 1);
    }

    private void DrawSprite(Texture2D texture, int x, int y, int width, int height)
        => _batch.Draw(texture, new Rectangle(x, y, width, height), Color.White);

    private static int TreeSpeciesRow(WorldSnapshot world, WorldCell cell, ulong cellSeed, Season season)
    {
        if (cell.ElevationM >= 125) return 1; // alpine tree line favors conifers

        var nearbyWater = 0;
        for (var y = Math.Max(0, cell.Y - 1); y <= Math.Min(world.Height - 1, cell.Y + 1); y++)
        for (var x = Math.Max(0, cell.X - 1); x <= Math.Min(world.Width - 1, cell.X + 1); x++)
        {
            if (x == cell.X && y == cell.Y) continue;
            if (world.Cells[y * world.Width + x].SurfaceWaterMm >= 100) nearbyWater++;
        }
        if (nearbyWater > 0 && (cellSeed >> 7) % 100 < 62) return 4; // riparian broadleaf groves

        const int regionSize = 8;
        var regionColumns = (SimulationState.Width + regionSize - 1) / regionSize;
        var regionIndex = (cell.Y / regionSize) * regionColumns + cell.X / regionSize;
        var regionBits = CounterRandom.VisualBits(world.Seed, regionIndex, 0x4153544552524154UL);
        var roll = (int)(regionBits % 100);
        var row = roll switch
        {
            < 32 => 1, // evergreen pine stands
            < 59 => 0, // broadleaf oak
            < 80 => 2, // pale-barked birch
            < 91 => 4, // willow groves
            _ => 3
        };

        if (season == Season.Autumn && row != 1 && cellSeed % 100 < 55)
            row = 3;
        else if (season != Season.Autumn && row == 3)
            row = 0;
        return row;
    }

    private void RefreshTerrainTexture(bool force = false)
    {
        var cadence = _overlayMode == 1 ? SimulationState.TicksPerHour : SimulationState.TicksPerDay;
        var revision = _snapshot.TickIndex / cadence;
        if (!force && revision == _terrainRevision && _overlayMode == _terrainOverlay) return;
        _terrainTexture.SetData(PixelTerrainTexture.Generate(_snapshot, Tile, _overlayMode, VisualSeason));
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
            VisualKind.Wildflower => (16, 12),
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
            var baseRock = (key.Variant % 8) switch
            {
                0 => new Color(103, 112, 105),
                1 => new Color(112, 103, 91),
                2 => new Color(91, 107, 108),
                3 => new Color(117, 112, 99),
                4 => new Color(87, 102, 94),
                5 => new Color(102, 105, 115),
                6 => new Color(119, 108, 91),
                _ => new Color(94, 113, 103)
            };
            double RockRoll(int index, ulong salt) => CounterRandom.Unit(
                (ulong)(key.Variant + 1), salt, (ulong)index, (ulong)key.BiomassBucket, 0, 0);
            var peakCount = 2 + key.Variant % 4;
            for (var peak = 0; peak < peakCount; peak++)
            {
                var cx = 3 + (int)(RockRoll(peak, 0x524F434B5045414BUL) * 18);
                var cy = 8 + (int)(RockRoll(peak, 0x524F434B48454947UL) * 7);
                var rx = 3 + (int)(RockRoll(peak, 0x524F434B57494454UL) * 4);
                var ry = 4 + (int)(RockRoll(peak, 0x524F434B44455054UL) * 5);
                for (var y = Math.Max(2, cy - ry); y <= Math.Min(height - 2, cy + ry); y++)
                for (var x = Math.Max(1, cx - rx); x <= Math.Min(width - 2, cx + rx); x++)
                {
                    var dx = (x - cx) / (double)rx;
                    var dy = (y - cy) / (double)ry;
                    var grain = (x * 11 + y * 17 + key.Variant * 7 + peak * 13) % 19;
                    var edge = 1.0 + ((grain % 7) - 3) * 0.035;
                    if (dx * dx + dy * dy > edge || grain == 0 || grain == 15) continue;
                    var litFace = x < cx - rx * 0.22 && y < cy + 1;
                    var shade = litFace ? 1.22f : y > cy + 2 ? 0.70f : grain % 6 == 0 ? 1.16f : 0.92f;
                    Put(x, y, Shade(baseRock, shade));
                }
            }
            // Fractures and loose chips vary the silhouette without adding a repeated outline.
            for (var fracture = 0; fracture < 3; fracture++)
            {
                var startX = 5 + (int)(RockRoll(fracture, 0x524F434B43524143UL) * 13);
                var startY = 4 + (int)(RockRoll(fracture, 0x524F434B43524144UL) * 7);
                var length = 2 + (int)(RockRoll(fracture, 0x524F434B4352414CUL) * 4);
                Line(pixels, width, height, startX, startY, startX - length, startY + length + 1,
                    Shade(baseRock, fracture == 0 ? 1.34f : 0.62f));
            }
            for (var chip = 0; chip < 3; chip++)
            {
                var x = 3 + (int)(RockRoll(chip, 0x524F434B43484950UL) * 19);
                var y = 15 + (int)(RockRoll(chip, 0x524F434B43484959UL) * 4);
                Put(x, y, Shade(baseRock, 0.66f + (float)RockRoll(chip, 0x524F434B43484946UL) * 0.25f));
            }
        }
        else if (key.Kind == VisualKind.Grass)
        {
            var color = GrassColor(key.Season);
            var bladeCount = 3 + key.Variant % 4 + key.BiomassBucket / 2;
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
        else if (key.Kind == VisualKind.Wildflower)
        {
            var stem = new Color(65, 100, 48);
            var petal = (key.Variant % 4) switch
            {
                0 => new Color(237, 230, 196),
                1 => new Color(232, 208, 113),
                2 => new Color(209, 173, 157),
                _ => new Color(223, 218, 183)
            };
            var center = new Color(205, 164, 83);
            var flowerCount = 2 + key.Variant % 3;
            for (var flower = 0; flower < flowerCount; flower++)
            {
                var x = 3 + (flower * 7 + key.Variant * 3) % 10;
                var top = 1 + (flower * 3 + key.Variant) % 5;
                Line(pixels, width, height, x, height - 1, x + (flower % 2 == 0 ? -1 : 1), top + 3, stem);
                Put(x - 1, top, petal);
                Put(x + 1, top, petal);
                Put(x, top - 1, petal);
                Put(x, top + 1, petal);
                Put(x, top, center);
                Put(x + 2, top + 1, Shade(stem, 0.82f));
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
        var species = key.Variant / 4;
        var artVariant = key.Variant % 4;
        var center = width / 2 + artVariant % 5 - 2;
        var conifer = species == 1;
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
            }[artVariant];
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
            var crown = LeafColor(key.Season, artVariant);
            if (species == 2) crown = Shade(crown, 1.12f); // birch foliage catches more light
            else if (species == 4) crown = Shade(crown, 0.86f); // willow crowns stay cool and deep
            var branchCount = 12;
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
                const int clusterCount = 14;
                for (var cluster = 0; cluster < clusterCount; cluster++)
                {
                    var angle = Math.PI * 2 * cluster / clusterCount - Math.PI / 2
                        + (Roll(cluster, 0x4C454146414E474CUL) - 0.5) * 0.42;
                    var radius = 9 + (int)(Roll(cluster, 0x4C45414652414449UL) * 9);
                    var cx = center + (int)Math.Round(Math.Cos(angle) * radius);
                    var cy = 28 + (int)Math.Round(Math.Sin(angle) * radius * 0.68);
                    var rx = 8 + (int)(Roll(cluster, 0x4C45414657494454UL) * 6);
                    var ry = 7 + (int)(Roll(cluster, 0x4C45414648454947UL) * 6) + (species == 4 ? 4 : 0);
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
                for (var branch = 1; branch < branchCount; branch += 3)
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
                var bend = (int)Math.Round(Math.Sin(y * 0.09 + artVariant + species * 0.37) * 1.5);
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
        Season.Spring => new Color(116, 157, 69),
        Season.Summer => new Color(64, 116, 49),
        Season.Autumn => new Color(148, 126, 55),
        _ => new Color(93, 105, 71)
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

    private bool RockVisible(WorldCell cell, ulong visualSeed)
    {
        var outcropPatch = CounterRandom.SpatialUnit(_snapshot.Seed, 0x524F434B50415443UL,
            cell.X, cell.Y, 7, 0, 3);
        if (outcropPatch < 0.42) return false;

        var westX = Math.Max(0, cell.X - 1);
        var eastX = Math.Min(_snapshot.Width - 1, cell.X + 1);
        var southY = Math.Max(0, cell.Y - 1);
        var northY = Math.Min(_snapshot.Height - 1, cell.Y + 1);
        var west = _snapshot.Cells[cell.Y * _snapshot.Width + westX].ElevationM;
        var east = _snapshot.Cells[cell.Y * _snapshot.Width + eastX].ElevationM;
        var south = _snapshot.Cells[southY * _snapshot.Width + cell.X].ElevationM;
        var north = _snapshot.Cells[northY * _snapshot.Width + cell.X].ElevationM;
        var eastSlope = (east - west) / Math.Max(1, eastX - westX) / Tile;
        var northSlope = (north - south) / Math.Max(1, northY - southY) / Tile;
        var slope = Math.Sqrt(eastSlope * eastSlope + northSlope * northSlope);
        var slopeExposure = Math.Clamp((slope - 0.18) * 0.14, 0, 0.14);
        var altitudeExposure = Math.Clamp((cell.ElevationM - 72) * 0.0013, 0, 0.04);
        var waterExposure = cell.SurfaceWaterMm >= 100 ? 0.05 : 0;
        var chance = Math.Clamp(0.003 + slopeExposure + altitudeExposure + waterExposure, 0.003, 0.22);
        return CounterRandom.Unit(visualSeed, 0x524F434BUL, (ulong)cell.Index, 0, 0, 0) < chance;
    }

    private void BuildTerrainGeometry()
    {
        foreach (var chunk in _terrainChunks)
        {
            chunk.Vertices.Dispose();
            chunk.Indices.Dispose();
        }
        _terrainChunks.Clear();
        _plateauVertexBuffer?.Dispose();
        _plateauIndexBuffer?.Dispose();
        _cliffVertexBuffer?.Dispose();
        var cellElevations = _snapshot.Cells.Select(cell => cell.ElevationM).ToArray();
        _centerElevationM = (cellElevations.Min() + cellElevations.Max()) * 0.5;
        _terrainHeightSampler = TerrainModel.CreateElevationSampler(_snapshot.Seed);
        _underlyingHeightSampler = TerrainModel.CreateUnderlyingElevationSampler(_snapshot.Seed);
        for (var startY = 0; startY < SimulationState.Height; startY += TerrainChunkCells)
        for (var startX = 0; startX < SimulationState.Width; startX += TerrainChunkCells)
        {
            var cellsWide = Math.Min(TerrainChunkCells, SimulationState.Width - startX);
            var cellsHigh = Math.Min(TerrainChunkCells, SimulationState.Height - startY);
            var mesh = TerrainSurfaceMeshBuilder.BuildChunk(_underlyingHeightSampler, startX, startY,
                cellsWide, cellsHigh, TerrainSubdivisions, SimulationState.Width, SimulationState.Height, Tile);
            var vertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionNormalTexture.VertexDeclaration,
                mesh.Vertices.Length, BufferUsage.WriteOnly);
            vertexBuffer.SetData(mesh.Vertices);
            var indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits,
                mesh.Indices.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(mesh.Indices);
            var minimumHeight = mesh.Vertices.Min(vertex => vertex.Position.Y);
            var maximumHeight = mesh.Vertices.Max(vertex => vertex.Position.Y);
            var bounds = new BoundingBox(
                new Vector3(startX * Tile, minimumHeight, startY * Tile),
                new Vector3((startX + cellsWide) * Tile, maximumHeight, (startY + cellsHigh) * Tile));
            _terrainChunks.Add(new TerrainRenderChunk(vertexBuffer, indexBuffer, mesh.PrimitiveCount, bounds));
        }
        BuildPlateauGeometry();
    }

    private void BuildPlateauGeometry()
    {
        const int radialRings = 20;
        const int cliffBands = 8;
        var features = TerrainModel.GenerateOutcropGeometries(_snapshot.Seed);
        var topVertices = new List<VertexPositionNormalTexture>(features.Count * (radialRings + 1) * (features[0].Rim.Count + 1));
        var topIndices = new List<int>(features.Count * radialRings * features[0].Rim.Count * 6);
        var cliffVertices = new List<VertexPositionColor>(features.Sum(feature => feature.Rim.Count) * cliffBands * 6);

        foreach (var feature in features)
        {
            var rockPhaseA = CounterRandom.Unit(_snapshot.Seed, 0x434C494646504831UL,
                (ulong)feature.FeatureIndex, 0, 0, 0) * Math.Tau;
            var rockPhaseB = CounterRandom.Unit(_snapshot.Seed, 0x434C494646504832UL,
                (ulong)feature.FeatureIndex, 0, 0, 0) * Math.Tau;
            var segments = feature.Rim.Count;
            var stride = segments + 1;
            var topVertexOffset = topVertices.Count;
            var featureTopVertices = new VertexPositionNormalTexture[(radialRings + 1) * stride];

            for (var ring = 0; ring <= radialRings; ring++)
            for (var segment = 0; segment <= segments; segment++)
            {
                var rim = feature.Rim[segment % segments];
                var radius = ring / (double)radialRings;
                var gridX = feature.CenterEastCell + (rim.EastCell - feature.CenterEastCell) * radius;
                var gridY = feature.CenterNorthCell + (rim.NorthCell - feature.CenterNorthCell) * radius;
                var elevation = ring == radialRings ? rim.CrownElevationM : _terrainHeightSampler(gridX, gridY);
                var position = new Vector3((float)(gridX * Tile), (float)elevation, (float)(gridY * Tile));
                var uv = new Vector2((float)(gridX / SimulationState.Width), (float)(gridY / SimulationState.Height));
                featureTopVertices[ring * stride + segment] = new VertexPositionNormalTexture(position, Vector3.Up, uv);
            }

            var featureTopIndices = new int[radialRings * segments * 6];
            var index = 0;
            for (var ring = 0; ring < radialRings; ring++)
            for (var segment = 0; segment < segments; segment++)
            {
                var inner = ring * stride + segment;
                var innerNext = inner + 1;
                var outer = (ring + 1) * stride + segment;
                var outerNext = outer + 1;
                featureTopIndices[index++] = topVertexOffset + inner;
                featureTopIndices[index++] = topVertexOffset + innerNext;
                featureTopIndices[index++] = topVertexOffset + outer;
                featureTopIndices[index++] = topVertexOffset + innerNext;
                featureTopIndices[index++] = topVertexOffset + outerNext;
                featureTopIndices[index++] = topVertexOffset + outer;
            }
            topVertices.AddRange(featureTopVertices);
            topIndices.AddRange(featureTopIndices);

            for (var segment = 0; segment < segments; segment++)
            {
                var first = feature.Rim[segment];
                var second = feature.Rim[(segment + 1) % segments];
                for (var band = 0; band < cliffBands; band++)
                {
                    var topFraction = band / (float)cliffBands;
                    var bottomFraction = (band + 1) / (float)cliffBands;
                    var firstTop = WallPoint(first, topFraction);
                    var secondTop = WallPoint(second, topFraction);
                    var firstBottom = WallPoint(first, bottomFraction);
                    var secondBottom = WallPoint(second, bottomFraction);
                    var angle = Math.Tau * segment / segments;
                    var verticalPhase = band * 0.72;
                    var localRock = 0.52 * Math.Sin(angle * 4.3 + rockPhaseA + verticalPhase)
                        + 0.31 * Math.Sin(angle * 9.7 - rockPhaseB - verticalPhase * 0.63)
                        + 0.17 * Math.Sin(angle * 17.2 + rockPhaseA * 0.37 + verticalPhase * 1.4);
                    var strata = (band % 3) switch { 0 => 3, 1 => -2, _ => 0 };
                    var fracturedTone = (int)Math.Round(localRock * 17) + strata;
                    var color = new Color((byte)Math.Clamp(103 + fracturedTone, 0, 255),
                        (byte)Math.Clamp(101 + fracturedTone, 0, 255),
                        (byte)Math.Clamp(91 + fracturedTone, 0, 255));
                    AddWallTriangle(firstTop, firstBottom, secondTop, color);
                    AddWallTriangle(secondTop, firstBottom, secondBottom, color);
                }
            }

            Vector3 WallPoint(TerrainRimPoint point, float depthFraction)
            {
                var baseElevation = (float)(point.ToeElevationM - 1.0);
                var y = (float)point.CrownElevationM + (baseElevation - (float)point.CrownElevationM) * depthFraction;
                return new Vector3((float)(point.EastCell * Tile), y, (float)(point.NorthCell * Tile));
            }

            void AddWallTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
            {
                cliffVertices.Add(new VertexPositionColor(a, color));
                cliffVertices.Add(new VertexPositionColor(b, color));
                cliffVertices.Add(new VertexPositionColor(c, color));
            }
        }

        _plateauPrimitiveCount = topIndices.Count / 3;
        _plateauVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionNormalTexture.VertexDeclaration,
            topVertices.Count, BufferUsage.WriteOnly);
        _plateauVertexBuffer.SetData(topVertices.ToArray());
        _plateauIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits,
            topIndices.Count, BufferUsage.WriteOnly);
        _plateauIndexBuffer.SetData(topIndices.ToArray());

        _cliffPrimitiveCount = cliffVertices.Count / 3;
        _cliffVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration,
            cliffVertices.Count, BufferUsage.WriteOnly);
        _cliffVertexBuffer.SetData(cliffVertices.ToArray());
    }

    private void DrawPlateauGeometry()
    {
        if (_plateauVertexBuffer is null || _plateauIndexBuffer is null || _cliffVertexBuffer is null) return;

        _terrainEffect.TextureEnabled = true;
        _terrainEffect.VertexColorEnabled = false;
        _terrainEffect.LightingEnabled = true;
        GraphicsDevice.SetVertexBuffer(_plateauVertexBuffer);
        GraphicsDevice.Indices = _plateauIndexBuffer;
        foreach (var pass in _terrainEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _plateauPrimitiveCount);
        }

        _terrainEffect.TextureEnabled = false;
        _terrainEffect.VertexColorEnabled = true;
        _terrainEffect.LightingEnabled = false;
        GraphicsDevice.SetVertexBuffer(_cliffVertexBuffer);
        GraphicsDevice.Indices = null;
        foreach (var pass in _terrainEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, _cliffPrimitiveCount);
        }
    }

    private void RefreshWaterGeometry(bool force = false)
    {
        var revision = _snapshot.TickIndex / SimulationState.TicksPerHour;
        if (!force && revision == _waterRevision) return;
        _waterRevision = revision;

        var cellCount = SimulationState.Width * SimulationState.Height;
        var waterMm = new double[cellCount];
        var wet = new double[cellCount];
        for (var index = 0; index < cellCount; index++)
        {
            waterMm[index] = _snapshot.Cells[index].SurfaceWaterMm;
            wet[index] = SmoothStep(180, 650, waterMm[index]);
        }

        var columns = SimulationState.Width * WaterSubdivisions;
        var rows = SimulationState.Height * WaterSubdivisions;
        var stride = columns + 1;
        var depths = new double[stride * (rows + 1)];
        var coverage = new double[depths.Length];
        for (var z = 0; z <= rows; z++)
        for (var x = 0; x <= columns; x++)
        {
            var gridX = x / (double)WaterSubdivisions;
            var gridY = z / (double)WaterSubdivisions;
            var vertex = z * stride + x;
            depths[vertex] = SampleCellField(waterMm, gridX - 0.5, gridY - 0.5);
            coverage[vertex] = SampleCellField(wet, gridX - 0.5, gridY - 0.5);
        }

        var noiseColumns = SimulationState.Width + 1;
        var noiseRows = SimulationState.Height + 1;
        var bankNoiseGrid = new double[noiseColumns * noiseRows];
        var waterGrainGrid = new double[bankNoiseGrid.Length];
        var seedSalt = CounterRandom.VisualBits(_snapshot.Seed, 0, 0x57415445524D4553UL);
        for (var y = 0; y < noiseRows; y++)
        for (var x = 0; x < noiseColumns; x++)
        {
            var waterX = x * WaterSubdivisions;
            var waterY = y * WaterSubdivisions;
            var bankCoarse = CounterRandom.SpatialUnit(_snapshot.Seed, 0x574154455242414EUL,
                waterX, waterY, WaterSubdivisions * 7, 0, 1);
            var bankMedium = CounterRandom.SpatialUnit(_snapshot.Seed, 0x574154455242414EUL,
                waterX, waterY, WaterSubdivisions * 3, 0, 2);
            var bankFine = CounterRandom.SpatialUnit(_snapshot.Seed, 0x574154455242414EUL,
                waterX, waterY, WaterSubdivisions, 0, 3);
            var index = y * noiseColumns + x;
            bankNoiseGrid[index] = bankCoarse * 0.57 + bankMedium * 0.30 + bankFine * 0.13;
            waterGrainGrid[index] = CounterRandom.SpatialUnit(_snapshot.Seed, seedSalt,
                waterX, waterY, WaterSubdivisions * 2, 1, 0);
        }

        var vertices = new List<VertexPositionColor>();
        var cachedVertices = new VertexPositionColor[depths.Length];
        var vertexReady = new bool[depths.Length];
        VertexPositionColor At(int x, int z)
        {
            var gridX = x / (double)WaterSubdivisions;
            var gridY = z / (double)WaterSubdivisions;
            var index = z * stride + x;
            if (vertexReady[index]) return cachedVertices[index];
            var depth = depths[index];
            var bankNoise = SampleGridField(bankNoiseGrid, noiseColumns, noiseRows, gridX, gridY);
            var bankOffset = (bankNoise - 0.5) * 0.90;
            var shore = SmoothStep(0.36 + bankOffset, 0.78 + bankOffset, coverage[index]);
            var depthTone = (float)Math.Clamp(depth / 2_800, 0, 1);
            var waterColor = Color.Lerp(new Color(87, 132, 111), new Color(38, 81, 87), depthTone);
            var waterGrain = SampleGridField(waterGrainGrid, noiseColumns, noiseRows, gridX, gridY);
            var pattern = (float)(0.91 + waterGrain * 0.18);
            waterColor = Shade(waterColor, pattern);
            var alpha = (byte)Math.Clamp((int)Math.Round(shore * 246), 0, 246);
            waterColor = new Color(waterColor.R, waterColor.G, waterColor.B, alpha);
            var surfaceHeight = _terrainHeightSampler(gridX, gridY)
                + Math.Clamp(depth / 1000, 0.06, 1.8);
            cachedVertices[index] = new VertexPositionColor(new Vector3((float)(gridX * Tile), (float)surfaceHeight,
                (float)(gridY * Tile)), waterColor);
            vertexReady[index] = true;
            return cachedVertices[index];
        }

        for (var z = 0; z < rows; z++)
        for (var x = 0; x < columns; x++)
        {
            var a = z * stride + x;
            var b = a + 1;
            var c = a + stride;
            var d = c + 1;
            if (Math.Max(Math.Max(coverage[a], coverage[b]), Math.Max(coverage[c], coverage[d])) < 0.02) continue;
            vertices.Add(At(x, z));
            vertices.Add(At(x, z + 1));
            vertices.Add(At(x + 1, z));
            vertices.Add(At(x + 1, z));
            vertices.Add(At(x, z + 1));
            vertices.Add(At(x + 1, z + 1));
        }

        AddRiverRibbons(vertices, waterMm);

        _waterVertexBuffer?.Dispose();
        _waterVertexBuffer = null;
        _waterPrimitiveCount = vertices.Count / 3;
        if (vertices.Count == 0) return;
        _waterVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration,
            vertices.Count, BufferUsage.WriteOnly);
        _waterVertexBuffer.SetData(vertices.ToArray());
    }

    private void AddRiverRibbons(List<VertexPositionColor> vertices, double[] waterMm)
    {
        var drainage = SurfaceWaterModel.AnalyzeDrainage(_snapshot.Cells.ToArray());
        var receiver = drainage.Receiver;
        var accumulation = drainage.Accumulation;
        var seedTint = (int)(CounterRandom.VisualBits(_snapshot.Seed, 0, 0x524956455254494EUL) & 7);
        var baseWater = new Color(51 + seedTint, 108 + seedTint, 104 + seedTint, 246);
        var channelNodes = new Dictionary<int, (float Radius, double Depth)>();
        var activeEdges = new bool[receiver.Length];
        var incomingChannels = new int[receiver.Length];

        double CellDepth(int index) => waterMm[index];
        Vector2 Center(WorldCell cell) => new((float)((cell.X + 0.5) * Tile), (float)((cell.Y + 0.5) * Tile));
        Vector3 SurfacePoint(Vector2 point, double depthMm)
        {
            var elevation = _terrainHeightSampler(point.X / Tile, point.Y / Tile);
            var waterDepth = Math.Clamp(depthMm / 1000, 0.12, 0.8);
            return new Vector3(point.X, (float)(elevation + waterDepth), point.Y);
        }

        float RiverBankOffsetM(Vector2 point, float radius, ulong bankSide)
        {
            var sampleX = (int)MathF.Round(point.X / Tile * WaterSubdivisions);
            var sampleY = (int)MathF.Round(point.Y / Tile * WaterSubdivisions);
            var coarse = CounterRandom.SpatialUnit(_snapshot.Seed, 0x524956455242414EUL,
                sampleX, sampleY, WaterSubdivisions * 5, bankSide, 1);
            var medium = CounterRandom.SpatialUnit(_snapshot.Seed, 0x524956455242414EUL,
                sampleX, sampleY, WaterSubdivisions * 2, bankSide, 2);
            var fine = CounterRandom.SpatialUnit(_snapshot.Seed, 0x524956455242414EUL,
                sampleX, sampleY, WaterSubdivisions, bankSide, 3);
            var noise = coarse * 0.55 + medium * 0.30 + fine * 0.15;
            var maximumOffset = Math.Min(Tile * 0.38, Math.Max(Tile * 0.06, radius * 0.24));
            return (float)((noise - 0.5) * 2 * maximumOffset);
        }

        for (var from = 0; from < receiver.Length; from++)
        {
            var to = receiver[from];
            if (to < 0 || accumulation[from] < VisibleRiverThresholdCells) continue;
            if (CellDepth(from) < 20 && CellDepth(to) < 20) continue;
            if (CellDepth(from) >= 250 && CellDepth(to) >= 250) continue;
            activeEdges[from] = true;
            incomingChannels[to]++;
        }

        for (var from = 0; from < receiver.Length; from++)
        {
            if (!activeEdges[from]) continue;
            var to = receiver[from];
            var first = _snapshot.Cells[from];
            var second = _snapshot.Cells[to];
            var start = Center(first);
            var end = Center(second);
            var delta = end - start;
            var length = delta.Length();
            if (length < 0.01f) continue;
            var direction = delta / length;
            var perpendicular = new Vector2(-direction.Y, direction.X);
            var incomingWidth = HalfWidthM(accumulation[from]);
            var outgoingWidth = HalfWidthM(accumulation[to]);
            var startDepth = Math.Max(20, CellDepth(from));
            var endDepth = Math.Max(20, CellDepth(to));
            RememberNode(from, incomingWidth, startDepth);
            RememberNode(to, outgoingWidth, endDepth);
            var color = new Color(baseWater.R, baseWater.G, baseWater.B, (byte)Math.Clamp(
                220 + (int)Math.Min(26, Math.Log2(Math.Max(1, accumulation[from] / SurfaceWaterModel.ChannelThresholdCells)) * 4), 0, 246));

            AddWaterfallIfTerrainDrops(start, end, perpendicular, incomingWidth, outgoingWidth,
                startDepth, endDepth, color);
        }

        for (var start = 0; start < activeEdges.Length; start++)
        {
            if (!activeEdges[start] || incomingChannels[start] == 1) continue;
            var chain = new List<int> { start };
            var current = start;
            while (activeEdges[current])
            {
                var next = receiver[current];
                chain.Add(next);
                if (incomingChannels[next] != 1 || !activeEdges[next]) break;
                current = next;
            }
            AddSmoothedRibbon(chain);
        }

        foreach (var (index, node) in channelNodes)
            AddChannelJunction(_snapshot.Cells[index], node.Radius, node.Depth);

        float HalfWidthM(double cellsOfFlow)
            => Tile * (float)Math.Clamp(0.28 + 0.18 * Math.Log2(cellsOfFlow / SurfaceWaterModel.ChannelThresholdCells), 0.28, 1.75);

        void RememberNode(int index, float radius, double depth)
        {
            if (!channelNodes.TryGetValue(index, out var current))
                channelNodes[index] = (radius, depth);
            else
                channelNodes[index] = (Math.Max(current.Radius, radius), Math.Max(current.Depth, depth));
        }

        void AddChannelJunction(WorldCell cell, float radius, double depthMm)
        {
            const int sides = 20;
            var center = Center(cell);
            var centerPoint = SurfacePoint(center, depthMm);
            centerPoint.Y += 0.025f;
            for (var side = 0; side < sides; side++)
            {
                var firstAngle = MathF.Tau * side / sides;
                var secondAngle = MathF.Tau * (side + 1) / sides;
                var firstDirection = new Vector2(MathF.Cos(firstAngle), MathF.Sin(firstAngle));
                var secondDirection = new Vector2(MathF.Cos(secondAngle), MathF.Sin(secondAngle));
                var first = center + firstDirection * (radius + RiverBankOffsetM(center + firstDirection * radius, radius, 3) * 0.55f);
                var second = center + secondDirection * (radius + RiverBankOffsetM(center + secondDirection * radius, radius, 3) * 0.55f);
                var firstPoint = SurfacePoint(first, depthMm);
                var secondPoint = SurfacePoint(second, depthMm);
                firstPoint.Y += 0.025f;
                secondPoint.Y += 0.025f;
                AddTriangle(centerPoint, firstPoint, secondPoint, baseWater, baseWater, baseWater);
            }
        }

        void AddSmoothedRibbon(IReadOnlyList<int> chain)
        {
            var path = chain.Select(index =>
            {
                var cell = _snapshot.Cells[index];
                return (Point: Center(cell), Radius: HalfWidthM(accumulation[index]),
                    Depth: Math.Max(20, CellDepth(index)));
            }).ToList();
            for (var iteration = 0; iteration < 2 && path.Count >= 2; iteration++)
            {
                var smoothed = new List<(Vector2 Point, float Radius, double Depth)> { path[0] };
                for (var index = 0; index < path.Count - 1; index++)
                {
                    var first = path[index];
                    var second = path[index + 1];
                    smoothed.Add(Interpolate(first, second, 0.25f));
                    smoothed.Add(Interpolate(first, second, 0.75f));
                }
                smoothed.Add(path[^1]);
                path = smoothed;
            }

            var left = new Vector3[path.Count];
            var right = new Vector3[path.Count];
            for (var index = 0; index < path.Count; index++)
            {
                var previous = path[Math.Max(0, index - 1)].Point;
                var next = path[Math.Min(path.Count - 1, index + 1)].Point;
                var tangent = next - previous;
                if (tangent.LengthSquared() < 1e-6f) tangent = Vector2.UnitX;
                else tangent.Normalize();
                var normal = new Vector2(-tangent.Y, tangent.X);
                var leftWidth = Math.Max(path[index].Radius * 0.58f,
                    path[index].Radius + RiverBankOffsetM(path[index].Point, path[index].Radius, 1));
                var rightWidth = Math.Max(path[index].Radius * 0.58f,
                    path[index].Radius + RiverBankOffsetM(path[index].Point, path[index].Radius, 2));
                left[index] = SurfacePoint(path[index].Point + normal * leftWidth, path[index].Depth);
                right[index] = SurfacePoint(path[index].Point - normal * rightWidth, path[index].Depth);
            }

            for (var index = 0; index < path.Count - 1; index++)
            {
                AddTriangle(left[index], right[index], left[index + 1], baseWater, baseWater, baseWater);
                AddTriangle(right[index], right[index + 1], left[index + 1], baseWater, baseWater, baseWater);
            }

            static (Vector2 Point, float Radius, double Depth) Interpolate(
                (Vector2 Point, float Radius, double Depth) first,
                (Vector2 Point, float Radius, double Depth) second,
                float amount)
                => (Vector2.Lerp(first.Point, second.Point, amount),
                    MathHelper.Lerp(first.Radius, second.Radius, amount),
                    first.Depth + (second.Depth - first.Depth) * amount);
        }

        void AddWaterfallIfTerrainDrops(
            Vector2 start, Vector2 end, Vector2 perpendicular, float incomingWidth, float outgoingWidth,
            double startDepthMm, double endDepthMm, Color channelColor)
        {
            const int samples = 16;
            const double visibleDropM = 8;
            var largestDrop = 0.0;
            var transition = -1;
            for (var sample = 0; sample < samples; sample++)
            {
                var firstT = sample / (double)samples;
                var secondT = (sample + 1) / (double)samples;
                var firstPoint = Vector2.Lerp(start, end, (float)firstT);
                var secondPoint = Vector2.Lerp(start, end, (float)secondT);
                var drop = _terrainHeightSampler(firstPoint.X / Tile, firstPoint.Y / Tile)
                    - _terrainHeightSampler(secondPoint.X / Tile, secondPoint.Y / Tile);
                if (drop <= largestDrop) continue;
                largestDrop = drop;
                transition = sample;
            }

            if (transition < 0 || largestDrop < visibleDropM) return;
            var topPoint = Vector2.Lerp(start, end, (transition + 0.15f) / samples);
            var bottomPoint = Vector2.Lerp(start, end, (transition + 0.85f) / samples);
            var topElevation = _terrainHeightSampler(topPoint.X / Tile, topPoint.Y / Tile);
            var bottomElevation = _terrainHeightSampler(bottomPoint.X / Tile, bottomPoint.Y / Tile);
            if (topElevation <= bottomElevation + visibleDropM) return;

            var halfWidth = Math.Max(incomingWidth, outgoingWidth) * 0.64f;
            var topY = (float)(topElevation + Math.Clamp(startDepthMm / 1000, 0.12, 0.8));
            var bottomY = (float)(bottomElevation + Math.Clamp(endDepthMm / 1000, 0.12, 0.45));
            var topLeft = new Vector3(topPoint.X + perpendicular.X * halfWidth, topY,
                topPoint.Y + perpendicular.Y * halfWidth);
            var topRight = new Vector3(topPoint.X - perpendicular.X * halfWidth, topY,
                topPoint.Y - perpendicular.Y * halfWidth);
            var bottomLeft = new Vector3(bottomPoint.X + perpendicular.X * halfWidth, bottomY,
                bottomPoint.Y + perpendicular.Y * halfWidth);
            var bottomRight = new Vector3(bottomPoint.X - perpendicular.X * halfWidth, bottomY,
                bottomPoint.Y - perpendicular.Y * halfWidth);
            var bright = new Color((byte)Math.Min(255, channelColor.R + 22),
                (byte)Math.Min(255, channelColor.G + 28), (byte)Math.Min(255, channelColor.B + 24), (byte)244);
            var shadowed = new Color((byte)Math.Max(0, channelColor.R - 12),
                (byte)Math.Max(0, channelColor.G - 8), (byte)Math.Max(0, channelColor.B - 4), (byte)218);
            AddTriangle(topLeft, topRight, bottomLeft, bright, bright, shadowed);
            AddTriangle(topRight, bottomRight, bottomLeft, bright, shadowed, shadowed);
        }

        void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color colorA, Color colorB, Color colorC)
        {
            vertices.Add(new(a, colorA));
            vertices.Add(new(b, colorB));
            vertices.Add(new(c, colorC));
        }
    }

    private static double SampleCellField(double[] values, double x, double y)
    {
        x = Math.Clamp(x, 0, SimulationState.Width - 1);
        y = Math.Clamp(y, 0, SimulationState.Height - 1);
        var x0 = (int)x;
        var y0 = (int)y;
        var x1 = Math.Min(SimulationState.Width - 1, x0 + 1);
        var y1 = Math.Min(SimulationState.Height - 1, y0 + 1);
        var tx = x - x0;
        var ty = y - y0;
        var top = values[y0 * SimulationState.Width + x0]
            + (values[y0 * SimulationState.Width + x1] - values[y0 * SimulationState.Width + x0]) * tx;
        var bottom = values[y1 * SimulationState.Width + x0]
            + (values[y1 * SimulationState.Width + x1] - values[y1 * SimulationState.Width + x0]) * tx;
        return top + (bottom - top) * ty;
    }

    private static double SampleGridField(double[] values, int columns, int rows, double x, double y)
    {
        x = Math.Clamp(x, 0, columns - 1);
        y = Math.Clamp(y, 0, rows - 1);
        var x0 = (int)x;
        var y0 = (int)y;
        var x1 = Math.Min(columns - 1, x0 + 1);
        var y1 = Math.Min(rows - 1, y0 + 1);
        var tx = x - x0;
        var ty = y - y0;
        var top = values[y0 * columns + x0] + (values[y0 * columns + x1] - values[y0 * columns + x0]) * tx;
        var bottom = values[y1 * columns + x0] + (values[y1 * columns + x1] - values[y1 * columns + x0]) * tx;
        return top + (bottom - top) * ty;
    }

    private static double SmoothStep(double edge0, double edge1, double value)
    {
        var t = Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1);
        return t * t * (3 - 2 * t);
    }

    private void UpdateCamera()
    {
        var target = new Vector3(TerrainExtentM * 0.5f, (float)_centerElevationM, TerrainExtentM * 0.5f);
        var horizontal = 1000f;
        var elevation = 1400f;
        var eye = target + new Vector3(0, elevation, horizontal);
        _view = Matrix.CreateLookAt(eye, target, Vector3.Up);
        var viewHeight = 550f;
        var viewWidth = 680f;
        _projection = Matrix.CreateOrthographic(viewWidth, viewHeight, 1f, 4000f);
    }

    private Vector2 GroundToScreen(double worldX, double elevationM, double worldZ)
    {
        var projected = GraphicsDevice.Viewport.Project(
            new Vector3((float)worldX, (float)elevationM, (float)worldZ), _projection, _view, Matrix.Identity);
        return new Vector2(projected.X, projected.Y);
    }

    private bool TryGetCellAtScreen(int screenX, int screenY, out WorldCell cell)
    {
        var viewport = GraphicsDevice.Viewport;
        var near = viewport.Unproject(new Vector3(screenX, screenY, 0), _projection, _view, Matrix.Identity);
        var far = viewport.Unproject(new Vector3(screenX, screenY, 1), _projection, _view, Matrix.Identity);
        var direction = Vector3.Normalize(far - near);
        for (var distance = 0f; distance < 3000f; distance += 3f)
        {
            var point = near + direction * distance;
            if (point.X < 0 || point.Z < 0 || point.X >= TerrainExtentM || point.Z >= TerrainExtentM) continue;
            var height = _terrainHeightSampler(point.X / Tile, point.Z / Tile);
            if (point.Y > height) continue;
            var x = Math.Clamp((int)(point.X / Tile), 0, SimulationState.Width - 1);
            var y = Math.Clamp((int)(point.Z / Tile), 0, SimulationState.Height - 1);
            cell = _snapshot.Cells[y * SimulationState.Width + x];
            return true;
        }
        cell = default;
        return false;
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
        BuildTerrainGeometry();
        RefreshTerrainTexture(force: true);
        RefreshWaterGeometry(force: true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var texture in _visualCache.Values) texture.Dispose();
            _pixel?.Dispose();
            _terrainTexture?.Dispose();
            _terrainEffect?.Dispose();
            _waterEffect?.Dispose();
            foreach (var chunk in _terrainChunks)
            {
                chunk.Vertices.Dispose();
                chunk.Indices.Dispose();
            }
            _plateauVertexBuffer?.Dispose();
            _plateauIndexBuffer?.Dispose();
            _cliffVertexBuffer?.Dispose();
            _waterVertexBuffer?.Dispose();
            _batch?.Dispose();
        }
        base.Dispose(disposing);
    }

    private sealed record TerrainRenderChunk(VertexBuffer Vertices, IndexBuffer Indices, int PrimitiveCount, BoundingBox Bounds);
    private enum VisualKind : byte { Rock, Grass, Woody, Shadow, Wildflower }
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
