#!/bin/bash
# build_stubs.sh <TDS_Vessel_Creator folder>: rebuilds the reference stubs: ThirdParty.dll (hand-written stand-ins for
# Netcode, Collections, UI, TMPro, UniTask), Assembly-CSharp.dll (generated from the Vessel Creator's API dump) and
# ModToolStubs.dll (the two ModTool editor classes the vessel builders call).
set -e
H=$(cd "$(dirname "$0")" && pwd)
VC=${1:?usage: build_stubs.sh <path to the unzipped TDS_Vessel_Creator>}
CSC="mono $H/roslyn/tools/csc.exe"
MONO=/usr/lib/mono/4.5
UREFS=""; for f in $H/unitymods/lib/net45/*.dll; do UREFS="$UREFS -r:$f"; done
BASE="-nologo -nostdlib+ -noconfig -target:library -langversion:9 -r:$MONO/mscorlib.dll -r:$MONO/Facades/netstandard.dll -r:$MONO/System.dll -r:$MONO/System.Core.dll"
cd $H/stubs
$CSC $BASE $UREFS -out:ThirdParty.dll ThirdParty.cs | grep -E "error" || true
python3 $H/gen_stubs.py "$VC/reference/api_assembly_csharp.txt" $H/unitytypes.tsv GameStubs.cs 2>/dev/null
$CSC $BASE -nowarn:CS0108,CS0114,CS0618,CS0169 $UREFS -r:ThirdParty.dll -out:Assembly-CSharp.dll GameStubs.cs | grep -E "error" || true
$CSC $BASE -out:ModToolStubs.dll ModToolStubs.cs | grep -E "error" || true
ls -la ThirdParty.dll Assembly-CSharp.dll ModToolStubs.dll
