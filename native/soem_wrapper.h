/*
 * SOEM.NET - .NET 8 P/Invoke bindings for SOEM (Simple Open EtherCAT Master)
 *
 * This wrapper provides a stable C API over SOEM's context-based ecx_* API,
 * suitable for P/Invoke from managed code.
 *
 * SOEM is dual-licensed under GPLv3 and a commercial license.
 * See native/soem/LICENSE.md for details.
 */

#ifndef _SOEM_WRAPPER_H_
#define _SOEM_WRAPPER_H_

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#ifdef _WIN32
#  define SOEM_API __declspec(dllexport)
#else
#  define SOEM_API __attribute__((visibility("default")))
#endif

/** Maximum adapter name length (matches EC_MAXLEN_ADAPTERNAME) */
#define SOEM_MAX_ADAPTERNAME 128

/** Maximum slave name length (matches EC_MAXNAME + 1) */
#define SOEM_MAX_SLAVENAME   41

/** EtherCAT slave states */
#define SOEM_STATE_NONE     0x00
#define SOEM_STATE_INIT     0x01
#define SOEM_STATE_PREOP    0x02
#define SOEM_STATE_BOOT     0x03
#define SOEM_STATE_SAFEOP   0x04
#define SOEM_STATE_OP       0x08
#define SOEM_STATE_ACK      0x10
#define SOEM_STATE_TRANS    0x20
#define SOEM_STATE_ERROR    0x40

/** Opaque master handle */
typedef void* soem_master_t;

/** Network adapter information */
typedef struct
{
   char name[SOEM_MAX_ADAPTERNAME];
   char desc[SOEM_MAX_ADAPTERNAME];
} soem_adapter_info_t;

/** Slave information (subset of ec_slavet) */
typedef struct
{
   uint16_t state;
   uint16_t al_status_code;
   uint16_t config_adr;
   uint16_t alias_adr;
   uint32_t manufacturer;
   uint32_t product_code;
   uint32_t revision;
   uint32_t serial;
   uint16_t output_bits;
   uint32_t output_bytes;
   uint16_t input_bits;
   uint32_t input_bytes;
   uint8_t  has_dc;
   char     name[SOEM_MAX_SLAVENAME];
} soem_slave_info_t;

/**
 * Enumerate available network adapters.
 * @param adapters  Output buffer for adapter information.
 * @param max_count Maximum number of adapters to return.
 * @return Number of adapters found (may be 0).
 */
SOEM_API int soem_find_adapters(soem_adapter_info_t* adapters, int max_count);

/**
 * Create a new SOEM master instance.
 * @return Opaque handle, or NULL on allocation failure.
 */
SOEM_API soem_master_t soem_master_create(void);

/**
 * Destroy a SOEM master instance and free all resources.
 * @param handle Master handle returned by soem_master_create().
 */
SOEM_API void soem_master_destroy(soem_master_t handle);

/**
 * Initialize the master on the specified network interface.
 * @param handle  Master handle.
 * @param ifname  Network interface name (e.g. "eth0" on Linux, adapter GUID on Windows).
 * @return 1 on success, 0 on failure.
 */
SOEM_API int soem_master_init(soem_master_t handle, const char* ifname);

/**
 * Close the network interface and release the master.
 * @param handle Master handle.
 */
SOEM_API void soem_master_close(soem_master_t handle);

/**
 * Auto-configure all EtherCAT slaves.
 * @param handle Master handle.
 * @return Number of slaves found (>0), or 0 if none found.
 */
SOEM_API int soem_master_config_init(soem_master_t handle);

/**
 * Map all slave PDOs into a process image buffer.
 * @param handle      Master handle.
 * @param iomap       Pointer to I/O map buffer.
 * @param iomap_size  Size of I/O map buffer in bytes.
 * @return Number of bytes used in the I/O map.
 */
SOEM_API int soem_master_config_map(soem_master_t handle, void* iomap, int iomap_size);

/**
 * Configure distributed clocks.
 * @param handle Master handle.
 * @return 1 if DC capable slaves found, 0 otherwise.
 */
SOEM_API int soem_master_config_dc(soem_master_t handle);

/**
 * Read the state of all slaves.
 * @param handle Master handle.
 * @return Lowest slave state found.
 */
SOEM_API int soem_master_read_state(soem_master_t handle);

/**
 * Write state to a specific slave (or all slaves if slave == 0).
 * @param handle Master handle.
 * @param slave  Slave index (1-based), or 0 for all slaves.
 * @param state  Requested EtherCAT state (SOEM_STATE_*).
 * @return Working counter.
 */
SOEM_API int soem_master_write_state(soem_master_t handle, uint16_t slave, uint16_t state);

/**
 * Check state of a slave and wait until it reaches the requested state.
 * @param handle    Master handle.
 * @param slave     Slave index (1-based), or 0 for all.
 * @param req_state Requested EtherCAT state (SOEM_STATE_*).
 * @param timeout   Timeout in microseconds.
 * @return Actual state of slave.
 */
SOEM_API uint16_t soem_master_state_check(soem_master_t handle, uint16_t slave,
                                           uint16_t req_state, int timeout);

/**
 * Get the number of slaves discovered during config_init.
 * @param handle Master handle.
 * @return Slave count.
 */
SOEM_API int soem_master_slave_count(soem_master_t handle);

/**
 * Get information about a specific slave.
 * @param handle Master handle.
 * @param slave  Slave index (1-based).
 * @param info   Output structure.
 * @return 1 on success, 0 if slave index is out of range.
 */
SOEM_API int soem_master_get_slave(soem_master_t handle, uint16_t slave,
                                    soem_slave_info_t* info);

/**
 * Send process data to all slaves.
 * @param handle Master handle.
 * @return Working counter.
 */
SOEM_API int soem_master_send_processdata(soem_master_t handle);

/**
 * Receive process data from all slaves.
 * @param handle  Master handle.
 * @param timeout Timeout in microseconds.
 * @return Working counter.
 */
SOEM_API int soem_master_receive_processdata(soem_master_t handle, int timeout);

/**
 * Read an SDO (Service Data Object) from a slave via CoE (CANopen over EtherCAT).
 * @param handle     Master handle.
 * @param slave      Slave index (1-based).
 * @param index      SDO index (e.g. 0x4001).
 * @param subindex   SDO subindex (e.g. 1).
 * @param buf        Output buffer to receive the SDO data.
 * @param buf_size   In: size of buf in bytes. Out: actual number of bytes read.
 * @param timeout_us Timeout in microseconds (standard: 700000).
 * @return Positive working counter on success, negative error code on failure.
 */
SOEM_API int soem_master_sdo_read(soem_master_t handle, uint16_t slave,
                                   uint16_t index, uint8_t subindex,
                                   void* buf, int* buf_size, int timeout_us);

#ifdef __cplusplus
}
#endif

#endif /* _SOEM_WRAPPER_H_ */
