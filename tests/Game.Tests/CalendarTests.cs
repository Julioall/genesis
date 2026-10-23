using Game.Contracts;

namespace Game.Tests;

public sealed class CalendarTests
{
    [Theory]
    [InlineData(0L, 79L * SimulationState.TicksPerDay)]
    [InlineData(79L * SimulationState.TicksPerDay, 171L * SimulationState.TicksPerDay)]
    [InlineData(171L * SimulationState.TicksPerDay, 263L * SimulationState.TicksPerDay)]
    [InlineData(263L * SimulationState.TicksPerDay, 354L * SimulationState.TicksPerDay)]
    [InlineData(354L * SimulationState.TicksPerDay, 444L * SimulationState.TicksPerDay)]
    [InlineData(79L * SimulationState.TicksPerDay + 70, 171L * SimulationState.TicksPerDay)]
    public void Next_season_boundary_is_the_start_of_the_next_season(long currentTick, long expectedTick)
    {
        Assert.Equal(expectedTick, Calendar.NextSeasonStartTick(currentTick));
    }

    [Fact]
    public void Next_season_boundary_rejects_negative_tick_indices()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Calendar.NextSeasonStartTick(-1));
    }
}
