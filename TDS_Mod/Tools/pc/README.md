# PC round tools

These scripts run one PC round on Dimitri's PC (Windows PowerShell 5.1, Unity 2022.3.41f1, the game) with as few
manual steps as possible. A round is: get the code onto the PC, apply it, build, self-test, and release once the tests
pass. Cloud threads write the handoff; a Remote Control session on the PC runs the steps below.

| Script | Where it runs | Does |
| --- | --- | --- |
| `make_handoff.py` | cloud | Packs a handoff from git: `TDS_Mod/Handoff/<Name>/` (HASHES.txt, README.md) to commit, and optionally the same as a zip |
| `Get-Handoff.ps1` | PC | Fetches a handoff from a branch (with the signed-in `gh`, else the read-only token below), laid out like an unpacked zip |
| `Apply-Handoff.ps1` | PC | Checks (default) or applies (`-Apply`) a handoff into `C:\Users\Dimitri\TDS_Mod`, with a backup |
| `Get-PcManifest.ps1` | PC | Writes `handoff\pc_manifest.txt`, the hash of every source file on the PC (`-Apply` runs it too) |
| `HandoffHash.ps1` | PC | The shared hash: sha256 of text with BOM and CRs removed, first 16 hex digits |
| `Run-SelfTests.ps1` | PC | Backs up the save, then runs each vessel's self-test in its own launch and writes a short summary; `-BootCheck` for release builds. The mode's whole flow (`-Vessels RogueWavesMode -Also <the other parts>`) takes about 15 minutes: pass `-TimeoutMinutes 30`, or the 12-minute default ends it before the weather's checks (2026-11-05) |
| `Run-Play.ps1` | PC | Runs one vessel's play build (its `Code/98_Play.cs` with kit `ModKitPlay`): backs up the save, launches, waits for `PLAY DONE`, keeps the log and writes the play lines, HUD dumps and captures into `handoff\play_logs` (`docs/reports/PLAYTEST_2026-10-26.md`) |
| `Run-TwoPlayers.ps1` | PC | Runs the mode's two-player round (`-Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_TWOPC`): a host and a real second player, two copies of the game on the loopback address. Copies the game to `D:\TDS_second` once, backs up and restores the save and the registry, and starts, kills and restarts the second copy when the host's log says; logs in `handoff\twoplayer_logs`. `-Extra` passes flags to both copies: `-rwskip` (only the tail), `-rwshort`, `-rwhosthero X -rwsecondhero Y`, `-rwmaps a,b`; the solo gaps build (`ROGUEWAVESMODE_GAPS`) takes `-rwonly rules,effects,scales,foes,ten,tour,blocked,hud`, and in a launch of its own `-rwonly light` (a map's lighting and fog, with `ROGUEWAVESMODE_SHOTS` for its pictures, `docs/reports/LIGHTING_2026-11-05.md` §4) |
| `../../Assets/ModKit/Editor/ModKitPipeline.cs` | PC (Unity) | Tools > ModKit > Build Queue: Build All Assets, Build Mod Prefab, Export and Restore for every vessel in `handoff\queue.txt`; Tools > ModKit > Build Pack: the release pack (below) |
| `Build-Pack.ps1` | PC | The release pack's code (each part's release build with the pack version from `TDS_Mod/ModSource/Pack/pack.txt`), then with `-Zip` the players' `RogueWaves-<version>.zip` in the handoff folder |

**Fix readings** (`docs/reports/TESTING_ROUND_3_2026-11-05.md`): a line "TEST INFO FINDING <id>: SEEN|not seen" reads the same before and after a fix and fails nothing. A vessel: `-Define <NAME>_SELFTEST,<NAME>_REVIEW` (Spool with `MODKIT_PLAY` for the Crafted by hand). The mode, solo: the gaps build with `-Extra "-rwonly review"`, and in a launch each `-rwonly throw`, `split`, `artrio`, `a2 -rwthrowicons`. Two players: `-Extra "-rwshort -rwreview -rwhosthero X -rwsecondhero Y"`.

## One-time setup (Dimitri)

None if the PC's GitHub CLI is signed in (`gh auth status`): `Get-Handoff.ps1` uses it. Otherwise:

1. On github.com: Settings > Developer settings > Personal access tokens > Fine-grained tokens > Generate new token.
   Repository access: only `dimitriphan/Rogue-Waves`. Permissions: Contents, Read-only. Expiry: 90 days.
2. Save the token as the only line of `C:\Users\Dimitri\.modprojects_token`.
3. The scripts reach the PC once in a handoff zip; after that every handoff carries the current copies.

Without `gh` or the token, everything still works with a zip: the thread packs `make_handoff.py ... --zip`, and Dimitri saves
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
   build and self-test Argo, then remove it from the Mods folder."
6. Start the PC session with a short brief: "Follow handoff\<Name>\README.md; Dimitri's OK is in the thread."

PC session (`<h>` is `Desktop\ClaudeProjects\handoff`):

1. Fetch the handoff: `Get-Handoff.ps1 -Ref <branch> -Name <Name>`. Without `gh` or the token, use the saved zip.
2. Check it: `<h>\<Name>\Apply-Handoff.ps1`. Stop and report if any line reads `DIFFERS`, `MISSING FROM HANDOFF`,
   `HANDOFF COPY DAMAGED`, `MISSING` or `PATCH DOESN'T FIT`, or the script exits non-zero (it does on any of them).
   Up to date, will copy, will add, will replace, will delete, already gone, will patch and patch done are all fine.
3. Apply it: `Apply-Handoff.ps1 -Apply`. This backs up what it replaces and writes a fresh `pc_manifest.txt`.
4. Let Unity compile: `execute_menu_item("Assets/Refresh")`, then wait until it has finished.
5. Build the code: `Build-<Name>Mod.ps1 -Define <NAME>_SELFTEST` for each vessel. Then write `<h>\queue.txt`, one
   vessel per line, adding ` twice` for a vessel whose assets need two passes. Since 2026-11-04 the game's Mods folder holds the release build of
   every pack part for Dimitri's own play (kit `TDS_Vessel_Creator/README.md`, "Facts about this PC"): park them in
   `handoff\parked` now, put them back when the round is done, and say in `pc/QUEUE.md` what was left (no script does
   either).
6. Build and export: `execute_menu_item("Tools/ModKit/Build Queue")`, once. Then read the `[ModKit]` lines in
   `%LOCALAPPDATA%\Unity\Editor\Editor.log` (and the `.info` file's time) until the queue is done: MCP replays can
   overwrite `<h>\queue_status.txt` after a good run (kit `docs/09` #76). Don't run the menu item again if the MCP call
   times out.
7. Self-test: `Run-SelfTests.ps1 -Vessels <A>,<B>`. Report each `*_summary.txt` in full. Send the `*_map.txt` files
   too; they hold the map snapshot.
8. Release, if the tests pass (0 FAIL; SETUP lines only under the release rule in `docs/08` section 5) and Dimitri's
   sentence covers it: `Build-<Name>Mod.ps1` with no `-Define`, the queue again, then
   `Run-SelfTests.ps1 -Vessels <the released parts> -BootCheck`, then zip as `docs/PACK.md` §5 says (the pack's
   zip; no part ships alone). The Build Queue exports each part as its own dev mod; the pack zip comes from the Pack
   step ("The release pack" below). Leave the game's Mods folder as Dimitri had it afterwards.
9. Run `Get-PcManifest.ps1` if anything changed after step 3. Report, release the lock, and stop the session.

Only one session works in TDS_Mod at a time: an export parks the other vessels' folders.

**The PC's permission guard and Dimitri's one OK** (RogueLab round 3, 2026-10-09): the PC session's auto mode refused
`Apply-Handoff.ps1 -Apply` (a downloaded script writing into `TDS_Mod`) while his sentence was only the session's
starting task; it went through once his thread message reached the session as a turn of its own. So one sentence
does cover the round: a blocked PC session waits for that message (or asks the thread to forward it) and the cloud
thread never asks Dimitri for a second phrase. To stop the prompt altogether, Dimitri can allow `Apply-Handoff.ps1`
and `Run-SelfTests.ps1` in the PC folder's Claude Code permission settings.

**Results reach the cloud thread as messages.** While a PC session runs, the thread's own folder tools are paused, and
after it ends they can stay missing for the rest of that turn. So the PC session sends the round's lines back with
`send_message` (each message under 50 KB, `<NAME> part i/n`), and the thread saves them from its own transcript.

## A change made on the PC

A fix a PC session makes goes back to the repository as a diff of what it changed, applied to the newest base
branch, never as whole files: the PC's `TDS_Mod` can be older than the base. PR #82 copied seven PC files over
PR #80's and put back the helper names kit 1.8 had moved, so the mode's release build failed with 6 errors
(`docs/reports/CODE_REVIEW_2026-11-04.md` §2). Run the cloud check on every part the diff touches before the PR.

## The release pack

Run in a PC round after the self-test passed (`docs/PACK.md`; `<h>` is `Desktop\ClaudeProjects\handoff`):

1. Move every dev build of a pack part out of `TDS\Mods` (`YosseleMod`, ...): it would register the same vessel.
2. `Build-Pack.ps1`: each part's release code, its `Version` set to the pack version.
3. `execute_menu_item("Tools/ModKit/Build Pack")`, once; read `<h>\pack_status.txt` (or the `[ModKit]` lines of
   Editor.log) until it ends in `PACK DONE ok` or `PACK FAILED`. The prefab names and the parked folders are put back
   either way.
4. `Run-SelfTests.ps1 -Vessels Yossele -BootCheck -Pack RogueWaves`: the pack enabled alone, every part's icons line
   and its `[RogueWaves:<Part>] v<version>` line.
5. `Build-Pack.ps1 -Zip`: `<h>\RogueWaves-<version>.zip` (the `RogueWavesMod` folder, disabled, and `README.txt`).
