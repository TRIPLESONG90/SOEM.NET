using System.Buffers.Binary;
using Soem.Net;

const uint OmronVendorId = 0x00000083;
const uint E3nwEctProductCode = 0x0000009F;

Console.WriteLine("=== SOEM.NET - E3NW-ECT Detection Level PDO Read ===");
Console.WriteLine();

var adapters = SoemAdapter.Enumerate();
if (adapters.Length > 0)
{
    Console.WriteLine("Available network adapters:");
    foreach (var a in adapters)
    {
        Console.WriteLine($"  [{a.Name}] {a.Description}");
    }
    Console.WriteLine();
}

string ifname = args.Length > 0 ? args[0] : (adapters.Length > 0 ? adapters[0].Name : string.Empty);
if (string.IsNullOrWhiteSpace(ifname))
{
    Console.Error.WriteLine("Usage: dotnet run --project samples/E3nwEctReadDl -- <ifname>");
    return 1;
}

using var master = new SoemMaster();
if (!master.Init(ifname))
{
    Console.Error.WriteLine($"Failed to initialize master on '{ifname}'.");
    return 2;
}

int slaveCount = master.ConfigInit();
if (slaveCount <= 0)
{
    Console.Error.WriteLine("No EtherCAT slave found.");
    master.Close();
    return 3;
}

int? e3nwPos = null;
for (int i = 1; i <= slaveCount; i++)
{
    var s = master.GetSlave(i);
    if (s.Manufacturer == OmronVendorId && s.ProductCode == E3nwEctProductCode)
    {
        e3nwPos = i;
        break;
    }
}

if (!e3nwPos.HasValue)
{
    Console.Error.WriteLine("E3NW-ECT not found (check vendor/product code). ");
    master.Close();
    return 4;
}

master.ConfigMap();
if (master.StateCheck(0, EcState.SafeOp, 50_000) != EcState.SafeOp)
{
    Console.Error.WriteLine("Not all slaves reached SAFE-OP.");
    master.Close();
    return 5;
}

master.SendProcessdata();
master.ReceiveProcessdata(2_000);
master.WriteState(0, EcState.Op);

bool reachedOp = false;
for (int i = 0; i < 40; i++)
{
    if (master.StateCheck(0, EcState.Op, 50_000) == EcState.Op)
    {
        reachedOp = true;
        break;
    }
}

if (!reachedOp)
{
    Console.Error.WriteLine("Not all slaves reached OP.");
    master.Close();
    return 6;
}

Console.WriteLine($"Using slave position: {e3nwPos.Value}");
Console.WriteLine("Reading detection levels (Ctrl+C to stop)...");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    master.SendProcessdata();
    master.ReceiveProcessdata(100_000);

    var io = master.IoMap;
    if (io.Length >= 8)
    {
        short u1In1 = BinaryPrimitives.ReadInt16LittleEndian(io.Slice(0, 2));
        short u1In2 = BinaryPrimitives.ReadInt16LittleEndian(io.Slice(2, 2));
        short u2In1 = BinaryPrimitives.ReadInt16LittleEndian(io.Slice(4, 2));
        short u2In2 = BinaryPrimitives.ReadInt16LittleEndian(io.Slice(6, 2));

        Console.WriteLine($"DL(Unit1 IN1/IN2) = {u1In1,6}, {u1In2,6} | DL(Unit2 IN1/IN2) = {u2In1,6}, {u2In2,6}");
    }
    else
    {
        Console.WriteLine($"WARN: IO map too small ({io.Length} bytes)");
    }

    Thread.Sleep(1);
}

master.WriteState(0, EcState.Init);
master.Close();
return 0;
