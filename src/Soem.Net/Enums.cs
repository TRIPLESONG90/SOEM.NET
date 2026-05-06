namespace Soem.Net;

/// <summary>
/// EtherCAT slave states as defined by the EtherCAT specification.
/// </summary>
public enum EcState : ushort
{
    /// <summary>No state (unknown or not initialized).</summary>
    None = 0x00,

    /// <summary>INIT state – device is initializing.</summary>
    Init = 0x01,

    /// <summary>PRE-OPERATIONAL state – mailbox communication available.</summary>
    PreOp = 0x02,

    /// <summary>BOOTSTRAP state – firmware update mode.</summary>
    Boot = 0x03,

    /// <summary>SAFE-OPERATIONAL state – inputs updated, outputs static.</summary>
    SafeOp = 0x04,

    /// <summary>OPERATIONAL state – fully operational.</summary>
    Op = 0x08,

    /// <summary>Acknowledge flag – combined with a state to acknowledge an error.</summary>
    Ack = 0x10,

    /// <summary>State transition in progress.</summary>
    Trans = 0x20,

    /// <summary>Error flag – indicates a slave error.</summary>
    Error = 0x40,
}
