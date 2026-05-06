cmake_minimum_required(VERSION 3.16)
project(SoemNet VERSION 1.0.0 LANGUAGES C)

# -------------------------------------------------------------------------
# Build the vendored SOEM static library
# -------------------------------------------------------------------------
add_subdirectory(soem)

# -------------------------------------------------------------------------
# Build soem_wrapper as a shared library (soem.dll / libsoem.so)
# -------------------------------------------------------------------------
add_library(soem_wrapper SHARED
    soem_wrapper.c
)

# Set output name without prefix for consistency across platforms
set_target_properties(soem_wrapper PROPERTIES
    OUTPUT_NAME "soem"
    C_VISIBILITY_PRESET hidden
    VISIBILITY_INLINES_HIDDEN ON
)

target_include_directories(soem_wrapper PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/soem/include
    ${CMAKE_CURRENT_BINARY_DIR}/soem/include
    ${CMAKE_CURRENT_SOURCE_DIR}/soem/osal
)

target_link_libraries(soem_wrapper PRIVATE soem)

# Platform-specific settings
if(WIN32)
    target_compile_definitions(soem_wrapper PRIVATE _CRT_SECURE_NO_WARNINGS)
elseif(UNIX)
    target_include_directories(soem_wrapper PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}/soem/oshw/linux
        ${CMAKE_CURRENT_SOURCE_DIR}/soem/osal/linux
    )
endif()

# -------------------------------------------------------------------------
# Install rules
# -------------------------------------------------------------------------
install(TARGETS soem_wrapper
    RUNTIME DESTINATION bin    # .dll on Windows
    LIBRARY DESTINATION lib    # .so on Linux
)

install(FILES soem_wrapper.h DESTINATION include)
