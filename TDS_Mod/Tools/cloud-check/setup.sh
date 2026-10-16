#!/bin/bash
# setup.sh <TDS_Vessel_Creator folder>: one-time setup on a Linux box without Unity or the game (needs mono-devel,
# python3, curl, unzip). Fetches Roslyn 3.8 (the compiler the game ships), Unity's engine modules and UnityEditor as
# reference assemblies from NuGet, lists which module holds each Unity type, builds the stubs and UnityLite.
set -e
H=$(cd "$(dirname "$0")" && pwd)
VC=$(cd "${1:?usage: setup.sh <path to the unzipped TDS_Vessel_Creator>}" && pwd)
command -v mono >/dev/null || { echo "mono is missing: apt-get install mono-devel"; exit 1; }
fetch() {   # <package id> <version> <folder>
  local lower; lower=$(echo "$1" | tr 'A-Z' 'a-z')
  [ -d "$H/$3" ] && return 0
  curl -sSfL -o "$H/$3.nupkg" "https://api.nuget.org/v3-flatcontainer/$lower/$2/$lower.$2.nupkg"
  # Unzip into a temporary folder and rename it at the end, so a half-unzipped folder never counts as done.
  rm -rf "$H/$3.tmp" && mkdir -p "$H/$3.tmp" && (cd "$H/$3.tmp" && unzip -q "../$3.nupkg") && mv "$H/$3.tmp" "$H/$3"
}
fetch Microsoft.Net.Compilers 3.8.0 roslyn
fetch UnityEngine.Modules 2021.3.33 unitymods
fetch Unity3D.SDK 2021.1.14.1 sdk
mono $H/roslyn/tools/csc.exe -nologo -out:$H/listtypes.exe $H/listtypes.cs
mono $H/listtypes.exe $H/unitymods/lib/net45/*.dll > $H/unitytypes.tsv 2>/dev/null
bash $H/build_stubs.sh "$VC"
M=/usr/lib/mono/4.5
mono $H/roslyn/tools/csc.exe -nologo -target:library -langversion:9 -nowarn:CS0618 -r:$M/System.IO.Compression.dll -r:$M/System.Core.dll \
  -out:$H/lite/UnityLite.dll $H/lite/UnityLite.cs
echo "ready"
