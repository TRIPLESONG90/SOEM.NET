using System.Reflection;
using System.Runtime.InteropServices;

namespace Soem.Net;

/// <summary>
/// Registers a custom DLL import resolver that provides actionable error
/// messages when the native <c>soem</c> library (or one of its dependencies)
/// cannot be loaded.
/// </summary>
/// <remarks>
/// On Windows, <c>soem.dll</c> depends on <c>wpcap.dll</c> and
/// <c>Packet.dll</c> supplied by Npcap.  If Npcap is not installed the
/// default <see cref="DllNotFoundException"/> message gives no hint about
/// the missing dependency.  This resolver intercepts that failure and
/// rethrows with a clear installation URL.
/// </remarks>
internal static class NativeLoader
{
    private const string NpcapDownloadUrl = "https://npcap.com/";

    static NativeLoader()
    {
        NativeLibrary.SetDllImportResolver(
            typeof(NativeLoader).Assembly,
            ResolveNativeLibrary);
    }

    /// <summary>
    /// Called once to ensure the static constructor runs and the resolver
    /// is registered before the first P/Invoke call is made.
    /// </summary>
    internal static void EnsureInitialized() { /* triggers static ctor */ }

    private static IntPtr ResolveNativeLibrary(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        if (libraryName != NativeMethods.LibName)
        {
            return IntPtr.Zero;
        }

        try
        {
            return NativeLibrary.Load(libraryName, assembly, searchPath);
        }
        catch (DllNotFoundException inner)
            when (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new DllNotFoundException(
                $"Unable to load '{libraryName}.dll' or one of its dependencies. " +
                $"On Windows, SOEM requires Npcap to be installed as a runtime dependency. " +
                $"Download and install Npcap from {NpcapDownloadUrl} and then retry.",
                inner);
        }
    }
}
