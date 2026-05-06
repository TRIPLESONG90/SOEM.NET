namespace Soem.Net;

/// <summary>
/// Provides enumeration of network adapters available for EtherCAT communication.
/// </summary>
public static class SoemAdapter
{
    private const int MaxAdapters = 16;

    /// <summary>
    /// Enumerates all network adapters available for EtherCAT communication.
    /// </summary>
    /// <returns>
    /// An array of <see cref="AdapterInfo"/> structures describing each adapter.
    /// Returns an empty array if no adapters are found.
    /// </returns>
    /// <remarks>
    /// On Linux, raw socket access requires elevated privileges or the
    /// <c>CAP_NET_RAW</c> capability. Consider running as root or granting
    /// the capability with:
    /// <code>sudo setcap cap_net_raw+ep /path/to/your-app</code>
    /// </remarks>
    /// <exception cref="DllNotFoundException">
    /// Thrown if the native <c>soem</c> library or one of its dependencies
    /// (e.g. Npcap on Windows) cannot be loaded.
    /// </exception>
    public static AdapterInfo[] Enumerate()
    {
        NativeLoader.EnsureInitialized();
        var buffer = new AdapterInfo[MaxAdapters];
        int count = NativeMethods.FindAdapters(ref buffer[0], MaxAdapters);
        if (count <= 0)
        {
            return [];
        }

        var result = new AdapterInfo[count];
        Array.Copy(buffer, result, count);
        return result;
    }
}
