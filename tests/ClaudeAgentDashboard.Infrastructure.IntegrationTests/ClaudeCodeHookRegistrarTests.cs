using System.Text.Json.Nodes;
using ClaudeAgentDashboard.Infrastructure.Hooks;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

public class ClaudeCodeHookRegistrarTests
{
    private static readonly string[] ExpectedHookEvents = ["UserPromptSubmit", "PreToolUse", "Stop", "Notification", "SessionEnd"];

    [Fact]
    public void RegisterHooks_Writes_Commands_For_All_Five_Events()
    {
        var path = TempSettingsPath();
        try
        {
            var registrar = new ClaudeCodeHookRegistrar(path);

            registrar.RegisterHooks(new Uri("http://127.0.0.1:51820/"));

            Assert.True(registrar.AreHooksRegistered());
            var hooks = ReadHooksObject(path);
            foreach (var hookEvent in ExpectedHookEvents)
            {
                Assert.True(hooks.ContainsKey(hookEvent), $"expected a '{hookEvent}' entry");
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RegisterHooks_Is_Idempotent_Not_Duplicating_Entries()
    {
        var path = TempSettingsPath();
        try
        {
            var registrar = new ClaudeCodeHookRegistrar(path);

            registrar.RegisterHooks(new Uri("http://127.0.0.1:51820/"));
            registrar.RegisterHooks(new Uri("http://127.0.0.1:51820/"));

            var hooks = ReadHooksObject(path);
            var stopEntries = hooks["Stop"]!.AsArray();
            Assert.Single(stopEntries);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RegisterHooks_Preserves_Unrelated_Existing_Settings()
    {
        var path = TempSettingsPath();
        try
        {
            File.WriteAllText(path, """
                {
                  "someOtherSetting": true,
                  "hooks": {
                    "SomeOtherTool": [ { "hooks": [ { "type": "command", "command": "echo hi" } ] } ]
                  }
                }
                """);
            var registrar = new ClaudeCodeHookRegistrar(path);

            registrar.RegisterHooks(new Uri("http://127.0.0.1:51820/"));

            var root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            Assert.True(root["someOtherSetting"]!.GetValue<bool>());
            var hooks = root["hooks"]!.AsObject();
            Assert.True(hooks.ContainsKey("SomeOtherTool"));
            Assert.True(hooks.ContainsKey("Stop"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AreHooksRegistered_Is_False_Before_And_True_After_Registration()
    {
        var path = TempSettingsPath();
        try
        {
            var registrar = new ClaudeCodeHookRegistrar(path);

            Assert.False(registrar.AreHooksRegistered());

            registrar.RegisterHooks(new Uri("http://127.0.0.1:51820/"));

            Assert.True(registrar.AreHooksRegistered());
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string TempSettingsPath() => Path.Combine(Path.GetTempPath(), $"claude-settings-{Guid.NewGuid()}.json");

    private static JsonObject ReadHooksObject(string path) =>
        JsonNode.Parse(File.ReadAllText(path))!.AsObject()["hooks"]!.AsObject();
}
