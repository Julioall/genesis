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
using Game.Modules.Biosphere;

var capturePath = GetCapturePath(args);
var snapshotPath = GetSnapshotPath(args);
var benchmarkSeconds = GetBenchmarkDurationSeconds(args);
var defaultPresentationWidth = capturePath is null && benchmarkSeconds is null ? 1600 : 1280;
var defaultPresentationHeight = capturePath is null && benchmarkSeconds is null ? 900 : 720;
var presentationWidth = GetPresentationDimension(args, "--capture-width", defaultPresentationWidth, 640, 3440);
var presentationHeight = GetPresentationDimension(args, "--capture-height", defaultPresentationHeight, 360, 1440);
var visualEvaluation = args.Contains("--visual-evaluation", StringComparer.Ordinal);
var perspectiveProjection = GetProjection(args) == "perspective";
var showWaterFixtures = args.Contains("--water-evaluation-fixtures", StringComparer.Ordinal)
    && !args.Contains("--snapshot-water-only", StringComparer.Ordinal);
var wideEvaluationView = args.Contains("--wide-evaluation-view", StringComparer.Ordinal);
var panoramaEvaluationView = args.Contains("--panorama-evaluation-view", StringComparer.Ordinal);
var referenceCompositionView = args.Contains("--reference-composition-view", StringComparer.Ordinal);
var treeBranchesOnly = args.Contains("--tree-branches-only", StringComparer.Ordinal);
var treeQualityView = args.Contains("--tree-quality-view", StringComparer.Ordinal) || treeBranchesOnly;
var referenceLandscapeView = args.Contains("--reference-landscape-view", StringComparer.Ordinal) || treeQualityView;
var lowerReferenceAngle = args.Contains("--lower-reference-angle", StringComparer.Ordinal);
var higherReferenceAngle = args.Contains("--higher-reference-angle", StringComparer.Ordinal);
var bakedTerrainHillshade = args.Contains("--baked-terrain-hillshade", StringComparer.Ordinal);
var hybridTreeLod = args.Contains("--hybrid-tree-lod", StringComparer.Ordinal);
var crossedTreeBillboards = args.Contains("--crossed-tree-billboards", StringComparer.Ordinal);
var treeBillboards = hybridTreeLod || crossedTreeBillboards || args.Contains("--tree-billboards", StringComparer.Ordinal);
var volumeCanopy = args.Contains("--volume-canopy", StringComparer.Ordinal);
var exaggeratedReliefView = args.Contains("--exaggerated-relief-view", StringComparer.Ordinal);
var distanceAwareTreeLod = args.Contains("--distance-aware-tree-lod", StringComparer.Ordinal);
var synchronizeWithVsync = !args.Contains("--disable-vsync", StringComparer.Ordinal);
var displayReliefScale = GetDisplayReliefScale(args, exaggeratedReliefView);
var initialCameraZoom = GetInitialCameraZoom(args, visualEvaluation && referenceLandscapeView, treeQualityView);
var initialCameraFocus = GetInitialCameraFocus(args);
var seed = GetSeed(args);
using var game = new NaturalWorldGame(capturePath, snapshotPath, benchmarkSeconds,
    presentationWidth, presentationHeight, visualEvaluation, perspectiveProjection,
    showWaterFixtures, wideEvaluationView, panoramaEvaluationView, referenceCompositionView, referenceLandscapeView,
    lowerReferenceAngle, higherReferenceAngle, bakedTerrainHillshade, hybridTreeLod, treeBillboards, crossedTreeBillboards,
    volumeCanopy, distanceAwareTreeLod, treeQualityView, treeBranchesOnly,
    displayReliefScale, initialCameraZoom, initialCameraFocus, seed, synchronizeWithVsync);
game.Run();

static string? GetCapturePath(string[] arguments)
{
    for (var index = 0; index < arguments.Length - 1; index++)
        if (arguments[index] == "--capture") return Path.GetFullPath(arguments[index + 1]);
    return null;
}

static string? GetSnapshotPath(string[] arguments)
{
    for (var index = 0; index < arguments.Length - 1; index++)
        if (arguments[index] == "--snapshot-file") return Path.GetFullPath(arguments[index + 1]);
    return null;
}

static int? GetBenchmarkDurationSeconds(string[] arguments)
{
    for (var index = 0; index < arguments.Length - 1; index++)
        if (arguments[index] == "--benchmark-seconds")
            return Math.Clamp(int.Parse(arguments[index + 1]), 1, 3600);
    return null;
}

static int GetPresentationDimension(string[] arguments, string option, int defaultValue, int minimum, int maximum)
{
    for (var index = 0; index < arguments.Length - 1; index++)
        if (arguments[index] == option)
            return Math.Clamp(int.Parse(arguments[index + 1], System.Globalization.CultureInfo.InvariantCulture), minimum, maximum);
    return defaultValue;
}

static ulong GetSeed(string[] arguments)
{
    for (var index = 0; index < arguments.Length - 1; index++)
        if (arguments[index] == "--seed") return ulong.Parse(arguments[index + 1]);
    return 1;
}

static double GetDisplayReliefScale(string[] arguments, bool exaggeratedReliefView)
{
    for (var index = 0; index < arguments.Length - 1; index++)
        if (arguments[index] == "--display-relief-scale")
            return Math.Clamp(double.Parse(arguments[index + 1], System.Globalization.CultureInfo.InvariantCulture), 0.40, 1.50);
    return exaggeratedReliefView ? 1.40 : 1.0;
}

static float GetInitialCameraZoom(string[] arguments, bool referenceLandscapeView, bool treeQualityView)
{
    for (var index = 0; index < arguments.Length - 1; index++)
        if (arguments[index] == "--camera-zoom")
            return Math.Clamp(float.Parse(arguments[index + 1], System.Globalization.CultureInfo.InvariantCulture), 0.45f,
                NaturalWorldGame.MaximumCameraZoom);
    return treeQualityView ? 44f : referenceLandscapeView ? 1.25f : 1f;
}

static Vector2? GetInitialCameraFocus(string[] arguments)
{
    for (var index = 0; index < arguments.Length - 2; index++)
        if (arguments[index] == "--camera-focus")
            return new Vector2(
                float.Parse(arguments[index + 1], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(arguments[index + 2], System.Globalization.CultureInfo.InvariantCulture));
    return null;
}

static string GetProjection(string[] arguments)
{
    for (var index = 0; index < arguments.Length - 1; index++)
        if (arguments[index] == "--projection")
            return arguments[index + 1].Equals("perspective", StringComparison.OrdinalIgnoreCase)
                ? "perspective"
                : "orthographic";
    return "orthographic";
}

internal sealed class NaturalWorldGame : Microsoft.Xna.Framework.Game
{
    private const int Tile = 10;
    internal const float MaximumCameraZoom = 64f;
    private const int NormalTerrainPixelsPerCell = 5;
    private const int EvaluationTerrainPixelsPerCell = 8;
    private const int TerrainSubdivisions = 3;
    private const int TerrainChunkCells = 16;
    private const int VisualBackdropCells = 14;
    private const int VisualBackdropSubdivisions = 2;
    private const int OceanSubdivisions = 160;
    private const int WaterSubdivisions = 3;
    private const int VisibleRiverSelectionThresholdCells = 36;
    private const int VisibleRiverWidthReferenceCells = 160;
    private const double MinimumVisibleChannelWaterMm = 20;
    private const float RiverRenderWidthScale = 3.2f;
    private const float EvaluationWatercourseWidthScale = 3.2f;
    // Larger silhouettes survive the island overview and read as clustered outcrops,
    // while rock placement and determinism still come from the same snapshot cells.
    private const float EvaluationRockWidthScale = 2.45f;
    private const float EvaluationRockHeightScale = 1.60f;
    private const float TerrainExtentM = SimulationState.Width * Tile;
    private const float TerrainDepthM = SimulationState.Height * Tile;
    private const float OceanHalfExtentM = TerrainExtentM * 2f;
    private static readonly int[] DepthOrder = Enumerable.Range(0, SimulationState.Width * SimulationState.Height)
        .OrderBy(index => index / SimulationState.Width)
        .ThenBy(index => index % SimulationState.Width)
        .ToArray();
    private readonly GraphicsDeviceManager _graphics;
    private readonly string? _capturePath;
    private readonly string? _snapshotPath;
    private readonly int? _benchmarkDurationSeconds;
    private readonly bool _visualEvaluation;
    private readonly bool _wideEvaluationView;
    private readonly bool _panoramaEvaluationView;
    private readonly bool _referenceCompositionView;
    private readonly bool _referenceLandscapeView;
    private readonly bool _lowerReferenceAngle;
    private readonly bool _higherReferenceAngle;
    private readonly bool _bakedTerrainHillshade;
    private readonly bool _hybridTreeLod;
    private readonly bool _treeBillboards;
    private readonly bool _crossedTreeBillboards;
    private readonly bool _volumeCanopy;
    private readonly bool _distanceAwareTreeLod;
    private readonly bool _treeQualityView;
    private readonly bool _treeBranchesOnly;
    private readonly double _displayReliefScale;
    private bool _showWaterFixtures;
    private readonly List<double> _benchmarkFrameTimesMs = [];
    private readonly Dictionary<VisualKey, Texture2D> _visualCache = [];
    private readonly Dictionary<(ulong Seed, double East, double North), double> _visualAnchorElevations = [];
    private readonly List<GroundSprite> _groundSprites = [];
    private readonly Dictionary<char, string[]> _glyphs = TinyFont.Glyphs;
    private SpriteBatch _batch = null!;
    private Texture2D _pixel = null!;
    private Texture2D _terrainTexture = null!;
    private BasicEffect _terrainEffect = null!;
    private BasicEffect _waterEffect = null!;
    private Effect _waterSurfaceEffect = null!;
    private BasicEffect _floraEffect = null!;
    private AlphaTestEffect? _treeEffect;
    private Texture2D? _treeMaterialAtlas;
    private readonly List<TerrainRenderChunk> _terrainChunks = [];
    private readonly List<TerrainColorRenderChunk> _terrainBackdropChunks = [];
    private readonly List<ProceduralTreeInstance> _floraTrees = [];
    private readonly List<ProceduralRockInstance> _evaluationRocks = [];
    private VertexBuffer? _plateauVertexBuffer;
    private IndexBuffer? _plateauIndexBuffer;
    private int _plateauPrimitiveCount;
    private VertexBuffer? _cliffVertexBuffer;
    private int _cliffPrimitiveCount;
    private VertexBuffer? _waterVertexBuffer;
    private int _waterPrimitiveCount;
    private VertexBuffer? _oceanVertexBuffer;
    private IndexBuffer? _oceanIndexBuffer;
    private int _oceanPrimitiveCount;
    private int _waterVertexCount;
    private CoastlinePoint[] _coastline = [];
    private VertexBuffer? _beachVertexBuffer;
    private int _beachPrimitiveCount;
    private DynamicVertexBuffer? _coastFoamVertexBuffer;
    private VertexPositionColor[] _coastFoamVertices = [];
    private VertexBuffer? _floraVertexBuffer;
    private int _floraPrimitiveCount;
    private VertexBuffer? _treeVertexBuffer;
    private int _treePrimitiveCount;
    private VertexBuffer? _evaluationShadowVertexBuffer;
    private int _evaluationShadowPrimitiveCount;
    private VertexBuffer? _evaluationRockVertexBuffer;
    private int _evaluationRockPrimitiveCount;
    private AlphaTestEffect? _treeBillboardEffect;
    private Texture2D[] _treeBillboardTextures = [];
    private readonly List<TreeBillboardRenderBatch> _treeBillboardBatches = [];
    private Matrix _view = Matrix.Identity;
    private Matrix _projection = Matrix.Identity;
    private Vector3 _cameraPosition;
    private Vector3 _cameraTarget;
    private Vector2 _cameraPanM;
    private Vector2 _treeGalleryCenterM;
    private readonly Vector2 _initialCameraPanM;
    private readonly float _initialCameraZoom;
    private float _cameraZoom = 1f;
    private MouseState _previousMouse;
    private bool _hasPreviousMouse;
    private bool _cameraNavigationDirty;
    private double _cameraNavigationIdleSeconds;
    private float _treeLodProjectionScale;
    private Func<double, double, double> _terrainHeightSampler = null!;
    private Func<double, double, double> _underlyingHeightSampler = null!;
    private ulong? _evaluationWatercourseSeed;
    private List<Vector2>? _evaluationWatercoursePath;
    private SurfaceDrainageField _surfaceDrainage;
    private int[] _waterDepressionId = [];
    private double _centerElevationM;
    private double _seaLevelM;
    private long _terrainRevision = -1;
    private long _waterRevision = -1;
    private long _groundSpritesRevision = -1;
    private ulong _groundSpritesSeed = ulong.MaxValue;
    private int _groundSpritesViewportWidth = -1;
    private int _groundSpritesViewportHeight = -1;
    private Season _groundSpritesSeason = Season.Summer;
    private int _terrainOverlay = -1;
    private WorldSimulation _world = null!;
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
    private double _benchmarkWaterBuildTotalMs;
    private double _benchmarkWaterBuildMaxMs;
    private int _benchmarkWaterBuildCount;
    private double _benchmarkSpriteBuildTotalMs;
    private double _benchmarkSpriteBuildMaxMs;
    private int _benchmarkSpriteBuildCount;
    private double _benchmarkTextureBuildTotalMs;
    private double _benchmarkTextureBuildMaxMs;
    private int _benchmarkTextureBuildCount;
    private int _floraTreeCount;
    private int _floraVertexCount;
    private int _groundCoverClusterCount;
    private int _understoryShrubClusterCount;
    private int _evaluationShadowVertexCount;
    private int _treeBillboardCount;
    private int _treeBillboardVertexCount;
    private bool _paused;
    private readonly bool _synchronizeWithVsync;
    private bool _perspectiveProjection;
    private bool _showHud = true;
    private int _overlayMode;
    private int _speedIndex = 2;
    private static readonly int[] Speeds = [1, 2, 6, 12, 24];
    private readonly string _savePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Game", "natural-world-save.json");

    public NaturalWorldGame(string? capturePath, string? snapshotPath, int? benchmarkDurationSeconds,
        int presentationWidth, int presentationHeight, bool visualEvaluation,
        bool perspectiveProjection, bool showWaterFixtures, bool wideEvaluationView,
        bool panoramaEvaluationView, bool referenceCompositionView, bool referenceLandscapeView,
        bool lowerReferenceAngle, bool higherReferenceAngle, bool bakedTerrainHillshade, bool hybridTreeLod, bool treeBillboards,
        bool crossedTreeBillboards, bool volumeCanopy,
        bool distanceAwareTreeLod, bool treeQualityView, bool treeBranchesOnly, double displayReliefScale, float initialCameraZoom,
        Vector2? initialCameraFocus, ulong seed, bool synchronizeWithVsync)
    {
        _capturePath = capturePath;
        _snapshotPath = snapshotPath;
        _synchronizeWithVsync = synchronizeWithVsync;
        _visualEvaluation = visualEvaluation;
        _wideEvaluationView = visualEvaluation && wideEvaluationView;
        _panoramaEvaluationView = visualEvaluation && panoramaEvaluationView;
        _referenceCompositionView = visualEvaluation && referenceCompositionView;
        _referenceLandscapeView = visualEvaluation && referenceLandscapeView;
        _lowerReferenceAngle = visualEvaluation && referenceLandscapeView && lowerReferenceAngle;
        _higherReferenceAngle = visualEvaluation && referenceLandscapeView && higherReferenceAngle;
        _bakedTerrainHillshade = visualEvaluation && bakedTerrainHillshade;
        _hybridTreeLod = visualEvaluation && referenceLandscapeView && hybridTreeLod;
        _treeBillboards = visualEvaluation && treeBillboards;
        _crossedTreeBillboards = visualEvaluation && crossedTreeBillboards;
        _volumeCanopy = visualEvaluation && (volumeCanopy || hybridTreeLod || referenceLandscapeView);
        _distanceAwareTreeLod = visualEvaluation && distanceAwareTreeLod;
        _treeQualityView = visualEvaluation && treeQualityView;
        _treeBranchesOnly = visualEvaluation && treeBranchesOnly;
        _displayReliefScale = visualEvaluation ? displayReliefScale : 1.0;
        _cameraZoom = Math.Clamp(initialCameraZoom, 0.45f, MaximumCameraZoom);
        _initialCameraZoom = _cameraZoom;
        var baseEastM = visualEvaluation
            ? _wideEvaluationView || _panoramaEvaluationView || _referenceCompositionView || _referenceLandscapeView
                ? TerrainExtentM * 0.5f + (_referenceLandscapeView ? 80f : 0f)
                : 320f
            : TerrainExtentM * 0.5f;
        var baseNorthM = TerrainDepthM * 0.5f;
        _treeGalleryCenterM = new Vector2(baseEastM, baseNorthM);
        if (initialCameraFocus is { } focus)
        {
            var focusEastM = Math.Clamp(focus.X, 0, TerrainExtentM);
            var focusNorthM = Math.Clamp(focus.Y, 0, TerrainDepthM);
            _initialCameraPanM = new Vector2(focusEastM - baseEastM, focusNorthM - baseNorthM);
        }
        else
        {
            _initialCameraPanM = Vector2.Zero;
        }
        _cameraPanM = _initialCameraPanM;
        _showWaterFixtures = showWaterFixtures;
        _perspectiveProjection = perspectiveProjection;
        _paused = visualEvaluation;
        _showHud = capturePath is null;
        _benchmarkDurationSeconds = benchmarkDurationSeconds;
        _world = _snapshotPath is null
            ? WorldSimulation.Create(seed)
            : WorldSimulation.Load(File.ReadAllText(_snapshotPath));
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = presentationWidth,
            PreferredBackBufferHeight = presentationHeight,
            SynchronizeWithVerticalRetrace = synchronizeWithVsync
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = synchronizeWithVsync;
        Window.Title = _treeQualityView ? "Procedural Tree Quality Gallery"
            : _bakedTerrainHillshade ? "Baked Terrain Hillshade Evaluation"
            : _hybridTreeLod ? "Hybrid Tree LOD Evaluation"
            : _crossedTreeBillboards ? "Crossed Tree Billboard Evaluation"
            : _lowerReferenceAngle ? "Lower Angle Landscape Evaluation"
            : _treeBillboards ? "Tree Billboard Evaluation"
            : _volumeCanopy ? "Procedural Volume Canopy Evaluation"
            : _distanceAwareTreeLod ? "Distance-aware Tree LOD Evaluation"
            : _displayReliefScale > 1.01 ? "Exaggerated Relief Evaluation"
            : _displayReliefScale < 0.99 ? "Compressed Relief Evaluation"
            : _referenceLandscapeView ? "Reference Landscape Composition"
            : _referenceCompositionView ? "Reference Composition Evaluation"
            : _panoramaEvaluationView ? "Panorama Evaluation"
            : visualEvaluation ? "Visual Evaluation" : "Temperate Valley (L0)";
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        _batch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _snapshot = _world.Snapshot();
        if (_treeQualityView) _treeGalleryCenterM = FindTreeQualityGalleryCenter();
        RefreshHydrologyPresentationFields();
        var terrainPixelsPerCell = _visualEvaluation ? EvaluationTerrainPixelsPerCell : NormalTerrainPixelsPerCell;
        _terrainTexture = new Texture2D(GraphicsDevice,
            SimulationState.Width * terrainPixelsPerCell, SimulationState.Height * terrainPixelsPerCell);
        RefreshTerrainTexture(force: true);
        _terrainEffect = new BasicEffect(GraphicsDevice)
        {
            TextureEnabled = true,
            VertexColorEnabled = false,
            LightingEnabled = true,
            FogEnabled = _referenceLandscapeView,
            FogColor = new Vector3(135f / 255f, 164f / 255f, 151f / 255f),
            FogStart = _referenceLandscapeView ? 1350f : 720f,
            FogEnd = _referenceLandscapeView ? 4200f : 1900f,
            AmbientLightColor = _visualEvaluation
                ? new Vector3(0.62f, 0.65f, 0.58f)
                : new Vector3(0.78f, 0.79f, 0.72f)
        };
        _terrainEffect.DirectionalLight0.Enabled = true;
        _terrainEffect.DirectionalLight0.DiffuseColor = _visualEvaluation
            ? new Vector3(0.50f, 0.49f, 0.44f)
            : new Vector3(0.22f, 0.22f, 0.19f);
        _terrainEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(0.45f, -1f, 0.35f));
        _terrainEffect.DirectionalLight0.SpecularColor = Vector3.Zero;
        _waterEffect = new BasicEffect(GraphicsDevice)
        {
            TextureEnabled = false,
            VertexColorEnabled = true,
            LightingEnabled = false,
            FogEnabled = _referenceLandscapeView,
            FogColor = new Vector3(135f / 255f, 164f / 255f, 151f / 255f),
            FogStart = _referenceLandscapeView ? 1500f : 720f,
            FogEnd = _referenceLandscapeView ? 5200f : 1900f
        };
        _floraEffect = new BasicEffect(GraphicsDevice)
        {
            TextureEnabled = false,
            VertexColorEnabled = true,
            LightingEnabled = false,
            FogEnabled = _referenceLandscapeView,
            FogColor = new Vector3(21f / 255f, 34f / 255f, 34f / 255f),
            FogStart = _referenceLandscapeView ? 1200f : 620f,
            FogEnd = _referenceLandscapeView ? 3600f : 1180f
        };
        _treeMaterialAtlas = ProceduralTreeMaterialAtlasBuilder.Build(GraphicsDevice);
        _treeEffect = new AlphaTestEffect(GraphicsDevice)
        {
            AlphaFunction = CompareFunction.Greater,
            ReferenceAlpha = 40,
            VertexColorEnabled = true,
            FogEnabled = _referenceLandscapeView,
            FogColor = new Vector3(21f / 255f, 34f / 255f, 34f / 255f),
            FogStart = _referenceLandscapeView ? 1200f : 620f,
            FogEnd = _referenceLandscapeView ? 3600f : 1180f,
            World = Matrix.Identity
        };
        if (_treeBillboards) InitializeTreeBillboards();
        BuildTerrainGeometry();
        RefreshWaterGeometry(force: true);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _waterSurfaceEffect = Content.Load<Effect>("Effects/Water");
        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var navigationChanged = UpdateCameraNavigation(gameTime, keyboard, Mouse.GetState());
        if (Pressed(keyboard, Keys.Escape)) Exit();
        if (Pressed(keyboard, Keys.Space)) _paused = !_paused;
        if (Pressed(keyboard, Keys.D1)) _overlayMode = 0;
        if (Pressed(keyboard, Keys.D2)) _overlayMode = 1;
        if (Pressed(keyboard, Keys.D3)) _overlayMode = 2;
        if (Pressed(keyboard, Keys.H)) _showHud = !_showHud;
        if (_visualEvaluation && Pressed(keyboard, Keys.C))
        {
            _perspectiveProjection = !_perspectiveProjection;
            _groundSpritesRevision = -1;
        }
        if (_visualEvaluation && Pressed(keyboard, Keys.W))
        {
            _showWaterFixtures = !_showWaterFixtures;
            RefreshWaterGeometry(force: true);
        }
        if (Pressed(keyboard, Keys.OemPlus) || Pressed(keyboard, Keys.Add)) _speedIndex = Math.Min(Speeds.Length - 1, _speedIndex + 1);
        if (Pressed(keyboard, Keys.OemMinus) || Pressed(keyboard, Keys.Subtract)) _speedIndex = Math.Max(0, _speedIndex - 1);
        if (Pressed(keyboard, Keys.F5)) SaveWorld();
        if (Pressed(keyboard, Keys.F9)) LoadWorld();

        if (_cameraNavigationDirty)
        {
            if (navigationChanged) _cameraNavigationIdleSeconds = 0;
            else _cameraNavigationIdleSeconds += Math.Clamp(gameTime.ElapsedGameTime.TotalSeconds, 0, 0.1);
            if (_cameraNavigationIdleSeconds >= 0.28)
            {
                // Flora and rocks are frustum-culled when their meshes are built. Refresh once
                // the user pauses navigation so panning does not rebuild millions of vertices
                // on every input frame.
                _groundSpritesRevision = -1;
                _cameraNavigationDirty = false;
            }
        }

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
        if (_visualEvaluation) EnsureGroundSprites();
        if (_treeBranchesOnly)
        {
            GraphicsDevice.Clear(new Color(75, 79, 76));
            DrawFloraGeometry();
        }
        else
        {
        GraphicsDevice.Clear(new Color(38, 86, 97));
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        DrawOceanSurface(gameTime);

        _terrainEffect.World = Matrix.Identity;
        _terrainEffect.View = _view;
        _terrainEffect.Projection = _projection;
        _terrainEffect.Texture = _terrainTexture;
        var terrainFrustum = new BoundingFrustum(_view * _projection);
        _terrainEffect.TextureEnabled = false;
        _terrainEffect.VertexColorEnabled = true;
        _terrainEffect.LightingEnabled = false;
        foreach (var chunk in _terrainBackdropChunks)
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
        _terrainEffect.TextureEnabled = true;
        _terrainEffect.VertexColorEnabled = false;
        _terrainEffect.LightingEnabled = _referenceLandscapeView;
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

        DrawBeachSurface();
        DrawPlateauGeometry();
        DrawEvaluationRockGeometry();
        DrawWaterSurface(gameTime);
        DrawCoastFoam(gameTime);
        DrawEvaluationTreeShadows();
        DrawEvaluationTreeBillboards();

        GraphicsDevice.DepthStencilState = DepthStencilState.None;
        _batch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
        DrawGroundObjects();
        _batch.End();
        DrawFloraGeometry();
        }
        if (_showHud)
        {
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            _batch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
            DrawHud();
            _batch.End();
        }
        if (_capturePath is not null)
        {
            CaptureFrameAndExit();
            base.Draw(gameTime);
            return;
        }
        RecordBenchmarkFrame();
        base.Draw(gameTime);
    }

    private void DrawWaterSurface(GameTime gameTime)
    {
        if (_waterVertexBuffer is null || _waterPrimitiveCount == 0) return;
        SetWaterSurfaceEffect(gameTime, 0f);
        // Water vertices store straight-alpha colors. AlphaBlend assumes premultiplied RGB,
        // which makes the pale glints much brighter than their authored colors.
        GraphicsDevice.BlendState = BlendState.NonPremultiplied;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.SetVertexBuffer(_waterVertexBuffer);
        GraphicsDevice.Indices = null;
        foreach (var pass in _waterSurfaceEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, _waterPrimitiveCount);
        }
        GraphicsDevice.BlendState = BlendState.Opaque;
    }

    private void SetWaterSurfaceEffect(GameTime gameTime, float waveAmplitude)
    {
        _waterSurfaceEffect.Parameters["WorldViewProjection"].SetValue(_view * _projection);
        _waterSurfaceEffect.Parameters["CameraPosition"].SetValue(_cameraPosition);
        _waterSurfaceEffect.Parameters["TimeSeconds"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);
        _waterSurfaceEffect.Parameters["WaveAmplitude"].SetValue(waveAmplitude);
        _waterSurfaceEffect.Parameters["FogEnabled"].SetValue(_referenceLandscapeView ? 1f : 0f);
        _waterSurfaceEffect.Parameters["FogStart"].SetValue(1500f);
        _waterSurfaceEffect.Parameters["FogEnd"].SetValue(5200f);
        _waterSurfaceEffect.Parameters["FogColor"].SetValue(new Vector4(159f / 255f, 200f / 255f,
            202f / 255f, 1f));
    }

    private void DrawBeachSurface()
    {
        if (_beachVertexBuffer is null || _beachPrimitiveCount == 0) return;
        _terrainEffect.World = Matrix.Identity;
        _terrainEffect.View = _view;
        _terrainEffect.Projection = _projection;
        _terrainEffect.TextureEnabled = false;
        _terrainEffect.VertexColorEnabled = true;
        _terrainEffect.LightingEnabled = false;
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.SetVertexBuffer(_beachVertexBuffer);
        GraphicsDevice.Indices = null;
        foreach (var pass in _terrainEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, _beachPrimitiveCount);
        }
    }

    private void DrawCoastFoam(GameTime gameTime)
    {
        if (_coastFoamVertexBuffer is null || _coastline.Length == 0) return;
        _coastFoamVertices = ProceduralBeachMeshBuilder.BuildWaveFoam(_coastline,
            _terrainHeightSampler, Tile, _seaLevelM, gameTime.TotalGameTime.TotalSeconds);
        _coastFoamVertexBuffer.SetData(_coastFoamVertices, 0, _coastFoamVertices.Length, SetDataOptions.Discard);
        _waterEffect.World = Matrix.Identity;
        _waterEffect.View = _view;
        _waterEffect.Projection = _projection;
        _waterEffect.VertexColorEnabled = true;
        GraphicsDevice.BlendState = BlendState.NonPremultiplied;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.SetVertexBuffer(_coastFoamVertexBuffer);
        GraphicsDevice.Indices = null;
        foreach (var pass in _waterEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, _coastFoamVertices.Length / 3);
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
        var privateMemoryMiB = process.PrivateMemorySize64 / (1024.0 * 1024.0);
        Console.WriteLine(FormattableString.Invariant(
            $"benchmark={( _visualEvaluation ? "visual-evaluation" : "desktop-l0")} projection={ProjectionName} resolution={GraphicsDevice.Viewport.Width}x{GraphicsDevice.Viewport.Height} vsync={_synchronizeWithVsync} fixedTimeStep={IsFixedTimeStep} seed={_snapshot.Seed} simulationPaused={_paused} ticksPerSecond={Speeds[_speedIndex]} warmupSeconds=5 measuredSeconds={_benchmarkMeasuredSeconds:F1} frames={ordered.Length} averageFps={averageFps:F2} p95FrameMs={p95Ms:F2} p99FrameMs={p99Ms:F2} maxFrameMs={ordered[^1]:F2} cpuPercentOfOneCore={oneCorePercent:F1} workingSetMiB={workingSetMiB:F1} privateMemoryMiB={privateMemoryMiB:F1} visibleTrees={_floraTreeCount} treeBillboards={_treeBillboardCount} treeBillboardVertices={_treeBillboardVertexCount} floraVertices={_floraVertexCount} groundCoverClusters={_groundCoverClusterCount} understoryShrubs={_understoryShrubClusterCount} shadowVertices={_evaluationShadowVertexCount} evaluationRocks={_evaluationRocks.Count} waterBuildMaxMs={_benchmarkWaterBuildMaxMs:F2} spriteBuildMaxMs={_benchmarkSpriteBuildMaxMs:F2} terrainTextureBuildMaxMs={_benchmarkTextureBuildMaxMs:F2}"));
        Exit();

        static double Percentile(double[] sorted, double percentile)
        {
            var index = Math.Clamp((int)Math.Ceiling(sorted.Length * percentile) - 1, 0, sorted.Length - 1);
            return sorted[index];
        }
    }

    private void DrawGroundObjects()
    {
        EnsureGroundSprites();
        foreach (var sprite in _groundSprites)
        {
            if (_visualEvaluation && sprite.Key.Kind is VisualKind.Grass or VisualKind.Shadow) continue;
            _batch.Draw(TextureFor(sprite.Key), sprite.Destination, Color.White);
        }
    }

    private void DrawEvaluationTreeShadows()
    {
        if (!_visualEvaluation || _evaluationShadowVertexBuffer is null || _evaluationShadowPrimitiveCount == 0) return;
        GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        GraphicsDevice.BlendState = BlendState.NonPremultiplied;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        _floraEffect.World = Matrix.Identity;
        _floraEffect.View = _view;
        _floraEffect.Projection = _projection;
        _floraEffect.TextureEnabled = false;
        _floraEffect.VertexColorEnabled = true;
        _floraEffect.LightingEnabled = false;
        GraphicsDevice.SetVertexBuffer(_evaluationShadowVertexBuffer);
        GraphicsDevice.Indices = null;
        foreach (var pass in _floraEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, _evaluationShadowPrimitiveCount);
        }
        GraphicsDevice.BlendState = BlendState.Opaque;
    }

    private void InitializeTreeBillboards()
    {
        var names = new[] { "oak", "pine", "birch", "autumn-maple", "willow" };
        _treeBillboardTextures = names.Select(name =>
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Content", "VisualEvaluation", "Trees", $"{name}.png");
            using var stream = File.OpenRead(path);
            return Texture2D.FromStream(GraphicsDevice, stream);
        }).ToArray();
        _treeBillboardEffect = new AlphaTestEffect(GraphicsDevice)
        {
            AlphaFunction = CompareFunction.Greater,
            ReferenceAlpha = 64,
            VertexColorEnabled = false,
            World = Matrix.Identity
        };
    }

    private void DrawEvaluationTreeBillboards()
    {
        if (!_treeBillboards || _treeBillboardEffect is null || _treeBillboardBatches.Count == 0) return;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
        _treeBillboardEffect.World = Matrix.Identity;
        _treeBillboardEffect.View = _view;
        _treeBillboardEffect.Projection = _projection;
        foreach (var batch in _treeBillboardBatches)
        {
            _treeBillboardEffect.Texture = _treeBillboardTextures[batch.RenderProfileRow];
            GraphicsDevice.SetVertexBuffer(batch.Vertices);
            GraphicsDevice.Indices = null;
            foreach (var pass in _treeBillboardEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, batch.PrimitiveCount);
            }
        }
        GraphicsDevice.SetVertexBuffer(null);
    }

    private void DrawFloraGeometry()
    {
        DrawProceduralTreeGeometry();
        if (_floraVertexBuffer is null || _floraPrimitiveCount == 0) return;

        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        _floraEffect.World = Matrix.Identity;
        _floraEffect.View = _view;
        _floraEffect.Projection = _projection;
        _floraEffect.TextureEnabled = false;
        _floraEffect.VertexColorEnabled = true;
        _floraEffect.LightingEnabled = false;
        GraphicsDevice.SetVertexBuffer(_floraVertexBuffer);
        GraphicsDevice.Indices = null;
        foreach (var pass in _floraEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, _floraPrimitiveCount);
        }
    }

    private void DrawProceduralTreeGeometry()
    {
        if (_treeVertexBuffer is null || _treePrimitiveCount == 0
            || _treeEffect is null || _treeMaterialAtlas is null) return;

        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
        _treeEffect.Texture = _treeMaterialAtlas;
        _treeEffect.World = Matrix.Identity;
        _treeEffect.View = _view;
        _treeEffect.Projection = _projection;
        _treeEffect.VertexColorEnabled = true;
        GraphicsDevice.SetVertexBuffer(_treeVertexBuffer);
        GraphicsDevice.Indices = null;
        foreach (var pass in _treeEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, _treePrimitiveCount);
        }
    }

    private void DrawEvaluationRockGeometry()
    {
        if (!_visualEvaluation || _evaluationRockVertexBuffer is null || _evaluationRockPrimitiveCount == 0) return;

        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        _floraEffect.World = Matrix.Identity;
        _floraEffect.View = _view;
        _floraEffect.Projection = _projection;
        _floraEffect.TextureEnabled = false;
        _floraEffect.VertexColorEnabled = true;
        _floraEffect.LightingEnabled = false;
        GraphicsDevice.SetVertexBuffer(_evaluationRockVertexBuffer);
        GraphicsDevice.Indices = null;
        foreach (var pass in _floraEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, _evaluationRockPrimitiveCount);
        }
    }

    private void EnsureGroundSprites()
    {
        var viewport = GraphicsDevice.Viewport;
        var revision = _snapshot.TickIndex / SimulationState.TicksPerDay;
        if (_groundSpritesRevision == revision && _groundSpritesSeed == _snapshot.Seed
            && _groundSpritesViewportWidth == viewport.Width && _groundSpritesViewportHeight == viewport.Height
            && _groundSpritesSeason == VisualSeason)
            return;
        var buildStarted = Stopwatch.GetTimestamp();
        var treeFrustum = new BoundingFrustum(_view * _projection);
        var visualBiomes = VisualBiomeField.Build(_snapshot);

        _groundSprites.Clear();
        _floraTrees.Clear();
        _evaluationRocks.Clear();
        void Add(VisualKey key, Vector3 ground, int x, int y, int width, int height)
            => _groundSprites.Add(new GroundSprite(key, new Rectangle(x, y, width, height), ground.Z, _groundSprites.Count));

        // Rebuild projected placements only when the snapshot, camera surface, or visual season changes.
        Span<TreeVisual> treeVisuals = stackalloc TreeVisual[5];
        foreach (var index in DepthOrder)
        {
            var cell = _snapshot.Cells[index];
            if (!IsIslandLand(cell.X + 0.5, cell.Y + 0.5)) continue;
            var biome = visualBiomes[cell.Index];
            var visualSeed = CounterRandom.VisualBits(_snapshot.Seed, cell.Index, 0x4752415353UL);
            var cellWorldX = (cell.X + 0.5) * Tile;
            var cellWorldZ = (cell.Y + 0.5) * Tile;

            if (RockVisible(cell, visualSeed))
            {
                var rockVariant = (int)(visualSeed & 7);
                var rockWidth = 23 + rockVariant * 3;
                var rockHeight = 16 + rockVariant * 2;
                var rockWorldX = cellWorldX + (CounterRandom.Unit(_snapshot.Seed, 0x524F434B504F5358UL,
                    (ulong)cell.Index, 0, 0, 0) - 0.5) * Tile * 0.56;
                var rockWorldZ = cellWorldZ + (CounterRandom.Unit(_snapshot.Seed, 0x524F434B504F535AUL,
                    (ulong)cell.Index, 0, 0, 0) - 0.5) * Tile * 0.56;
                var rockElevation = VisualAnchorElevation(rockWorldX / Tile, rockWorldZ / Tile);
                var rockGround = ProjectGround(rockWorldX, rockElevation, rockWorldZ);
                if (_visualEvaluation)
                {
                    var widthM = (8.0f + rockVariant * 1.25f) * EvaluationRockWidthScale;
                    var heightM = (3.8f + (rockVariant % 4) * 1.0f) * EvaluationRockHeightScale;
                    var rockCenter = new Vector3((float)rockWorldX, (float)rockElevation, (float)rockWorldZ);
                    if (treeFrustum.Intersects(new BoundingSphere(rockCenter + Vector3.Up * heightM * 0.5f, widthM)))
                    {
                        _evaluationRocks.Add(new ProceduralRockInstance(rockCenter, widthM, heightM,
                            visualSeed ^ (ulong)rockVariant, rockVariant));
                        var satelliteCount = CounterRandom.Unit(_snapshot.Seed, 0x524F434B434C5553UL,
                            (ulong)cell.Index, 0, 0, 0) < 0.34 ? 2 : 1;
                        for (var satellite = 0; satellite < satelliteCount; satellite++)
                        {
                            var satelliteIndex = (ulong)satellite;
                            var angle = (float)(CounterRandom.Unit(_snapshot.Seed, 0x524F434B53415441UL,
                                (ulong)cell.Index, satelliteIndex, 0, 0) * Math.Tau);
                            var satelliteWidth = widthM * (0.32f + (float)CounterRandom.Unit(_snapshot.Seed,
                                0x524F434B53415457UL, (ulong)cell.Index, satelliteIndex, 0, 0) * 0.22f);
                            var satelliteHeight = heightM * (0.38f + (float)CounterRandom.Unit(_snapshot.Seed,
                                0x524F434B53415448UL, (ulong)cell.Index, satelliteIndex, 0, 0) * 0.25f);
                            var distance = widthM * 0.36f + satelliteWidth * 0.18f;
                            var satelliteX = rockWorldX + Math.Cos(angle) * distance;
                            var satelliteZ = rockWorldZ + Math.Sin(angle) * distance;
                            var cellX = Math.Clamp((int)(satelliteX / Tile), 0, _snapshot.Width - 1);
                            var cellY = Math.Clamp((int)(satelliteZ / Tile), 0, _snapshot.Height - 1);
                            if (_snapshot.Cells[cellY * _snapshot.Width + cellX].SurfaceWaterMm >= 420) continue;
                            var satelliteElevation = VisualAnchorElevation(satelliteX / Tile, satelliteZ / Tile);
                            var satelliteCenter = new Vector3((float)satelliteX, (float)satelliteElevation, (float)satelliteZ);
                            if (treeFrustum.Intersects(new BoundingSphere(
                                    satelliteCenter + Vector3.Up * satelliteHeight * 0.5f, satelliteWidth)))
                                _evaluationRocks.Add(new ProceduralRockInstance(satelliteCenter, satelliteWidth,
                                    satelliteHeight, visualSeed ^ (0x534154454C4C4954UL + satelliteIndex),
                                    (rockVariant + satellite + 1) & 7));
                        }
                    }
                }
                else
                {
                    Add(new(VisualKind.Rock, rockVariant, 1, VisualSeason), rockGround,
                        (int)rockGround.X - rockWidth / 2, (int)rockGround.Y - rockHeight + 4,
                        rockWidth, rockHeight);
                }
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
                        var tuftWorldX = cellWorldX + (CounterRandom.Unit(_snapshot.Seed, 0x4752415353504F53UL,
                            (ulong)cell.Index, (ulong)tuft, 0, 0) - 0.5) * Tile * 0.72;
                        var tuftWorldZ = cellWorldZ + (CounterRandom.Unit(_snapshot.Seed, 0x4752415353504F5AUL,
                            (ulong)cell.Index, (ulong)tuft, 0, 0) - 0.5) * Tile * 0.72;
                        var tuftElevation = VisualAnchorElevation(tuftWorldX / Tile, tuftWorldZ / Tile);
                        var tuftGround = ProjectGround(tuftWorldX, tuftElevation, tuftWorldZ);
                        var offsetY = (int)(tuftSeed >> 14 & 7) - 3 - (tuft == 1 ? 2 : 0);
                        Add(new(VisualKind.Grass, (int)(tuftSeed >> 8 & 15), bucket, VisualSeason), tuftGround,
                            (int)tuftGround.X - width / 2, (int)tuftGround.Y - height + offsetY, width, height);
                    }
                }

                // Flowering is omitted until species and reproductive state are exposed by the snapshot.
            }

            if (cell.Woody.Present && cell.Woody.BiomassGPerM2 > 1)
            {
                var bucket = Math.Clamp((int)(cell.Woody.BiomassGPerM2 / 450), 1, 4);
                var archetypeRow = TreeRenderProfileRow(_snapshot.Seed, cell.X, cell.Y, visualSeed);
                // A snapshot cell aggregates woody biomass over 100 m² rather than describing
                // one organism. Represent each patch as a seed-stable cohort and divide its
                // biomass proxy among those render-only stems. The resulting size is a visual
                // heuristic, not an estimate of age, DBH, or species allometry.
                var woodyNeighbors = CountWoodyNeighbors(_snapshot, cell.X, cell.Y);
                var treeCount = 1
                    + (bucket >= 3 ? 1 : 0)
                    + (woodyNeighbors >= 4 ? 1 : 0)
                    + (bucket >= 4 && woodyNeighbors >= 7 ? 1 : 0);
                var competition = Math.Clamp(woodyNeighbors / 8f, 0f, 1f);
                var biomassPerVisualStem = cell.Woody.BiomassGPerM2 / treeCount;
                var crownCover = (float)BotanyModel.Cover(
                    cell.Woody with { BiomassGPerM2 = biomassPerVisualStem }, PlantKind.Woody);
                var displayHeight = TreeDisplayHeightMeters(archetypeRow);
                // Biomass is a patch-cover signal, not an individual age or height measurement.
                var crownDevelopment = Math.Clamp(0.50f + 0.48f * crownCover, 0.12f, 1f);
                for (var tree = 0; tree < treeCount; tree++)
                {
                    var treeSeed = CounterRandom.VisualBits(_snapshot.Seed, cell.Index,
                        0x47524F5645545245UL + (ulong)tree);
                    var scale = tree switch
                    {
                        0 => 1.0,
                        1 => 0.48 + CounterRandom.Unit(_snapshot.Seed,
                            0x47524F564553495AUL, (ulong)cell.Index, (ulong)tree, 0, 0) * 0.16,
                        2 => 0.30 + CounterRandom.Unit(_snapshot.Seed,
                            0x47524F564553495AUL, (ulong)cell.Index, (ulong)tree, 0, 0) * 0.16,
                        _ => 0.19 + CounterRandom.Unit(_snapshot.Seed,
                            0x47524F564553495AUL, (ulong)cell.Index, (ulong)tree, 0, 0) * 0.14
                    };
                    var individualHeight = 0.80f + (float)CounterRandom.Unit(_snapshot.Seed,
                        0x5452454548454947UL, (ulong)cell.Index, (ulong)tree, 0, 0) * 0.28f;
                    var height = displayHeight * individualHeight * (1f + competition * 0.07f) * (float)scale;
                    var maturity = Math.Clamp(crownDevelopment * (0.70f + (float)scale * 0.30f), 0.12f, 1f);
                    var clusterPhase = CounterRandom.Unit(_snapshot.Seed, 0x54524545434C5553UL,
                        (ulong)cell.Index, 0, 0, 0) * Math.Tau;
                    var treeAngle = clusterPhase + tree * 2.39996323
                        + (CounterRandom.Unit(_snapshot.Seed, 0x54524545504F5341UL,
                            (ulong)cell.Index, (ulong)tree, 0, 0) - 0.5) * 0.72;
                    var treeRadius = Tile * 0.39 * Math.Sqrt((tree + 0.3) / treeCount)
                        * (0.82 + CounterRandom.Unit(_snapshot.Seed, 0x54524545504F5352UL,
                            (ulong)cell.Index, (ulong)tree, 0, 0) * 0.30);
                    var clusterOffsetX = (CounterRandom.Unit(_snapshot.Seed, 0x54524545504F5358UL,
                        (ulong)cell.Index, 0, 0, 0) - 0.5) * Tile * 0.28;
                    var clusterOffsetZ = (CounterRandom.Unit(_snapshot.Seed, 0x54524545504F535AUL,
                        (ulong)cell.Index, 0, 0, 0) - 0.5) * Tile * 0.28;
                    var treeWorldX = cellWorldX + clusterOffsetX + Math.Cos(treeAngle) * treeRadius;
                    var treeWorldZ = cellWorldZ + clusterOffsetZ + Math.Sin(treeAngle) * treeRadius;
                    var treeElevation = VisualAnchorElevation(treeWorldX / Tile, treeWorldZ / Tile);
                    var treeGround = ProjectGround(treeWorldX, treeElevation, treeWorldZ);
                    var artVariant = archetypeRow * 4 + (int)(treeSeed >> 11 & 3);
                    var shadowWidth = (int)Math.Round(height * 1.30f) + 9 + (int)(treeSeed & 5);
                    treeVisuals[tree] = new TreeVisual(treeSeed, height, maturity, artVariant, shadowWidth,
                        treeWorldX, treeWorldZ, treeElevation, treeGround);
                }

                for (var tree = 1; tree < treeCount; tree++)
                {
                    var value = treeVisuals[tree];
                    var position = tree;
                    while (position > 0 && treeVisuals[position - 1].WorldZ > value.WorldZ)
                    {
                        treeVisuals[position] = treeVisuals[position - 1];
                        position--;
                    }
                    treeVisuals[position] = value;
                }

                var shadowAnchor = treeVisuals[0];
                for (var tree = 0; tree < treeCount; tree++)
                {
                    var visual = treeVisuals[tree];
                    if (visual.Height > shadowAnchor.Height) shadowAnchor = visual;
                }
                var shadowWorldX = shadowAnchor.WorldX + shadowAnchor.Height * 0.46f;
                var shadowWorldZ = shadowAnchor.WorldZ - shadowAnchor.Height * 0.52f;
                var shadowElevation = VisualAnchorElevation(shadowWorldX / Tile, shadowWorldZ / Tile);
                var shadowCenter = ProjectGround(shadowWorldX, shadowElevation, shadowWorldZ);
                var shadowHeight = Math.Max(8, (int)Math.Round(shadowAnchor.Height * 0.38f));
                if (!_visualEvaluation)
                    Add(new(VisualKind.Shadow, (int)(shadowAnchor.Seed & 7), bucket, VisualSeason), shadowCenter,
                        (int)shadowCenter.X - shadowAnchor.ShadowWidth / 2,
                        (int)shadowCenter.Y - shadowHeight / 2,
                        shadowAnchor.ShadowWidth, shadowHeight);

                for (var tree = 0; tree < treeCount; tree++)
                {
                    var visual = treeVisuals[tree];
                    var center = new Vector3((float)visual.WorldX,
                        (float)(visual.ElevationM + visual.Height * 0.48f), (float)visual.WorldZ);
                    if (treeFrustum.Intersects(new BoundingSphere(center, visual.Height * 0.78f)))
                    {
                        _floraTrees.Add(new ProceduralTreeInstance(
                            new Vector3((float)visual.WorldX, (float)visual.ElevationM, (float)visual.WorldZ),
                            visual.Height,
                            visual.ArtVariant / 4,
                            visual.Maturity,
                            visual.Seed,
                            VisualSeason));
                    }
                }
            }
        }

        if (_treeQualityView)
        {
            // A render-only gallery isolates the five current architectural recipes at
            // full maturity. It never writes to the snapshot or simulation state.
            _floraTrees.Clear();
            _evaluationRocks.Clear();
            for (var row = 0; row < 5; row++)
            {
                var worldX = _treeGalleryCenterM.X + (row - 2) * 18f;
                var worldZ = _treeGalleryCenterM.Y;
                var elevation = VisualAnchorElevation(worldX / Tile, worldZ / Tile);
                var height = TreeDisplayHeightMeters(row);
                var seed = CounterRandom.VisualBits(_snapshot.Seed, row, 0x545245455155414CUL);
                var ground = new Vector3(worldX, (float)elevation, worldZ);
                var crownCenter = ground + Vector3.Up * (height * 0.5f);
                if (treeFrustum.Intersects(new BoundingSphere(crownCenter, height * 0.78f)))
                    _floraTrees.Add(new ProceduralTreeInstance(ground, height, row, 1f, seed, VisualSeason));
            }
        }

        _groundSprites.Sort(static (first, second) =>
        {
            var depth = second.Depth.CompareTo(first.Depth);
            return depth != 0 ? depth : first.SortOrder.CompareTo(second.SortOrder);
        });

        var floraPixelsPerWorldUnit = viewport.Height / (_visualEvaluation ? 550f : 850f);
        var useDistanceAwareTreeLod = _distanceAwareTreeLod || _hybridTreeLod || _referenceLandscapeView;
        var treeLodProjectionScale = useDistanceAwareTreeLod
            ? _treeLodProjectionScale
            : floraPixelsPerWorldUnit;
        List<ProceduralTreeInstance> volumeTrees;
        List<ProceduralTreeInstance> billboardTrees;
        if (_hybridTreeLod)
        {
            volumeTrees = [];
            billboardTrees = [];
            foreach (var tree in _floraTrees)
            {
                var crownCenter = tree.Ground + Vector3.Up * (tree.Height * 0.5f);
                var distance = Math.Max(1f, Vector3.Distance(_cameraPosition, crownCenter));
                var projectedHeight = _perspectiveProjection
                    ? tree.Height * _treeLodProjectionScale / distance
                    : tree.Height * _treeLodProjectionScale;
                (projectedHeight >= 48 ? volumeTrees : billboardTrees).Add(tree);
            }
        }
        else
        {
            volumeTrees = _treeBillboards ? [] : _floraTrees;
            billboardTrees = _treeBillboards ? _floraTrees : [];
        }
        var treeVertices = volumeTrees.Count == 0
            ? []
            : ProceduralFloraMeshBuilder.Build(volumeTrees, treeLodProjectionScale,
                useDistanceAwareTreeLod && _perspectiveProjection, _cameraPosition,
                _volumeCanopy, useDistanceAwareTreeLod, _treeBranchesOnly);
        var billboardMeshes = _treeBillboards
            ? ProceduralTreeBillboardMeshBuilder.Build(billboardTrees,
                _referenceLandscapeView ? new Vector3(800, 0, 1400)
                    : _wideEvaluationView ? new Vector3(580, 0, 580)
                    : _referenceCompositionView ? new Vector3(255, 0, 381) : Vector3.UnitZ,
                floraPixelsPerWorldUnit, _crossedTreeBillboards || _hybridTreeLod)
            : [];
        foreach (var batch in _treeBillboardBatches) batch.Vertices.Dispose();
        _treeBillboardBatches.Clear();
        foreach (var mesh in billboardMeshes)
        {
            var buffer = new VertexBuffer(GraphicsDevice, VertexPositionTexture.VertexDeclaration,
                mesh.Vertices.Length, BufferUsage.WriteOnly);
            buffer.SetData(mesh.Vertices);
            _treeBillboardBatches.Add(new TreeBillboardRenderBatch(mesh.RenderProfileRow, buffer, mesh.Vertices.Length / 3));
        }
        var groundCover = _visualEvaluation && !_treeBranchesOnly
            ? BuildVisibleGroundCover(visualBiomes)
            : new GroundCoverBuildResult([], 0, 0);
        var shadowVertices = _visualEvaluation && !_treeBranchesOnly
            ? ProceduralTreeShadowMeshBuilder.Build(_floraTrees, _evaluationRocks, _terrainHeightSampler, Tile)
            : [];
        var floraVertices = groundCover.Vertices;
        var rockVertices = _visualEvaluation ? ProceduralRockMeshBuilder.Build(_evaluationRocks) : [];
        _floraTreeCount = _floraTrees.Count;
        _floraVertexCount = treeVertices.Length + floraVertices.Length;
        _groundCoverClusterCount = groundCover.Clusters;
        _understoryShrubClusterCount = groundCover.UnderstoryShrubs;
        _evaluationShadowVertexCount = shadowVertices.Length;
        _treeBillboardCount = _treeBillboards ? billboardTrees.Count : 0;
        _treeBillboardVertexCount = billboardMeshes.Sum(mesh => mesh.Vertices.Length);
        if (_capturePath is not null)
        {
            var heights = _floraTrees.Select(tree => tree.Height).Order().ToArray();
            var heightAt = (double percentile) => heights.Length == 0 ? 0f
                : heights[Math.Clamp((int)Math.Ceiling(heights.Length * percentile) - 1, 0, heights.Length - 1)];
            Console.WriteLine(FormattableString.Invariant(
                $"floraTrees={_floraTreeCount} treeHeightP10M={heightAt(0.10):F1} treeHeightP50M={heightAt(0.50):F1} treeHeightP90M={heightAt(0.90):F1} treeBillboards={_treeBillboardCount} treeBillboardVertices={_treeBillboardVertexCount} treeMeshVertices={treeVertices.Length} groundCoverVertices={floraVertices.Length} floraVertices={_floraVertexCount} groundCoverClusters={groundCover.Clusters} understoryShrubs={groundCover.UnderstoryShrubs} shadowVertices={shadowVertices.Length} evaluationRocks={_evaluationRocks.Count}"));
        }
        _floraVertexBuffer?.Dispose();
        _floraVertexBuffer = null;
        _floraPrimitiveCount = floraVertices.Length / 3;
        if (floraVertices.Length > 0)
        {
            _floraVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration,
                floraVertices.Length, BufferUsage.WriteOnly);
            _floraVertexBuffer.SetData(floraVertices);
        }
        _treeVertexBuffer?.Dispose();
        _treeVertexBuffer = null;
        _treePrimitiveCount = treeVertices.Length / 3;
        if (treeVertices.Length > 0)
        {
            _treeVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColorTexture.VertexDeclaration,
                treeVertices.Length, BufferUsage.WriteOnly);
            _treeVertexBuffer.SetData(treeVertices);
        }
        _evaluationShadowVertexBuffer?.Dispose();
        _evaluationShadowVertexBuffer = null;
        _evaluationShadowPrimitiveCount = shadowVertices.Length / 3;
        if (shadowVertices.Length > 0)
        {
            _evaluationShadowVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration,
                shadowVertices.Length, BufferUsage.WriteOnly);
            _evaluationShadowVertexBuffer.SetData(shadowVertices);
        }
        _evaluationRockVertexBuffer?.Dispose();
        _evaluationRockVertexBuffer = null;
        _evaluationRockPrimitiveCount = rockVertices.Length / 3;
        if (rockVertices.Length > 0)
        {
            _evaluationRockVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration,
                rockVertices.Length, BufferUsage.WriteOnly);
            _evaluationRockVertexBuffer.SetData(rockVertices);
        }

        _groundSpritesRevision = revision;
        _groundSpritesSeed = _snapshot.Seed;
        _groundSpritesViewportWidth = viewport.Width;
        _groundSpritesViewportHeight = viewport.Height;
        _groundSpritesSeason = VisualSeason;
        RecordBenchmarkBuild(buildStarted, ref _benchmarkSpriteBuildTotalMs,
            ref _benchmarkSpriteBuildMaxMs, ref _benchmarkSpriteBuildCount);
    }

    private readonly record struct TreeVisual(
        ulong Seed, float Height, float Maturity, int ArtVariant, int ShadowWidth,
        double WorldX, double WorldZ, double ElevationM, Vector3 Ground);

    private readonly record struct GroundSprite(VisualKey Key, Rectangle Destination, float Depth, int SortOrder);

    private double VisualAnchorElevation(double eastCell, double northCell)
    {
        var key = (_snapshot.Seed, eastCell, northCell);
        if (_visualAnchorElevations.TryGetValue(key, out var elevation)) return elevation;
        elevation = _terrainHeightSampler(eastCell, northCell);
        _visualAnchorElevations.Add(key, elevation);
        return elevation;
    }

    private void DrawHud()
    {
        var viewport = GraphicsDevice.Viewport;
        var mode = _overlayMode switch { 1 => "SOIL WATER", 2 => "BIOMASS", _ => "LANDSCAPE" };
        const int cardX = 22;
        const int cardY = 20;
        const int cardWidth = 560;
        const int cardHeight = 108;
        _batch.Draw(_pixel, new Rectangle(cardX, cardY, cardWidth, cardHeight), new Color(10, 18, 20, 174));
        _batch.Draw(_pixel, new Rectangle(cardX, cardY, 3, cardHeight), new Color(157, 174, 112, 225));
        DrawText(_visualEvaluation ? "VISUAL GATE" : "TEMPERATE VALLEY",
            cardX + 20, cardY + 13, new Color(231, 226, 201), 2);
        DrawText($"DAY {_snapshot.DayOfYear:D3} {SeasonName(_snapshot.Season)}  {mode}",
            cardX + 21, cardY + 42, new Color(149, 203, 190), 1);
        var controls = _visualEvaluation
            ? $"C CAMERA {ProjectionName}  SNAPSHOT {(_paused ? "PAUSED" : "RUNNING")}  W {(_showWaterFixtures ? "HIDE" : "SHOW")} FIXTURES  SPACE SIM"
            : $"{(_paused ? "PAUSED" : $"RUN {Speeds[_speedIndex]} TICKS/S")}  1 LAND  2 WATER  3 BIOMASS  SPACE PAUSE  H HIDE";
        DrawText(controls,
            cardX + 21, cardY + 61, new Color(207, 216, 207), 1);
        DrawText($"WHEEL / PGUP-DN ZOOM {Math.Round(_cameraZoom * 100):F0} PCT  L/M/R DRAG PAN  ARROWS PAN  HOME RESET",
            cardX + 21, cardY + 79, new Color(207, 216, 207), 1);

        var mouse = Mouse.GetState();
        if (_capturePath is null && !IsCameraPanButtonDown(mouse)
            && TryGetCellAtScreen(mouse.X, mouse.Y, out var cell))
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

    private static float TreeDisplayHeightMeters(int archetypeRow)
        => archetypeRow switch
        {
            0 => 29f, // broad-spreading form
            1 => 35f, // tall conical form
            2 => 24f, // upright form
            3 => 27f, // rounded broadleaf form
            4 => 22f, // pendulous broadleaf form
            _ => 27f
        };

    private Vector2 FindTreeQualityGalleryCenter()
    {
        var center = new Vector2(TerrainExtentM * 0.5f + 80f, TerrainDepthM * 0.5f);
        var greatestWoodyCover = 0d;
        foreach (var cell in _snapshot.Cells)
        {
            if (!cell.Woody.Present || cell.SurfaceWaterMm >= MinimumVisibleChannelWaterMm
                || cell.Woody.BiomassGPerM2 <= greatestWoodyCover)
                continue;
            greatestWoodyCover = cell.Woody.BiomassGPerM2;
            center = new Vector2((float)((cell.X + 0.5) * Tile), (float)((cell.Y + 0.5) * Tile));
        }
        return center;
    }

    private static int TreeRenderProfileRow(ulong worldSeed, int cellX, int cellY, ulong cellSeed)
    {
        // These are render-only architectural archetypes, not species or habitat assignments.
        // Coarse deterministic fields create broad visual stands without claiming that wetness
        // selects willow or elevation selects a conifer for this uncalibrated world.
        var localVariation = cellSeed % 100;
        var neighborhoodGrove = CounterRandom.SpatialUnit(worldSeed, 0x4153544552524154UL,
            cellX, cellY, 6, 0x47524F564553UL, 1);
        var landscapeGrove = CounterRandom.SpatialUnit(worldSeed, 0x4153544552524154UL,
            cellX, cellY, 18, 0x47524F564553UL, 2);
        var roll = Math.Clamp((int)Math.Round(localVariation * 0.30
            + neighborhoodGrove * 100 * 0.46
            + landscapeGrove * 100 * 0.24), 0, 99);
        var row = roll switch
        {
            < 32 => 1, // conical, needle-bearing form
            < 59 => 0, // broad-spreading form
            < 80 => 2, // upright form
            < 91 => 4, // pendulous broadleaf form
            _ => 3
        };

        return row;
    }

    private void RefreshTerrainTexture(bool force = false)
    {
        var cadence = _overlayMode == 1 ? SimulationState.TicksPerHour : SimulationState.TicksPerDay;
        var revision = _snapshot.TickIndex / cadence;
        if (!force && revision == _terrainRevision && _overlayMode == _terrainOverlay) return;
        var buildStarted = Stopwatch.GetTimestamp();
        var pixelsPerCell = _visualEvaluation ? EvaluationTerrainPixelsPerCell : NormalTerrainPixelsPerCell;
        _terrainTexture.SetData(PixelTerrainTexture.Generate(_snapshot, pixelsPerCell, _overlayMode, VisualSeason,
            naturalMaterial: _visualEvaluation, bakeLighting: !_referenceLandscapeView || _bakedTerrainHillshade));
        _terrainRevision = revision;
        _terrainOverlay = _overlayMode;
        RecordBenchmarkBuild(buildStarted, ref _benchmarkTextureBuildTotalMs,
            ref _benchmarkTextureBuildMaxMs, ref _benchmarkTextureBuildCount);
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
            var color = GrassColor();
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
            // Small rosettes break the repeated blade rhythm without implying unmodeled flowers.
            for (var cluster = 0; cluster < 2; cluster++)
            {
                var cx = 4 + cluster * 8 + key.Variant % 3;
                var cy = height - 2;
                for (var leaf = -1; leaf <= 1; leaf++)
                    Line(pixels, width, height, cx, cy, cx + leaf * 3, cy - Math.Abs(leaf) - 1, Shade(color, leaf == 0 ? 0.86f : 1.12f));
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
            var pine = new Color(47, 89, 55);
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
            var crown = LeafColor(key.Season);
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
                    var clusterColor = crown;
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

    private static Color GrassColor() => new(76, 128, 58);

    private static Color LeafColor(Season season)
        => season == Season.Winter ? new Color(0, 0, 0, 0) : new Color(76, 128, 58);

    private static Color Shade(Color color, float factor)
        => new((byte)(color.R * factor), (byte)(color.G * factor), (byte)(color.B * factor), color.A);

    private static int CountWoodyNeighbors(WorldSnapshot snapshot, int cellX, int cellY)
    {
        var count = 0;
        for (var offsetY = -1; offsetY <= 1; offsetY++)
        for (var offsetX = -1; offsetX <= 1; offsetX++)
        {
            if (offsetX == 0 && offsetY == 0) continue;
            var x = cellX + offsetX;
            var y = cellY + offsetY;
            if (x < 0 || y < 0 || x >= snapshot.Width || y >= snapshot.Height) continue;
            var woody = snapshot.Cells[y * snapshot.Width + x].Woody;
            if (woody.Present && woody.BiomassGPerM2 > 1) count++;
        }
        return count;
    }

    private bool RockVisible(WorldCell cell, ulong visualSeed)
    {
        // These meshes represent exposed outcrops, not submerged bed material. Preserve
        // shallow shoreline stones but keep deep snapshot water from drawing rocks on top.
        if (cell.SurfaceWaterMm >= 420) return false;

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
        var chance = Math.Clamp(0.003 + slopeExposure + altitudeExposure, 0.003, 0.22);
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
        foreach (var chunk in _terrainBackdropChunks)
        {
            chunk.Vertices.Dispose();
            chunk.Indices.Dispose();
        }
        _terrainBackdropChunks.Clear();
        _plateauVertexBuffer?.Dispose();
        _plateauIndexBuffer?.Dispose();
        _cliffVertexBuffer?.Dispose();
        var cellElevations = _snapshot.Cells.Select(cell => cell.ElevationM).ToArray();
        _centerElevationM = (cellElevations.Min() + cellElevations.Max()) * 0.5;
        _seaLevelM = DisplayElevation(cellElevations.Min()) - 5.0;
        var channelIncision = _visualEvaluation ? BuildVisibleChannelIncisionField() : null;
        var reliefDetail = _visualEvaluation
            ? ProceduralReliefDetailField.Build(_snapshot, channelIncision, Tile)
            : null;
        _terrainHeightSampler = CreateIslandElevationSampler(
            CreateDisplayElevationSampler(TerrainModel.CreateElevationSampler(_snapshot.Seed), channelIncision, reliefDetail));
        _underlyingHeightSampler = CreateIslandElevationSampler(
            CreateDisplayElevationSampler(TerrainModel.CreateUnderlyingElevationSampler(_snapshot.Seed), channelIncision, reliefDetail));
        _coastline = ProceduralIslandShape.BuildCoastline(_snapshot.Seed,
            SimulationState.Width, SimulationState.Height);
        BuildOceanGeometry();
        BuildBeachGeometry();
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

    private void BuildBeachGeometry()
    {
        _beachVertexBuffer?.Dispose();
        _coastFoamVertexBuffer?.Dispose();
        var beachVertices = ProceduralBeachMeshBuilder.BuildBeach(_coastline,
            _terrainHeightSampler, Tile, _seaLevelM);
        _beachVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration,
            beachVertices.Length, BufferUsage.WriteOnly);
        _beachVertexBuffer.SetData(beachVertices);
        _beachPrimitiveCount = beachVertices.Length / 3;
        _coastFoamVertices = ProceduralBeachMeshBuilder.BuildWaveFoam(_coastline,
            _terrainHeightSampler, Tile, _seaLevelM, 0);
        _coastFoamVertexBuffer = new DynamicVertexBuffer(GraphicsDevice,
            VertexPositionColor.VertexDeclaration, _coastFoamVertices.Length, BufferUsage.WriteOnly);
        _coastFoamVertexBuffer.SetData(_coastFoamVertices);
    }

    private void BuildOceanGeometry()
    {
        _oceanVertexBuffer?.Dispose();
        _oceanIndexBuffer?.Dispose();
        var mesh = ProceduralOceanMeshBuilder.Build(_snapshot.Seed,
            TerrainExtentM * 0.5, TerrainDepthM * 0.5, OceanHalfExtentM,
            _seaLevelM, Tile, OceanSubdivisions);
        var vertices = new VertexPositionColorTexture[mesh.Vertices.Length];
        for (var index = 0; index < mesh.Vertices.Length; index++)
        {
            var source = mesh.Vertices[index];
            vertices[index] = new VertexPositionColorTexture(source.Position, source.Color, Vector2.Zero);
        }
        _oceanVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColorTexture.VertexDeclaration,
            vertices.Length, BufferUsage.WriteOnly);
        _oceanVertexBuffer.SetData(vertices);
        _oceanIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits,
            mesh.Indices.Length, BufferUsage.WriteOnly);
        _oceanIndexBuffer.SetData(mesh.Indices);
        _oceanPrimitiveCount = mesh.PrimitiveCount;
    }

    private GroundCoverBuildResult BuildVisibleGroundCover(VisualBiomeWeights[] visualBiomes)
    {
        var viewport = GraphicsDevice.Viewport;
        var planeHeight = _cameraTarget.Y;
        var screenCorners = new[]
        {
            new Vector2(0, 0),
            new Vector2(viewport.Width, 0),
            new Vector2(0, viewport.Height),
            new Vector2(viewport.Width, viewport.Height)
        };
        var minimumEast = double.PositiveInfinity;
        var minimumNorth = double.PositiveInfinity;
        var maximumEast = double.NegativeInfinity;
        var maximumNorth = double.NegativeInfinity;
        var intersections = 0;
        foreach (var corner in screenCorners)
        {
            var near = viewport.Unproject(new Vector3(corner, 0), _projection, _view, Matrix.Identity);
            var far = viewport.Unproject(new Vector3(corner, 1), _projection, _view, Matrix.Identity);
            var ray = far - near;
            if (MathF.Abs(ray.Y) < 0.0001f) continue;
            var distance = (planeHeight - near.Y) / ray.Y;
            if (distance < 0) continue;
            var point = near + ray * distance;
            minimumEast = Math.Min(minimumEast, point.X);
            minimumNorth = Math.Min(minimumNorth, point.Z);
            maximumEast = Math.Max(maximumEast, point.X);
            maximumNorth = Math.Max(maximumNorth, point.Z);
            intersections++;
        }

        if (intersections < 3)
            return ProceduralGroundCoverMeshBuilder.Build(_snapshot, _terrainHeightSampler, Tile,
                visualBiomes, 0, 0, SimulationState.Width - 1, SimulationState.Height - 1);

        // Hills and tree crowns extend beyond the target-height plane. Keep a broad, deterministic
        // apron so their near-ground vegetation does not pop at the screen edge.
        const double viewMarginM = 140;
        var minimumCellX = (int)Math.Floor((minimumEast - viewMarginM) / Tile);
        var minimumCellY = (int)Math.Floor((minimumNorth - viewMarginM) / Tile);
        var maximumCellX = (int)Math.Ceiling((maximumEast + viewMarginM) / Tile);
        var maximumCellY = (int)Math.Ceiling((maximumNorth + viewMarginM) / Tile);
        return ProceduralGroundCoverMeshBuilder.Build(_snapshot, _terrainHeightSampler, Tile,
            visualBiomes, minimumCellX, minimumCellY, maximumCellX, maximumCellY);
    }

    private void DrawOceanSurface(GameTime gameTime)
    {
        if (_oceanVertexBuffer is null || _oceanIndexBuffer is null || _oceanPrimitiveCount == 0) return;
        SetWaterSurfaceEffect(gameTime, 1f);
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.SetVertexBuffer(_oceanVertexBuffer);
        GraphicsDevice.Indices = _oceanIndexBuffer;
        foreach (var pass in _waterSurfaceEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _oceanPrimitiveCount);
        }
        GraphicsDevice.Indices = null;
    }

    private double DisplayElevation(double elevationM)
    {
        return _centerElevationM + (elevationM - _centerElevationM) * _displayReliefScale;
    }

    private Func<double, double, double> CreateIslandElevationSampler(Func<double, double, double> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (east, north) =>
        {
            var signedDistance = ProceduralIslandShape.SignedDistanceCells(_snapshot.Seed,
                east, north, SimulationState.Width, SimulationState.Height);
            if (signedDistance <= 0)
                return _seaLevelM - Math.Min(22, 0.65 + -signedDistance * 4.5);

            var originalElevation = source(east, north);
            var coastalShelf = _seaLevelM + 0.45 + Math.Min(signedDistance, 3.0) * 0.9;
            var inlandBlend = SmoothStep(2.5, 14, signedDistance);
            return coastalShelf + (originalElevation - coastalShelf) * inlandBlend;
        };
    }

    private bool IsIslandLand(double eastCell, double northCell, double inlandClearanceCells = 1.0)
        => ProceduralIslandShape.SignedDistanceCells(_snapshot.Seed, eastCell, northCell,
            SimulationState.Width, SimulationState.Height) > inlandClearanceCells;

    private Func<double, double, double> CreateDisplayElevationSampler(
        Func<double, double, double> source, double[]? channelIncision, double[]? reliefDetail)
    {
        ArgumentNullException.ThrowIfNull(source);
        var flatlandLevel = _centerElevationM - 12;
        return (east, north) =>
        {
            var elevation = source(east, north);
            if (_visualEvaluation)
                elevation = VisualBiomeField.FlattenForPresentation(_snapshot.Seed, east, north,
                    SimulationState.Width, SimulationState.Height, elevation, flatlandLevel);
            var displayElevation = DisplayElevation(elevation);
            if (reliefDetail is not null)
                displayElevation += SampleCellField(reliefDetail, east - 0.5, north - 0.5);
            if (channelIncision is not null)
                displayElevation -= SampleCellField(channelIncision, east - 0.5, north - 0.5);
            return displayElevation;
        };
    }

    private double[] BuildVisibleChannelIncisionField()
    {
        var waterMm = _snapshot.Cells.Select(cell => cell.SurfaceWaterMm).ToArray();
        var activeEdges = BuildVisibleRiverEdges(waterMm, out _);
        var receiver = _surfaceDrainage.Receiver;
        var accumulation = _surfaceDrainage.Accumulation;
        var incision = new double[receiver.Length];

        for (var from = 0; from < activeEdges.Length; from++)
        {
            if (!activeEdges[from]) continue;
            var to = receiver[from];
            var start = RiverCenterlineCell(from);
            var end = RiverCenterlineCell(to);
            var startX = (double)start.X;
            var startY = (double)start.Y;
            var endX = (double)end.X;
            var endY = (double)end.Y;
            var deltaX = endX - startX;
            var deltaY = endY - startY;
            var segmentLengthSquared = deltaX * deltaX + deltaY * deltaY;
            var areaScale = Math.Max(0, Math.Log2(accumulation[from] / (double)VisibleRiverWidthReferenceCells));
            var radiusCells = Math.Clamp(1.8 + areaScale * 0.70, 1.8, 6.0);
            var depthM = Math.Clamp(3.0 + areaScale * 1.40, 3.0, 12.0);
            var searchRadius = (int)Math.Ceiling(radiusCells * 3);
            var minX = Math.Max(0, (int)Math.Floor(Math.Min(startX, endX) - searchRadius));
            var maxX = Math.Min(SimulationState.Width - 1,
                (int)Math.Ceiling(Math.Max(startX, endX) + searchRadius));
            var minY = Math.Max(0, (int)Math.Floor(Math.Min(startY, endY) - searchRadius));
            var maxY = Math.Min(SimulationState.Height - 1,
                (int)Math.Ceiling(Math.Max(startY, endY) + searchRadius));
            var inverseTwoRadiusSquared = 0.5 / (radiusCells * radiusCells);

            for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
            {
                var pointX = x + 0.5 - startX;
                var pointY = y + 0.5 - startY;
                var t = Math.Clamp((pointX * deltaX + pointY * deltaY) / segmentLengthSquared, 0, 1);
                var distanceX = pointX - deltaX * t;
                var distanceY = pointY - deltaY * t;
                var distanceSquared = distanceX * distanceX + distanceY * distanceY;
                var depth = depthM * Math.Exp(-distanceSquared * inverseTwoRadiusSquared);
                var index = y * SimulationState.Width + x;
                if (depth > incision[index]) incision[index] = depth;
            }
        }

        return incision;
    }

    private Vector2 RiverCenterlineCell(int index)
    {
        var x = index % SimulationState.Width;
        var y = index / SimulationState.Width;
        var flow = Vector2.Zero;
        var receiver = _surfaceDrainage.Receiver[index];
        if (receiver >= 0)
        {
            flow = new Vector2(receiver % SimulationState.Width - x,
                receiver / SimulationState.Width - y);
        }
        else
        {
            var strongestIncoming = double.NegativeInfinity;
            for (var offsetY = -1; offsetY <= 1; offsetY++)
            for (var offsetX = -1; offsetX <= 1; offsetX++)
            {
                if (offsetX == 0 && offsetY == 0) continue;
                var neighborX = x + offsetX;
                var neighborY = y + offsetY;
                if (neighborX < 0 || neighborY < 0
                    || neighborX >= SimulationState.Width || neighborY >= SimulationState.Height) continue;
                var neighbor = neighborY * SimulationState.Width + neighborX;
                var incomingAccumulation = _surfaceDrainage.Accumulation[neighbor];
                if (_surfaceDrainage.Receiver[neighbor] != index
                    || incomingAccumulation <= strongestIncoming) continue;
                strongestIncoming = incomingAccumulation;
                flow = new Vector2(-offsetX, -offsetY);
            }
        }
        if (flow.LengthSquared() < 0.001f) flow = Vector2.UnitX;
        else flow.Normalize();
        var normal = new Vector2(-flow.Y, flow.X);
        var areaScale = Math.Max(0, Math.Log2(
            _surfaceDrainage.Accumulation[index] / (double)VisibleRiverWidthReferenceCells));
        var broad = CounterRandom.SpatialUnit(_snapshot.Seed, 0x52495645524D4541UL,
            x, y, 24, 0, 1);
        var medium = CounterRandom.SpatialUnit(_snapshot.Seed, 0x52495645524D4541UL,
            x, y, 8, 0, 2);
        var meander = ((broad - 0.5) * 0.74 + (medium - 0.5) * 0.26) * 2;
        var amplitudeCells = Math.Clamp(0.70 + areaScale * 0.72, 0.70, 5.5);
        return new Vector2(x + 0.5f, y + 0.5f) + normal * (float)(meander * amplitudeCells);
    }

    private void BuildPlateauGeometry()
    {
        const int radialRings = 20;
        const int cliffBands = 12;
        var features = TerrainModel.GenerateOutcropGeometries(_snapshot.Seed)
            .Where(feature => feature.Rim.All(point => IsIslandLand(point.EastCell, point.NorthCell, 0.5)))
            .ToArray();
        if (features.Length == 0) return;
        var topVertices = new List<VertexPositionNormalTexture>(features.Length * (radialRings + 1) * (features[0].Rim.Count + 1));
        var topIndices = new List<int>(features.Length * radialRings * features[0].Rim.Count * 6);
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
                var elevation = ring == radialRings
                    ? DisplayElevation(rim.CrownElevationM)
                    : _terrainHeightSampler(gridX, gridY);
                var position = new Vector3((float)(gridX * Tile), (float)elevation, (float)(gridY * Tile));
                const double normalStep = 0.08;
                var eastSlope = (_terrainHeightSampler(gridX + normalStep, gridY)
                    - _terrainHeightSampler(gridX - normalStep, gridY)) / (Tile * normalStep * 2);
                var northSlope = (_terrainHeightSampler(gridX, gridY + normalStep)
                    - _terrainHeightSampler(gridX, gridY - normalStep)) / (Tile * normalStep * 2);
                var normal = Vector3.Normalize(new Vector3((float)-eastSlope, 1, (float)-northSlope));
                var uv = new Vector2((float)(gridX / SimulationState.Width), (float)(gridY / SimulationState.Height));
                featureTopVertices[ring * stride + segment] = new VertexPositionNormalTexture(position, normal, uv);
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
                    var angle = Math.Atan2(first.NorthCell - feature.CenterNorthCell,
                        first.EastCell - feature.CenterEastCell);
                    var verticalPhase = band * 0.72;
                    var localRock = 0.52 * Math.Sin(angle * 4.3 + rockPhaseA + verticalPhase)
                        + 0.31 * Math.Sin(angle * 9.7 - rockPhaseB - verticalPhase * 0.63)
                        + 0.17 * Math.Sin(angle * 17.2 + rockPhaseA * 0.37 + verticalPhase * 1.4);
                    var radialNormal = Vector3.Normalize(new Vector3((float)Math.Cos(angle), 0, (float)Math.Sin(angle)));
                    var rockLight = 0.78f + Math.Max(0, Vector3.Dot(radialNormal,
                        Vector3.Normalize(new Vector3(-0.42f, 0.62f, 0.44f)))) * 0.34f;
                    var fractured = (float)(localRock * 0.24
                        + (CounterRandom.Unit(_snapshot.Seed, 0x434C494646465241UL,
                            (ulong)(feature.FeatureIndex * 100_000 + segment), (ulong)band, 0, 4) - 0.5) * 0.12);
                    var strata = (band % 3) switch { 0 => -0.045f, 1 => 0.035f, _ => 0f };
                    var faceColor = Shade(new Color(125, 118, 101), rockLight + fractured + strata);
                    var split = 0.93f + (float)CounterRandom.Unit(_snapshot.Seed, 0x434C49464653504CUL,
                        (ulong)(feature.FeatureIndex * 100_000 + segment), (ulong)band, 0, 5) * 0.14f;
                    AddWallTriangle(firstTop, firstBottom, secondTop, faceColor);
                    AddWallTriangle(secondTop, firstBottom, secondBottom, Shade(faceColor, split));
                }
            }

            Vector3 WallPoint(TerrainRimPoint point, float depthFraction)
            {
                var crownElevation = (float)DisplayElevation(point.CrownElevationM);
                var baseElevation = (float)DisplayElevation(point.ToeElevationM - 1.0);
                var angle = Math.Atan2(point.NorthCell - feature.CenterNorthCell,
                    point.EastCell - feature.CenterEastCell);
                var taper = Math.Sin(depthFraction * Math.PI);
                var fracture = Math.Sin(angle * 4.3 + rockPhaseA)
                    + 0.38 * Math.Sin(angle * 11.7 - rockPhaseB)
                    + 0.16 * Math.Sin(angle * 24.5 + rockPhaseA * 0.3);
                var normalizedAngle = (angle + Math.Tau) % Math.Tau;
                var rimSegment = (int)Math.Round(normalizedAngle / Math.Tau * segments) % segments;
                var wallBand = (ulong)Math.Clamp((int)Math.Round(depthFraction * cliffBands), 0, cliffBands);
                var brokenFace = (CounterRandom.Unit(_snapshot.Seed, 0x434C49464642524FUL,
                    (ulong)feature.FeatureIndex, (ulong)rimSegment, wallBand, 0) - 0.5) * 0.9;
                var radialOffset = (fracture * 1.15 + brokenFace * 0.52) * taper * 1.24;
                var ledge = (Math.Sin(angle * 7.1 + rockPhaseB + depthFraction * 3.4) * 0.34
                    + brokenFace * 0.20) * taper * 1.22;
                var y = crownElevation + (baseElevation - crownElevation) * depthFraction + (float)ledge;
                return new Vector3((float)((point.EastCell + Math.Cos(angle) * radialOffset) * Tile), y,
                    (float)((point.NorthCell + Math.Sin(angle) * radialOffset) * Tile));
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
        _terrainEffect.LightingEnabled = _referenceLandscapeView;
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
        var buildStarted = Stopwatch.GetTimestamp();
        _waterRevision = revision;

        var cellCount = SimulationState.Width * SimulationState.Height;
        var waterMm = new double[cellCount];
        var surfaceStageM = new double[cellCount];
        var wet = new double[cellCount];
        for (var index = 0; index < cellCount; index++)
        {
            waterMm[index] = _snapshot.Cells[index].SurfaceWaterMm;
            var cell = _snapshot.Cells[index];
            surfaceStageM[index] = _terrainHeightSampler(cell.X + 0.5, cell.Y + 0.5)
                + waterMm[index] / 1000;
            // Render closed, topographic basins as level pools. Water outside those basins
            // is drawn along wet D8 channel edges below, so deep channel cells do not expand
            // into broad raster patches that erase the river's connected course.
            wet[index] = _waterDepressionId[index] >= 0
                ? SmoothStep(96, 420, waterMm[index])
                : 0;
        }
        wet = SmoothRenderWaterMask(wet, SimulationState.Width, SimulationState.Height);
        for (var index = 0; index < wet.Length; index++)
        {
            var cell = _snapshot.Cells[index];
            var coastFade = SmoothStep(-0.4, 1.7,
                ProceduralIslandShape.SignedDistanceCells(_snapshot.Seed, cell.X + 0.5, cell.Y + 0.5,
                    SimulationState.Width, SimulationState.Height));
            wet[index] *= coastFade;
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
            var coastFade = SmoothStep(-0.2, 1.4,
                ProceduralIslandShape.SignedDistanceCells(_snapshot.Seed, gridX, gridY,
                    SimulationState.Width, SimulationState.Height));
            coverage[vertex] = SampleCellField(wet, gridX - 0.5, gridY - 0.5) * coastFade;
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
            var bankOffset = (bankNoise - 0.5) * 0.24;
            var shore = SmoothStep(0.08 + bankOffset, 0.42 + bankOffset, coverage[index]);
            var depthTone = (float)Math.Clamp(depth / 2_800, 0, 1);
            var waterColor = Color.Lerp(new Color(77, 132, 130), new Color(38, 86, 97), depthTone);
            var waterGrain = SampleGridField(waterGrainGrid, noiseColumns, noiseRows, gridX, gridY);
            var pattern = (float)(0.91 + waterGrain * 0.18);
            waterColor = Shade(waterColor, pattern);
            var alpha = (byte)Math.Clamp((int)Math.Round(shore * 246), 0, 246);
            waterColor = new Color(waterColor.R, waterColor.G, waterColor.B, alpha);
            // Interpolate the actual snapshot water-surface elevation (bed + depth) between
            // cells. Sampling a different bed height under every ribbon edge made connected
            // channels kink vertically and created visible lips at tributary junctions.
            var surfaceHeight = SampleCellField(surfaceStageM, gridX - 0.5, gridY - 0.5) + 0.025;
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
        if (_visualEvaluation && _showWaterFixtures)
        {
            var evaluationWatercourse = AddEvaluationWatercourseFixture(vertices);
            AddEvaluationWaterfallFixture(vertices, evaluationWatercourse);
        }

        _waterVertexBuffer?.Dispose();
        _waterVertexBuffer = null;
        _waterPrimitiveCount = vertices.Count / 3;
        _waterVertexCount = vertices.Count;
        if (vertices.Count == 0)
        {
            RecordBenchmarkBuild(buildStarted, ref _benchmarkWaterBuildTotalMs,
                ref _benchmarkWaterBuildMaxMs, ref _benchmarkWaterBuildCount);
            return;
        }
        var receiver = _surfaceDrainage.Receiver;
        var accumulation = _surfaceDrainage.Accumulation;
        var renderVertices = new VertexPositionColorTexture[vertices.Count];
        for (var vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
        {
            var source = vertices[vertexIndex];
            var gridX = Math.Clamp((int)Math.Round(source.Position.X / Tile), 0, SimulationState.Width - 1);
            var gridY = Math.Clamp((int)Math.Round(source.Position.Z / Tile), 0, SimulationState.Height - 1);
            var cellIndex = gridY * SimulationState.Width + gridX;
            var cell = _snapshot.Cells[cellIndex];
            var flowDirection = Vector2.Zero;
            if (cell.SurfaceWaterMm >= MinimumVisibleChannelWaterMm
                && cell.SurfaceWaterMm < 250
                && accumulation[cellIndex] >= VisibleRiverSelectionThresholdCells
                && receiver[cellIndex] >= 0)
            {
                var downstream = receiver[cellIndex];
                var delta = new Vector2((downstream % SimulationState.Width) - gridX,
                    (downstream / SimulationState.Width) - gridY);
                if (delta.LengthSquared() > 0) flowDirection = Vector2.Normalize(delta);
            }
            renderVertices[vertexIndex] = new VertexPositionColorTexture(source.Position, source.Color, flowDirection);
        }
        _waterVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColorTexture.VertexDeclaration,
            renderVertices.Length, BufferUsage.WriteOnly);
        _waterVertexBuffer.SetData(renderVertices);
        RecordBenchmarkBuild(buildStarted, ref _benchmarkWaterBuildTotalMs,
            ref _benchmarkWaterBuildMaxMs, ref _benchmarkWaterBuildCount);
    }

    private void RecordBenchmarkBuild(long started, ref double totalMs, ref double maxMs, ref int count)
    {
        if (_benchmarkDurationSeconds is null || _benchmarkWarmupSeconds < 5) return;
        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        totalMs += elapsedMs;
        maxMs = Math.Max(maxMs, elapsedMs);
        count++;
    }

    private void AddRiverRibbons(List<VertexPositionColor> vertices, double[] waterMm)
    {
        var drainage = _surfaceDrainage;
        var receiver = drainage.Receiver;
        var accumulation = drainage.Accumulation;
        var seedTint = (int)(CounterRandom.VisualBits(_snapshot.Seed, 0, 0x524956455254494EUL) & 7);
        var baseWater = new Color(56 + seedTint, 114 + seedTint, 118 + seedTint, 246);
        var channelNodes = new Dictionary<int, (float Radius, double Stage)>();
        var activeEdges = BuildVisibleRiverEdges(waterMm, out var mainStem);
        var incomingChannels = new int[receiver.Length];
        var renderedWaterfalls = 0;
        var maximumChannelSegmentDropM = double.NegativeInfinity;

        double CellDepth(int index) => waterMm[index];
        Vector2 Center(WorldCell cell)
        {
            var center = RiverCenterlineCell(cell.Index);
            return new Vector2(center.X * Tile, center.Y * Tile);
        }
        double CellStageM(int index)
        {
            var cell = _snapshot.Cells[index];
            return _terrainHeightSampler(cell.X + 0.5, cell.Y + 0.5) + CellDepth(index) / 1000;
        }

        Vector3 SurfacePoint(Vector2 point, double stageM)
            => new(point.X, (float)(stageM + 0.025), point.Y);

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

        if (_capturePath is not null)
        {
            var potentialEdges = Enumerable.Range(0, receiver.Length)
                .Count(index => receiver[index] >= 0 && accumulation[index] >= VisibleRiverSelectionThresholdCells);
            var wetCells = waterMm.Count(depth => depth >= 20);
            var visiblePoolCells = waterMm.Count(depth => depth >= 96);
            var cellsAbove180Mm = waterMm.Count(depth => depth >= 180);
            var deepPoolCells = waterMm.Count(depth => depth >= 420);
            var lakeDepthCells = waterMm.Count(depth => depth >= 650);
            var wetPotentialEdges = Enumerable.Range(0, receiver.Length)
                .Count(index => receiver[index] >= 0 && accumulation[index] >= VisibleRiverSelectionThresholdCells
                    && waterMm[index] >= MinimumVisibleChannelWaterMm
                    && waterMm[receiver[index]] >= MinimumVisibleChannelWaterMm);
            var subLakePotentialEdges = Enumerable.Range(0, receiver.Length)
                .Count(index => receiver[index] >= 0 && accumulation[index] >= VisibleRiverSelectionThresholdCells
                    && !(waterMm[index] >= 250 && waterMm[receiver[index]] >= 250)
                    && waterMm[index] >= MinimumVisibleChannelWaterMm
                    && waterMm[receiver[index]] >= MinimumVisibleChannelWaterMm);
            Console.WriteLine(FormattableString.Invariant(
                $"snapshotWaterCells={wetCells} cellsAbove96Mm={visiblePoolCells} cellsAbove180Mm={cellsAbove180Mm} cellsAbove420Mm={deepPoolCells} lakeDepthCells={lakeDepthCells} maxSurfaceWaterMm={waterMm.Max():F1} maxAccumulation={accumulation.Max():F1} potentialChannelEdges={potentialEdges} wetPotentialEdges={wetPotentialEdges} subLakePotentialEdges={subLakePotentialEdges}"));
        }

        if (_capturePath is not null)
            Console.WriteLine($"dominantPathEdges={mainStem.Count(active => active)} connectedRiverEdges={activeEdges.Count(active => active)}");

        // Do not draw an isolated one- or two-edge drainage fragment as a complete stream.
        // Its snapshot water remains represented by the surface-water mesh above.
        if (mainStem.Count(active => active) < 3)
            Array.Clear(activeEdges);
        for (var from = 0; from < activeEdges.Length; from++)
            if (activeEdges[from] && receiver[from] >= 0)
                incomingChannels[receiver[from]]++;

        if (_capturePath is not null)
        {
            var junctions = Enumerable.Range(0, receiver.Length)
                .Where(index => activeEdges[index] && incomingChannels[index] > 1)
                .ToArray();
            var crossingDiagonals = 0;
            for (var y = 0; y < SimulationState.Height - 1; y++)
            for (var x = 0; x < SimulationState.Width - 1; x++)
            {
                var northwest = y * SimulationState.Width + x;
                var northeast = northwest + 1;
                var southwest = northwest + SimulationState.Width;
                var southeast = southwest + 1;
                if (activeEdges[northwest] && receiver[northwest] == southeast
                    && activeEdges[northeast] && receiver[northeast] == southwest)
                    crossingDiagonals++;
            }
            Console.WriteLine(FormattableString.Invariant(
                $"riverEdges={activeEdges.Count(active => active)} junctions={junctions.Length} threeWay={junctions.Count(index => incomingChannels[index] >= 3)} crossingDiagonals={crossingDiagonals}"));
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
            var startDepth = CellDepth(from);
            var endDepth = CellDepth(to);
            var startStage = CellStageM(from);
            var endStage = CellStageM(to);
            RememberNode(from, incomingWidth, startStage);
            RememberNode(to, outgoingWidth, endStage);
            var color = new Color(baseWater.R, baseWater.G, baseWater.B, (byte)Math.Clamp(
                220 + (int)Math.Min(26, Math.Log2(Math.Max(1, accumulation[from] / SurfaceWaterModel.ChannelThresholdCells)) * 4), 0, 246));

            // A fall requires an already connected upstream channel, meaningful surface water
            // at both ends, and a real drop in the water surface. Terrain cliffs alone are dry.
            if (incomingChannels[from] > 0 && startDepth >= 180 && endDepth >= MinimumVisibleChannelWaterMm)
                AddWaterfallIfTerrainDrops(start, end, perpendicular, incomingWidth, outgoingWidth,
                    startStage, endStage, color);
        }
        if (_capturePath is not null)
            Console.WriteLine(FormattableString.Invariant(
                $"renderedWaterfalls={renderedWaterfalls} maxChannelSegmentDropM={Math.Max(0, maximumChannelSegmentDropM):F2}"));

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
            if (incomingChannels[index] > 1)
                AddChannelJunction(_snapshot.Cells[index], node.Radius * 0.72f, node.Stage);

        float HalfWidthM(double cellsOfFlow)
            => Tile * RiverRenderWidthScale
                * (float)Math.Clamp(0.24 + 0.14 * Math.Log2(cellsOfFlow / SurfaceWaterModel.ChannelThresholdCells), 0.24, 1.30);

        void RememberNode(int index, float radius, double stageM)
        {
            if (!channelNodes.TryGetValue(index, out var current))
                channelNodes[index] = (radius, stageM);
            else
                channelNodes[index] = (Math.Max(current.Radius, radius), current.Stage);
        }

        void AddChannelJunction(WorldCell cell, float radius, double stageM)
        {
            const int sides = 20;
            var center = Center(cell);
            var centerPoint = SurfacePoint(center, stageM);
            for (var side = 0; side < sides; side++)
            {
                var firstAngle = MathF.Tau * side / sides;
                var secondAngle = MathF.Tau * (side + 1) / sides;
                var firstDirection = new Vector2(MathF.Cos(firstAngle), MathF.Sin(firstAngle));
                var secondDirection = new Vector2(MathF.Cos(secondAngle), MathF.Sin(secondAngle));
                var first = center + firstDirection * (radius + RiverBankOffsetM(center + firstDirection * radius, radius, 3) * 0.55f);
                var second = center + secondDirection * (radius + RiverBankOffsetM(center + secondDirection * radius, radius, 3) * 0.55f);
                var firstPoint = SurfacePoint(first, stageM);
                var secondPoint = SurfacePoint(second, stageM);
                AddTriangle(centerPoint, firstPoint, secondPoint, baseWater, baseWater, baseWater);
            }
        }

        void AddSmoothedRibbon(IReadOnlyList<int> chain)
        {
            var controlPoints = chain.Select(index =>
            {
                var cell = _snapshot.Cells[index];
                return (Point: Center(cell), Radius: HalfWidthM(accumulation[index]), Stage: CellStageM(index));
            }).ToList();
            var path = new List<(Vector2 Point, float Radius, double Stage)>
            {
                controlPoints[0]
            };
            for (var segment = 0; segment < controlPoints.Count - 1; segment++)
            {
                var p0 = controlPoints[Math.Max(0, segment - 1)];
                var p1 = controlPoints[segment];
                var p2 = controlPoints[segment + 1];
                var p3 = controlPoints[Math.Min(controlPoints.Count - 1, segment + 2)];
                for (var sample = 1; sample <= 4; sample++)
                {
                    var t = sample * 0.25f;
                    path.Add((CatmullRom(p0.Point, p1.Point, p2.Point, p3.Point, t),
                        MathHelper.Lerp(p1.Radius, p2.Radius, t),
                        p1.Stage + (p2.Stage - p1.Stage) * t));
                }
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
                left[index] = SurfacePoint(path[index].Point + normal * leftWidth, path[index].Stage);
                right[index] = SurfacePoint(path[index].Point - normal * rightWidth, path[index].Stage);
            }

            for (var index = 0; index < path.Count - 1; index++)
            {
                AddTriangle(left[index], right[index], left[index + 1], baseWater, baseWater, baseWater);
                AddTriangle(right[index], right[index + 1], left[index + 1], baseWater, baseWater, baseWater);
            }

            static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
            {
                var t2 = t * t;
                var t3 = t2 * t;
                return 0.5f * ((2 * p1) + (-p0 + p2) * t
                    + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2
                    + (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
            }
        }

        void AddWaterfallIfTerrainDrops(
            Vector2 start, Vector2 end, Vector2 perpendicular, float incomingWidth, float outgoingWidth,
            double startStageM, double endStageM, Color channelColor)
        {
            const int samples = 16;
            const double visibleDropM = 5;
            var segmentDrop = _terrainHeightSampler(start.X / Tile, start.Y / Tile)
                - _terrainHeightSampler(end.X / Tile, end.Y / Tile);
            maximumChannelSegmentDropM = Math.Max(maximumChannelSegmentDropM, segmentDrop);
            if (segmentDrop < 8 || startStageM - endStageM < visibleDropM) return;
            var largestDrop = 0.0;
            var transition = -1;
            for (var sample = 0; sample < samples; sample++)
            {
                var firstT = sample / (double)samples;
                var secondT = (sample + 1) / (double)samples;
                var firstPoint = Vector2.Lerp(start, end, (float)firstT);
                var secondPoint = Vector2.Lerp(start, end, (float)secondT);
                var firstStage = MathHelper.Lerp((float)startStageM, (float)endStageM, (float)firstT);
                var secondStage = MathHelper.Lerp((float)startStageM, (float)endStageM, (float)secondT);
                var drop = firstStage - secondStage;
                if (drop <= largestDrop) continue;
                largestDrop = drop;
                transition = sample;
            }

            if (transition < 0 || largestDrop <= 0) return;
            var topPoint = Vector2.Lerp(start, end, (transition + 0.15f) / samples);
            var bottomPoint = Vector2.Lerp(start, end, (transition + 0.85f) / samples);
            var topElevation = MathHelper.Lerp((float)startStageM, (float)endStageM, (transition + 0.15f) / samples);
            var bottomElevation = MathHelper.Lerp((float)startStageM, (float)endStageM, (transition + 0.85f) / samples);
            if (topElevation <= bottomElevation + visibleDropM) return;

            renderedWaterfalls++;
            var halfWidth = Math.Max(incomingWidth, outgoingWidth) * 0.64f;
            var topY = topElevation;
            var bottomY = bottomElevation;
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

    private bool[] BuildVisibleRiverEdges(double[] waterMm, out bool[] mainStem)
    {
        var receiver = _surfaceDrainage.Receiver;
        var accumulation = _surfaceDrainage.Accumulation;
        var activeEdges = new bool[receiver.Length];
        for (var from = 0; from < receiver.Length; from++)
        {
            var to = receiver[from];
            if (to < 0 || accumulation[from] < VisibleRiverSelectionThresholdCells) continue;
            var fromX = from % SimulationState.Width;
            var fromY = from / SimulationState.Width;
            var toX = to % SimulationState.Width;
            var toY = to / SimulationState.Width;
            if (!IsIslandLand(fromX + 0.5, fromY + 0.5, -0.8)
                || !IsIslandLand(toX + 0.5, toY + 0.5, -0.8)) continue;
            // A rendered channel must carry snapshot water through both adjacent cells.
            // A single wet endpoint used to stretch a blue ribbon across a dry slope.
            if (waterMm[from] < MinimumVisibleChannelWaterMm
                || waterMm[to] < MinimumVisibleChannelWaterMm) continue;
            activeEdges[from] = true;
        }

        mainStem = new bool[receiver.Length];
        var mainSource = -1;
        var longestPath = 0;
        for (var candidate = 0; candidate < receiver.Length; candidate++)
        {
            if (!activeEdges[candidate]) continue;
            var pathLength = 0;
            var cursor = candidate;
            var remaining = receiver.Length;
            while (cursor >= 0 && remaining-- > 0 && activeEdges[cursor])
            {
                pathLength++;
                cursor = receiver[cursor];
            }
            if (pathLength < longestPath
                || pathLength == longestPath && mainSource >= 0 && accumulation[candidate] <= accumulation[mainSource])
                continue;
            longestPath = pathLength;
            mainSource = candidate;
        }

        var mainIndex = mainSource;
        var remainingMainCells = receiver.Length;
        while (mainIndex >= 0 && remainingMainCells-- > 0 && activeEdges[mainIndex])
        {
            mainStem[mainIndex] = true;
            mainIndex = receiver[mainIndex];
        }

        // Keep tributaries that drain into the dominant wet course and remove isolated fragments.
        var reachesMainStem = new sbyte[receiver.Length];
        for (var index = 0; index < mainStem.Length; index++)
            if (mainStem[index]) reachesMainStem[index] = 1;
        for (var candidate = 0; candidate < activeEdges.Length; candidate++)
        {
            if (!activeEdges[candidate] || reachesMainStem[candidate] != 0) continue;
            var path = new List<int>();
            var cursor = candidate;
            var remaining = receiver.Length;
            while (cursor >= 0 && remaining-- > 0 && activeEdges[cursor]
                && !mainStem[cursor] && reachesMainStem[cursor] == 0)
            {
                path.Add(cursor);
                cursor = receiver[cursor];
            }

            var connected = cursor >= 0 && activeEdges[cursor]
                && (mainStem[cursor] || reachesMainStem[cursor] > 0);
            foreach (var index in path)
                reachesMainStem[index] = connected ? (sbyte)1 : (sbyte)-1;
        }
        for (var index = 0; index < activeEdges.Length; index++)
            if (activeEdges[index] && reachesMainStem[index] <= 0)
                activeEdges[index] = false;

        if (mainStem.Count(active => active) < 3)
            Array.Clear(activeEdges);
        return activeEdges;
    }

    private IReadOnlyList<Vector2> AddEvaluationWatercourseFixture(List<VertexPositionColor> vertices)
    {
        // The canonical seed currently exposes almost no surface flow. This renderer-only
        // watercourse follows downhill samples of the existing terrain and does not alter hydrology.
        const float searchStep = 0.34f;
        List<Vector2>? bestPath = _evaluationWatercourseSeed == _snapshot.Seed
            ? _evaluationWatercoursePath
            : null;
        var routeCacheHit = bestPath is not null;
        var routeSearchStarted = Stopwatch.GetTimestamp();
        var bestScore = double.NegativeInfinity;
        var flowDirections = new[]
        {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            Vector2.Normalize(new Vector2(1, 1)), Vector2.Normalize(new Vector2(-1, 1)),
            Vector2.Normalize(new Vector2(1, -1)), Vector2.Normalize(new Vector2(-1, -1))
        };

        List<Vector2> TraceDownhill(Vector2 start, out double elevationDrop)
        {
            var path = new List<Vector2>(192) { start };
            var current = start;
            var currentHeight = _terrainHeightSampler(current.X, current.Y);
            var initialHeight = currentHeight;
            for (var step = 0; step < 220; step++)
            {
                var next = current;
                var nextHeight = currentHeight;
                foreach (var direction in flowDirections)
                {
                    var candidate = current + direction * searchStep;
                    if (candidate.X < 5 || candidate.Y < 5
                        || candidate.X > SimulationState.Width - 5 || candidate.Y > SimulationState.Height - 5
                        || !IsIslandLand(candidate.X, candidate.Y, -0.2))
                        continue;
                    var candidateHeight = _terrainHeightSampler(candidate.X, candidate.Y);
                    if (candidateHeight >= nextHeight - 0.002) continue;
                    next = candidate;
                    nextHeight = candidateHeight;
                }
                if (next == current) break;
                current = next;
                currentHeight = nextHeight;
                path.Add(current);
            }
            elevationDrop = initialHeight - currentHeight;
            return path;
        }

        double ScoreWatercourse(Vector2 start, IReadOnlyList<Vector2> path, double drop)
        {
            if (path.Count < 24 || drop < 1.2) return double.NegativeInfinity;
            var length = path.Zip(path.Skip(1), (first, second) => Vector2.Distance(first, second)).Sum();
            var directDistance = Vector2.Distance(path[0], path[^1]);
            var sinuosity = length / Math.Max(1, directDistance);
            if (length < 26 || sinuosity < 1.20 || sinuosity > 2.55) return double.NegativeInfinity;
            var centerDistance = Vector2.Distance(start,
                new Vector2(SimulationState.Width * 0.5f, SimulationState.Height * 0.375f));
            var end = path[^1];
            var boundaryClearance = Math.Min(Math.Min(start.X, start.Y),
                Math.Min(Math.Min(SimulationState.Width - start.X, SimulationState.Height - start.Y),
                    Math.Min(Math.Min(end.X, end.Y),
                        Math.Min(SimulationState.Width - end.X, SimulationState.Height - end.Y))));
            return length + Math.Min(drop, 70) * 0.12 - centerDistance * 0.18
                + Math.Min(boundaryClearance, 16) * 0.25;
        }

        if (!routeCacheHit)
        {
            var searchStepCells = Math.Max(4, SimulationState.Width / 16);
            for (var y = SimulationState.Height / 8; y <= SimulationState.Height * 3 / 4; y += searchStepCells)
            for (var x = SimulationState.Width / 8; x <= SimulationState.Width * 7 / 8; x += searchStepCells)
            {
                var start = new Vector2(x + 0.37f, y + 0.61f);
                if (!IsIslandLand(start.X, start.Y)) continue;
                var path = TraceDownhill(start, out var drop);
                var score = ScoreWatercourse(start, path, drop);
                if (score <= bestScore) continue;
                bestScore = score;
                bestPath = path;
            }

        // Include real procedural cliff rims as possible source points so the evaluation stream
        // can begin at a visible drop instead of selecting a disconnected waterfall afterward.
            var bestRimScore = double.NegativeInfinity;
            List<Vector2>? bestRimPath = null;
            var bestCascadeScore = double.NegativeInfinity;
            List<Vector2>? bestCascadePath = null;
            var bestCascadeFeature = -1;
            var bestCascadeDrop = 0.0;
            var outcropRouteSummaries = new List<(int Feature, double Score, double Length, double Sinuosity,
                double Drop, Vector2 Start, Vector2 End)>();
            foreach (var outcrop in TerrainModel.GenerateOutcropGeometries(_snapshot.Seed))
            {
                var featureBestScore = double.NegativeInfinity;
                (double Score, double Length, double Sinuosity, double Drop, Vector2 Start, Vector2 End) featureBest = default;
                for (var segment = 0; segment < outcrop.Rim.Count; segment += 8)
                {
                    var rim = outcrop.Rim[segment];
                    var start = new Vector2((float)rim.EastCell, (float)rim.NorthCell);
                    var coastClearance = ProceduralIslandShape.SignedDistanceCells(_snapshot.Seed,
                        start.X, start.Y, SimulationState.Width, SimulationState.Height);
                    if (coastClearance < 28) continue;
                    var path = TraceDownhill(start, out var drop);
                    var length = path.Zip(path.Skip(1), (first, second) => Vector2.Distance(first, second)).Sum();
                    if (drop >= 8 && path.Count >= 12 && length >= 5)
                    {
                        // Some cliff-fed courses are short or nearly straight and do not pass
                        // the scenic sinuosity score below. Keep the highest sustained real
                        // terrain drop as a visible render-only cascade candidate.
                        var cascadeScore = drop + Math.Min(length, 30) * 0.35;
                        if (cascadeScore > bestCascadeScore)
                        {
                            bestCascadeScore = cascadeScore;
                            bestCascadePath = path;
                            bestCascadeFeature = outcrop.FeatureIndex;
                            bestCascadeDrop = drop;
                        }
                    }
                    var score = ScoreWatercourse(start, path, drop);
                    if (double.IsNegativeInfinity(score)) continue;
                    var sinuosity = length / Math.Max(1, Vector2.Distance(path[0], path[^1]));
                    if (score > featureBestScore)
                    {
                        featureBestScore = score;
                        featureBest = (score, length, sinuosity, drop, path[0], path[^1]);
                    }
                    if (score <= bestRimScore) continue;
                    bestRimScore = score;
                    bestRimPath = path;
                }
                if (featureBestScore > double.NegativeInfinity)
                    outcropRouteSummaries.Add((outcrop.FeatureIndex, featureBest.Score, featureBest.Length,
                        featureBest.Sinuosity, featureBest.Drop, featureBest.Start, featureBest.End));
            }

            if (bestRimPath is not null)
            {
                bestPath = bestRimPath;
                bestScore = bestRimScore;
            }
            else if (bestCascadePath is not null)
            {
                bestPath = bestCascadePath;
                bestScore = bestCascadeScore;
                if (_capturePath is not null)
                    Console.WriteLine(FormattableString.Invariant(
                        $"fixtureCascadeRoute feature={bestCascadeFeature} dropM={bestCascadeDrop:F2} points={bestCascadePath.Count}"));
            }

            if (_capturePath is not null)
                foreach (var summary in outcropRouteSummaries.OrderByDescending(summary => summary.Score))
                    Console.WriteLine(FormattableString.Invariant(
                        $"rimRoute feature={summary.Feature} score={summary.Score:F2} lengthCells={summary.Length:F2} sinuosity={summary.Sinuosity:F2} dropM={summary.Drop:F2} start=({summary.Start.X:F2},{summary.Start.Y:F2}) end=({summary.End.X:F2},{summary.End.Y:F2})"));
        }

        if (_capturePath is not null)
            Console.WriteLine(FormattableString.Invariant(
                $"fixtureRouteCache={(routeCacheHit ? "hit" : "miss")} searchMs={Stopwatch.GetElapsedTime(routeSearchStarted).TotalMilliseconds:F2}"));

        if (bestPath is null) return Array.Empty<Vector2>();
        if (!routeCacheHit)
        {
            _evaluationWatercourseSeed = _snapshot.Seed;
            _evaluationWatercoursePath = bestPath;
        }

        var sections = new List<Vector2>(bestPath.Count * 2);
        for (var index = 0; index < bestPath.Count - 1; index++)
        {
            var p0 = bestPath[Math.Max(0, index - 1)];
            var p1 = bestPath[index];
            var p2 = bestPath[index + 1];
            var p3 = bestPath[Math.Min(bestPath.Count - 1, index + 2)];
            for (var subdivision = 0; subdivision < 2; subdivision++)
            {
                var t = subdivision * 0.5f;
                var t2 = t * t;
                var t3 = t2 * t;
                var point = (p1 * 2
                    + (p2 - p0) * t
                    + (p0 * 2 - p1 * 5 + p2 * 4 - p3) * t2
                    + (-p0 + p1 * 3 - p2 * 3 + p3) * t3) * 0.5f;
                sections.Add(point);
            }
        }
        sections.Add(bestPath[^1]);
        if (sections.Count < 3) return Array.Empty<Vector2>();

        if (_capturePath is not null)
        {
            var routeLength = sections.Zip(sections.Skip(1), (a, b) => Vector2.Distance(a, b)).Sum();
            Console.WriteLine(FormattableString.Invariant(
                $"fixtureWatercourseStart=({sections[0].X:F2},{sections[0].Y:F2}) end=({sections[^1].X:F2},{sections[^1].Y:F2}) points={sections.Count} lengthCells={routeLength:F2}"));
        }

        var leftBank = new Vector3[sections.Count];
        var rightBank = new Vector3[sections.Count];
        for (var index = 0; index < sections.Count; index++)
        {
            var before = sections[Math.Max(0, index - 1)];
            var after = sections[Math.Min(sections.Count - 1, index + 1)];
            var tangent = Vector2.Normalize(after - before);
            var perpendicular = new Vector2(-tangent.Y, tangent.X);
            var progress = index / (float)(sections.Count - 1);
            var endTaper = SmoothStep(0, 0.05, 1 - progress);
            var sourceTaper = SmoothStep(0, 0.025, progress);
            var width = (13.5f + progress * 17.5f + MathF.Sin(progress * MathF.PI * 3) * 1.2f)
                * EvaluationWatercourseWidthScale
                * (float)(sourceTaper * endTaper);
            var irregularity = MathF.Sin(index * 0.79f) * 0.62f + MathF.Sin(index * 0.23f) * 0.48f;
            var center = sections[index] * Tile;
            var left = center + perpendicular * (width * 0.5f + irregularity);
            var right = center - perpendicular * (width * 0.5f - irregularity);
            var leftHeight = _terrainHeightSampler(left.X / Tile, left.Y / Tile) + 0.22;
            var rightHeight = _terrainHeightSampler(right.X / Tile, right.Y / Tile) + 0.22;
            leftBank[index] = new Vector3(left.X, (float)leftHeight, left.Y);
            rightBank[index] = new Vector3(right.X, (float)rightHeight, right.Y);
        }

        for (var index = 0; index < sections.Count - 1; index++)
        {
            var a = leftBank[index];
            var b = rightBank[index];
            var c = leftBank[index + 1];
            var d = rightBank[index + 1];
            var progress = index / (float)(sections.Count - 1);
            var waterColor = Color.Lerp(new Color(62, 120, 126, 238), new Color(38, 88, 100, 242), progress);
            vertices.Add(new VertexPositionColor(a, waterColor));
            vertices.Add(new VertexPositionColor(c, waterColor));
            vertices.Add(new VertexPositionColor(b, waterColor));
            vertices.Add(new VertexPositionColor(b, waterColor));
            vertices.Add(new VertexPositionColor(c, waterColor));
            vertices.Add(new VertexPositionColor(d, waterColor));

            // A restrained, broken highlight gives the low-poly sheet some directional light.
            if (index % 7 == 2)
            {
                var midpoint = (a + b + c + d) * 0.25f + Vector3.Up * 0.035f;
                var along = Vector3.Normalize((c + d) * 0.5f - (a + b) * 0.5f);
                var across = Vector3.Normalize((a + c) - (b + d));
                var glint = new Color(135, 163, 152, 88);
                vertices.Add(new VertexPositionColor(midpoint - along * 1.2f - across * 0.18f, glint));
                vertices.Add(new VertexPositionColor(midpoint + along * 1.2f, glint));
                vertices.Add(new VertexPositionColor(midpoint - along * 0.8f + across * 0.18f, glint));
            }
        }

        AddEvaluationLakeFixture(vertices, sections[^1]);
        return sections;
    }

    private void AddEvaluationWaterfallFixture(List<VertexPositionColor> vertices, IReadOnlyList<Vector2> watercourse)
    {
        // This visible cascade is a presentation fixture attached to the test watercourse and
        // a real procedural outcrop. It never implies that the current hydrology snapshot
        // contains a waterfall or writes back into the simulation.
        if (watercourse.Count < 3) return;

        PlateauGeometry? selectedOutcrop = null;
        TerrainRimPoint selectedPoint = default;
        Vector2 selectedRoutePoint = default;
        var selectedRouteIndex = -1;
        var selectedDistance = double.PositiveInfinity;
        foreach (var feature in TerrainModel.GenerateOutcropGeometries(_snapshot.Seed))
        foreach (var candidate in feature.Rim)
        {
            var drop = candidate.CrownElevationM - candidate.ToeElevationM;
            if (drop < 8) continue;
            for (var routeIndex = 0; routeIndex < watercourse.Count; routeIndex++)
            {
                var routePoint = watercourse[routeIndex];
                var distance = Vector2.Distance(routePoint,
                    new Vector2((float)candidate.EastCell, (float)candidate.NorthCell));
                if (distance >= selectedDistance) continue;
                selectedDistance = distance;
                selectedOutcrop = feature;
                selectedPoint = candidate;
                selectedRoutePoint = routePoint;
                selectedRouteIndex = routeIndex;
            }
        }

        if (_capturePath is not null)
            Console.WriteLine(FormattableString.Invariant(
                $"fixtureWaterfallCandidate={(selectedOutcrop?.FeatureIndex ?? -1)} rim=({selectedPoint.EastCell:F2},{selectedPoint.NorthCell:F2}) routeDistanceCells={selectedDistance:F3} routeIndex={selectedRouteIndex} dropM={selectedPoint.CrownElevationM - selectedPoint.ToeElevationM:F2}"));

        // Only call it an integrated fall when its lip lies on the rendered stream.
        if (selectedOutcrop is null || selectedDistance > 1.2 || selectedRouteIndex < 0) return;

        var point = selectedPoint;
        var dropM = point.CrownElevationM - point.ToeElevationM;
        var topY = (float)DisplayElevation(point.CrownElevationM) + 0.18f;
        var bottomY = (float)DisplayElevation(point.ToeElevationM) + 0.30f;
        if (topY - bottomY < 5f) return;

        var direction = watercourse[Math.Min(selectedRouteIndex + 1, watercourse.Count - 1)]
            - watercourse[Math.Max(0, selectedRouteIndex - 1)];
        if (direction.LengthSquared() < 1e-6f) return;
        direction.Normalize();
        var acrossRiver = new Vector2(-direction.Y, direction.X);
        var rimCenter = new Vector2((float)(point.EastCell * Tile), (float)(point.NorthCell * Tile));
        var routeCenter = selectedRoutePoint * Tile;
        var center = Vector2.Lerp(rimCenter, routeCenter, 0.8f);
        var progress = selectedRouteIndex / (float)(watercourse.Count - 1);
        var width = (13.5f + progress * 17.5f) * EvaluationWatercourseWidthScale;

        var poolRouteIndex = selectedRouteIndex;
        var limit = Math.Min(watercourse.Count - 1, selectedRouteIndex + 90);
        for (var index = selectedRouteIndex; index <= limit; index++)
        {
            var sample = watercourse[index];
            if (_terrainHeightSampler(sample.X, sample.Y) > point.ToeElevationM + 1.4) continue;
            poolRouteIndex = index;
            break;
        }
        var poolRoute = watercourse[poolRouteIndex] * Tile;
        var poolGround = _terrainHeightSampler(poolRoute.X / Tile, poolRoute.Y / Tile);
        var poolCenter = new Vector3(poolRoute.X, (float)(poolGround + 0.34), poolRoute.Y);

        if (_capturePath is not null)
            Console.WriteLine(FormattableString.Invariant(
                $"fixtureWaterfallOutcrop={selectedOutcrop.FeatureIndex} rim=({point.EastCell:F2},{point.NorthCell:F2}) routeDistanceCells={selectedDistance:F3} dropM={dropM:F2} poolRouteIndex={poolRouteIndex}"));

        const int rows = 12;
        const int columns = 5;
        var colors = new[]
        {
            new Color(56, 108, 117, 214), new Color(66, 122, 128, 220),
            new Color(91, 145, 140, 218), new Color(76, 132, 133, 216),
            new Color(48, 96, 108, 210)
        };
        var sheet = new Vector3[rows + 1, columns + 1];
        for (var row = 0; row <= rows; row++)
        for (var column = 0; column <= columns; column++)
        {
            var t = row / (float)rows;
            var across = (column / (float)columns - 0.5f) * width;
            var flutter = MathF.Sin(t * MathF.PI * 2.4f + column * 0.72f) * 0.32f;
            var position = center + acrossRiver * across + direction * (0.35f + t * 1.05f + flutter);
            sheet[row, column] = new Vector3(position.X, topY + (bottomY - topY) * t, position.Y);
        }

        for (var row = 0; row < rows; row++)
        for (var column = 0; column < columns; column++)
        {
            var a = sheet[row, column];
            var b = sheet[row, column + 1];
            var c = sheet[row + 1, column];
            var d = sheet[row + 1, column + 1];
            var color = colors[column];
            vertices.Add(new VertexPositionColor(a, color));
            vertices.Add(new VertexPositionColor(c, color));
            vertices.Add(new VertexPositionColor(b, color));
            vertices.Add(new VertexPositionColor(b, color));
            vertices.Add(new VertexPositionColor(c, color));
            vertices.Add(new VertexPositionColor(d, color));
        }

        const int poolSegments = 24;
        for (var index = 0; index < poolSegments; index++)
        {
            var a = MathF.Tau * index / poolSegments;
            var b = MathF.Tau * (index + 1) / poolSegments;
            Vector3 PoolEdge(float angle)
            {
                var along = MathF.Cos(angle) * 8.5f * EvaluationWatercourseWidthScale;
                var across = MathF.Sin(angle) * 6.3f * EvaluationWatercourseWidthScale;
                return poolCenter + new Vector3(direction.X * along + acrossRiver.X * across,
                    0.025f, direction.Y * along + acrossRiver.Y * across);
            }
            var first = PoolEdge(a);
            var second = PoolEdge(b);
            var poolColor = (index & 1) == 0 ? colors[1] : colors[3];
            vertices.Add(new VertexPositionColor(poolCenter, poolColor));
            vertices.Add(new VertexPositionColor(first, poolColor));
            vertices.Add(new VertexPositionColor(second, poolColor));
        }
    }

    private void AddEvaluationLakeFixture(List<VertexPositionColor> vertices, Vector2 downstreamCell)
    {
        // The lake is a renderer fixture anchored to the route's lowest nearby terrain sample.
        // Its level, footprint, and visual detail never feed back into Hydrosphere or a snapshot.
        const int angularSegments = 64;
        const int radialRings = 5;
        var center = downstreamCell;
        var searchOrigin = downstreamCell;
        var minimumHeight = _terrainHeightSampler(center.X, center.Y);
        for (var offsetY = -1.5f; offsetY <= 1.5001f; offsetY += 0.25f)
        for (var offsetX = -1.5f; offsetX <= 1.5001f; offsetX += 0.25f)
        {
            var sampleX = searchOrigin.X + offsetX;
            var sampleY = searchOrigin.Y + offsetY;
            if (sampleX < 1 || sampleY < 1
                || sampleX >= SimulationState.Width - 1 || sampleY >= SimulationState.Height - 1) continue;
            var sampleHeight = _terrainHeightSampler(sampleX, sampleY);
            if (sampleHeight >= minimumHeight) continue;
            minimumHeight = sampleHeight;
            center = new Vector2(sampleX, sampleY);
        }

        var waterLevel = (float)minimumHeight + 0.8f;
        var edgeRadii = new float[angularSegments];
        var phase = (float)(CounterRandom.Unit(_snapshot.Seed, 0x4C414B4550484153UL, 0, 0, 0, 0) * Math.Tau);
        for (var segment = 0; segment < angularSegments; segment++)
        {
            var angle = phase + MathF.Tau * segment / angularSegments;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var radiusToEastEdge = direction.X >= 0
                ? (SimulationState.Width - 1.2f - center.X) / Math.Max(0.001f, direction.X)
                : (center.X - 1.2f) / Math.Max(0.001f, -direction.X);
            var radiusToNorthEdge = direction.Y >= 0
                ? (SimulationState.Height - 1.2f - center.Y) / Math.Max(0.001f, direction.Y)
                : (center.Y - 1.2f) / Math.Max(0.001f, -direction.Y);
            var maxRadius = Math.Min(7.4f, Math.Min(radiusToEastEdge, radiusToNorthEdge));
            var radius = maxRadius;
            for (var distance = 0.65f; distance <= maxRadius; distance += 0.22f)
            {
                var point = center + direction * distance;
                if (_terrainHeightSampler(point.X, point.Y) <= waterLevel + 0.18f) continue;
                radius = Math.Max(2.2f, distance - 0.18f);
                break;
            }

            var shorelineNoise = (float)(CounterRandom.Unit(_snapshot.Seed, 0x4C414B4553484F52UL,
                (ulong)segment, 0, 0, 0) - 0.5) * 0.36f;
            edgeRadii[segment] = Math.Clamp(radius + shorelineNoise, 2.2f, maxRadius);
        }

        Vector3 LakePoint(int segment, float radialFraction)
        {
            var angle = phase + MathF.Tau * segment / angularSegments;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var point = center + direction * (edgeRadii[segment % angularSegments] * radialFraction);
            return new Vector3(point.X * Tile, waterLevel, point.Y * Tile);
        }

        Color LakeColor(float radialFraction, int segment)
        {
            var centerColor = new Color(39, 91, 103, 238);
            var shoreColor = new Color(81, 137, 131, 218);
            var color = Color.Lerp(centerColor, shoreColor, radialFraction);
            var mottling = 0.94f + (float)CounterRandom.Unit(_snapshot.Seed, 0x4C414B454D4F5454UL,
                (ulong)segment, (ulong)(radialFraction * 100), 0, 0) * 0.12f;
            return new Color((byte)Math.Clamp(color.R * mottling, 0, 255),
                (byte)Math.Clamp(color.G * mottling, 0, 255),
                (byte)Math.Clamp(color.B * mottling, 0, 255), color.A);
        }

        var centerPoint = new Vector3(center.X * Tile, waterLevel, center.Y * Tile);
        var firstRingFraction = 1f / radialRings;
        for (var segment = 0; segment < angularSegments; segment++)
        {
            var next = (segment + 1) % angularSegments;
            vertices.Add(new VertexPositionColor(centerPoint, LakeColor(0.08f, segment)));
            vertices.Add(new VertexPositionColor(LakePoint(segment, firstRingFraction), LakeColor(firstRingFraction, segment)));
            vertices.Add(new VertexPositionColor(LakePoint(next, firstRingFraction), LakeColor(firstRingFraction, next)));
        }

        for (var ring = 1; ring < radialRings; ring++)
        {
            var innerFraction = ring / (float)radialRings;
            var outerFraction = (ring + 1f) / radialRings;
            for (var segment = 0; segment < angularSegments; segment++)
            {
                var next = (segment + 1) % angularSegments;
                var innerA = LakePoint(segment, innerFraction);
                var innerB = LakePoint(next, innerFraction);
                var outerA = LakePoint(segment, outerFraction);
                var outerB = LakePoint(next, outerFraction);
                vertices.Add(new VertexPositionColor(innerA, LakeColor(innerFraction, segment)));
                vertices.Add(new VertexPositionColor(outerA, LakeColor(outerFraction, segment)));
                vertices.Add(new VertexPositionColor(innerB, LakeColor(innerFraction, next)));
                vertices.Add(new VertexPositionColor(innerB, LakeColor(innerFraction, next)));
                vertices.Add(new VertexPositionColor(outerA, LakeColor(outerFraction, segment)));
                vertices.Add(new VertexPositionColor(outerB, LakeColor(outerFraction, next)));
            }
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

    private static double[] SmoothRenderWaterMask(double[] source, int width, int height)
    {
        // Tiny isolated wet components cannot read as ponds at this scale and otherwise become
        // square pixels. This is a render-only visibility rule; the snapshot water is untouched.
        var visited = new bool[source.Length];
        for (var index = 0; index < source.Length; index++)
        {
            if (visited[index] || source[index] < 0.36) continue;
            var component = new List<int>();
            var frontier = new Stack<int>();
            frontier.Push(index);
            visited[index] = true;
            while (frontier.Count > 0)
            {
                var current = frontier.Pop();
                component.Add(current);
                var x = current % width;
                var y = current / width;
                for (var offsetY = -1; offsetY <= 1; offsetY++)
                for (var offsetX = -1; offsetX <= 1; offsetX++)
                {
                    if (Math.Abs(offsetX) + Math.Abs(offsetY) != 1) continue;
                    var neighborX = x + offsetX;
                    var neighborY = y + offsetY;
                    if (neighborX < 0 || neighborY < 0 || neighborX >= width || neighborY >= height) continue;
                    var neighbor = neighborY * width + neighborX;
                    if (visited[neighbor] || source[neighbor] < 0.36) continue;
                    visited[neighbor] = true;
                    frontier.Push(neighbor);
                }
            }
            if (component.Count >= 3) continue;
            foreach (var cell in component) source[cell] = 0;
        }

        // Shoreline filtering is render-only coverage; water depth and stored cells stay intact.
        for (var pass = 0; pass < 2; pass++)
        {
            var smoothed = new double[source.Length];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                var value = source[index] * 0.52;
                value += source[y * width + Math.Max(0, x - 1)] * 0.09;
                value += source[y * width + Math.Min(width - 1, x + 1)] * 0.09;
                value += source[Math.Max(0, y - 1) * width + x] * 0.09;
                value += source[Math.Min(height - 1, y + 1) * width + x] * 0.09;
                value += source[Math.Max(0, y - 1) * width + Math.Max(0, x - 1)] * 0.03;
                value += source[Math.Max(0, y - 1) * width + Math.Min(width - 1, x + 1)] * 0.03;
                value += source[Math.Min(height - 1, y + 1) * width + Math.Max(0, x - 1)] * 0.03;
                value += source[Math.Min(height - 1, y + 1) * width + Math.Min(width - 1, x + 1)] * 0.03;
                smoothed[index] = value;
            }
            source = smoothed;
        }
        return source;
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
        if (_visualEvaluation)
        {
            var baseEastM = _treeQualityView ? _treeGalleryCenterM.X
                : _wideEvaluationView || _panoramaEvaluationView || _referenceCompositionView || _referenceLandscapeView
                ? TerrainExtentM * 0.5f + (_referenceLandscapeView ? 80f : 0f) : 320f;
            var baseNorthM = _treeQualityView ? _treeGalleryCenterM.Y : TerrainDepthM * 0.5f;
            ClampCameraPan(baseEastM, baseNorthM);
            var centerEastM = baseEastM + _cameraPanM.X;
            var centerNorthM = baseNorthM + _cameraPanM.Y;
            var centerHeight = (float)_terrainHeightSampler(centerEastM / Tile, centerNorthM / Tile);
            var closeViewBlend = _referenceLandscapeView
                ? Math.Clamp((_cameraZoom - 1f) / 31f, 0f, 1f)
                : 0f;
            var targetHeightOffset = _treeQualityView ? 20f : MathHelper.Lerp(-14f, 12f, closeViewBlend);
            var evaluationTarget = new Vector3(centerEastM, centerHeight + targetHeightOffset, centerNorthM);
            var cameraOffset = _treeQualityView
                ? new Vector3(0f, 1100f, 2400f)
                : _referenceLandscapeView
                ? _higherReferenceAngle ? new Vector3(80f, 3400f, 1220f)
                    : _lowerReferenceAngle ? new Vector3(170f, 1100f, 1450f) : new Vector3(120f, 1850f, 1510f)
                : _wideEvaluationView
                ? new Vector3(580f, 900f, 580f)
                : _referenceCompositionView
                    ? new Vector3(255f, 457f, 381f)
                    : _panoramaEvaluationView
                        ? new Vector3(0f, 430f, 360f)
                        : new Vector3(0, 350f, 250f);
            var evaluationEye = evaluationTarget + cameraOffset / _cameraZoom;
            _cameraTarget = evaluationTarget;
            _cameraPosition = evaluationEye;
            _view = Matrix.CreateLookAt(evaluationEye, evaluationTarget, Vector3.Up);
            var aspect = GraphicsDevice.Viewport.Width / (float)GraphicsDevice.Viewport.Height;
            var verticalFov = _referenceLandscapeView ? 48f : _wideEvaluationView ? 44f : _panoramaEvaluationView ? 42f : 32f;
            var orthographicWidth = _referenceLandscapeView ? 2800f
                : _wideEvaluationView ? 1500f : _referenceCompositionView ? 640f
                : _panoramaEvaluationView ? 740f : 440f;
            orthographicWidth /= _cameraZoom;
            var orthographicHeight = orthographicWidth / aspect;
            _treeLodProjectionScale = _perspectiveProjection
                ? GraphicsDevice.Viewport.Height / (2f * MathF.Tan(MathHelper.ToRadians(verticalFov) * 0.5f))
                : GraphicsDevice.Viewport.Height / orthographicHeight;
            var farClip = _referenceLandscapeView ? 9000f : 4000f;
            _projection = _perspectiveProjection
                ? Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(verticalFov), aspect, 1f, farClip)
                : Matrix.CreateOrthographic(orthographicWidth, orthographicHeight, 1f, farClip);
            return;
        }

        var mapCenterEastM = TerrainExtentM * 0.5f;
        var mapCenterNorthM = TerrainDepthM * 0.5f;
        ClampCameraPan(mapCenterEastM, mapCenterNorthM);
        var target = new Vector3(mapCenterEastM + _cameraPanM.X,
            (float)_centerElevationM, mapCenterNorthM + _cameraPanM.Y);
        var horizontal = 1050f;
        var elevation = 1350f;
        var viewHeight = 550f;
        var viewWidth = 680f;
        var eye = target + new Vector3(0, elevation, horizontal) / _cameraZoom;
        _cameraTarget = target;
        _cameraPosition = eye;
        viewHeight /= _cameraZoom;
        viewWidth /= _cameraZoom;
        _treeLodProjectionScale = GraphicsDevice.Viewport.Height / viewHeight;
        _view = Matrix.CreateLookAt(eye, target, Vector3.Up);
        _projection = Matrix.CreateOrthographic(viewWidth, viewHeight, 1f, 4000f);
    }

    private bool UpdateCameraNavigation(GameTime gameTime, KeyboardState keyboard, MouseState mouse)
    {
        if (!_hasPreviousMouse)
        {
            _previousMouse = mouse;
            _hasPreviousMouse = true;
        }

        var changed = false;
        var oldMouse = _previousMouse;
        if (Pressed(keyboard, Keys.Home)
            && (MathF.Abs(_cameraZoom - _initialCameraZoom) > 0.0001f || _cameraPanM != _initialCameraPanM))
        {
            _cameraZoom = _initialCameraZoom;
            _cameraPanM = _initialCameraPanM;
            UpdateCamera();
            changed = true;
        }

        var zoomDelta = mouse.ScrollWheelValue - oldMouse.ScrollWheelValue;
        if (zoomDelta != 0
            && ZoomCameraAtScreen(mouse.X, mouse.Y, MathF.Pow(1.12f, zoomDelta / 120f)))
            changed = true;

        var keyboardZoomDirection = (Pressed(keyboard, Keys.PageUp) ? 1 : 0)
            - (Pressed(keyboard, Keys.PageDown) ? 1 : 0);
        if (keyboardZoomDirection != 0)
        {
            var viewport = GraphicsDevice.Viewport;
            if (ZoomCameraAtScreen(viewport.Width / 2, viewport.Height / 2,
                    MathF.Pow(1.12f, keyboardZoomDirection)))
                changed = true;
        }

        if (IsCameraPanButtonDown(mouse) && IsCameraPanButtonDown(oldMouse))
        {
            var viewport = GraphicsDevice.Viewport;
            var previousX = Math.Clamp(oldMouse.X, 0, viewport.Width - 1);
            var previousY = Math.Clamp(oldMouse.Y, 0, viewport.Height - 1);
            var currentX = Math.Clamp(mouse.X, 0, viewport.Width - 1);
            var currentY = Math.Clamp(mouse.Y, 0, viewport.Height - 1);
            var planeHeight = CameraPanPlaneHeight();
            if ((previousX != currentX || previousY != currentY)
                && TryGetScreenPointOnPlane(previousX, previousY, planeHeight, out var previousPoint)
                && TryGetScreenPointOnPlane(currentX, currentY, planeHeight, out var currentPoint))
            {
                _cameraPanM.X += previousPoint.X - currentPoint.X;
                _cameraPanM.Y += previousPoint.Z - currentPoint.Z;
                UpdateCamera();
                changed = true;
            }
        }

        var pan = Vector2.Zero;
        if (keyboard.IsKeyDown(Keys.Left)) pan.X -= 1;
        if (keyboard.IsKeyDown(Keys.Right)) pan.X += 1;
        if (keyboard.IsKeyDown(Keys.Up)) pan.Y -= 1;
        if (keyboard.IsKeyDown(Keys.Down)) pan.Y += 1;
        if (pan != Vector2.Zero)
        {
            var deltaSeconds = Math.Clamp((float)gameTime.ElapsedGameTime.TotalSeconds, 0, 0.05f);
            _cameraPanM += Vector2.Normalize(pan) * (720f * deltaSeconds / _cameraZoom);
            UpdateCamera();
            changed = true;
        }

        if (changed)
        {
            _cameraNavigationDirty = true;
            _cameraNavigationIdleSeconds = 0;
        }
        _previousMouse = mouse;
        return changed;
    }

    private bool ZoomCameraAtScreen(int screenX, int screenY, float zoomFactor)
    {
        var planeHeight = CameraPanPlaneHeight();
        var canAnchor = TryGetScreenPointOnPlane(screenX, screenY, planeHeight, out var anchorBefore);
        var nextZoom = Math.Clamp(_cameraZoom * zoomFactor, 0.45f, MaximumCameraZoom);
        if (MathF.Abs(nextZoom - _cameraZoom) <= 0.0001f) return false;

        _cameraZoom = nextZoom;
        UpdateCamera();
        if (canAnchor && TryGetScreenPointOnPlane(screenX, screenY, planeHeight, out var anchorAfter))
        {
            _cameraPanM.X += anchorBefore.X - anchorAfter.X;
            _cameraPanM.Y += anchorBefore.Z - anchorAfter.Z;
            UpdateCamera();
        }
        return true;
    }

    private static bool IsCameraPanButtonDown(MouseState state)
        => state.LeftButton == ButtonState.Pressed
            || state.MiddleButton == ButtonState.Pressed
            || state.RightButton == ButtonState.Pressed;

    private void ClampCameraPan(float baseEastM, float baseNorthM)
    {
        const float coastMarginM = 450f;
        _cameraPanM.X = Math.Clamp(_cameraPanM.X,
            -coastMarginM - baseEastM, TerrainExtentM + coastMarginM - baseEastM);
        _cameraPanM.Y = Math.Clamp(_cameraPanM.Y,
            -coastMarginM - baseNorthM, TerrainDepthM + coastMarginM - baseNorthM);
    }

    private float CameraPanPlaneHeight()
        => (float)_terrainHeightSampler(_cameraTarget.X / Tile, _cameraTarget.Z / Tile);

    private bool TryGetScreenPointOnPlane(int screenX, int screenY, float planeHeight, out Vector3 point)
    {
        var viewport = GraphicsDevice.Viewport;
        var near = viewport.Unproject(new Vector3(screenX, screenY, 0), _projection, _view, Matrix.Identity);
        var far = viewport.Unproject(new Vector3(screenX, screenY, 1), _projection, _view, Matrix.Identity);
        var direction = far - near;
        if (MathF.Abs(direction.Y) < 0.0001f)
        {
            point = default;
            return false;
        }
        var distance = (planeHeight - near.Y) / direction.Y;
        if (distance < 0)
        {
            point = default;
            return false;
        }
        point = near + direction * distance;
        return float.IsFinite(point.X) && float.IsFinite(point.Z);
    }

    private string ProjectionName => _perspectiveProjection ? "PERSPECTIVE" : "ORTHOGRAPHIC";

    private Vector3 ProjectGround(double worldX, double elevationM, double worldZ)
    {
        return GraphicsDevice.Viewport.Project(
            new Vector3((float)worldX, (float)elevationM, (float)worldZ), _projection, _view, Matrix.Identity);
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
            if (point.X < 0 || point.Z < 0 || point.X >= TerrainExtentM || point.Z >= TerrainDepthM) continue;
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

    private Season VisualSeason => _snapshot.Season;

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
        RefreshHydrologyPresentationFields();
        _groundSpritesRevision = -1;
        _tickAccumulator = 0;
        BuildTerrainGeometry();
        RefreshTerrainTexture(force: true);
        RefreshWaterGeometry(force: true);
    }

    private void RefreshHydrologyPresentationFields()
    {
        var terrain = _snapshot.Cells.ToArray();
        _surfaceDrainage = SurfaceWaterModel.AnalyzeDrainage(terrain);
        _waterDepressionId = SurfaceWaterModel.AnalyzeHydrology(terrain).DepressionId;
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
            _waterSurfaceEffect?.Dispose();
            _floraEffect?.Dispose();
            _treeEffect?.Dispose();
            _treeMaterialAtlas?.Dispose();
            foreach (var chunk in _terrainChunks)
            {
                chunk.Vertices.Dispose();
                chunk.Indices.Dispose();
            }
            foreach (var chunk in _terrainBackdropChunks)
            {
                chunk.Vertices.Dispose();
                chunk.Indices.Dispose();
            }
            _plateauVertexBuffer?.Dispose();
            _plateauIndexBuffer?.Dispose();
            _cliffVertexBuffer?.Dispose();
            _waterVertexBuffer?.Dispose();
            _oceanVertexBuffer?.Dispose();
            _oceanIndexBuffer?.Dispose();
            _floraVertexBuffer?.Dispose();
            _treeVertexBuffer?.Dispose();
            _evaluationShadowVertexBuffer?.Dispose();
            _evaluationRockVertexBuffer?.Dispose();
            _treeBillboardEffect?.Dispose();
            foreach (var texture in _treeBillboardTextures) texture.Dispose();
            foreach (var batch in _treeBillboardBatches) batch.Vertices.Dispose();
            _batch?.Dispose();
        }
        base.Dispose(disposing);
    }

    private sealed record TerrainRenderChunk(VertexBuffer Vertices, IndexBuffer Indices, int PrimitiveCount, BoundingBox Bounds);
    private sealed record TerrainColorRenderChunk(VertexBuffer Vertices, IndexBuffer Indices, int PrimitiveCount, BoundingBox Bounds);
    private sealed record TreeBillboardRenderBatch(int RenderProfileRow, VertexBuffer Vertices, int PrimitiveCount);
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
