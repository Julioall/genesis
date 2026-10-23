using Game.Contracts;
using Game.Procedural;

namespace Game.Modules.Geosphere;

public readonly record struct TerrainRimPoint(
    double EastCell,
    double NorthCell,
    double CrownElevationM,
    double ToeElevationM);

public sealed record PlateauGeometry(
    int FeatureIndex,
    double CenterEastCell,
    double CenterNorthCell,
    IReadOnlyList<TerrainRimPoint> Rim);

/// <summary>Deterministic physical terrain generation and terrain-derived solar exposure.</summary>
public static class TerrainModel
{
    public static Func<double, double, double> CreateElevationSampler(ulong seed)
    {
        var generator = new TerrainGenerator(seed);
        return generator.Elevation;
    }

    public static Func<double, double, double> CreateUnderlyingElevationSampler(ulong seed)
    {
        var generator = new TerrainGenerator(seed);
        return generator.UnderlyingElevation;
    }

    public static PlateauGeometry GeneratePlateauGeometry(ulong seed, int rimSegments = 160)
        => GenerateOutcropGeometries(seed, rimSegments)[0];

    public static IReadOnlyList<PlateauGeometry> GenerateOutcropGeometries(ulong seed, int rimSegments = 160)
    {
        if (rimSegments < 12) throw new ArgumentOutOfRangeException(nameof(rimSegments), "A plateau rim needs at least 12 segments.");
        return new TerrainGenerator(seed).OutcropGeometries(rimSegments);
    }

    public static double[] GenerateElevationField(ulong seed)
    {
        var generator = new TerrainGenerator(seed);
        var elevations = new double[SimulationState.Width * SimulationState.Height];
        for (var y = 0; y < SimulationState.Height; y++)
        for (var x = 0; x < SimulationState.Width; x++)
            elevations[y * SimulationState.Width + x] = generator.Elevation(x + 0.5, y + 0.5);
        return elevations;
    }

    public static double Elevation(int x, int y, ulong seed)
        => new TerrainGenerator(seed).Elevation(x + 0.5, y + 0.5);

    public static double SlopeMagnitude(double[] elevations, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(elevations);
        if (elevations.Length != SimulationState.Width * SimulationState.Height)
            throw new ArgumentException("Terrain slope needs one elevation per simulation cell.", nameof(elevations));
        if (x < 0 || x >= SimulationState.Width || y < 0 || y >= SimulationState.Height)
            throw new ArgumentOutOfRangeException(nameof(x), "Slope coordinates must be inside the terrain grid.");

        var gradient = GridGradient(elevations, x, y);
        var cellSizeM = Math.Sqrt(SimulationState.CellAreaM2);
        return Math.Sqrt(gradient.East * gradient.East + gradient.North * gradient.North) / cellSizeM;
    }

    public static double TerrainSolarFactor(
        double[] elevationsM, int x, int y, double sunEast, double sunNorth, double sunUp)
    {
        ArgumentNullException.ThrowIfNull(elevationsM);
        if (elevationsM.Length != SimulationState.Width * SimulationState.Height)
            throw new ArgumentException("Terrain solar exposure needs one elevation per simulation cell.", nameof(elevationsM));
        return TerrainSolarFactor(CreateTerrainSolarNormal(elevationsM, x, y), sunEast, sunNorth, sunUp);
    }

    public static TerrainSolarNormalV1[] CreateTerrainSolarNormals(double[] elevationsM)
    {
        ArgumentNullException.ThrowIfNull(elevationsM);
        var cellCount = SimulationState.Width * SimulationState.Height;
        if (elevationsM.Length != cellCount)
            throw new ArgumentException("Terrain solar exposure needs one elevation per simulation cell.", nameof(elevationsM));
        var normals = new TerrainSolarNormalV1[cellCount];
        for (var index = 0; index < cellCount; index++)
            normals[index] = CreateTerrainSolarNormal(elevationsM, index % SimulationState.Width,
                index / SimulationState.Width);
        return normals;
    }

    public static double[] TerrainSolarFactors(
        TerrainSolarNormalV1[] normals, double sunEast, double sunNorth, double sunUp)
    {
        ArgumentNullException.ThrowIfNull(normals);
        var cellCount = SimulationState.Width * SimulationState.Height;
        if (normals.Length != cellCount)
            throw new ArgumentException("Terrain solar normals must match the L0 world grid.", nameof(normals));
        var factors = new double[cellCount];
        if (sunUp <= 0) return factors;
        for (var index = 0; index < cellCount; index++)
            factors[index] = TerrainSolarFactor(normals[index], sunEast, sunNorth, sunUp);
        return factors;
    }

    private static TerrainSolarNormalV1 CreateTerrainSolarNormal(double[] elevationsM, int x, int y)
    {
        var gridGradient = GridGradient(elevationsM, x, y);
        var cellSizeM = Math.Sqrt(SimulationState.CellAreaM2);
        var eastGradient = gridGradient.East / cellSizeM;
        var northGradient = gridGradient.North / cellSizeM;
        var length = Math.Sqrt(eastGradient * eastGradient + northGradient * northGradient + 1);
        var normalEast = -eastGradient / length;
        var normalNorth = -northGradient / length;
        var normalUp = 1 / length;
        return new(normalEast, normalNorth, normalUp);
    }

    private static double TerrainSolarFactor(TerrainSolarNormalV1 normal,
        double sunEast, double sunNorth, double sunUp)
    {
        if (sunUp <= 0) return 0;
        return Math.Clamp(Math.Max(0, normal.East * sunEast + normal.North * sunNorth + normal.Up * sunUp)
                          / Math.Max(sunUp, 0.05), 0, 1.2);
    }

    private static (double East, double North) GridGradient(double[] elevations, int x, int y)
    {
        if (x < 0 || x >= SimulationState.Width || y < 0 || y >= SimulationState.Height)
            throw new ArgumentOutOfRangeException(nameof(x), "Gradient coordinates must be inside the terrain grid.");
        var east = x == 0 ? elevations[y * SimulationState.Width + 1] - elevations[y * SimulationState.Width]
            : x == SimulationState.Width - 1
                ? elevations[y * SimulationState.Width + x] - elevations[y * SimulationState.Width + x - 1]
                : (elevations[y * SimulationState.Width + x + 1] - elevations[y * SimulationState.Width + x - 1]) / 2;
        var north = y == 0 ? elevations[SimulationState.Width + x] - elevations[x]
            : y == SimulationState.Height - 1
                ? elevations[y * SimulationState.Width + x] - elevations[(y - 1) * SimulationState.Width + x]
                : (elevations[(y + 1) * SimulationState.Width + x]
                    - elevations[(y - 1) * SimulationState.Width + x]) / 2;
        return (east, north);
    }

    private sealed class TerrainGenerator
    {
        // Leave a representable margin above the public 18 m wall-floor after rim sampling.
        private const double MinimumPlateauReliefM = 18.000001;
        private const double MaximumPlateauWallM = 39.999999;
        private const double RidgeWidthMultiplier = 1.45;
        private const double RidgeAmplitudeMultiplier = 0.80;
        private readonly ValueNoise _broad;
        private readonly ValueNoise _regional;
        private readonly ValueNoise _detail;
        private readonly ValueNoise _mesoRelief;
        private readonly ValueNoise _microRelief;
        private readonly ValueNoise _ridgeMask;
        private readonly ValueNoise _rangeAxis;
        private readonly ValueNoise _mountainMask;
        private readonly ValueNoise _rimDetail;
        private readonly MountainRange[] _mountainRanges;
        private readonly RidgeSpur[] _ridgeSpurs;
        private readonly double _riverPhase;
        private readonly double _scaleX;
        private readonly double _scaleY;
        private readonly OutcropFeature[] _outcrops;

        public TerrainGenerator(ulong seed)
        {
            _scaleX = SimulationState.Width / 64.0;
            _scaleY = SimulationState.Height / 64.0;
            _broad = new ValueNoise(seed, 26, 0x5445525241494E31UL);
            _regional = new ValueNoise(seed, 12, 0x5445525241494E32UL);
            _detail = new ValueNoise(seed, 4, 0x5445525241494E33UL);
            _mesoRelief = new ValueNoise(seed, 2, 0x4D45534F52454C46UL);
            _microRelief = new ValueNoise(seed, 1, 0x4D4943524F524C46UL);
            _ridgeMask = new ValueNoise(seed, 10, 0x52494447454D4153UL);
            _rangeAxis = new ValueNoise(seed, 12, 0x5249444745415849UL);
            _mountainMask = new ValueNoise(seed, 8, 0x4D4F554E5441494EUL);
            _rimDetail = new ValueNoise(seed, 2, 0x504C415445415552UL);
            _riverPhase = CounterRandom.Unit(seed, CounterRandom.TerrainTag, 40, 1, 0, 0) * Math.PI * 2;
            var westPhase = CounterRandom.Unit(seed, CounterRandom.TerrainTag, 41, 1, 0, 0) * Math.PI * 2;
            var eastPhase = CounterRandom.Unit(seed, CounterRandom.TerrainTag, 48, 1, 0, 0) * Math.PI * 2;
            _mountainRanges =
            [
                new MountainRange(
                    0,
                    (12 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 49, 1, 0, 0) * 4) * _scaleX,
                    -0.12 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 50, 1, 0, 0) * 0.24,
                    (4.6 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 51, 1, 0, 0) * 1.0) * _scaleX * RidgeWidthMultiplier,
                    (170 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 52, 1, 0, 0) * 20) * RidgeAmplitudeMultiplier,
                    westPhase),
                new MountainRange(
                    1,
                    (48 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 53, 1, 0, 0) * 4) * _scaleX,
                    -0.12 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 54, 1, 0, 0) * 0.24,
                    (4.0 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 55, 1, 0, 0) * 0.9) * _scaleX * RidgeWidthMultiplier,
                    (132 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 56, 1, 0, 0) * 18) * RidgeAmplitudeMultiplier,
                    eastPhase)
            ];
            _ridgeSpurs = CreateRidgeSpurs(seed);
            var plateauX = (43 + (CounterRandom.Unit(seed, CounterRandom.TerrainTag, 42, 1, 0, 0) - 0.5) * 8) * _scaleX;
            var plateauY = (18 + (CounterRandom.Unit(seed, CounterRandom.TerrainTag, 43, 1, 0, 0) - 0.5) * 6) * _scaleY;
            var plateauRadiusX = (2.9 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 44, 1, 0, 0) * 0.9) * _scaleX;
            var plateauRadiusY = (1.9 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 45, 1, 0, 0) * 0.7) * _scaleY;
            var plateauHeight = 94 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 46, 1, 0, 0) * 8;
            var plateauPhase = CounterRandom.Unit(seed, CounterRandom.TerrainTag, 47, 1, 0, 0) * Math.PI * 2;
            var seededOutcrops = new List<OutcropFeature>
            {
                CreateOutcrop(0, plateauX, plateauY, plateauRadiusX, plateauRadiusY, plateauHeight, plateauPhase),
                CreateOutcrop(1,
                    (15 + (CounterRandom.Unit(seed, CounterRandom.TerrainTag, 57, 1, 0, 0) - 0.5) * 3) * _scaleX,
                    (17 + (CounterRandom.Unit(seed, CounterRandom.TerrainTag, 58, 1, 0, 0) - 0.5) * 4) * _scaleY,
                    (1.15 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 59, 1, 0, 0) * 0.55) * _scaleX,
                    (1.05 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 60, 1, 0, 0) * 0.55) * _scaleY,
                    126 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 61, 1, 0, 0) * 8,
                    CounterRandom.Unit(seed, CounterRandom.TerrainTag, 62, 1, 0, 0) * Math.PI * 2),
                CreateOutcrop(2,
                    (57 + (CounterRandom.Unit(seed, CounterRandom.TerrainTag, 63, 1, 0, 0) - 0.5) * 3) * _scaleX,
                    (19 + (CounterRandom.Unit(seed, CounterRandom.TerrainTag, 64, 1, 0, 0) - 0.5) * 4) * _scaleY,
                    (1.05 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 65, 1, 0, 0) * 0.6) * _scaleX,
                    (1.15 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 66, 1, 0, 0) * 0.55) * _scaleY,
                    128 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 67, 1, 0, 0) * 8,
                    CounterRandom.Unit(seed, CounterRandom.TerrainTag, 68, 1, 0, 0) * Math.PI * 2)
            };

            // Add smaller tablelands on both mountain flanks. Their locations follow the seeded
            // ridge axes, so the cliffs belong to the same physical landforms as the drainage.
            for (var rangeIndex = 0; rangeIndex < _mountainRanges.Length; rangeIndex++)
            for (var shelf = 0; shelf < 2; shelf++)
            {
                var range = _mountainRanges[rangeIndex];
                var featureIndex = 3 + rangeIndex * 2 + shelf;
                var id = (ulong)featureIndex;
                var north = (shelf == 0 ? 36.0 : 52.0) * _scaleY;
                north += (CounterRandom.Unit(seed, CounterRandom.TerrainTag, 100 + id, 1, 0, 0) - 0.5) * 3.0 * _scaleY;
                var east = MountainAxis(range, north)
                    + (CounterRandom.Unit(seed, CounterRandom.TerrainTag, 110 + id, 1, 0, 0) - 0.5) * 2.6 * _scaleX;
                var radiusEast = (0.92 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 120 + id, 1, 0, 0) * 0.32) * _scaleX;
                var radiusNorth = (0.82 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 130 + id, 1, 0, 0) * 0.35) * _scaleY;
                var crown = UnderlyingElevation(east, north)
                    + 24 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 140 + id, 1, 0, 0) * 8;
                var phase = CounterRandom.Unit(seed, CounterRandom.TerrainTag, 150 + id, 1, 0, 0) * Math.PI * 2;
                seededOutcrops.Add(CreateOutcrop(featureIndex, east, north, radiusEast, radiusNorth, crown, phase));
            }

            _outcrops = seededOutcrops.ToArray();
        }

        public double Elevation(double x, double y)
        {
            foreach (var outcrop in _outcrops)
                if (OutcropDistance(x, y, outcrop) <= 1 + 1e-10)
                    return outcrop.CrownElevationM;
            return UnderlyingElevation(x, y);
        }

        public double UnderlyingElevation(double x, double y)
        {
            var normalizedX = x / _scaleX;
            var normalizedY = y / _scaleY;
            var broad = _broad.Sample(normalizedX, normalizedY) * 2 - 1;
            var regional = _regional.Sample(normalizedX, normalizedY) * 2 - 1;
            var detail = _detail.Sample(normalizedX, normalizedY) * 2 - 1;
            var valleyCenter = RiverCenter(y);
            var valleyDistance = x - valleyCenter;
            var valleyInfluence = Math.Exp(-Math.Pow(valleyDistance / (13.5 * _scaleX), 2));
            var noiseFactor = 1 - 0.82 * valleyInfluence;
            var height = 62 + (broad * 15 + regional * 8 + detail * 2.8) * noiseFactor;

            // Main uplift axes are seeded independently from the river meander. Broad flanks, a narrow
            // crest, segmented massifs, and tapered saddles create connected but non-parallel ranges.
            foreach (var range in _mountainRanges)
            {
                var axis = MountainAxis(range, y);
                var across = x - axis;
                var shoulder = Math.Exp(-Math.Pow(across / range.WidthCells, 2));
                var crest = Math.Exp(-Math.Pow(across / (range.WidthCells * 0.24), 2));
                var brokenMassif = 0.54 + 0.46 * SmoothStep(0.16, 0.88,
                    _mountainMask.Sample((normalizedX + range.Phase * 2.5) * 0.38,
                        (normalizedY + range.Phase) * 0.42));
                var summitRhythm = 0.62 + 0.38 * Math.Pow(0.5 + 0.5 * Math.Sin(y * 0.34 / _scaleY + range.Phase * 1.3), 1.4);
                var rangeEnds = SmoothStep(-2 * _scaleY, 7 * _scaleY, y)
                    * (1 - SmoothStep(SimulationState.Height - 8 * _scaleY, SimulationState.Height, y));
                height += range.AmplitudeM * (0.76 * shoulder + 0.24 * crest)
                    * brokenMassif * summitRhythm * rangeEnds;

                var ridgeContinuity = 0.34 + 0.66 * SmoothStep(0.14, 0.86,
                    _ridgeMask.Sample(normalizedX * 0.35, normalizedY));
                var innerFold = axis + (range.Index == 0 ? 1 : -1) * 7.0 * _scaleX;
                height += 3.5 * ridgeContinuity * Math.Exp(-Math.Pow((x - innerFold) / (3.8 * _scaleX), 2)) * rangeEnds;
            }

            // Lower, narrower tributary ridges extend from the mountain shoulders toward the valley.
            // D8 drainage is derived only after these physical elevations have been combined.
            foreach (var spur in _ridgeSpurs)
            {
                if (x < spur.MinEast || x > spur.MaxEast || y < spur.MinNorth || y > spur.MaxNorth) continue;
                var firstDistanceSquared = DistanceSquaredToSegment(x, y, spur.StartEast, spur.StartNorth,
                    spur.BendEast, spur.BendNorth, out var firstT);
                var secondDistanceSquared = DistanceSquaredToSegment(x, y, spur.BendEast, spur.BendNorth,
                    spur.EndEast, spur.EndNorth, out var secondT);
                var distanceSquared = Math.Min(firstDistanceSquared, secondDistanceSquared);
                var progress = firstDistanceSquared <= secondDistanceSquared ? firstT * 0.5 : 0.5 + secondT * 0.5;
                var taper = 1 - 0.48 * SmoothStep(0.68, 1, progress);
                height += spur.AmplitudeM * Math.Exp(-distanceSquared / spur.WidthSquared) * taper;
            }

            // A wide lowland carries the meander; a shallow narrow incision keeps the river in its valley.
            height -= 22 * valleyInfluence;
            height -= 8 * Math.Exp(-Math.Pow(valleyDistance / (2.6 * _scaleX), 2));
            height += (y - (SimulationState.Height - 1) * 0.5) * 0.42; // the valley falls from the southern edge toward the north

            // Small, continuous rock and soil undulations break the long smooth flanks. Keep them
            // subdued in the wet valley floor so drainage remains legible; the renderer samples
            // this same physical field at a finer resolution than the ecological grid.
            var meso = _mesoRelief.Sample(normalizedX, normalizedY) * 2 - 1;
            var micro = _microRelief.Sample(normalizedX, normalizedY) * 2 - 1;
            var reliefMask = noiseFactor * (0.22 + 0.78 * SmoothStep(50, 112, height));
            height += (meso * 4.8 + micro * 2.6) * reliefMask;
            return Math.Clamp(height, 8, 340);
        }

        public IReadOnlyList<PlateauGeometry> OutcropGeometries(int rimSegments)
        {
            var features = new PlateauGeometry[_outcrops.Length];
            for (var featureIndex = 0; featureIndex < _outcrops.Length; featureIndex++)
            {
                var feature = _outcrops[featureIndex];
                var rim = new TerrainRimPoint[rimSegments];
                for (var segment = 0; segment < rimSegments; segment++)
                {
                    var angle = 2 * Math.PI * segment / rimSegments;
                    var shape = BoundaryScale(angle, feature);
                    var x = feature.CenterEastCell + Math.Cos(angle) * feature.RadiusEastCells * shape;
                    var y = feature.CenterNorthCell + Math.Sin(angle) * feature.RadiusNorthCells * shape;
                    rim[segment] = new TerrainRimPoint(x, y, feature.CrownElevationM, UnderlyingElevation(x, y));
                }

                features[featureIndex] = new PlateauGeometry(feature.Index,
                    feature.CenterEastCell, feature.CenterNorthCell, Array.AsReadOnly(rim));
            }

            return Array.AsReadOnly(features);
        }

        private OutcropFeature CreateOutcrop(
            int index,
            double centerEastCell,
            double centerNorthCell,
            double radiusEastCells,
            double radiusNorthCells,
            double baseCrownElevationM,
            double phase)
        {
            var originalRadiusEast = radiusEastCells;
            var originalRadiusNorth = radiusNorthCells;
            // Preserve the seeded location and shrink only as much as needed to keep every point
            // on the physical rim within the wall-height limit.
            for (var scale = 1.0; scale >= 0.1; scale -= 0.025)
            {
                radiusEastCells = originalRadiusEast * scale;
                radiusNorthCells = originalRadiusNorth * scale;
                var feature = new OutcropFeature(index, centerEastCell, centerNorthCell,
                    radiusEastCells, radiusNorthCells, baseCrownElevationM, phase);
                var minimumRimToe = double.PositiveInfinity;
                var maximumRimToe = double.NegativeInfinity;
                for (var segment = 0; segment < 160; segment++)
                {
                    var angle = 2 * Math.PI * segment / 160;
                    var shape = BoundaryScale(angle, feature);
                    var x = centerEastCell + Math.Cos(angle) * radiusEastCells * shape;
                    var y = centerNorthCell + Math.Sin(angle) * radiusNorthCells * shape;
                    var toe = UnderlyingElevation(x, y);
                    minimumRimToe = Math.Min(minimumRimToe, toe);
                    maximumRimToe = Math.Max(maximumRimToe, toe);
                }

                if (maximumRimToe - minimumRimToe > MaximumPlateauWallM - MinimumPlateauReliefM)
                    continue;
                if (centerEastCell - radiusEastCells < 0 || centerNorthCell - radiusNorthCells < 0
                    || centerEastCell + radiusEastCells >= SimulationState.Width
                    || centerNorthCell + radiusNorthCells >= SimulationState.Height)
                    continue;

                return feature with
                {
                    CrownElevationM = Math.Min(340, Math.Clamp(baseCrownElevationM,
                        maximumRimToe + MinimumPlateauReliefM, minimumRimToe + MaximumPlateauWallM))
                };
            }

            throw new InvalidOperationException($"Could not place outcrop {index} with walls no higher than {MaximumPlateauWallM} m.");
        }

        private double OutcropDistance(double x, double y, OutcropFeature feature)
        {
            var normalizedX = (x - feature.CenterEastCell) / feature.RadiusEastCells;
            var normalizedY = (y - feature.CenterNorthCell) / feature.RadiusNorthCells;
            var angle = Math.Atan2(normalizedY, normalizedX);
            return Math.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY) / BoundaryScale(angle, feature);
        }

        private double BoundaryScale(double angle, OutcropFeature feature)
            => 1 + 0.12 * Math.Sin(angle * 3 + feature.Phase)
                + 0.065 * Math.Sin(angle * 5 - feature.Phase * 0.63)
                + 0.035 * Math.Sin(angle * 7 + feature.Phase * 0.41)
                + 0.02 * Math.Sin(angle * 11 - feature.Phase * 0.27)
                + 0.024 * Math.Sin(angle * 13 + feature.Phase * 0.81)
                + 0.016 * Math.Sin(angle * 19 - feature.Phase * 0.48)
                + 0.08 * (_rimDetail.Sample(20 + feature.Index * 13 + Math.Cos(angle) * 7,
                    20 + feature.Index * 11 + Math.Sin(angle) * 7) - 0.5);

        private double RiverCenter(double y)
            => SimulationState.Width * (34.0 / 64.0)
                + 8.5 * _scaleX * Math.Sin(y * 0.105 / _scaleY + _riverPhase)
                + 3.2 * _scaleX * Math.Sin(y * 0.29 / _scaleY + _riverPhase * 0.63);

        private double MountainAxis(MountainRange range, double y)
        {
            var axisNoiseX = range.Index == 0 ? 8 : 36;
            var wandering = (_rangeAxis.Sample(axisNoiseX, y * 0.72) - 0.5) * 7.0 * _scaleX;
            return range.AnchorEastCells + range.AxisSlope * (y - (SimulationState.Height - 1) * 0.5) * _scaleX / _scaleY
                + 1.8 * _scaleX * Math.Sin(y * 0.14 / _scaleY + range.Phase)
                + 0.8 * _scaleX * Math.Sin(y * 0.33 / _scaleY + range.Phase * 0.71)
                + wandering;
        }

        private RidgeSpur[] CreateRidgeSpurs(ulong seed)
        {
            var spurs = new RidgeSpur[_mountainRanges.Length * 3];
            var index = 0;
            foreach (var range in _mountainRanges)
            for (var branch = 0; branch < 3; branch++)
            {
                var id = (ulong)(range.Index * 3 + branch);
                var random = CounterRandom.Unit(seed, CounterRandom.TerrainTag, 80 + id, 1, 0, 0);
                var bend = CounterRandom.Unit(seed, CounterRandom.TerrainTag, 90 + id, 1, 0, 0);
                var direction = branch == 2 ? -1.0 : 1.0;
                var startNorth = branch switch
                {
                    0 => (10 + random * 4) * _scaleY,
                    1 => (29 + random * 4) * _scaleY,
                    _ => (51 - random * 4) * _scaleY
                };
                var endNorth = Math.Clamp(startNorth + direction * (11 + bend * 5) * _scaleY,
                    3 * _scaleY, (SimulationState.Height - 4) * _scaleY);
                var valleySide = range.Index == 0 ? -1 : 1;
                var startEast = MountainAxis(range, startNorth)
                    + (range.Index == 0 ? 1 : -1) * range.WidthCells * 0.72;
                var endEast = RiverCenter(endNorth) + valleySide * (5.5 + bend * 2.5) * _scaleX;
                var bendEast = (startEast + endEast) * 0.5
                    + (range.Index == 0 ? 1 : -1) * (bend - 0.5) * 5.0 * _scaleX;
                var bendNorth = (startNorth + endNorth) * 0.5 + (random - 0.5) * 4.0 * _scaleY;
                var width = (1.65 + random * 0.7) * _scaleX;
                var amplitude = 24 + bend * 12;
                spurs[index++] = new RidgeSpur(startEast, startNorth, bendEast, bendNorth,
                    endEast, endNorth, width * width, amplitude,
                    Math.Min(startEast, Math.Min(bendEast, endEast)) - width * 4,
                    Math.Max(startEast, Math.Max(bendEast, endEast)) + width * 4,
                    Math.Min(startNorth, Math.Min(bendNorth, endNorth)) - width * 4,
                    Math.Max(startNorth, Math.Max(bendNorth, endNorth)) + width * 4);
            }

            return spurs;
        }

        private static double DistanceSquaredToSegment(
            double x, double y, double startX, double startY, double endX, double endY, out double position)
        {
            var dx = endX - startX;
            var dy = endY - startY;
            var lengthSquared = dx * dx + dy * dy;
            position = lengthSquared <= 1e-12
                ? 0
                : Math.Clamp(((x - startX) * dx + (y - startY) * dy) / lengthSquared, 0, 1);
            var offsetX = x - (startX + dx * position);
            var offsetY = y - (startY + dy * position);
            return offsetX * offsetX + offsetY * offsetY;
        }

        private readonly record struct MountainRange(
            int Index,
            double AnchorEastCells,
            double AxisSlope,
            double WidthCells,
            double AmplitudeM,
            double Phase);

        private readonly record struct RidgeSpur(
            double StartEast,
            double StartNorth,
            double BendEast,
            double BendNorth,
            double EndEast,
            double EndNorth,
            double WidthSquared,
            double AmplitudeM,
            double MinEast,
            double MaxEast,
            double MinNorth,
            double MaxNorth);

        private readonly record struct OutcropFeature(
            int Index,
            double CenterEastCell,
            double CenterNorthCell,
            double RadiusEastCells,
            double RadiusNorthCells,
            double CrownElevationM,
            double Phase);

        private static double SmoothStep(double edge0, double edge1, double value)
        {
            var t = Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1);
            return t * t * (3 - 2 * t);
        }

    }

    private sealed class ValueNoise
    {
        private readonly double[,] _values;
        private readonly int _spacing;

        public ValueNoise(ulong seed, int spacing, ulong salt)
        {
            _spacing = spacing;
            var columns = SimulationState.Width / spacing + 2;
            var rows = SimulationState.Height / spacing + 2;
            _values = new double[columns, rows];
            for (var y = 0; y < rows; y++)
            for (var x = 0; x < columns; x++)
                _values[x, y] = CounterRandom.Unit(seed, salt, (ulong)x, (ulong)y, 0, 0);
        }

        public double Sample(double x, double y)
        {
            var fx = Math.Clamp(x / _spacing, 0, _values.GetLength(0) - 1.000001);
            var fy = Math.Clamp(y / _spacing, 0, _values.GetLength(1) - 1.000001);
            var x0 = (int)fx;
            var y0 = (int)fy;
            var tx = Fade(fx - x0);
            var ty = Fade(fy - y0);
            var top = _values[x0, y0] + (_values[x0 + 1, y0] - _values[x0, y0]) * tx;
            var bottom = _values[x0, y0 + 1] + (_values[x0 + 1, y0 + 1] - _values[x0, y0 + 1]) * tx;
            return top + (bottom - top) * ty;
        }

        private static double Fade(double value) => value * value * (3 - 2 * value);
    }
}
