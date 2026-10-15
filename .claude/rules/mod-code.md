---
paths:
  - "TDS_Mod/ModSource/**/*.cs"
  - "TDS_Mod/ModSource/**/*.ps1"
  - "TDS_Vessel_Creator/template/**"
  - "TDS_Vessel_Creator/kit/**"
  - "TDS_Vessel_Creator/examples/code/**"
---
# Mod code

Read `TDS_Vessel_Creator/docs/09_PITFALLS.md` before writing, `docs/05_CODE_COOKBOOK.md` for the module layout and
recipes, `docs/07_ONLINE.md` for anything that changes state (host decides, engine syncs, clients only draw).

- Every id carries the vessel name (`Hero_<Name>`, `Tower_<Name>x`, `Mob_<Name>x`, `Debuff_<Name>x`). One global table.
- `ModSource/Shared` is the live kit (1.6; `TDS_Vessel_Creator/kit/Shared` is a copy, keep them equal). Changing it:
  keep tab order and the first 10 boat positions, bump the version in `TDS_Vessel_Creator/kit/README.md` and the file
  headers, compile every vessel.
- The game compiles with Roslyn 3.8, no `System.dll`/`System.Core`, no `System.IO`/`System.Reflection`.
- Check here before pushing (setup once per container, `TDS_Mod/Tools/cloud-check/README.md`):

  ```bash
  cd TDS_Mod/ModSource
  python3 ../Tools/cloud-check/check_mod.py <Name>/Code Shared <Name> ""
  python3 ../Tools/cloud-check/check_mod.py <Name>/Code Shared <Name> "<NAME>_SELFTEST,<NAME>_SHOTS"
  ../Tools/cloud-check/check_editor.sh ../Assets/<Name>Mod/Editor
  ```

- A clean compile is not a test. Say so in `IMPLEMENTATION_NOTES.md`: compiled here, self-test and play on the PC.
