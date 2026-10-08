# PC round tools

These scripts run one PC round on Dimitri's PC (Windows PowerShell 5.1, Unity 2022.3.41f1, the game) with as few
manual steps as possible. A round is: get the code onto the PC, apply it, build, self-test, and release once the tests
pass. Cloud threads write the handoff; a Remote Control session on the PC runs the steps below.

| Script | Where it runs | Does |
| --- | --- | --- |
| `make_handoff.py` | cloud | Packs a handoff from git: `TDS_Mod/Handoff/<Name>/` (HASHES.txt, README.md) to commit, and optionally the same as a zip |
| `Get-Handoff.ps1` | PC | Fetches a handoff from a branch (needs the read-only token below), laid out like an unpacked zip |
| `Apply-Handoff.ps1` | PC | Checks (default) or applies (`-Apply`) a handoff into `C:\Users\Dimitri\TDS_Mod`, with a backup |
| `Get-PcManifest.ps1` | PC | Writes `handoff\pc_manifest.txt`, the hash of every source file on the PC (`-Apply` runs it too) |
| `HandoffHash.ps1` | PC | The shared hash: sha256 of text with BOM and CRs removed, first 16 hex digits |
| `Run-SelfTests.ps1` | PC | Backs up the save, then runs each vessel's self-test in its own launch and writes a short summary; `-BootCheck` for release builds |
| `../../Assets/ModKit/Editor/ModKitPipeline.cs` | PC (Unity) | Tools > ModKit > Build Queue: Build All Assets, Build Mod Prefab, Export and Restore for every vessel in `handoff\queue.txt` |

## One-time setup (Dimitri)

1. On github.com: Settings > Developer settings > Personal access tokens > Fine-grained tokens > Generate new token.
   Repository access: only `dimitriphan/Rogue-Waves` (a token made for ModProjects does not reach this repository). Permissions: Contents, Read-only. Expiry: 90 days.
2. Save the token as the only line of `C:\Users\Dimitri\.modprojects_token`.
3. The scripts reach the PC once in a handoff zip; after that every handoff carries the current copies.

Without the token, everything still works with a zip: the thread packs `make_handoff.py ... --zip`, and Dimitri saves
it into `Desktop\ClaudeProjects\handoff\<Name>` and unpacks it there.

## A round

Cloud thread:

1. Take the PC lock: `pc/QUEUE.md` among the Claude Project's files (add it to the Rogue Waves project if it is
   missing; only one session works in `TDS_Mod` at a time).
2. Compile-check every vessel the round touches (`Tools/cloud-check`).
3. Pack against what the PC has. Use `--manifest` with the newest `pc_manifest.txt` the PC sent, or else `--since`
   with the commit of the last handoff the PC applied:
   `python3 TDS_Mod/Tools/pc/make_handoff.py <Name> --manifest pc_manifest.txt --only Argo,Shared`
4. Commit and push `TDS_Mod/Handoff/<Name>/`.
5. Ask Dimitri for one typed sentence covering the whole round, for example: "Fetch and apply handoff Argo_round10,
   build and self-test Argo, and if it passes, release and install Argo."
6. Start the PC session with a short brief: "Follow handoff\<Name>\README.md; Dimitri's OK is in the thread."

PC session (`<h>` is `Desktop\ClaudeProjects\handoff`):

1. Fetch the handoff: `Get-Handoff.ps1 -Ref <branch> -Name <Name>`. Without the token, use the saved zip.
2. Check it: `<h>\<Name>\Apply-Handoff.ps1`. Every line must read up to date, will copy, will add or patch done.
   Anything else: stop and report.
3. Apply it: `Apply-Handoff.ps1 -Apply`. This backs up what it replaces and writes a fresh `pc_manifest.txt`.
4. Let Unity compile: `execute_menu_item("Assets/Refresh")`, then wait until it has finished.
5. Build the code: `Build-<Name>Mod.ps1 -Define <NAME>_SELFTEST` for each vessel. Then write `<h>\queue.txt`, one
   vessel per line, adding ` twice` for Sprigg.
6. Build and export: `execute_menu_item("Tools/ModKit/Build Queue")`, once. Then read `<h>\queue_status.txt` until it
   ends in `QUEUE DONE`. Don't run the menu item again if the MCP call times out.
7. Self-test: `Run-SelfTests.ps1 -Vessels <A>,<B>`. Report each `*_summary.txt` in full. Send the `*_map.txt` files
   too; they hold the map snapshot.
8. Release, if the tests pass (0 FAIL; SETUP lines only under the release rule in `docs/08` section 5) and Dimitri's
   sentence covers it: `Build-<Name>Mod.ps1` with no `-Define`, the queue again, then
   `Run-SelfTests.ps1 -Vessels <all installed vessel mods> -BootCheck`, then zip as `docs/04` says.
9. Run `Get-PcManifest.ps1` if anything changed after step 3. Report, release the lock, and stop the session.

Only one session works in TDS_Mod at a time: an export parks the other vessels' folders.
