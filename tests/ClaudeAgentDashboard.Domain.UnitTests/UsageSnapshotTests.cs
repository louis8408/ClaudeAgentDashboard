using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Domain.UnitTests;

public class UsageSnapshotTests
{
    [Fact]
    public void Constructor_Computes_ContextWindowTokensAvailable_From_The_Default_Constant()
    {
        var snapshot = new UsageSnapshot(
            tokensUsed: 5_000, contextWindowTokensInUse: 1_000, readAt: DateTimeOffset.UtcNow);

        Assert.Equal(UsageSnapshot.DefaultContextWindowTokens - 1_000, snapshot.ContextWindowTokensAvailable);
    }

    [Fact]
    public void ContextWindowTokensAvailable_Floors_At_Zero_When_InUse_Exceeds_The_Default_Constant()
    {
        var snapshot = new UsageSnapshot(
            tokensUsed: 1, contextWindowTokensInUse: UsageSnapshot.DefaultContextWindowTokens + 50_000,
            readAt: DateTimeOffset.UtcNow);

        Assert.Equal(0, snapshot.ContextWindowTokensAvailable);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void Constructor_Throws_For_A_Negative_Token_Field(long tokensUsed, long contextWindowTokensInUse)
    {
        Assert.Throws<ArgumentException>(() =>
            new UsageSnapshot(tokensUsed, contextWindowTokensInUse, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void DefaultContextWindowTokens_Is_200000()
    {
        Assert.Equal(200_000, UsageSnapshot.DefaultContextWindowTokens);
    }
}
