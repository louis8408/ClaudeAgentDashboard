using System.Reflection;
using NetArchTest.Rules;

namespace ClaudeAgentDashboard.Architecture.Tests;

public class LayeringTests
{
    private const string DomainNamespace = "ClaudeAgentDashboard.Domain";
    private const string ApplicationNamespace = "ClaudeAgentDashboard.Application";
    private const string InfrastructureNamespace = "ClaudeAgentDashboard.Infrastructure";
    private const string PresentationNamespace = "ClaudeAgentDashboard.Presentation";

    // Loaded by simple name rather than via typeof() on a specific type, so this
    // file compiles and the guard is active even before any real Domain/Application/
    // Infrastructure/Presentation types exist yet.
    private static Assembly LoadByName(string assemblyName) => Assembly.Load(assemblyName);

    [Fact]
    public void Domain_Should_Not_Depend_On_Outer_Layers()
    {
        var result = Types.InAssembly(LoadByName(DomainNamespace))
            .Should()
            .NotHaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, PresentationNamespace, "Avalonia")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_Should_Only_Depend_On_Domain()
    {
        var result = Types.InAssembly(LoadByName(ApplicationNamespace))
            .Should()
            .NotHaveDependencyOnAny(InfrastructureNamespace, PresentationNamespace, "Avalonia")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Infrastructure_Should_Only_Be_Referenced_From_The_Composition_Root()
    {
        var result = Types.InAssembly(LoadByName(PresentationNamespace))
            .That()
            .DoNotHaveNameMatching("CompositionRoot")
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string Describe(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
