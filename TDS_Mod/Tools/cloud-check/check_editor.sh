#!/bin/bash
# check_editor.sh <EditorDir>: compile-checks a vessel's Unity editor generator against UnityEditor/UnityEngine refs.
H=$(cd "$(dirname "$0")" && pwd)
M=/usr/lib/mono/4.5
mono $H/roslyn/tools/csc.exe -nologo -nostdlib+ -noconfig -target:library -langversion:9 -nowarn:CS0618,CS0219,CS0168,CS0414,CS0649 \
  -r:$M/mscorlib.dll -r:$M/System.dll -r:$M/System.Core.dll -r:$M/Facades/netstandard.dll \
  -r:$H/sdk/lib/UnityEngine.dll -r:$H/sdk/lib/UnityEditor.dll -r:$H/stubs/ModToolStubs.dll -out:/tmp/editor_check.dll "$1"/*.cs 2>&1 | grep -E "error|warning CS0" | grep -v "CS1701\|CS1702" ; echo "editor check done: $1"
