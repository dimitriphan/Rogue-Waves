#!/bin/bash
# run_preview.sh <EditorDir> <Prefix> <OutDir> [s|m|a flags]
# Compiles the vessel's editor generators (all Editor/*.cs except the builder, mod-prefab and preview menus, which
# need UnityEditor) with UnityLite, and runs them.
set -e
H=$(cd "$(dirname "$0")" && pwd)
ED=$1; P=$2; OUT=$3; FLAGS=${4:-sma}
NS=${P}Build
W=$(mktemp -d)
SPR=$(python3 "$H/sprite_calls.py" "$ED/${P}Builder.cs" "$P")
python3 - "$H/Driver.template.cs" "$W/Driver.cs" "$NS" "$P" "$SPR" <<'PY'
import sys
t=open(sys.argv[1]).read().replace('{NS}',sys.argv[3]).replace('{P}',sys.argv[4]).replace('{SPRITES}',sys.argv[5])
open(sys.argv[2],'w').write(t)
PY
FILES=$(ls $ED/*.cs | grep -v -E "(Builder|ModPrefab|Preview)\.cs$")
mono $H/../roslyn/tools/csc.exe -nologo -nowarn:CS0618,CS0219,CS0168,CS0414,CS0649 -langversion:9 -r:$H/UnityLite.dll -r:/usr/lib/mono/4.5/System.IO.Compression.dll -r:/usr/lib/mono/4.5/System.Core.dll -r:/usr/lib/mono/4.5/Facades/netstandard.dll -out:$W/run.exe $FILES $W/Driver.cs | grep -v "warning" || true
MONO_PATH=$H mono $W/run.exe $OUT $FLAGS
rm -rf $W
