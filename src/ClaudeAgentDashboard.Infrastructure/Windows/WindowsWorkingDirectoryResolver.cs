using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace ClaudeAgentDashboard.Infrastructure.Windows;

/// <summary>
/// Resolves a process's real current working directory on Windows by reading its own Process
/// Environment Block (PEB) — WMI's <c>Win32_Process</c> does not expose this at all (research.md
/// R15), which is the actual root cause of hook-to-session correlation (FR-018) silently never
/// succeeding for the ordinary case of a bare <c>claude</c> invocation, whose command line never
/// contains its working directory.
///
/// Uses the undocumented but long-stable <c>NtQueryInformationProcess</c> plus
/// <c>ReadProcessMemory</c> against same-user processes only (no elevation, matching the "no
/// elevated privileges" constraint) — 64-bit target processes only; a 32-bit target under WOW64
/// has a different PEB layout this class does not attempt to read. Every failure path (access
/// denied, unsupported architecture, process already gone, anything else) returns null rather
/// than throwing, per the spec's edge case: correlation degrades to the existing weaker
/// command-line-based fallback, it never crashes the watcher.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WindowsWorkingDirectoryResolver
{
    private const int ProcessQueryLimitedInformation = 0x1000;
    private const int ProcessVmRead = 0x0010;
    private const int ProcessBasicInformationClass = 0;

    public static string? Resolve(int processId)
    {
        var processHandle = IntPtr.Zero;

        try
        {
            processHandle = OpenProcess(ProcessQueryLimitedInformation | ProcessVmRead, false, processId);
            if (processHandle == IntPtr.Zero)
            {
                return null;
            }

            if (!IsSameArchitecture(processHandle))
            {
                return null;
            }

            var pebAddress = GetPebAddress(processHandle);
            if (pebAddress == IntPtr.Zero)
            {
                return null;
            }

            var processParametersAddress = ReadPointer(processHandle, pebAddress + 0x20);
            if (processParametersAddress == IntPtr.Zero)
            {
                return null;
            }

            return ReadCurrentDirectory(processHandle, processParametersAddress);
        }
        catch (Win32Exception)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        finally
        {
            if (processHandle != IntPtr.Zero)
            {
                CloseHandle(processHandle);
            }
        }
    }

    private static bool IsSameArchitecture(IntPtr processHandle)
    {
        if (!Environment.Is64BitOperatingSystem)
        {
            return true;
        }

        if (!IsWow64Process(processHandle, out var targetIsWow64))
        {
            return false;
        }

        var thisIsWow64 = !Environment.Is64BitProcess;
        return targetIsWow64 == thisIsWow64;
    }

    private static IntPtr GetPebAddress(IntPtr processHandle)
    {
        var info = new ProcessBasicInformation();
        var status = NtQueryInformationProcess(
            processHandle, ProcessBasicInformationClass, ref info, Marshal.SizeOf<ProcessBasicInformation>(), out _);

        return status == 0 ? info.PebBaseAddress : IntPtr.Zero;
    }

    private static IntPtr ReadPointer(IntPtr processHandle, IntPtr address)
    {
        var buffer = new byte[IntPtr.Size];
        if (!ReadProcessMemory(processHandle, address, buffer, buffer.Length, out var bytesRead) || bytesRead != buffer.Length)
        {
            return IntPtr.Zero;
        }

        return IntPtr.Size == 8 ? (IntPtr)BitConverter.ToInt64(buffer, 0) : (IntPtr)BitConverter.ToInt32(buffer, 0);
    }

    private static string? ReadCurrentDirectory(IntPtr processHandle, IntPtr processParametersAddress)
    {
        // RTL_USER_PROCESS_PARAMETERS.CurrentDirectory.DosPath (UNICODE_STRING) starts at
        // offset 0x38 on 64-bit Windows: Length (ushort) at +0x00, MaximumLength (ushort) at
        // +0x02, Buffer (pointer) at +0x08 (4 bytes of alignment padding in between).
        const int currentDirectoryOffset = 0x38;

        var header = new byte[8];
        if (!ReadProcessMemory(processHandle, processParametersAddress + currentDirectoryOffset, header, header.Length, out var headerRead)
            || headerRead != header.Length)
        {
            return null;
        }

        var length = BitConverter.ToUInt16(header, 0);
        if (length == 0)
        {
            return null;
        }

        var bufferPointer = ReadPointer(processHandle, processParametersAddress + currentDirectoryOffset + 8);
        if (bufferPointer == IntPtr.Zero)
        {
            return null;
        }

        var stringBytes = new byte[length];
        if (!ReadProcessMemory(processHandle, bufferPointer, stringBytes, stringBytes.Length, out var stringRead)
            || stringRead != stringBytes.Length)
        {
            return null;
        }

        var path = Encoding.Unicode.GetString(stringBytes);
        return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessBasicInformation
    {
        public IntPtr ExitStatus;
        public IntPtr PebBaseAddress;
        public IntPtr AffinityMask;
        public IntPtr BasePriority;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(
        IntPtr processHandle, int processInformationClass, ref ProcessBasicInformation processInformation,
        int processInformationLength, out int returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadProcessMemory(
        IntPtr processHandle, IntPtr baseAddress, byte[] buffer, int size, out int bytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWow64Process(IntPtr processHandle, [MarshalAs(UnmanagedType.Bool)] out bool wow64Process);
}
