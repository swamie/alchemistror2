#!/usr/bin/env bash
# Builds the mod and makes a Thunderstore / r2modman zip in ./build
# Usage:  ./build.sh                 (restores reference libs from NuGet)
#         ./build.sh /path/to/dlls   (offline, folder of reference dlls)
set -euo pipefail
cd "$(dirname "$0")"

extra=()
if [ "${1:-}" != "" ]; then extra+=("-p:RoR2LibsDir=$1"); fi

dotnet build src/AlchemistMod/AlchemistMod.csproj -c Release "${extra[@]}"

rm -rf build && mkdir -p build/package/plugins
cp src/AlchemistMod/bin/Release/netstandard2.1/AlchemistMod.dll build/package/plugins/
cp thunderstore/manifest.json thunderstore/icon.png README.md build/package/
(cd build/package && zip -qr ../Alchemist.zip .)
echo "Done: build/Alchemist.zip"
