# 11 · Tooling: reading the game, driving Unity, PowerShell

## 1. Reading the game's code without a decompiler

The game ships `Trivial.Mono.Cecil.dll`; Windows PowerShell can load it and walk the IL of `Assembly-CSharp.dll`.
Setup (once): copy both DLLs next to the scripts in `tools/cecil/`:

```powershell
Copy-Item "D:\Steam\steamapps\common\TDS\TDS\DATA\Managed\Assembly-CSharp.dll" tools\cecil\
Copy-Item "D:\Steam\steamapps\common\TDS\TDS\DATA\Managed\Trivial.Mono.Cecil.dll" tools\cecil\
```

| Script | Use |
|---|---|
| `api-dump.ps1 [-Out file]` | every type with fields and method signatures (`reference/api_assembly_csharp.txt` is one, from 2026-10-02); diff after a game update |
| `il.ps1 -Type T -Method M [-Max n] [-From i]` | a method's IL in readable form; nested types as `Outer/Inner` (`"Entity_Master_Client/<Start>d__25"` for a coroutine) |
| `callers.ps1 -Type T -Members A,B` | every game method that calls or reads `T.A`/`T.B` |
| `strings.ps1 -Want 'Text' [-Like]` | every method that uses a string literal (find who reads a save key, a Vis name, a stat name) |
| `survey.ps1 -Match 'Regex' [-Out f]` | per matching type: fields, and per method its string literals and the engine members it uses (fast overview of an AI) |

Typical questions and how they were answered:

- *How does the game attach a debuff look?* `Select-String reference\api_assembly_csharp.txt 'Vfxdebuff'` → the
  declaring type (`Entity_Master_Radio`) → `il.ps1 -Type Entity_Master_Radio -Method Vfxdebuff_ClientRpc` → it resolves
  `On.Worldobject` and calls `Helper_Vfxtools.Debuff` → `il.ps1 -Type Helper_Vfxtools -Method Debuff`.
- *What does the game pass as the target?* `callers.ps1 -Type Entity_Master_Radio -Members Vfxdebuffcheck` → read one
  caller → it passes `Entity_Master.Beingref.Value`.
- *Why is my value empty?* `callers.ps1 -Type Link_Being -Members Beingrefparallel` → `Game_Entity.Spawn` copies the
  struct before setting `Worldobject`.

Read the IL around the call (`Select-String -Context 8,4`) instead of whole methods; coroutines and lambdas live in
nested compiler types (`<Name>d__N`, `<>c__DisplayClassN_M`).

**In-game data dumps**: a debug module can log anything the game has loaded (`examples/code/Sprigg_98_Survey.cs`,
`Sprigg_95_Research.cs`): every vessel's beings, AIs, stats, trees, every enemy, every Vfx and command name, every journal page.
`reference/vessel_data_survey.txt` is that dump for the shipped vessels.

## 2. Unity through UnityMCP

MCP for Unity (CoplayDev) 9.3.2, registered as the user-scope MCP server `UnityMCP` (stdio) →
`C:\Users\Dimitri\.mcp-for-unity\venv-9.3.2\Scripts\mcp-for-unity.exe`; the editor side has `UseHttpTransport=false`,
bridge port 6400.

| Tool | Use |
|---|---|
| `execute_menu_item` | `Tools/<Name>/Build All Assets`, `Build Mod Prefab`, `Park Other Mods`, `Export Mod`, `Restore Parked Mods`, `Preview Sprites`, `Preview Models`; `Tools/ModKit/Build Queue` for several mods in one call (`TDS_Mod/Tools/pc/README.md` step 6, `09` #76) |
| `refresh_unity` (`compile: request`) | make Unity import and compile edited scripts; for a **new folder** run `execute_menu_item("Assets/Refresh")` too (refresh_unity alone left a new folder unimported) |
| `read_console` (`types: ["error"]`) | compile errors and the generator's logs |

Gotchas:
- Long menu items (Export, Build All Assets) time out over MCP and **are retried**: Export runs twice. Verify results
  on disk (timestamps, file counts, `Build/prefab_log.txt`), not from the tool's reply.
- After editing editor scripts, confirm the recompile happened (`Library/ScriptAssemblies/Assembly-CSharp-Editor.dll`
  newer than the edit) before running the generator.
- Don't use HTTP transport (port 8080 is NVIDIA Broadcast's). Don't let the package or `uvx` reinstall the server: uv
  fails on pywin32 (Defender locks a file), and unpinned dependencies pull an incompatible `mcp` 2.x. The venv was built
  from pins in `~/.mcp-for-unity/requirements-9.3.2.txt`.

## 3. PowerShell 5.1 gotchas

- No `&&`, `||`, ternary, `??`; chain with `;` and `if ($?) {}`.
- `--%` swallows the rest of the line, including `;` commands.
- In an array literal the comma binds before `+`: `@('a', 'b' + $x)` is three items. Parenthesise.
- Hashtables, including `[ordered]@{}`, **ignore key case**: `$h['Mummer']` and `$h['MUMMER']` are one key (the first
  scaffold script silently renamed nothing because of this). Use a list of pairs for case-sensitive maps.
- Variable names ignore case too: a loop over `$r` overwrites `$R`.
- `Set-Content`/`Add-Content` write ANSI by default; `Get-Content -Raw` reads UTF-8 files without BOM as ANSI. Use
  `[IO.File]::ReadAllText(p, [Text.Encoding]::UTF8)` / `WriteAllText(p, t, (New-Object Text.UTF8Encoding($false)))`.
- A `-File` script gets `-Define A,B` as one string; use `-Command "& 'script.ps1' -Define A,B"`.
- `Get-Process TDSgame | % { $_.CloseMainWindow() }` closes the game; there is no process named `TDS`.
- Big logs: stream with `Get-Content | Where-Object` and filter out `[<Name>Dump]` lines before matching.

## 4. Files on this PC

| | |
|---|---|
| Game DLLs | `D:\Steam\steamapps\common\TDS\TDS\DATA\Managed` |
| Game assets (one Addressables bundle) | `TDS\DATA\StreamingAssets\aa\StandaloneWindows64\defaultlocalgroup_assets_all_*.bundle` (~300 MB). To look at shipped art, load it in the TDS_Mod editor with stub ScriptableObject classes named like the game's types (typetrees bind by field name); delete the stubs before exporting. In practice an in-game dump (`Shot`, art dump) is easier. |
| Localization (readable text) | `TDS\DATA\resources.assets` (English key, translations) |
| Mod tools package | `TDS\TDS Mod Tools.unitypackage` (newer than the copy in `Desktop\ClaudeProjects`) |
| Claude CLI | not on PATH; bundled under `%APPDATA%\Claude\claude-code\<ver>\claude.exe` |
