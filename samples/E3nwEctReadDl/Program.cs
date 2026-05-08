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

string ifname = "\\Device\\NPF_{4B24BC95-EDE8-4F2B-B09A-C7287DC677A3}";
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
    Console.WriteLine($"Slave {i}: {s.Name} / Vendor=0x{s.Manufacturer:X8} / Product=0x{s.ProductCode:X8}");

    if (s.Manufacturer == OmronVendorId && s.ProductCode == E3nwEctProductCode)
    {
        e3nwPos = i;
        break;
    }
}

if (!e3nwPos.HasValue)
{
    Console.Error.WriteLine("E3NW-ECT not found (check vendor/product code).");
    master.Close();
    return 4;
}

Console.WriteLine($"E3NW-ECT found at slave position {e3nwPos.Value}");

// 가능하면 설정 전에 Pre-OP 확인
var preState = master.StateCheck(1, EcState.PreOp, 50_000);
Console.WriteLine($"State before PDO config: {preState}");

// --- 여기서 PDO assignment / mode 설정 ---
try
{
    const byte speedPriority = 1;
    master.SdoWrite(e3nwPos.Value, 0x300C, 0x01, new byte[] { speedPriority });
    Console.WriteLine("Set 0x300C:01 = 1 (Speed Priority)");
}
catch (Exception ex)
{
    Console.WriteLine($"WARN: failed to set 0x300C:01: {ex.Message}");
}

try
{
    ushort[] txPdoList = { 0x1B10, 0x1B11, 0x1B12, 0x1B13 };

    // 1) assignment clear
    master.SdoWrite(e3nwPos.Value, 0x1C13, 0x00, new byte[] { 0x00 });
    Console.WriteLine("Set 0x1C13:00 = 0");

    // 2) write sub1..subN
    for (int i = 0; i < txPdoList.Length; i++)
    {
        byte subIndex = (byte)(i + 1);
        Span<byte> payload = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, txPdoList[i]);

        master.SdoWrite(e3nwPos.Value, 0x1C13, subIndex, payload);
        Console.WriteLine($"Set 0x1C13:{subIndex:X2} = 0x{txPdoList[i]:X4}");
    }

    // 3) assignment count restore
    master.SdoWrite(e3nwPos.Value, 0x1C13, 0x00, new byte[] { (byte)txPdoList.Length });
    Console.WriteLine($"Set 0x1C13:00 = {txPdoList.Length}");

    Console.WriteLine("Configured 0x1C13 TxPDO assignment: 1B10, 1B11, 1B12, 1B13");
}
catch (Exception ex)
{
    Console.WriteLine($"WARN: failed to configure 0x1C13: {ex.Message}");
}

// 설정 후 바로 map
master.ConfigMap();

// SAFE-OP 확인
if (master.StateCheck(1, EcState.SafeOp, 50_000) != EcState.SafeOp)
{
    Console.Error.WriteLine("Not all slaves reached SAFE-OP.");
    master.Close();
    return 5;
}

Console.WriteLine("All slaves reached SAFE-OP.");

// OP 전환 전 valid processdata 1회
master.SendProcessdata();
master.ReceiveProcessdata(2_000);

// OP 요청
master.WriteState(0, EcState.Op);

bool reachedOp = false;
for (int i = 0; i < 40; i++)
{
    if (master.StateCheck(1, EcState.Op, 50_000) == EcState.Op)
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
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

while (!cts.Token.IsCancellationRequested)
{
    master.SendProcessdata();
    master.ReceiveProcessdata(100_000);
    Console.WriteLine(master.ReadState());
    var io = master.IoMap;
    if (io.Length >= 8)
    {
        short u1In1 = BinaryPrimitives.ReadInt16LittleEndian(io.Slice(0, 2));
        short u1In2 = BinaryPrimitives.ReadInt16LittleEndian(io.Slice(2, 2));
        short u2In1 = BinaryPrimitives.ReadInt16LittleEndian(io.Slice(4, 2));
        short u2In2 = BinaryPrimitives.ReadInt16LittleEndian(io.Slice(6, 2));

        Console.WriteLine(
            $"DL(Unit1 IN1/IN2) = {u1In1,6}, {u1In2,6} | " +
            $"DL(Unit2 IN1/IN2) = {u2In1,6}, {u2In2,6}");
    }
    else
    {
        Console.WriteLine($"WARN: IO map too small ({io.Length} bytes)");
    }

    Thread.Sleep(10);
}

master.WriteState(0, EcState.Init);
master.Close();
return 0;