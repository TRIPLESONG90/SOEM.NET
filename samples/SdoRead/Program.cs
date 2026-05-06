// SdoRead – SOEM.NET sample application
//
// Demonstrates reading SDO objects from EtherCAT slaves via CoE
// (CANopen over EtherCAT).  This is the C# equivalent of the pysoem snippet:
//
//   master = pysoem.Master()
//   master.open(adapter.name)
//   if master.config_init() > 0:
//       slave = master.slaves[0]
//       print("Slave:", slave.name)
//       while True:
//           value1 = slave.sdo_read(0x4001, 1)
//           sensor1 = int.from_bytes(value1, byteorder='little', signed=False)
//           value2 = slave.sdo_read(0x4081, 1)
//           sensor2 = int.from_bytes(value2, byteorder='little', signed=False)
//           print("Sensor1:", sensor1)
//           print("Sensor2:", sensor2)
//   master.close()
//
// Usage:
//   sudo dotnet run --project samples/SdoRead -- <ifname>
//   sudo dotnet run --project samples/SdoRead -- <ifname> <adapter-index>
//
// NOTE: Raw socket access requires root privileges or CAP_NET_RAW on Linux.
//   sudo setcap cap_net_raw+ep ./SdoRead

using Soem.Net;

// -------------------------------------------------------------------------
// 1. Show available adapters
// -------------------------------------------------------------------------
Console.WriteLine("=== SOEM.NET – SdoRead ===");
Console.WriteLine();

var adapters = SoemAdapter.Enumerate();
if (adapters.Length == 0)
{
    Console.WriteLine("No adapters found – check privileges.");
    return 1;
}

Console.WriteLine("Available network adapters:");
for (int i = 0; i < adapters.Length; i++)
{
    Console.WriteLine($"  [{i}] {adapters[i].Name}  {adapters[i].Description}");
}
Console.WriteLine();

// -------------------------------------------------------------------------
// 2. Choose the interface to use
// -------------------------------------------------------------------------
// Command-line: SdoRead [<ifname>] [<adapter-index>]
//   - No args         → use adapter at index 0.
//   - One arg         → treat as interface name.
//   - Two+ args       → treat first as interface name, second as adapter index to display.
string ifname;
if (args.Length >= 1)
{
    ifname = args[0];
}
else
{
    // Default to the first adapter (Python snippet used index 5 for a specific machine).
    ifname = adapters[0].Name;
}

Console.WriteLine($"Using interface: {ifname}");
Console.WriteLine();

// -------------------------------------------------------------------------
// 3. Initialize the master and scan for slaves
// -------------------------------------------------------------------------
using var master = new SoemMaster();

if (!master.Init(ifname))
{
    Console.Error.WriteLine($"Failed to initialize master on '{ifname}'.");
    Console.Error.WriteLine("Ensure the interface exists and you have sufficient permissions.");
    Console.Error.WriteLine("On Linux: sudo setcap cap_net_raw+ep <your-app-path>");
    return 2;
}

int slaveCount = master.ConfigInit();
Console.WriteLine($"Slaves found: {slaveCount}");

if (slaveCount == 0)
{
    Console.WriteLine("No EtherCAT slaves detected. Check wiring and power.");
    master.Close();
    return 0;
}

// Use slave 1 (first slave), equivalent to Python's master.slaves[0].
const int slaveIndex = 1;
SlaveInfo slave = master.GetSlave(slaveIndex);
Console.WriteLine($"Slave: {slave.Name}");
Console.WriteLine();

// -------------------------------------------------------------------------
// 4. Poll sensor values via SDO read in a loop (Ctrl+C to stop)
// -------------------------------------------------------------------------
Console.WriteLine("Reading sensors (press Ctrl+C to stop) ...");
Console.WriteLine();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    // Sensor 1 – object 0x4001, subindex 1
    byte[] value1 = master.SdoRead(slaveIndex, 0x4001, 1);
    uint sensor1 = ToUInt32LittleEndian(value1);

    // Sensor 2 – object 0x4081, subindex 1
    byte[] value2 = master.SdoRead(slaveIndex, 0x4081, 1);
    uint sensor2 = ToUInt32LittleEndian(value2);

    Console.WriteLine($"Sensor1: {sensor1}");
    Console.WriteLine($"Sensor2: {sensor2}");
}

Console.WriteLine();
Console.WriteLine("Stopped.");
return 0;

// -------------------------------------------------------------------------
// Helper – little-endian bytes → uint (equivalent to int.from_bytes(..., 'little'))
// -------------------------------------------------------------------------
static uint ToUInt32LittleEndian(ReadOnlySpan<byte> data)
{
    return data.Length switch
    {
        1 => data[0],
        2 => (uint)System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(data),
        4 => System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data),
        _ => data.Length >= 4
            ? System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data[..4])
            : (uint)(data.Length > 0 ? data[0] : 0)
    };
}
