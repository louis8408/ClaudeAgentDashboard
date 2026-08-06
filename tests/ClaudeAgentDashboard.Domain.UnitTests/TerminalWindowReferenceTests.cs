using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Domain.UnitTests;

public class TerminalWindowReferenceTests
{
    [Fact]
    public void New_Reference_Is_Resolvable()
    {
        var reference = new TerminalWindowReference(4321);

        Assert.True(reference.IsResolvable);
        Assert.Equal(4321, reference.OwningProcessId);
    }

    [Fact]
    public void MarkUnresolvable_Sets_IsResolvable_False()
    {
        var reference = new TerminalWindowReference(4321);

        reference.MarkUnresolvable();

        Assert.False(reference.IsResolvable);
    }

    [Fact]
    public void MarkUnresolvable_Is_OneWay()
    {
        var reference = new TerminalWindowReference(4321);
        reference.MarkUnresolvable();

        reference.MarkUnresolvable();

        Assert.False(reference.IsResolvable);
    }
}
