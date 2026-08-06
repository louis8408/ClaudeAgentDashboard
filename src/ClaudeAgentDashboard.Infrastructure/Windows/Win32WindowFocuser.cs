using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.Windows;

/// <summary>
/// Brings a terminal window to the foreground on Windows via user32.dll P/Invoke
/// (research.md R4), applying the standard AttachThreadInput workaround for the
/// foreground-lock restriction.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class Win32WindowFocuser : IWindowFocuser
{
    private const int SwRestore = 9;

    public FocusResult Focus(TerminalWindowReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        if (!reference.IsResolvable)
        {
            return FocusResult.WindowNoLongerAvailable;
        }

        var targetWindow = FindTopLevelWindow(reference.OwningProcessId);
        if (targetWindow == IntPtr.Zero)
        {
            reference.MarkUnresolvable();
            return FocusResult.WindowNoLongerAvailable;
        }

        BringToForeground(targetWindow);
        return FocusResult.Focused;
    }

    private static IntPtr FindTopLevelWindow(int processId)
    {
        var found = IntPtr.Zero;

        EnumWindows((hWnd, _) =>
        {
            GetWindowThreadProcessId(hWnd, out var windowProcessId);
            if (windowProcessId != (uint)processId || !IsWindowVisible(hWnd))
            {
                return true;
            }

            found = hWnd;
            return false;
        }, IntPtr.Zero);

        return found;
    }

    private static void BringToForeground(IntPtr hWnd)
    {
        if (IsIconic(hWnd))
        {
            ShowWindow(hWnd, SwRestore);
        }

        var foregroundWindow = GetForegroundWindow();
        var foregroundThreadId = foregroundWindow == IntPtr.Zero ? 0u : GetWindowThreadProcessId(foregroundWindow, out _);
        var currentThreadId = GetCurrentThreadId();

        var attached = foregroundThreadId != 0
            && foregroundThreadId != currentThreadId
            && AttachThreadInput(currentThreadId, foregroundThreadId, true);

        try
        {
            SetForegroundWindow(hWnd);
        }
        finally
        {
            if (attached)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
        }
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}
