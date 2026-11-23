# Builds the mod and makes a Thunderstore / r2modman zip in ./build
# Usage:  ./build.ps1            (restores reference libs from NuGet)
#         ./build.ps1 -LibsDir "C:\path\to\dlls"   (offline, folder of reference dlls)
param([string]$LibsDir = "")

$ErrorActionPreference = "Stop"
$extra = @()
if ($LibsDir -ne "") { $extra += "-p:RoR2LibsDir=$LibsDir" }

dotnet build src/AlchemistMod/AlchemistMod.csproj -c Release @extra

$out = "build/package"
Remove-Item -Recurse -Force build -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path "$out/plugins" | Out-Null
Copy-Item src/AlchemistMod/bin/Release/netstandard2.1/AlchemistMod.dll "$out/plugins/"
Copy-Item thunderstore/manifest.json, thunderstore/icon.png, README.md $out
Compress-Archive -Path "$out/*" -DestinationPath build/Alchemist.zip -Force
Write-Host "Done: build/Alchemist.zip"
