using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.Windows;

/// <summary>
/// Brings a terminal window to the foreground on Windows via user32.dll P/Invoke
/// (research.md R4), applying the standard AttachThreadInput workaround for the
/// foreground-lock restriction.
///
/// A console app hosted inside a terminal (Windows Terminal, Visual Studio's integrated
/// terminal, VS Code, etc.) owns no top-level window of its own — confirmed empirically:
/// Claude Code's own process reports <c>MainWindowHandle == 0</c> when run under Visual
/// Studio's terminal; the actual visible window belongs to devenv.exe, an ancestor process.
/// When the target process itself owns no window, this walks up its parent chain and uses
/// the first ancestor that does — the best generically achievable outcome for an embedded
/// terminal, since the terminal pane itself isn't a distinct top-level window Win32 can find.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class Win32WindowFocuser : IWindowFocuser
{
    private const int SwRestore = 9;
    private const int MaxAncestorDepth = 8;

    private readonly Func<int, int?> _getParentProcessId;
    private readonly Func<int, bool> _processExists;

    public Win32WindowFocuser()
        : this(WmiGetParentProcessId, ProcessExists)
    {
    }

    /// <summary>Test-only seam: lets tests fake process ancestry/liveness while still exercising real Win32 window APIs.</summary>
    public Win32WindowFocuser(Func<int, int?> getParentProcessId, Func<int, bool> processExists)
    {
        _getParentProcessId = getParentProcessId;
        _processExists = processExists;
    }

    public FocusResult Focus(TerminalWindowReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        if (!reference.IsResolvable)
        {
            return FocusResult.WindowNoLongerAvailable;
        }

        var targetWindow = FindWindowForProcessOrAncestors(reference.OwningProcessId);
        if (targetWindow == IntPtr.Zero)
        {
            // Only a genuinely gone process permanently disables this reference (FR-011) — a
            // still-running process whose window just couldn't be resolved this time (e.g.
            // its terminal host's window is itself momentarily unavailable) stays retryable
            // rather than being permanently broken by one failed lookup.
            if (!_processExists(reference.OwningProcessId))
            {
                reference.MarkUnresolvable();
            }

            return FocusResult.WindowNoLongerAvailable;
        }

        BringToForeground(targetWindow);
        return FocusResult.Focused;
    }

    private IntPtr FindWindowForProcessOrAncestors(int processId)
    {
        int? currentProcessId = processId;

        for (var depth = 0; currentProcessId is not null && depth < MaxAncestorDepth; depth++)
        {
            var window = FindTopLevelWindow(currentProcessId.Value);
            if (window != IntPtr.Zero)
            {
                return window;
            }

            currentProcessId = _getParentProcessId(currentProcessId.Value);
        }

        return IntPtr.Zero;
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

    private static int? WmiGetParentProcessId(int processId)
    {
        using var searcher = new ManagementObjectSearcher($"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {processId}");
        using var results = searcher.Get();
        using var process = results.Cast<ManagementBaseObject>().FirstOrDefault();

        if (process is null)
        {
            return null;
        }

        var parentProcessId = Convert.ToInt32(process["ParentProcessId"]);
        return parentProcessId == 0 ? null : parentProcessId;
    }

    private static bool ProcessExists(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
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
