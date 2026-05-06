#!/usr/bin/env bash
# build-native.sh – Build the SOEM native shared library for the current platform.
#
# Usage:
#   ./build-native.sh [--release|--debug] [--output DIR]
#
# The built library is copied to src/Soem.Net/runtimes/<rid>/native/
# so it is ready for `dotnet pack`.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_TYPE="Release"
BUILD_DIR="${REPO_ROOT}/build/native"

# Detect RID
case "$(uname -s)-$(uname -m)" in
  Linux-x86_64)  RID="linux-x64" ;;
  Linux-aarch64) RID="linux-arm64" ;;
  Darwin-x86_64) RID="osx-x64" ;;
  Darwin-arm64)  RID="osx-arm64" ;;
  *)
    echo "Unsupported platform: $(uname -s)-$(uname -m)" >&2
    exit 1
    ;;
esac

NATIVE_DIR="${REPO_ROOT}/src/Soem.Net/runtimes/${RID}/native"
NATIVE_LIB="${NATIVE_DIR}/libsoem.so"

echo "=== SOEM.NET native build ==="
echo "RID:        ${RID}"
echo "Build type: ${BUILD_TYPE}"
echo "Output:     ${NATIVE_LIB}"
echo

# Check prerequisites
if ! command -v cmake &>/dev/null; then
  echo "ERROR: cmake not found. Install it with: sudo apt-get install cmake" >&2
  exit 1
fi

# Install libpcap-dev on Debian/Ubuntu if needed
if [[ "${RID}" == linux-* ]] && ! dpkg -l libpcap-dev &>/dev/null 2>&1; then
  echo "Installing libpcap-dev..."
  sudo apt-get install -y libpcap-dev
fi

# Configure
cmake -B "${BUILD_DIR}" \
      -S "${REPO_ROOT}/native" \
      -DCMAKE_BUILD_TYPE="${BUILD_TYPE}"

# Build
cmake --build "${BUILD_DIR}" --config "${BUILD_TYPE}" --parallel

# Copy to runtimes directory
mkdir -p "${NATIVE_DIR}"
cp "${BUILD_DIR}/libsoem.so" "${NATIVE_LIB}"

echo
echo "Built: ${NATIVE_LIB}"
echo "Done. Run 'dotnet pack src/Soem.Net/Soem.Net.csproj' to create the NuGet package."
