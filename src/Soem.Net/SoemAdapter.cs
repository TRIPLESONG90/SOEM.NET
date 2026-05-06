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
    public static AdapterInfo[] Enumerate()
    {
        var buffer = new AdapterInfo[MaxAdapters];
        int count = NativeMethods.FindAdapters(buffer, MaxAdapters);
        if (count <= 0)
        {
            return [];
        }

        var result = new AdapterInfo[count];
        Array.Copy(buffer, result, count);
        return result;
    }
}
