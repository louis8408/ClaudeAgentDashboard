using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace ClaudeAgentDashboard.Infrastructure.Windows;

/// <summary>
/// Registers the Start Menu shortcut an unpackaged Win32 app needs before Windows will
/// actually render a toast raised under a given AppUserModelId. Without this, WinRT's
/// <c>ToastNotifier.Show</c> reports <c>Setting: Enabled</c> and throws nothing — the call
/// silently succeeds while the shell drops the toast, because it has no Start Menu identity
/// to resolve that AppUserModelId to (confirmed empirically: real toasts never appeared
/// despite <see cref="WindowsToastNotifier"/> reporting successful delivery). This is the
/// standard fix documented by Microsoft's own unpackaged-app toast samples.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WindowsToastShortcutRegistrar
{
    private static PropertyKey AppUserModelIdKey = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3", 5);

    public static void EnsureRegistered(string shortcutPath, string executablePath, string appUserModelId, string? iconPath = null)
    {
        if (File.Exists(shortcutPath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(shortcutPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var shellLink = (IShellLinkW)new ShellLinkCoClass();
        shellLink.SetPath(executablePath);
        shellLink.SetWorkingDirectory(Path.GetDirectoryName(executablePath) ?? string.Empty);
        shellLink.SetArguments(string.Empty);
        if (iconPath is not null)
        {
            // The toast's app-identity row (icon + name shown atop the notification) reads
            // this shortcut's own icon — without it, a registered-but-icon-less AUMID shows a
            // generic placeholder there even though the toast body itself renders correctly.
            shellLink.SetIconLocation(iconPath, 0);
        }

        var propertyStore = (IPropertyStore)shellLink;
        var variant = PropVariant.FromString(appUserModelId);
        try
        {
            propertyStore.SetValue(ref AppUserModelIdKey, ref variant);
            propertyStore.Commit();
        }
        finally
        {
            variant.Dispose();
        }

        ((IPersistFile)shellLink).Save(shortcutPath, true);
    }

    /// <summary>Reads back a shortcut's icon location — used to verify registration (and by tests).</summary>
    public static string? ReadIconLocation(string shortcutPath)
    {
        var shellLink = (IShellLinkW)new ShellLinkCoClass();
        ((IPersistFile)shellLink).Load(shortcutPath, dwMode: 0);

        var buffer = new StringBuilder(260);
        shellLink.GetIconLocation(buffer, buffer.Capacity, out _);
        var iconLocation = buffer.ToString();
        return string.IsNullOrEmpty(iconLocation) ? null : iconLocation;
    }

    /// <summary>Reads back a shortcut's AppUserModelId property — used to verify registration (and by tests).</summary>
    public static string? ReadAppUserModelId(string shortcutPath)
    {
        var shellLink = (IShellLinkW)new ShellLinkCoClass();
        ((IPersistFile)shellLink).Load(shortcutPath, dwMode: 0);

        var propertyStore = (IPropertyStore)shellLink;
        propertyStore.GetValue(ref AppUserModelIdKey, out var variant);
        try
        {
            return variant.ToStringValue();
        }
        finally
        {
            variant.Dispose();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey(string fmtid, int pid)
    {
        public Guid Fmtid = new(fmtid);
        public int Pid = pid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropVariant : IDisposable
    {
        private const ushort VtLpwstr = 31;

        public ushort Vt;
        private readonly ushort _reserved1;
        private readonly ushort _reserved2;
        private readonly ushort _reserved3;
        public IntPtr PointerValue;

        public static PropVariant FromString(string value) => new()
        {
            Vt = VtLpwstr,
            PointerValue = Marshal.StringToCoTaskMemUni(value),
        };

        public readonly string? ToStringValue() =>
            Vt == VtLpwstr && PointerValue != IntPtr.Zero ? Marshal.PtrToStringUni(PointerValue) : null;

        public void Dispose()
        {
            PropVariantClear(ref this);
        }

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PropVariant pvar);
    }

    // Deliberately empty and deliberately not sealed: this is a COM activation marker type
    // (CoCreateInstance target) whose actual behavior comes entirely from the COM object the
    // runtime activates for this CLSID — not from any C# implementation. It must stay
    // unsealed for the runtime-COM-interop casts below (to IShellLinkW/IPersistFile/
    // IPropertyStore) to compile at all: a sealed class with no declared interface list is
    // provably uncastable, but classic COM interop resolves these via QueryInterface at
    // runtime regardless of what's statically declared here.
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    [SuppressMessage("Major Code Smell", "S3260", Justification = "Must stay unsealed for COM interop casts to compile; behavior comes from COM activation, not C#.")]
    private class ShellLinkCoClass;

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        void GetCount(out uint cProps);
        void GetAt(uint iProp, out PropertyKey pkey);
        void GetValue(ref PropertyKey key, out PropVariant pv);
        void SetValue(ref PropertyKey key, ref PropVariant propvar);
        void Commit();
    }

    [ComImport]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string? pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }
}
