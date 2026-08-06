using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.MacOS;

/// <summary>
/// Raises a real macOS notification via <c>UNUserNotificationCenter</c>, called through the
/// raw Objective-C runtime (research.md R2), requiring the app to run as a proper .app
/// bundle with a valid bundle identifier so macOS grants notification authorization.
///
/// KNOWN GAP (unverified — this project was developed on Windows, with no macOS machine
/// available to test against): <see cref="NotificationActivated"/> never fires here.
/// Receiving the click response requires the process to register a class conforming to
/// <c>UNUserNotificationCenterDelegate</c>, which means constructing an Objective-C class at
/// runtime (objc_allocateClassPair + class_addMethod) and correctly invoking the Objective-C
/// block parameter Cocoa passes to the delegate's completion handler. That block-invocation
/// ABI is a materially riskier piece of native interop to ship unverified than anything else
/// in this codebase, so it's deferred rather than guessed at. All calls below intentionally
/// pass a null completion handler (nil is a valid argument for these APIs) specifically to
/// avoid needing any block-handling code for the delivery path itself.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class MacUserNotifier : INotifier
{
    private const int UNAuthorizationOptionAlert = 1 << 2;
    private const int UNAuthorizationOptionSound = 1 << 1;

#pragma warning disable CS0067
    public event Action<Guid>? NotificationActivated;
#pragma warning restore CS0067

    public Task<bool> NotifyAttention(AgentSession session, AttentionReason reason)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var center = CurrentNotificationCenter();
            RequestAuthorization(center);
            ShowNotification(center, session.Label, reason);
            return Task.FromResult(true);
        }
        catch (EntryPointNotFoundException)
        {
            return Task.FromResult(false);
        }
        catch (DllNotFoundException)
        {
            return Task.FromResult(false);
        }
    }

    private static IntPtr CurrentNotificationCenter()
    {
        var centerClass = objc_getClass("UNUserNotificationCenter");
        return objc_msgSend(centerClass, sel_registerName("currentNotificationCenter"));
    }

    private static void RequestAuthorization(IntPtr center)
    {
        var options = UNAuthorizationOptionAlert | UNAuthorizationOptionSound;
        objc_msgSend_void_int_ptr(
            center,
            sel_registerName("requestAuthorizationWithOptions:completionHandler:"),
            options,
            IntPtr.Zero);
    }

    private static void ShowNotification(IntPtr center, string label, AttentionReason reason)
    {
        var contentClass = objc_getClass("UNMutableNotificationContent");
        var content = objc_msgSend(objc_msgSend(contentClass, sel_registerName("alloc")), sel_registerName("init"));

        using var title = new NsStringHandle("Claude Agent Dashboard");
        using var body = new NsStringHandle(DescribeReason(label, reason));
        objc_msgSend_void_ptr(content, sel_registerName("setTitle:"), title.Handle);
        objc_msgSend_void_ptr(content, sel_registerName("setBody:"), body.Handle);

        var requestClass = objc_getClass("UNNotificationRequest");
        using var identifier = new NsStringHandle(Guid.NewGuid().ToString());
        var request = objc_msgSend_id_id_id(
            requestClass,
            sel_registerName("requestWithIdentifier:content:trigger:"),
            identifier.Handle,
            content,
            IntPtr.Zero);

        objc_msgSend_void_ptr_ptr(center, sel_registerName("addNotificationRequest:withCompletionHandler:"), request, IntPtr.Zero);
    }

    private static string DescribeReason(string label, AttentionReason reason) => reason switch
    {
        AttentionReason.Idle => $"'{label}' is idle and waiting for your next instruction.",
        AttentionReason.WaitingForInput => $"'{label}' needs your input.",
        AttentionReason.Ended => $"'{label}' has ended.",
        _ => $"'{label}' needs your attention.",
    };

    /// <summary>Owns an autoreleased-by-us NSString created via stringWithUTF8String:.</summary>
    private readonly struct NsStringHandle : IDisposable
    {
        private readonly IntPtr _utf8;

        public IntPtr Handle { get; }

        public NsStringHandle(string value)
        {
            _utf8 = Marshal.StringToCoTaskMemUTF8(value);
            var nsStringClass = objc_getClass("NSString");
            Handle = objc_msgSend_id_ptr(nsStringClass, sel_registerName("stringWithUTF8String:"), _utf8);
        }

        public void Dispose() => Marshal.FreeCoTaskMem(_utf8);

        [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend_id_ptr(IntPtr receiver, IntPtr selector, IntPtr utf8String);
    }

    private const string ObjCRuntime = "/usr/lib/libobjc.dylib";

    [DllImport(ObjCRuntime)]
    private static extern IntPtr objc_getClass(string className);

    [DllImport(ObjCRuntime)]
    private static extern IntPtr sel_registerName(string selectorName);

    [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_id_id_id(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

    [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void_ptr(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void_ptr_ptr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void_int_ptr(IntPtr receiver, IntPtr selector, int arg1, IntPtr arg2);
}
