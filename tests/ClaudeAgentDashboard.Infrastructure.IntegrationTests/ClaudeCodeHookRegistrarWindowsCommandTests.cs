using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json.Nodes;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Infrastructure.Hooks;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

// NOTE: Claude Code spawns hook commands through an intermediary shell (cmd.exe on Windows),
// not by handing the string directly to CreateProcess. A command string that runs fine when
// pasted straight into a PowerShell prompt can still be mangled by cmd.exe's own quote/paren
// parsing on the way there — which is exactly what happened in production (nested single
// quotes inside "-Command \"...\"" broke at the first "(" after cmd.exe re-parsed it). This
// test reproduces that real invocation path (cmd.exe /c <generated command>) rather than
// invoking PowerShell directly, so it actually catches that class of bug.
[SupportedOSPlatform("windows")]
public class ClaudeCodeHookRegistrarWindowsCommandTests
{
    [SkippableFact]
    public async Task Generated_Windows_Command_Successfully_Posts_The_Piped_Payload_Via_Cmd()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        using var listener = new HookEventListener(preferredPort: 51950);
        var received = new TaskCompletionSource<ActivitySignal>();
        listener.SignalReceived += signal => received.TrySetResult(signal);

        var settingsPath = Path.Combine(Path.GetTempPath(), $"claude-settings-{Guid.NewGuid()}.json");
        try
        {
            var registrar = new ClaudeCodeHookRegistrar(settingsPath);
            registrar.RegisterHooks(listener.BaseAddress);
            var command = ReadCommand(settingsPath, "Stop");

            var startInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo)!;
            await process.StandardInput.WriteAsync("""{"cwd":"C:\\work\\my-project"}""");
            process.StandardInput.Close();
            var stderr = await process.StandardError.ReadToEndAsync();

            var exited = process.WaitForExit(10_000);
            Assert.True(exited, "hook process did not exit in time");
            Assert.True(process.ExitCode == 0, $"hook process failed (exit {process.ExitCode}): {stderr}");

            var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.Same(received.Task, completed);

            var signal = await received.Task;
            Assert.Equal("C:\\work\\my-project", signal.CorrelationKey);
        }
        finally
        {
            File.Delete(settingsPath);
        }
    }

    private static string ReadCommand(string settingsPath, string hookEventName)
    {
        var root = JsonNode.Parse(File.ReadAllText(settingsPath))!.AsObject();
        var entries = root["hooks"]!.AsObject()[hookEventName]!.AsArray();
        return entries[0]!["hooks"]!.AsArray()[0]!["command"]!.GetValue<string>();
    }
}
