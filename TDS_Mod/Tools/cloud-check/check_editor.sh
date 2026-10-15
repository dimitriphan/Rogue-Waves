#!/bin/bash
# check_editor.sh <EditorDir>: compile-checks a vessel's Unity editor generator against UnityEditor/UnityEngine refs.
# Exits 1 when the compile has errors.
H=$(cd "$(dirname "$0")" && pwd)
M=/usr/lib/mono/4.5
mkdir -p "$H/out"
OUT=$(mono $H/roslyn/tools/csc.exe -nologo -nostdlib+ -noconfig -target:library -langversion:9 -nowarn:CS0618,CS0219,CS0168,CS0414,CS0649 \
  -r:$M/mscorlib.dll -r:$M/System.dll -r:$M/System.Core.dll -r:$M/Facades/netstandard.dll \
  -r:$H/sdk/lib/UnityEngine.dll -r:$H/sdk/lib/UnityEditor.dll -r:$H/stubs/ModToolStubs.dll -out:"$H/out/editor_check.dll" "$1"/*.cs 2>&1 | grep -E "error|warning CS0" | grep -v "CS1701\|CS1702")
[ -n "$OUT" ] && echo "$OUT"
echo "editor check done: $1"
echo "$OUT" | grep -q "error" && exit 1
exit 0
