using System.Runtime.InteropServices;

namespace Soem.Net;

/// <summary>
/// Information about a network adapter available for EtherCAT communication.
/// Mirrors the native <c>soem_adapter_info_t</c> structure.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct AdapterInfo
{
    /// <summary>System adapter name (e.g. "eth0" on Linux, GUID on Windows).</summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Name;

    /// <summary>Human-readable adapter description.</summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Description;
}

/// <summary>
/// Information about a discovered EtherCAT slave.
/// Mirrors the native <c>soem_slave_info_t</c> structure.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct SlaveInfo
{
    /// <summary>Current EtherCAT state of the slave.</summary>
    public ushort State;

    /// <summary>AL (Application Layer) status code from the slave.</summary>
    public ushort AlStatusCode;

    /// <summary>Configured station address.</summary>
    public ushort ConfigAddress;

    /// <summary>Alias address (from EEPROM).</summary>
    public ushort AliasAddress;

    /// <summary>Vendor/Manufacturer ID from EEPROM.</summary>
    public uint Manufacturer;

    /// <summary>Product code from EEPROM.</summary>
    public uint ProductCode;

    /// <summary>Revision number from EEPROM.</summary>
    public uint Revision;

    /// <summary>Serial number from EEPROM.</summary>
    public uint Serial;

    /// <summary>Number of output (RxPDO) bits.</summary>
    public ushort OutputBits;

    /// <summary>Number of output (RxPDO) bytes (0 if OutputBits &lt; 8).</summary>
    public uint OutputBytes;

    /// <summary>Number of input (TxPDO) bits.</summary>
    public ushort InputBits;

    /// <summary>Number of input (TxPDO) bytes (0 if InputBits &lt; 8).</summary>
    public uint InputBytes;

    /// <summary>Non-zero if the slave supports Distributed Clock (DC).</summary>
    public byte HasDc;

    /// <summary>Readable slave name from EEPROM.</summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
    public string Name;

    /// <summary>Returns the EtherCAT state as the <see cref="EcState"/> enum.</summary>
    public EcState EcState => (EcState)(State & 0x0F);

    /// <summary>Returns <see langword="true"/> if the slave supports distributed clocks.</summary>
    public bool SupportsDc => HasDc != 0;
}
