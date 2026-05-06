using System.Runtime.InteropServices;

namespace Soem.Net;

/// <summary>
/// Information about a network adapter available for EtherCAT communication.
/// Mirrors the native <c>soem_adapter_info_t</c> structure.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct AdapterInfo
{
    private fixed byte _name[128];
    private fixed byte _desc[128];

    /// <summary>System adapter name (e.g. "eth0" on Linux, GUID on Windows).</summary>
    public string Name
    {
        get { fixed (byte* p = _name) return Marshal.PtrToStringAnsi((IntPtr)p) ?? string.Empty; }
    }

    /// <summary>Human-readable adapter description.</summary>
    public string Description
    {
        get { fixed (byte* p = _desc) return Marshal.PtrToStringAnsi((IntPtr)p) ?? string.Empty; }
    }
}

/// <summary>
/// Information about a discovered EtherCAT slave.
/// Mirrors the native <c>soem_slave_info_t</c> structure.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct SlaveInfo
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

    private fixed byte _name[41];

    /// <summary>Readable slave name from EEPROM.</summary>
    public string Name
    {
        get { fixed (byte* p = _name) return Marshal.PtrToStringAnsi((IntPtr)p) ?? string.Empty; }
    }

    /// <summary>Returns the EtherCAT state as the <see cref="EcState"/> enum.</summary>
    public EcState EcState => (EcState)(State & 0x0F);

    /// <summary>Returns <see langword="true"/> if the slave supports distributed clocks.</summary>
    public bool SupportsDc => HasDc != 0;
}
