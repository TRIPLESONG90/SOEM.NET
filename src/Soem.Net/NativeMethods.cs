using System.Runtime.InteropServices;

namespace Soem.Net;

/// <summary>
/// P/Invoke declarations for the native <c>soem</c> shared library
/// (<c>soem.dll</c> on Windows, <c>libsoem.so</c> on Linux).
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>
    /// The name of the native library (without extension or prefix).
    /// The .NET runtime resolves this to <c>soem.dll</c> on Windows and
    /// <c>libsoem.so</c> on Linux using the RID-specific <c>runtimes/</c> path.
    /// </summary>
    private const string LibName = "soem";

    // -----------------------------------------------------------------------
    // Adapter enumeration
    // -----------------------------------------------------------------------

    /// <summary>
    /// Enumerates available network adapters.
    /// Uses DllImport because AdapterInfo contains marshalled string fields.
    /// </summary>
    [DllImport(LibName, EntryPoint = "soem_find_adapters", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int FindAdapters(
        [Out] AdapterInfo[] adapters,
        int maxCount);

    // -----------------------------------------------------------------------
    // Master lifecycle
    // -----------------------------------------------------------------------

    /// <summary>Creates a new SOEM master context.</summary>
    [LibraryImport(LibName, EntryPoint = "soem_master_create")]
    internal static partial IntPtr MasterCreate();

    /// <summary>Destroys a SOEM master context and frees all resources.</summary>
    [LibraryImport(LibName, EntryPoint = "soem_master_destroy")]
    internal static partial void MasterDestroy(IntPtr handle);

    // -----------------------------------------------------------------------
    // Initialization
    // -----------------------------------------------------------------------

    /// <summary>Opens the network interface and initializes the master.</summary>
    [LibraryImport(LibName, EntryPoint = "soem_master_init",
        StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int MasterInit(IntPtr handle, string ifname);

    /// <summary>Closes the network interface.</summary>
    [LibraryImport(LibName, EntryPoint = "soem_master_close")]
    internal static partial void MasterClose(IntPtr handle);

    // -----------------------------------------------------------------------
    // Configuration
    // -----------------------------------------------------------------------

    /// <summary>Auto-configures all discovered EtherCAT slaves.</summary>
    [LibraryImport(LibName, EntryPoint = "soem_master_config_init")]
    internal static partial int MasterConfigInit(IntPtr handle);

    /// <summary>Maps all slave PDOs into the supplied I/O map buffer.</summary>
    [LibraryImport(LibName, EntryPoint = "soem_master_config_map")]
    internal static partial int MasterConfigMap(IntPtr handle, IntPtr iomap, int iomapSize);

    /// <summary>Configures distributed clocks for all DC-capable slaves.</summary>
    [LibraryImport(LibName, EntryPoint = "soem_master_config_dc")]
    internal static partial int MasterConfigDc(IntPtr handle);

    // -----------------------------------------------------------------------
    // State management
    // -----------------------------------------------------------------------

    /// <summary>Reads the current state of all slaves.</summary>
    [LibraryImport(LibName, EntryPoint = "soem_master_read_state")]
    internal static partial int MasterReadState(IntPtr handle);

    /// <summary>Writes the requested state to the specified slave (0 = all).</summary>
    [LibraryImport(LibName, EntryPoint = "soem_master_write_state")]
    internal static partial int MasterWriteState(IntPtr handle, ushort slave);

    /// <summary>
    /// Waits until the slave reaches the requested state or the timeout expires.
    /// </summary>
    [LibraryImport(LibName, EntryPoint = "soem_master_state_check")]
    internal static partial ushort MasterStateCheck(
        IntPtr handle, ushort slave, ushort reqState, int timeoutUs);

    // -----------------------------------------------------------------------
    // Slave information
    // -----------------------------------------------------------------------

    /// <summary>Returns the number of slaves found during configuration.</summary>
    [LibraryImport(LibName, EntryPoint = "soem_master_slave_count")]
    internal static partial int MasterSlaveCount(IntPtr handle);

    /// <summary>
    /// Retrieves information about the specified slave (1-based index).
    /// Uses DllImport because SlaveInfo contains marshalled string fields.
    /// </summary>
    [DllImport(LibName, EntryPoint = "soem_master_get_slave", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int MasterGetSlave(
        IntPtr handle, ushort slave, out SlaveInfo info);

    // -----------------------------------------------------------------------
    // Process data
    // -----------------------------------------------------------------------

    /// <summary>Sends process data to all slaves.</summary>
    [LibraryImport(LibName, EntryPoint = "soem_master_send_processdata")]
    internal static partial int MasterSendProcessdata(IntPtr handle);

    /// <summary>Receives process data from all slaves.</summary>
    [LibraryImport(LibName, EntryPoint = "soem_master_receive_processdata")]
    internal static partial int MasterReceiveProcessdata(IntPtr handle, int timeoutUs);
}
