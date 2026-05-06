using System.Runtime.InteropServices;

namespace Soem.Net;

/// <summary>
/// Provides a managed wrapper around an EtherCAT master instance.
/// This class manages the lifetime of the underlying native SOEM context.
/// </summary>
/// <remarks>
/// <para>
/// Typical usage:
/// <code>
/// using var master = new SoemMaster();
/// master.Init("eth0");
/// int slaveCount = master.ConfigInit();
/// master.ReadState();
/// for (int i = 1; i &lt;= master.SlaveCount; i++)
/// {
///     var slave = master.GetSlave(i);
///     Console.WriteLine($"Slave {i}: {slave.Name}, State={slave.EcState}");
/// }
/// </code>
/// </para>
/// <para>
/// On Linux, raw socket access requires either root privileges or the
/// <c>CAP_NET_RAW</c> capability. Grant it with:
/// <code>sudo setcap cap_net_raw+ep /path/to/your-app</code>
/// </para>
/// </remarks>
public sealed class SoemMaster : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;
    private bool _initialized;

    /// <summary>Default I/O map size in bytes (4 KB).</summary>
    private const int DefaultIoMapSize = 4096;

    private byte[]? _ioMap;
    private GCHandle _ioMapPin;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoemMaster"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the native SOEM context cannot be allocated.
    /// </exception>
    /// <exception cref="DllNotFoundException">
    /// Thrown if the native <c>soem</c> library or one of its dependencies
    /// (e.g. Npcap on Windows) cannot be loaded.
    /// </exception>
    public SoemMaster()
    {
        NativeLoader.EnsureInitialized();
        _handle = NativeMethods.MasterCreate();
        if (_handle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Failed to allocate native SOEM master context.");
        }
    }

    /// <summary>
    /// Gets the number of EtherCAT slaves discovered during the last
    /// <see cref="ConfigInit"/> call.
    /// </summary>
    public int SlaveCount
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.MasterSlaveCount(_handle);
        }
    }

    /// <summary>
    /// Opens the specified network interface and initializes the EtherCAT master.
    /// </summary>
    /// <param name="ifname">
    /// Network interface name. On Linux this is the interface name (e.g. <c>"eth0"</c>);
    /// on Windows it is the adapter's GUID string as returned by
    /// <see cref="SoemAdapter.Enumerate"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if initialization succeeded; <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="ifname"/> is null or empty.
    /// </exception>
    public bool Init(string ifname)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(ifname);

        int result = NativeMethods.MasterInit(_handle, ifname);
        _initialized = result != 0;
        return _initialized;
    }

    /// <summary>
    /// Closes the network interface and releases associated resources.
    /// The master can be re-initialized by calling <see cref="Init"/> again.
    /// </summary>
    public void Close()
    {
        ThrowIfDisposed();
        NativeMethods.MasterClose(_handle);
        _initialized = false;
        ReleaseIoMap();
    }

    /// <summary>
    /// Scans the EtherCAT network and auto-configures all discovered slaves.
    /// </summary>
    /// <returns>
    /// The number of slaves found, or 0 if no slaves were detected.
    /// </returns>
    /// <remarks>
    /// Must be called after <see cref="Init"/> succeeds.
    /// </remarks>
    public int ConfigInit()
    {
        ThrowIfDisposed();
        return NativeMethods.MasterConfigInit(_handle);
    }

    /// <summary>
    /// Maps all slave PDOs into an internal I/O map buffer.
    /// </summary>
    /// <param name="ioMapSize">
    /// Size of the I/O map in bytes. Defaults to 4096 bytes if not specified.
    /// </param>
    /// <returns>Number of bytes used in the I/O map.</returns>
    /// <remarks>
    /// Call after <see cref="ConfigInit"/> to enable process data exchange.
    /// </remarks>
    public int ConfigMap(int ioMapSize = DefaultIoMapSize)
    {
        ThrowIfDisposed();
        ReleaseIoMap();

        _ioMap = new byte[ioMapSize];
        _ioMapPin = GCHandle.Alloc(_ioMap, GCHandleType.Pinned);
        IntPtr ptr = _ioMapPin.AddrOfPinnedObject();
        return NativeMethods.MasterConfigMap(_handle, ptr, ioMapSize);
    }

    /// <summary>
    /// Configures distributed clocks for all DC-capable slaves.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if at least one DC-capable slave was found.
    /// </returns>
    public bool ConfigDc()
    {
        ThrowIfDisposed();
        return NativeMethods.MasterConfigDc(_handle) != 0;
    }

    /// <summary>
    /// Reads the current state of all slaves.
    /// </summary>
    /// <returns>The lowest slave state found across all slaves.</returns>
    public int ReadState()
    {
        ThrowIfDisposed();
        return NativeMethods.MasterReadState(_handle);
    }

    /// <summary>
    /// Writes the requested state to the specified slave.
    /// </summary>
    /// <param name="slave">
    /// Slave index (1-based), or 0 to write to all slaves.
    /// </param>
    /// <param name="state">EtherCAT state to request.</param>
    /// <returns>Working counter value.</returns>
    public int WriteState(ushort slave, EcState state)
    {
        ThrowIfDisposed();
        // Set state on slave struct first then issue write
        // (SOEM reads the state field from the slavelist before writing)
        return NativeMethods.MasterWriteState(_handle, slave);
    }

    /// <summary>
    /// Waits until the slave reaches the requested state or the timeout expires.
    /// </summary>
    /// <param name="slave">
    /// Slave index (1-based), or 0 to check all slaves.
    /// </param>
    /// <param name="reqState">Requested EtherCAT state.</param>
    /// <param name="timeoutUs">Timeout in microseconds.</param>
    /// <returns>The actual state of the slave after the check.</returns>
    public EcState StateCheck(ushort slave, EcState reqState, int timeoutUs = 2_000_000)
    {
        ThrowIfDisposed();
        ushort result = NativeMethods.MasterStateCheck(
            _handle, slave, (ushort)reqState, timeoutUs);
        return (EcState)result;
    }

    /// <summary>
    /// Retrieves information about the specified slave.
    /// </summary>
    /// <param name="slave">Slave index (1-based).</param>
    /// <returns>A <see cref="SlaveInfo"/> structure with the slave's details.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="slave"/> is out of range.
    /// </exception>
    public SlaveInfo GetSlave(int slave)
    {
        ThrowIfDisposed();
        if (slave < 1 || slave > SlaveCount)
        {
            throw new ArgumentOutOfRangeException(nameof(slave),
                $"Slave index must be between 1 and {SlaveCount}.");
        }

        int ok = NativeMethods.MasterGetSlave(_handle, (ushort)slave, out SlaveInfo info);
        if (ok == 0)
        {
            throw new InvalidOperationException(
                $"Failed to retrieve information for slave {slave}.");
        }
        return info;
    }

    /// <summary>
    /// Sends process data to all slaves (one EtherCAT frame per call).
    /// </summary>
    /// <returns>Working counter value.</returns>
    public int SendProcessdata()
    {
        ThrowIfDisposed();
        return NativeMethods.MasterSendProcessdata(_handle);
    }

    /// <summary>
    /// Receives process data from all slaves.
    /// </summary>
    /// <param name="timeoutUs">Timeout in microseconds (default: 2000 µs).</param>
    /// <returns>Working counter value.</returns>
    public int ReceiveProcessdata(int timeoutUs = 2000)
    {
        ThrowIfDisposed();
        return NativeMethods.MasterReceiveProcessdata(_handle, timeoutUs);
    }

    /// <summary>
    /// Gets a span over the raw I/O map buffer, or an empty span if
    /// <see cref="ConfigMap"/> has not been called.
    /// </summary>
    public ReadOnlySpan<byte> IoMap =>
        _ioMap is not null ? _ioMap.AsSpan() : ReadOnlySpan<byte>.Empty;

    /// <summary>
    /// Reads an SDO (Service Data Object) from the specified slave via
    /// CoE (CANopen over EtherCAT).
    /// </summary>
    /// <param name="slave">Slave index (1-based).</param>
    /// <param name="index">SDO object index (e.g. <c>0x4001</c>).</param>
    /// <param name="subindex">SDO subindex (e.g. <c>1</c>).</param>
    /// <param name="bufferSize">
    /// Maximum number of bytes to read. Defaults to 256 bytes.
    /// </param>
    /// <param name="timeoutUs">
    /// Timeout in microseconds. Defaults to 700 000 µs (<c>EC_TIMEOUTRXM</c>).
    /// </param>
    /// <returns>
    /// A byte array containing the raw SDO data. The length reflects the
    /// actual number of bytes returned by the slave.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="slave"/> is out of range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the native SDO read call fails (negative working counter).
    /// </exception>
    public unsafe byte[] SdoRead(
        int slave,
        ushort index,
        byte subindex,
        int bufferSize = 256,
        int timeoutUs = 700_000)
    {
        ThrowIfDisposed();
        if (slave < 1 || slave > SlaveCount)
        {
            throw new ArgumentOutOfRangeException(nameof(slave),
                $"Slave index must be between 1 and {SlaveCount}.");
        }

        byte[] buffer = new byte[bufferSize];
        int actualSize = bufferSize;

        fixed (byte* pBuf = buffer)
        {
            int wkc = NativeMethods.MasterSdoRead(
                _handle,
                (ushort)slave,
                index,
                subindex,
                (IntPtr)pBuf,
                ref actualSize,
                timeoutUs);

            if (wkc < 0)
            {
                throw new InvalidOperationException(
                    $"SDO read failed for slave {slave}, index 0x{index:X4}:{subindex} " +
                    $"(wkc={wkc}).");
            }
        }

        // Return a trimmed copy containing only the bytes actually read.
        byte[] result = new byte[actualSize];
        Array.Copy(buffer, result, actualSize);
        return result;
    }

    // -----------------------------------------------------------------------
    // IDisposable
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ReleaseIoMap();

        if (_handle != IntPtr.Zero)
        {
            if (_initialized)
            {
                NativeMethods.MasterClose(_handle);
            }
            NativeMethods.MasterDestroy(_handle);
            _handle = IntPtr.Zero;
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void ReleaseIoMap()
    {
        if (_ioMapPin.IsAllocated)
        {
            _ioMapPin.Free();
        }
        _ioMap = null;
    }
}
