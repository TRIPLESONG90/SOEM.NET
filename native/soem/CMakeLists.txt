# This software is dual-licensed under GPLv3 and a commercial
# license. See the file LICENSE.md distributed with this software for
# full license information.

cmake_minimum_required(VERSION 3.16)
project(SOEM VERSION 2.0.0 LANGUAGES C)

set(SOEM_SOURCE_DIR ${CMAKE_CURRENT_SOURCE_DIR})
set(SOEM_BINARY_DIR ${CMAKE_CURRENT_BINARY_DIR})

list(APPEND CMAKE_MODULE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/cmake")

# Configurable sizes
set(EC_BUFSIZE EC_MAXECATFRAME)
set(EC_MAXBUF 16)
set(EC_MAXEEPBITMAP 128)
set(EC_MAXEEPBUF "EC_MAXEEPBITMAP << 5")
set(EC_LOGGROUPOFFSET 16)
set(EC_MAXELIST 64)
set(EC_MAXNAME 40)
set(EC_MAXSLAVE 200)
set(EC_MAXGROUP 2)
set(EC_MAXIOSEGMENTS 64)
set(EC_MAXMBX 1486)
set(EC_MBXPOOLSIZE 32)
set(EC_MAXEEPDO 0x200)
set(EC_MAXSM 8)
set(EC_MAXFMMU 4)
set(EC_MAXLEN_ADAPTERNAME 128)
set(EC_MAX_MAPT 1)
set(EC_MAXODLIST 1024)
set(EC_MAXOELIST 256)
set(EC_SOE_MAXNAME 60)
set(EC_SOE_MAXMAPPING 64)

# Timeouts
set(EC_TIMEOUTRET 2000)
set(EC_TIMEOUTRET3 "EC_TIMEOUTRET * 3")
set(EC_TIMEOUTSAFE 20000)
set(EC_TIMEOUTEEP 20000)
set(EC_TIMEOUTTXM 20000)
set(EC_TIMEOUTRXM 700000)
set(EC_TIMEOUTSTATE 2000000)
set(EC_DEFAULTRETRIES 3)

# MAC addresses
set(EC_PRIMARY_MAC_ARRAY   "{0x0101, 0x0101, 0x0101}")
set(EC_SECONDARY_MAC_ARRAY "{0x0404, 0x0404, 0x0404}")

configure_file(
  ${SOEM_SOURCE_DIR}/include/soem/ec_options.h.in
  ${SOEM_BINARY_DIR}/include/soem/ec_options.h
)

add_library(soem STATIC
  src/ec_base.c
  src/ec_coe.c
  src/ec_config.c
  src/ec_dc.c
  src/ec_eoe.c
  src/ec_foe.c
  src/ec_main.c
  src/ec_print.c
  src/ec_soe.c
)

# Enable PIC so the static library can be linked into a shared library
set_target_properties(soem PROPERTIES POSITION_INDEPENDENT_CODE ON)

target_include_directories(soem PUBLIC
  $<BUILD_INTERFACE:${SOEM_SOURCE_DIR}/include>
  $<BUILD_INTERFACE:${SOEM_BINARY_DIR}/include>
  $<BUILD_INTERFACE:${SOEM_SOURCE_DIR}/osal>
)

target_compile_definitions(soem PRIVATE
  $<$<STREQUAL:${CMAKE_C_BYTE_ORDER},BIG_ENDIAN>:EC_BIG_ENDIAN>
)

# Platform configuration
include(${CMAKE_CURRENT_SOURCE_DIR}/cmake/${CMAKE_SYSTEM_NAME}.cmake)
