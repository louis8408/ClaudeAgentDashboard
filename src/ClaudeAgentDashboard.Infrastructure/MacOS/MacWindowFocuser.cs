using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.MacOS;

/// <summary>
/// Brings the owning application of a terminal window to the foreground on macOS via
/// <c>NSRunningApplication.activateWithOptions:</c>, called through the raw Objective-C
/// runtime (research.md R4) since plain .NET 8 has no managed AppKit binding.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class MacWindowFocuser : IWindowFocuser
{
    // NSApplicationActivateAllWindows | NSApplicationActivateIgnoringOtherApps
    private const int ActivateOptions = (1 << 0) | (1 << 1);

    public FocusResult Focus(TerminalWindowReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        if (!reference.IsResolvable)
        {
            return FocusResult.WindowNoLongerAvailable;
        }

        var runningApplication = RunningApplicationForProcessId(reference.OwningProcessId);
        if (runningApplication == IntPtr.Zero)
        {
            reference.MarkUnresolvable();
            return FocusResult.WindowNoLongerAvailable;
        }

        Activate(runningApplication);
        return FocusResult.Focused;
    }

    private static IntPtr RunningApplicationForProcessId(int processId)
    {
        var nsRunningApplicationClass = objc_getClass("NSRunningApplication");
        var selector = sel_registerName("runningApplicationWithProcessIdentifier:");
        return objc_msgSend_IntPtr_int(nsRunningApplicationClass, selector, processId);
    }

    private static void Activate(IntPtr runningApplication)
    {
        var selector = sel_registerName("activateWithOptions:");
        objc_msgSend_void_int(runningApplication, selector, ActivateOptions);
    }

    private const string ObjCRuntime = "/usr/lib/libobjc.dylib";

    [DllImport(ObjCRuntime)]
    private static extern IntPtr objc_getClass(string className);

    [DllImport(ObjCRuntime)]
    private static extern IntPtr sel_registerName(string selectorName);

    [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_IntPtr_int(IntPtr receiver, IntPtr selector, int arg);

    [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void_int(IntPtr receiver, IntPtr selector, int arg);
}
