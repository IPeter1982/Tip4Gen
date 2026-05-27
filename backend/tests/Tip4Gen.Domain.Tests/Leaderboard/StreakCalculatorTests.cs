using Tip4Gen.Domain.Leaderboard;

namespace Tip4Gen.Domain.Tests.Leaderboard;

public class StreakCalculatorTests
{
    [Fact]
    public void Empty_sequence_is_zero()
    {
        Assert.Equal(0, StreakCalculator.LongestStreak(Array.Empty<int>()));
    }

    [Fact]
    public void All_below_threshold_is_zero()
    {
        Assert.Equal(0, StreakCalculator.LongestStreak(new[] { 0, 1, 2, 2, 0 }));
    }

    [Fact]
    public void Threshold_is_inclusive_at_3()
    {
        Assert.Equal(1, StreakCalculator.LongestStreak(new[] { 3 }));
        Assert.Equal(0, StreakCalculator.LongestStreak(new[] { 2 }));
    }

    [Fact]
    public void Single_uninterrupted_run_returns_its_length()
    {
        Assert.Equal(4, StreakCalculator.LongestStreak(new[] { 5, 3, 10, 7 }));
    }

    [Fact]
    public void Run_is_broken_by_below_threshold_value()
    {
        // 3, 5 (run=2), 0 (resets), 4, 6, 3 (run=3) → 3 wins
        Assert.Equal(3, StreakCalculator.LongestStreak(new[] { 3, 5, 0, 4, 6, 3 }));
    }

    [Fact]
    public void Longest_of_multiple_runs_wins()
    {
        // 5,5 (2), 0, 3,3,3,3,3 (5), 1, 10 (1) → 5
        Assert.Equal(5, StreakCalculator.LongestStreak(new[] { 5, 5, 0, 3, 3, 3, 3, 3, 1, 10 }));
    }

    [Fact]
    public void Rejects_null_input()
    {
        Assert.Throws<ArgumentNullException>(() => StreakCalculator.LongestStreak(null!));
    }
}
