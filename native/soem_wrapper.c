/*
 * SOEM.NET - .NET 8 P/Invoke bindings for SOEM (Simple Open EtherCAT Master)
 *
 * This wrapper provides a stable C API over SOEM's context-based ecx_* API,
 * suitable for P/Invoke from managed code.
 *
 * SOEM is dual-licensed under GPLv3 and a commercial license.
 * See native/soem/LICENSE.md for details.
 */

#include "soem_wrapper.h"

#include <stdlib.h>
#include <string.h>

/* Include SOEM headers in the order expected by the library.
 * ec_type.h must precede ec_main.h so that SOEM's typedef'd
 * types (uint8, int32, etc.) are available. */
#include "soem/ec_options.h"
#include "soem/ec_type.h"
#include "nicdrv.h"
#include "soem/ec_main.h"
#include "soem/ec_config.h"
#include "soem/ec_dc.h"
#include "soem/ec_coe.h"

/* Internal master structure */
typedef struct
{
   ecx_contextt ctx;
} soem_master_impl_t;

/* -------------------------------------------------------------------------
 * Adapter enumeration
 * ---------------------------------------------------------------------- */

SOEM_API int soem_find_adapters(soem_adapter_info_t* adapters, int max_count)
{
   ec_adaptert* head;
   ec_adaptert* adapter;
   int count = 0;

   if (adapters == NULL || max_count <= 0)
   {
      return 0;
   }

   head = adapter = ec_find_adapters();
   while (adapter != NULL && count < max_count)
   {
      strncpy(adapters[count].name, adapter->name, SOEM_MAX_ADAPTERNAME - 1);
      adapters[count].name[SOEM_MAX_ADAPTERNAME - 1] = '\0';
      strncpy(adapters[count].desc, adapter->desc, SOEM_MAX_ADAPTERNAME - 1);
      adapters[count].desc[SOEM_MAX_ADAPTERNAME - 1] = '\0';
      adapter = adapter->next;
      count++;
   }

   ec_free_adapters(head);
   return count;
}

/* -------------------------------------------------------------------------
 * Master lifecycle
 * ---------------------------------------------------------------------- */

SOEM_API soem_master_t soem_master_create(void)
{
   soem_master_impl_t* impl = (soem_master_impl_t*)calloc(1, sizeof(soem_master_impl_t));
   return (soem_master_t)impl;
}

SOEM_API void soem_master_destroy(soem_master_t handle)
{
   soem_master_impl_t* impl = (soem_master_impl_t*)handle;
   if (impl != NULL)
   {
      free(impl);
   }
}

SOEM_API int soem_master_init(soem_master_t handle, const char* ifname)
{
   soem_master_impl_t* impl = (soem_master_impl_t*)handle;
   if (impl == NULL || ifname == NULL)
   {
      return 0;
   }
   return ecx_init(&impl->ctx, ifname);
}

SOEM_API void soem_master_close(soem_master_t handle)
{
   soem_master_impl_t* impl = (soem_master_impl_t*)handle;
   if (impl != NULL)
   {
      ecx_close(&impl->ctx);
   }
}

/* -------------------------------------------------------------------------
 * Configuration
 * ---------------------------------------------------------------------- */

SOEM_API int soem_master_config_init(soem_master_t handle)
{
   soem_master_impl_t* impl = (soem_master_impl_t*)handle;
   if (impl == NULL)
   {
      return 0;
   }
   return ecx_config_init(&impl->ctx);
}

SOEM_API int soem_master_config_map(soem_master_t handle, void* iomap, int iomap_size)
{
   soem_master_impl_t* impl = (soem_master_impl_t*)handle;
   (void)iomap_size;
   if (impl == NULL || iomap == NULL)
   {
      return 0;
   }
   return ecx_config_map_group(&impl->ctx, iomap, 0);
}

SOEM_API int soem_master_config_dc(soem_master_t handle)
{
   soem_master_impl_t* impl = (soem_master_impl_t*)handle;
   if (impl == NULL)
   {
      return 0;
   }
   return ecx_configdc(&impl->ctx);
}

/* -------------------------------------------------------------------------
 * State management
 * ---------------------------------------------------------------------- */

SOEM_API int soem_master_read_state(soem_master_t handle)
{
   soem_master_impl_t* impl = (soem_master_impl_t*)handle;
   if (impl == NULL)
   {
      return 0;
   }
   return ecx_readstate(&impl->ctx);
}

SOEM_API int soem_master_write_state(soem_master_t handle, uint16_t slave)
{
   soem_master_impl_t* impl = (soem_master_impl_t*)handle;
   if (impl == NULL)
   {
      return 0;
   }
   return ecx_writestate(&impl->ctx, slave);
}

SOEM_API uint16_t soem_master_state_check(soem_master_t handle, uint16_t slave,
                                           uint16_t req_state, int timeout)
{
   soem_master_impl_t* impl = (soem_master_impl_t*)handle;
   if (impl == NULL)
   {
      return 0;
   }
   return ecx_statecheck(&impl->ctx, slave, req_state, timeout);
}

/* -------------------------------------------------------------------------
 * Slave information
 * ---------------------------------------------------------------------- */

SOEM_API int soem_master_slave_count(soem_master_t handle)
{
   soem_master_impl_t* impl = (soem_master_impl_t*)handle;
   if (impl == NULL)
   {
      return 0;
   }
   return impl->ctx.slavecount;
}

SOEM_API int soem_master_get_slave(soem_master_t handle, uint16_t slave,
                                    soem_slave_info_t* info)
{
   soem_master_impl_t* impl = (soem_master_impl_t*)handle;
   ec_slavet* sl;

   if (impl == NULL || info == NULL)
   {
      return 0;
   }
   if (slave < 1 || slave > (uint16_t)impl->ctx.slavecount)
   {
      return 0;
   }

   sl = &impl->ctx.slavelist[slave];
   info->state          = sl->state;
   info->al_status_code = sl->ALstatuscode;
   info->config_adr     = sl->configadr;
   info->alias_adr      = sl->aliasadr;
   info->manufacturer   = sl->eep_man;
   info->product_code   = sl->eep_id;
   info->revision       = sl->eep_rev;
   info->serial         = sl->eep_ser;
   info->output_bits    = sl->Obits;
   info->output_bytes   = sl->Obytes;
   info->input_bits     = sl->Ibits;
   info->input_bytes    = sl->Ibytes;
   info->has_dc         = sl->hasdc ? 1 : 0;
   strncpy(info->name, sl->name, SOEM_MAX_SLAVENAME - 1);
   info->name[SOEM_MAX_SLAVENAME - 1] = '\0';

   return 1;
}

/* -------------------------------------------------------------------------
 * Process data
 * ---------------------------------------------------------------------- */

SOEM_API int soem_master_send_processdata(soem_master_t handle)
{
   soem_master_impl_t* impl = (soem_master_impl_t*)handle;
   if (impl == NULL)
   {
      return 0;
   }
   return ecx_send_processdata(&impl->ctx);
}

SOEM_API int soem_master_receive_processdata(soem_master_t handle, int timeout)
{
   soem_master_impl_t* impl = (soem_master_impl_t*)handle;
   if (impl == NULL)
   {
      return 0;
   }
   return ecx_receive_processdata(&impl->ctx, timeout);
}

/* -------------------------------------------------------------------------
 * SDO (CoE – CANopen over EtherCAT)
 * ---------------------------------------------------------------------- */

SOEM_API int soem_master_sdo_read(soem_master_t handle, uint16_t slave,
                                   uint16_t index, uint8_t subindex,
                                   void* buf, int* buf_size, int timeout_us)
{
   soem_master_impl_t* impl = (soem_master_impl_t*)handle;
   if (impl == NULL || buf == NULL || buf_size == NULL)
   {
      return -1;
   }
   return ecx_SDOread(&impl->ctx, slave, index, subindex, FALSE, buf_size, buf, timeout_us);
}
