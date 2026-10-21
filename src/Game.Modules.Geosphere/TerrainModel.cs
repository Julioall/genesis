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

    public static (double East, double North) Gradient(WorldCell[] cells, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(cells);
        if (cells.Length != SimulationState.Width * SimulationState.Height)
            throw new ArgumentException("Terrain gradients need one elevation per simulation cell.", nameof(cells));

        double At(int px, int py) => cells[py * SimulationState.Width + px].ElevationM;
        var gradient = GridGradient(At, x, y);
        var cellSizeM = Math.Sqrt(SimulationState.CellAreaM2);
        return (gradient.East / cellSizeM, gradient.North / cellSizeM);
    }

    public static double SlopeMagnitude(double[] elevations, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(elevations);
        if (elevations.Length != SimulationState.Width * SimulationState.Height)
            throw new ArgumentException("Terrain slope needs one elevation per simulation cell.", nameof(elevations));
        if (x < 0 || x >= SimulationState.Width || y < 0 || y >= SimulationState.Height)
            throw new ArgumentOutOfRangeException(nameof(x), "Slope coordinates must be inside the terrain grid.");

        double At(int px, int py) => elevations[py * SimulationState.Width + px];
        var gradient = GridGradient(At, x, y);
        var cellSizeM = Math.Sqrt(SimulationState.CellAreaM2);
        return Math.Sqrt(gradient.East * gradient.East + gradient.North * gradient.North) / cellSizeM;
    }

    public static double TerrainSolarFactor(WorldCell[] cells, WorldCell cell, double sunEast, double sunNorth, double sunUp)
    {
        var gradient = Gradient(cells, cell.X, cell.Y);
        var length = Math.Sqrt(gradient.East * gradient.East + gradient.North * gradient.North + 1);
        var normalEast = -gradient.East / length;
        var normalNorth = -gradient.North / length;
        var normalUp = 1 / length;
        if (sunUp <= 0) return 0;
        return Math.Clamp(Math.Max(0, normalEast * sunEast + normalNorth * sunNorth + normalUp * sunUp)
                          / Math.Max(sunUp, 0.05), 0, 1.2);
    }

    private static (double East, double North) GridGradient(Func<int, int, double> at, int x, int y)
    {
        if (x < 0 || x >= SimulationState.Width || y < 0 || y >= SimulationState.Height)
            throw new ArgumentOutOfRangeException(nameof(x), "Gradient coordinates must be inside the terrain grid.");
        var east = x == 0 ? at(1, y) - at(0, y)
            : x == SimulationState.Width - 1 ? at(x, y) - at(x - 1, y)
            : (at(x + 1, y) - at(x - 1, y)) / 2;
        var north = y == 0 ? at(x, 1) - at(x, 0)
            : y == SimulationState.Height - 1 ? at(x, y) - at(x, y - 1)
            : (at(x, y + 1) - at(x, y - 1)) / 2;
        return (east, north);
    }

    private sealed class TerrainGenerator
    {
        private const double MinimumPlateauReliefM = 18;
        private readonly ValueNoise _broad;
        private readonly ValueNoise _regional;
        private readonly ValueNoise _detail;
        private readonly ValueNoise _mesoRelief;
        private readonly ValueNoise _microRelief;
        private readonly ValueNoise _ridgeMask;
        private readonly ValueNoise _mountainMask;
        private readonly ValueNoise _rimDetail;
        private readonly MountainRange[] _mountainRanges;
        private readonly double _riverPhase;
        private readonly OutcropFeature[] _outcrops;

        public TerrainGenerator(ulong seed)
        {
            _broad = new ValueNoise(seed, 26, 0x5445525241494E31UL);
            _regional = new ValueNoise(seed, 12, 0x5445525241494E32UL);
            _detail = new ValueNoise(seed, 4, 0x5445525241494E33UL);
            _mesoRelief = new ValueNoise(seed, 2, 0x4D45534F52454C46UL);
            _microRelief = new ValueNoise(seed, 1, 0x4D4943524F524C46UL);
            _ridgeMask = new ValueNoise(seed, 10, 0x52494447454D4153UL);
            _mountainMask = new ValueNoise(seed, 8, 0x4D4F554E5441494EUL);
            _rimDetail = new ValueNoise(seed, 2, 0x504C415445415552UL);
            _riverPhase = CounterRandom.Unit(seed, CounterRandom.TerrainTag, 40, 1, 0, 0) * Math.PI * 2;
            var westPhase = CounterRandom.Unit(seed, CounterRandom.TerrainTag, 41, 1, 0, 0) * Math.PI * 2;
            var eastPhase = CounterRandom.Unit(seed, CounterRandom.TerrainTag, 48, 1, 0, 0) * Math.PI * 2;
            _mountainRanges =
            [
                new MountainRange(
                    -(18.5 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 49, 1, 0, 0) * 3),
                    -0.12 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 50, 1, 0, 0) * 0.24,
                    4.6 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 51, 1, 0, 0) * 1.0,
                    100 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 52, 1, 0, 0) * 14,
                    westPhase),
                new MountainRange(
                    18.5 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 53, 1, 0, 0) * 3,
                    -0.12 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 54, 1, 0, 0) * 0.24,
                    4.0 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 55, 1, 0, 0) * 0.9,
                    78 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 56, 1, 0, 0) * 10,
                    eastPhase)
            ];
            var plateauX = 43 + (CounterRandom.Unit(seed, CounterRandom.TerrainTag, 42, 1, 0, 0) - 0.5) * 8;
            var plateauY = 18 + (CounterRandom.Unit(seed, CounterRandom.TerrainTag, 43, 1, 0, 0) - 0.5) * 6;
            var plateauRadiusX = 2.9 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 44, 1, 0, 0) * 0.9;
            var plateauRadiusY = 1.9 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 45, 1, 0, 0) * 0.7;
            var plateauHeight = 94 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 46, 1, 0, 0) * 8;
            var plateauPhase = CounterRandom.Unit(seed, CounterRandom.TerrainTag, 47, 1, 0, 0) * Math.PI * 2;
            _outcrops =
            [
                CreateOutcrop(0, plateauX, plateauY, plateauRadiusX, plateauRadiusY, plateauHeight, plateauPhase),
                CreateOutcrop(1,
                    15 + (CounterRandom.Unit(seed, CounterRandom.TerrainTag, 57, 1, 0, 0) - 0.5) * 3,
                    17 + (CounterRandom.Unit(seed, CounterRandom.TerrainTag, 58, 1, 0, 0) - 0.5) * 4,
                    1.15 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 59, 1, 0, 0) * 0.55,
                    1.05 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 60, 1, 0, 0) * 0.55,
                    126 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 61, 1, 0, 0) * 8,
                    CounterRandom.Unit(seed, CounterRandom.TerrainTag, 62, 1, 0, 0) * Math.PI * 2),
                CreateOutcrop(2,
                    57 + (CounterRandom.Unit(seed, CounterRandom.TerrainTag, 63, 1, 0, 0) - 0.5) * 3,
                    19 + (CounterRandom.Unit(seed, CounterRandom.TerrainTag, 64, 1, 0, 0) - 0.5) * 4,
                    1.05 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 65, 1, 0, 0) * 0.6,
                    1.15 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 66, 1, 0, 0) * 0.55,
                    128 + CounterRandom.Unit(seed, CounterRandom.TerrainTag, 67, 1, 0, 0) * 8,
                    CounterRandom.Unit(seed, CounterRandom.TerrainTag, 68, 1, 0, 0) * Math.PI * 2)
            ];
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
            var broad = _broad.Sample(x, y) * 2 - 1;
            var regional = _regional.Sample(x, y) * 2 - 1;
            var detail = _detail.Sample(x, y) * 2 - 1;
            var valleyCenter = RiverCenter(y);
            var valleyDistance = x - valleyCenter;
            var valleyInfluence = Math.Exp(-Math.Pow(valleyDistance / 13.5, 2));
            var noiseFactor = 1 - 0.82 * valleyInfluence;
            var height = 62 + (broad * 15 + regional * 8 + detail * 2.8) * noiseFactor;

            // Two seeded, curved mountain chains frame the lowland. Broad flanks, a narrow crest,
            // segmented massifs, and tapered saddles create connected ridges instead of round noise peaks.
            foreach (var range in _mountainRanges)
            {
                var axis = valleyCenter + range.OffsetFromValleyCells
                    + range.AxisSlope * (y - 31.5)
                    + 1.8 * Math.Sin(y * 0.14 + range.Phase)
                    + 0.8 * Math.Sin(y * 0.33 + range.Phase * 0.71);
                var across = x - axis;
                var shoulder = Math.Exp(-Math.Pow(across / range.WidthCells, 2));
                var crest = Math.Exp(-Math.Pow(across / (range.WidthCells * 0.24), 2));
                var brokenMassif = 0.54 + 0.46 * SmoothStep(0.16, 0.88,
                    _mountainMask.Sample((x + range.Phase * 2.5) * 0.38, (y + range.Phase) * 0.42));
                var summitRhythm = 0.62 + 0.38 * Math.Pow(0.5 + 0.5 * Math.Sin(y * 0.34 + range.Phase * 1.3), 1.4);
                var rangeEnds = SmoothStep(-2, 7, y) * (1 - SmoothStep(56, 64, y));
                height += range.AmplitudeM * (0.76 * shoulder + 0.24 * crest)
                    * brokenMassif * summitRhythm * rangeEnds;
            }

            // Smaller fold texture breaks up long uniform slopes while keeping the major forms legible.
            var ridgeContinuity = 0.34 + 0.66 * SmoothStep(0.14, 0.86, _ridgeMask.Sample(x * 0.35, y));
            height += 5 * ridgeContinuity * Math.Exp(-Math.Pow((x - valleyCenter - 12) / 4.8, 2));
            height += 4 * ridgeContinuity * Math.Exp(-Math.Pow((x - valleyCenter + 13) / 5.4, 2));

            // A wide lowland carries the meander; a shallow narrow incision keeps the river in its valley.
            height -= 22 * valleyInfluence;
            height -= 8 * Math.Exp(-Math.Pow(valleyDistance / 2.6, 2));
            height += (y - 31.5) * 0.42; // the valley falls from the southern edge toward the north

            // Small, continuous rock and soil undulations break the long smooth flanks. Keep them
            // subdued in the wet valley floor so drainage remains legible; the renderer samples
            // this same physical field at a finer resolution than the ecological grid.
            var meso = _mesoRelief.Sample(x, y) * 2 - 1;
            var micro = _microRelief.Sample(x, y) * 2 - 1;
            var reliefMask = noiseFactor * (0.22 + 0.78 * SmoothStep(50, 112, height));
            height += (meso * 4.8 + micro * 2.6) * reliefMask;
            return Math.Clamp(height, 8, 190);
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
            var feature = new OutcropFeature(index, centerEastCell, centerNorthCell,
                radiusEastCells, radiusNorthCells, baseCrownElevationM, phase);
            var highestRimToe = double.NegativeInfinity;
            for (var segment = 0; segment < 160; segment++)
            {
                var angle = 2 * Math.PI * segment / 160;
                var shape = BoundaryScale(angle, feature);
                var x = centerEastCell + Math.Cos(angle) * radiusEastCells * shape;
                var y = centerNorthCell + Math.Sin(angle) * radiusNorthCells * shape;
                highestRimToe = Math.Max(highestRimToe, UnderlyingElevation(x, y));
            }

            return feature with
            {
                CrownElevationM = Math.Min(190, Math.Max(baseCrownElevationM, highestRimToe + MinimumPlateauReliefM))
            };
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
            => 34 + 8.5 * Math.Sin(y * 0.105 + _riverPhase)
                + 3.2 * Math.Sin(y * 0.29 + _riverPhase * 0.63);

        private readonly record struct MountainRange(
            double OffsetFromValleyCells,
            double AxisSlope,
            double WidthCells,
            double AmplitudeM,
            double Phase);

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
