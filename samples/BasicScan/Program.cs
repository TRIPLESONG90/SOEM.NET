// BasicScan – SOEM.NET sample application
//
// Demonstrates EtherCAT master initialization, slave discovery, and state reading.
//
// Usage:
//   sudo dotnet run --project samples/BasicScan -- eth0
//
// NOTE: Raw socket access requires root privileges or CAP_NET_RAW on Linux.
//   sudo setcap cap_net_raw+ep ./BasicScan

using Soem.Net;

// -------------------------------------------------------------------------
// 1. Show available adapters
// -------------------------------------------------------------------------
Console.WriteLine("=== SOEM.NET – BasicScan ===");
Console.WriteLine();
Console.WriteLine("Available network adapters:");
var adapters = SoemAdapter.Enumerate();
if (adapters.Length == 0)
{
    Console.WriteLine("  (none found – check privileges)");
}
else
{
    foreach (var a in adapters)
    {
        Console.WriteLine($"  [{a.Name}]  {a.Description}");
    }
}
Console.WriteLine();

// -------------------------------------------------------------------------
// 2. Choose the interface to use
// -------------------------------------------------------------------------
string ifname = args.Length > 0
    ? args[0]
    : (adapters.Length > 0 ? adapters[0].Name : string.Empty);

if (string.IsNullOrEmpty(ifname))
{
    Console.Error.WriteLine("No interface specified and no adapters found.");
    Console.Error.WriteLine("Usage: BasicScan <ifname>");
    return 1;
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

Console.WriteLine($"Master initialized on '{ifname}'.");

int slaveCount = master.ConfigInit();
Console.WriteLine($"Slaves found: {slaveCount}");
Console.WriteLine();

if (slaveCount == 0)
{
    Console.WriteLine("No EtherCAT slaves detected. Check wiring and power.");
    master.Close();
    return 0;
}

// -------------------------------------------------------------------------
// 4. Map PDOs and read states
// -------------------------------------------------------------------------
master.ConfigMap();
master.ConfigDc();
master.ReadState();

// -------------------------------------------------------------------------
// 5. Print slave information
// -------------------------------------------------------------------------
Console.WriteLine($"{"#",-4} {"Name",-24} {"State",-12} {"Vendor",-12} {"ProductCode",-14} {"Obytes",-8} {"Ibytes",-8} {"DC"}");
Console.WriteLine(new string('-', 95));

for (int i = 1; i <= slaveCount; i++)
{
    SlaveInfo s = master.GetSlave(i);
    Console.WriteLine(
        $"{i,-4} {s.Name,-24} {s.EcState,-12} " +
        $"0x{s.Manufacturer:X8}  0x{s.ProductCode:X8}    " +
        $"{s.OutputBytes,-8} {s.InputBytes,-8} {(s.SupportsDc ? "yes" : "no")}");
}

Console.WriteLine();
master.Close();
Console.WriteLine("Done.");
return 0;
