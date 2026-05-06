# build-native.ps1 – Build the SOEM native shared library for Windows x64.
#
# Usage:
#   .\build-native.ps1 [-BuildType Release|Debug] [-NpcapSdkDir <path>]
#
# Prerequisites:
#   - CMake >= 3.16  (in PATH)
#   - Visual Studio 2026 with C++ workload (or Build Tools for VS 2026)
#   - Npcap SDK (downloaded automatically if -NpcapSdkDir is not supplied)
#
# The built DLL is copied to src\Soem.Net\runtimes\win-x64\native\
# so it is ready for `dotnet pack`.

[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$BuildType = 'Release',

    # Optional: path to an already-extracted Npcap SDK directory.
    # If omitted, the script downloads npcap-sdk-1.13.zip from npcap.com.
    [string]$NpcapSdkDir = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot   = $PSScriptRoot
$BuildDir   = Join-Path $RepoRoot 'build\native'
$RuntimeDir = Join-Path $RepoRoot 'src\Soem.Net\runtimes\win-x64\native'
$WpcapLibDir = Join-Path $RepoRoot 'native\soem\oshw\win32\wpcap\Lib\x64'

Write-Host '=== SOEM.NET native build (Windows x64) ==='
Write-Host "Build type: $BuildType"
Write-Host "Output:     $RuntimeDir\soem.dll"
Write-Host ''

# ---- Prerequisites check ---------------------------------------------------

if (-not (Get-Command cmake -ErrorAction SilentlyContinue)) {
    Write-Error 'cmake not found. Install CMake from https://cmake.org/download/ and add it to PATH.'
}

# ---- Npcap SDK import libraries --------------------------------------------

if ($NpcapSdkDir -and (Test-Path $NpcapSdkDir)) {
    Write-Host "Using Npcap SDK from: $NpcapSdkDir"
    $SdkLib = Join-Path $NpcapSdkDir 'Lib\x64'
} else {
    $ZipPath = Join-Path $env:TEMP 'npcap-sdk.zip'
    $SdkPath = Join-Path $env:TEMP 'npcap-sdk'

    if (-not (Test-Path $ZipPath)) {
        Write-Host 'Downloading Npcap SDK 1.13...'
        Invoke-WebRequest -Uri 'https://npcap.com/dist/npcap-sdk-1.13.zip' -OutFile $ZipPath
    }

    if (-not (Test-Path $SdkPath)) {
        Write-Host 'Extracting Npcap SDK...'
        Expand-Archive -Path $ZipPath -DestinationPath $SdkPath
    }

    $SdkLib = Join-Path $SdkPath 'Lib\x64'
}

# Copy import libs into the vendored wpcap directory expected by CMake
New-Item -ItemType Directory -Force -Path $WpcapLibDir | Out-Null
Copy-Item -Path (Join-Path $SdkLib '*') -Destination $WpcapLibDir -Force
Write-Host "Npcap import libs copied to: $WpcapLibDir"

# ---- CMake configure -------------------------------------------------------

# If an existing cache was configured with a different generator, clear it.
$CacheFile = Join-Path $BuildDir 'CMakeCache.txt'
if (Test-Path $CacheFile) {
    $CacheGenerator = (Select-String -Path $CacheFile -Pattern '^CMAKE_GENERATOR:INTERNAL=' -ErrorAction SilentlyContinue |
        Select-Object -First 1).Line
    if ($CacheGenerator) {
        $CacheGenerator = $CacheGenerator -replace '^CMAKE_GENERATOR:INTERNAL=', ''
        if ($CacheGenerator -ne 'Visual Studio 18 2026') {
            Write-Host "Existing CMake cache uses generator '$CacheGenerator'. Cleaning $BuildDir..."
            Remove-Item -Recurse -Force $BuildDir
        }
    }
}

Write-Host ''
Write-Host 'Configuring...'
cmake -B $BuildDir `
      -S (Join-Path $RepoRoot 'native') `
      -G 'Visual Studio 18 2026' `
      -A x64 `
      -DCMAKE_BUILD_TYPE=$BuildType
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# ---- CMake build -----------------------------------------------------------

Write-Host ''
Write-Host 'Building...'
cmake --build $BuildDir --config $BuildType --parallel
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# ---- Copy output -----------------------------------------------------------

New-Item -ItemType Directory -Force -Path $RuntimeDir | Out-Null
$BuiltDll = Join-Path $BuildDir "$BuildType\soem.dll"
Copy-Item -Path $BuiltDll -Destination (Join-Path $RuntimeDir 'soem.dll') -Force

Write-Host ''
Write-Host "Built: $RuntimeDir\soem.dll"
Write-Host "Done. Run 'dotnet pack src\Soem.Net\Soem.Net.csproj' to create the NuGet package."
