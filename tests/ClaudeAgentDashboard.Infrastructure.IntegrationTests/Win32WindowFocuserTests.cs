using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;
using ClaudeAgentDashboard.Infrastructure.Windows;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

[SupportedOSPlatform("windows")]
public class Win32WindowFocuserTests
{
    private const int SW_MINIMIZE = 6;

    [SkippableFact]
    public void Focus_Restores_A_Minimized_Window_And_Reports_Focused()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        using var notepad = StartNotepad();
        try
        {
            var handle = WaitForMainWindowHandle(notepad);
            ShowWindow(handle, SW_MINIMIZE);
            WaitUntil(() => IsIconic(handle));
            Assert.True(IsIconic(handle));

            var focuser = new Win32WindowFocuser();
            var reference = new TerminalWindowReference(notepad.Id);

            var result = focuser.Focus(reference);

            Assert.Equal(FocusResult.Focused, result);
            Assert.True(reference.IsResolvable);
            WaitUntil(() => !IsIconic(handle));
            Assert.False(IsIconic(handle));
        }
        finally
        {
            TryKill(notepad);
        }
    }

    [SkippableFact]
    public void Focus_Reports_WindowNoLongerAvailable_After_Process_Exits()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        using var notepad = StartNotepad();
        WaitForMainWindowHandle(notepad);
        var reference = new TerminalWindowReference(notepad.Id);

        notepad.Kill();
        notepad.WaitForExit(5000);

        var focuser = new Win32WindowFocuser();
        var result = focuser.Focus(reference);

        Assert.Equal(FocusResult.WindowNoLongerAvailable, result);
        Assert.False(reference.IsResolvable);
    }

    [SkippableFact]
    public void Focus_Returns_NotAvailable_Without_Attempting_OS_Calls_When_Already_Unresolvable()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        var reference = new TerminalWindowReference(int.MaxValue);
        reference.MarkUnresolvable();

        var focuser = new Win32WindowFocuser();
        var result = focuser.Focus(reference);

        Assert.Equal(FocusResult.WindowNoLongerAvailable, result);
    }

    // charmap.exe (Character Map) is used instead of notepad.exe: on modern Windows,
    // "notepad.exe" resolves to an MSIX-packaged app whose launching process is a stub
    // that hands off to a differently-PID'd process, so the returned Process never gets
    // a MainWindowHandle. charmap.exe is a genuine classic Win32 GUI app present on all
    // supported Windows versions, with no elevation required.
    private static Process StartNotepad() =>
        Process.Start(new ProcessStartInfo("charmap.exe") { UseShellExecute = true })
        ?? throw new InvalidOperationException("Failed to start charmap.exe.");

    private static IntPtr WaitForMainWindowHandle(Process process)
    {
        process.WaitForInputIdle(5000);

        for (var i = 0; i < 50; i++)
        {
            process.Refresh();
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return process.MainWindowHandle;
            }

            Thread.Sleep(100);
        }

        throw new TimeoutException("notepad.exe never produced a main window handle.");
    }

    private static void WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 50; i++)
        {
            if (condition())
            {
                return;
            }

            Thread.Sleep(100);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Already exited between the check and the kill — fine.
        }
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);
}
