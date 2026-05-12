# SOEM.NET

.NET 8 P/Invoke bindings for **SOEM** (Simple Open EtherCAT Master).

Provides an idiomatic C# API for EtherCAT master communication on Windows and Linux,
packaged as a NuGet package with native binaries included.

[![Build and Pack](https://github.com/TRIPLESONG90/SOEM.NET/actions/workflows/build.yml/badge.svg)](https://github.com/TRIPLESONG90/SOEM.NET/actions/workflows/build.yml)

## Features

- **Pure P/Invoke** – no Python or Cython dependency; C# calls SOEM directly
- **Cross-platform** – Windows (x64) and Linux (x64, ARM64)
- **.NET 8** target with `LibraryImport`/`DllImport` interop
- **NuGet package** with native binaries under `runtimes/<rid>/native/`
- **Safe wrappers** – `SoemMaster` manages native context lifetime via `IDisposable`
- **Adapter enumeration** via `SoemAdapter.Enumerate()`

## Supported Platforms (RIDs)

| Runtime ID  | OS           | Architecture |
|-------------|--------------|--------------|
| `linux-x64` | Linux        | x64          |
| `linux-arm64` | Linux      | ARM64        |
| `win-x64`   | Windows      | x64          |

## Installation

```sh
dotnet add package Soem.Net
```

Or via the Package Manager Console:

```powershell
Install-Package Soem.Net
```

## Quick Start

### 1. Enumerate adapters

```csharp
using Soem.Net;

var adapters = SoemAdapter.Enumerate();
foreach (var adapter in adapters)
{
    Console.WriteLine($"[{adapter.Name}] {adapter.Description}");
}
```

### 2. Initialize master and scan for slaves

```csharp
using Soem.Net;

using var master = new SoemMaster();

// Open the network interface (eth0 on Linux, GUID on Windows)
if (!master.Init("eth0"))
{
    Console.Error.WriteLine("Failed to initialize. Check interface name and permissions.");
    return;
}

// Auto-configure all slaves
int slaveCount = master.ConfigInit();
Console.WriteLine($"Found {slaveCount} slave(s)");

// Map PDOs into the internal I/O map
master.ConfigMap();

// Configure distributed clocks (optional)
master.ConfigDc();

// Read current state of all slaves
master.ReadState();

for (int i = 1; i <= master.SlaveCount; i++)
{
    SlaveInfo slave = master.GetSlave(i);
    Console.WriteLine($"  Slave {i}: {slave.Name} | State={slave.EcState} " +
                      $"| Vendor=0x{slave.Manufacturer:X8} | Product=0x{slave.ProductCode:X8}");
}

master.Close();
```

### 3. Process data exchange

```csharp
// After ConfigMap(), enter cyclic process data loop:
while (running)
{
    master.SendProcessdata();
    int wkc = master.ReceiveProcessdata(timeoutUs: 2000);

    // Access raw I/O map if needed:
    ReadOnlySpan<byte> ioMap = master.IoMap;

    Thread.Sleep(1);   // 1 ms cycle
}
```

## Linux: Raw Socket Permissions

SOEM uses raw Ethernet sockets to communicate with EtherCAT slaves.
On Linux this requires either root privileges or the `CAP_NET_RAW` capability.

**Option A – run as root:**
```sh
sudo dotnet run
```

**Option B – grant capability (recommended for production):**
```sh
sudo setcap cap_net_raw+ep /path/to/your-published-app
```

## Windows: Npcap Runtime Requirement

On Windows, `soem.dll` uses the [Npcap](https://npcap.com/) packet capture
library (`wpcap.dll` / `Packet.dll`) to send raw Ethernet frames.
**Npcap must be installed on any machine that runs a SOEM.NET application.**

If Npcap is not installed you will see:

```
System.DllNotFoundException: Unable to load DLL 'soem' or one of its dependencies
```

**Install Npcap:**

1. Download the installer from <https://npcap.com/>
2. Run the installer (administrator privileges required)
3. Restart your application

> **Note:** WinPcap is end-of-life and may also work, but Npcap is strongly
> recommended.  The Npcap SDK (import libraries) is only required when
> **building** the native library from source (see *Building from Source*
> below); end-users only need the Npcap runtime installer.

## Building from Source

### Prerequisites

- .NET 8 SDK
- CMake ≥ 3.16
- GCC/Clang (Linux) or Visual Studio 2026 (Windows)
- `libpcap-dev` on Linux: `sudo apt-get install libpcap-dev`
- Npcap SDK on Windows (build-time only; downloaded automatically by `build-native.ps1`)

### Build native library

**Linux (current machine architecture):**
```sh
./build-native.sh
```

**Windows (x64):**
```powershell
# Automated (downloads Npcap SDK automatically):
.\build-native.ps1

# Or manually:
cmake -B build/native -S native -G "Visual Studio 18 2026" -A x64
# If you previously configured with another generator, clean cache first:
# Remove-Item build/native/CMakeCache.txt; Remove-Item build/native/CMakeFiles -Recurse -Force
cmake --build build/native --config Release
Copy-Item build/native/Release/soem.dll src/Soem.Net/runtimes/win-x64/native/
```

### Build and pack NuGet

```sh
dotnet build SOEM.NET.slnx --configuration Release
dotnet pack src/Soem.Net/Soem.Net.csproj --configuration Release --output artifacts/nuget
```

## Repository Structure

```
SOEM.NET/
├── native/
│   ├── CMakeLists.txt          # Top-level CMake (builds soem_wrapper shared lib)
│   ├── soem_wrapper.h          # Stable C API exported for P/Invoke
│   ├── soem_wrapper.c          # Thin wrapper over SOEM ecx_* context API
│   └── soem/                   # Vendored SOEM source (Apache-licensed subset)
│       ├── LICENSE.md
│       ├── CMakeLists.txt
│       ├── src/                # SOEM C source files
│       ├── include/soem/       # SOEM public headers
│       ├── osal/               # OS abstraction layer (Linux + Windows)
│       └── oshw/               # OS hardware layer (Linux + Windows)
├── src/
│   └── Soem.Net/
│       ├── Soem.Net.csproj     # SDK-style project (net8.0)
│       ├── NativeMethods.cs    # P/Invoke declarations
│       ├── SoemMaster.cs       # High-level master wrapper
│       ├── SoemAdapter.cs      # Adapter enumeration
│       ├── Structs.cs          # Interop structs (AdapterInfo, SlaveInfo)
│       ├── Enums.cs            # EcState enum
│       └── runtimes/
│           ├── linux-x64/native/libsoem.so
│           ├── linux-arm64/native/libsoem.so
│           └── win-x64/native/soem.dll
├── samples/
│   └── BasicScan/
│       ├── BasicScan.csproj
│       └── Program.cs          # Demonstrates init + scan + state print
└── .github/
    └── workflows/
        └── build.yml           # CI: build native + managed + pack NuGet
```

## SOEM License

SOEM is dual-licensed under **GPLv3** and a commercial license.
See [`native/soem/LICENSE.md`](native/soem/LICENSE.md) for details.
The SOEM.NET wrapper code in `src/` and `native/soem_wrapper.*` is provided
under the same GPLv3 terms unless you obtain a commercial SOEM license.

## Acknowledgements

- [SOEM – Simple Open EtherCAT Master](https://github.com/OpenEtherCATsociety/SOEM)
  by RT-Labs AB.
