# Quality checks 4 to 7 (2026-11-05)

Dimitri, in the project (2026-11-05 00:30Z): "81 is merged, resume with new project instructions and plugins in mind."
The coordinator's list of token-heavy checks had seven items; 1 to 3 ran on 2026-11-04 (PRs #84, #81, #83). These are
the other four: **4** save and load fuzz tests for the campaign save, **5** the Scope Editor and the scope against the
docs and code since the rename, **6** an art and sprite audit, **7** an audit of `docs/PLAN.md` Part C and its Log.
Base: `claude/phase-1-6lfnar` after PR #81 (`c384019`). Cloud only: nothing touched the PC.

**Pass cap.** One pass per check, then one re-check of every fix (the fuzz rerun, the compiles, the path checks).
**An issue** is a save path that throws, hangs, strands the boat or loses data; a statement that contradicts the code,
Part C or the folders; a broken path or file reference; a stale name (the Cardinal names before 2026-11-04) or status;
a sprite that is referenced and missing, unlisted, the wrong size, or private (a base vessel's game sprite) or from a mod
before the scope. Not issues: style, wording, and what PR #78 kept on purpose (the kit's Mummer template, Riptide and
RogueLab, dated reports, the PLAN Log and Decisions as history, Dimitri's own words).

| Check | Found | Fixed here | Left for Dimitri |
| --- | --- | --- | --- |
| 4 Save and load fuzz | 3 bugs (confirmed by the fuzz, before and after), 5 plausible | 3 | 1 (§1.4) |
| 5 Scope Editor and scope | 2 | 1 (a note claiming a count the code doesn't show) | 1 (the editor and his copy are behind) |
| 6 Art and sprites | 0 | 0 | 0 |
| 7 PLAN Part C and Log | 6 | 5 | 1 (the old Decisions keep the old names, as history) |

## 1. Check 4: save and load fuzz

### 1.1 What was written

`TDS_Mod/ModSource/RogueWavesMode/Code/98_SaveFuzz.cs` (`ROGUEWAVESMODE_SELFTEST` only, out of a release build):
seeded random and damaged values through the campaign save's readers and writers, seven checks, 300 cases each where a
case is cheap (100 where it writes the save):

| Check | Invariant |
| --- | --- |
| F1 | a player line round-trips through `RWRun.Encode` and `Decode` (GUID, `name:` and `session:` keys; names with commas, quotes, tabs, accents, CJK) |
| F2 | a damaged player line (fields dropped, added, emptied, out of range) never throws, never gives negative Pure Aura or an empty id |
| F3 | 0 to 6 players and 0 to 8 Cardinals round-trip through the save (`RWRun.Save`, `Clear`, `Load`) |
| F4 | a damaged player count (`-5`, `abc`, `2147483647`, ...) or Cardinals key (`,a#1,,b#2,`) loads at most `RWRun.MaxSaved` players, no empty id, in under 500 ms |
| F5 | a chart world's name round-trips through `RWWorld.Name` and `Parse` (any seed, 0 to 10 maps, retries -1 to 3) |
| F6 | a damaged campaign key (seed, route, retries, replay, playing) never throws, and what `RWCampaign.Saved` accepts is a route on the chart, 0 to 3 retries, and a map to replay or in play that is one of the next row's |
| F7 | a whole campaign's keys, along real links, round-trip through `Saved` |

It runs twice:

- **In game**, from the mode's self-test (`99_SelfTest.cs`, on the boat after K1/I1 and C0, before its campaign starts),
  against the real save, in memory only: `RWUtil.HoldFlush` keeps every write off the disk until each key it touched
  is put back (with the players and Cardinals in memory), then it flushes once, so a crash mid-fuzz leaves the file as
  it was. The six chart seeds it uses, and seeds 0 and 5 from two damaged seed keys, log their `CHART` lines once each. The mode's self-test should now read **155**
  (148 + F1-F7). **Not run on the PC yet.**
- **Here**, with `TDS_Mod/Tools/cloud-check/save_fuzz.py`: it copies the save's methods out of the mode's code as they
  stand at each run (`RWChart` whole, `RWPlayer`, `RWRun`'s save methods, `RWWorld`'s names and routes,
  `RWCampaign.Saved`, the keys), puts a dictionary in place of the game's save and 45 made-up maps in place of the pool,
  and compiles them with the same `98_SaveFuzz.cs`. A method that moved or was renamed is a compile error, never a pass.

### 1.2 Results here

On the base, before any fix (the huge count swapped for 200,000 so the run could end):

```
PASS F1 a player line round-trips through Encode and Decode (300 cases)
FAIL F2 ... (300 cases): 31 broke; first case 3: an empty Trinket id from '99999999999999\t,,,\t...'
PASS F3 players and Cardinals round-trip through the save (100 cases)
FAIL F4 ... (100 cases): 56 broke; first case 2 (count '-5', Cardinals ',EAkfsMB2l#6,'): an empty Cardinal id; slowest 314 ms
PASS F5 a chart world's name round-trips through Name and Parse (300 cases)
FAIL F6 ... (300 cases, 216 accepted, 6 routes cut back): 61 broke; first case 5 (seed '20284766', route '13'): route 13 leaves the chart of seed 20284766
PASS F7 a whole campaign's keys round-trip through Saved (100 cases)
```

With the count as written (`2147483647`) the run did not end in 120 s: 200,000 entries took 314 ms here, so the
largest count is about an hour of reads. After the fixes (`cd TDS_Mod/ModSource && python3 ../Tools/cloud-check/save_fuzz.py`):

```
PASS F1 ... PASS F2 ... PASS F3 ...
PASS F4 a damaged player count or Cardinals key loads safely and fast (100 cases): slowest 1 ms
PASS F5 ... 
PASS F6 a damaged campaign key never throws or strands the boat (300 cases, 216 accepted, 61 routes cut back)
PASS F7 ...
save fuzz: all passed; 0 keys left in the stand-in save, 61 warnings logged
```

"Ran here" means the mode's own methods on a stand-in save, not Easy Save 3 in the game.

### 1.3 Fixed (all on paths a normal save never takes: a damaged or hand-edited save)

| # | Where | Was | Now |
| --- | --- | --- | --- |
| SAVE-1 | `06_Campaign.cs` `Saved` | A saved route that leaves the chart was accepted. `RWWorld.Ensure` then never builds that world ("the route leaves the chart", logged once), so after Continue the boat waits for it with no end. The comment above `Saved` said a damaged key can't strand the boat; only the replay and playing columns were checked | The route is cut back to its last charted map, with one warning in the log, and the replay and playing columns, saved for the longer route's next row, are dropped |
| SAVE-2 | `14_Run.cs` `Load`, `Forget`, `SavedCrew` | The player count was read as is: `2147483647` reads the save two billion times on Continue (and on the Continue card's hover text), about an hour with the game frozen | At most `RWRun.MaxSaved` (64) players are read, and `Save` writes no more than that (a warning past it); the game has 2 to 4 |
| SAVE-3 | `14_Run.cs` `Load`, `Decode` | `"a,,b"` or a trailing comma loaded an empty Cardinal, Direction or Trinket id; a negative Pure Aura loaded as is | Empty ids are dropped; Pure Aura below 0 loads as 0 (it is only ever spent down to 0, `17_Cardinals.cs` `Buy`, `18_Shop.cs`) |

Release, `ROGUEWAVESMODE_SELFTEST` and `_SELFTEST,_GAPS` compile at 0 errors (§6).

### 1.4 Plausible, not fixed

1. **A player's key can change between sessions.** `RWRun.KeyOf` keys a player by their kept GUID, else
   `name:<Steam name>`, else `session:<id>`. A player first seen under a fallback and later under their GUID (or the
   reverse) gets a second entry, and what the first one held (Pure Aura, Directions, Trinkets) is left behind. Whether
   the host ever lacks a GUID for a seated player is a PC fact (kit `docs/19` §5 reads it from code). A friend on
   another PC or another save slot gets a new GUID by design (§5 there), and so starts the campaign again with nothing.
   The code review's MODE-C9 (session ids at every point a key is made) is the same question from the other side. PC
   check: after a Continue with a friend, every saved line's key is a GUID and none is duplicated.
2. **The campaign and the run are saved by two flushes** (`RWCampaign.Save`, then `RWRun.Save`, in `Won` and `New`).
   A crash between them leaves the route one map on and the run at the map's start: what that map earned is lost.
   A window of a few milliseconds; noted, not worth the change.
3. **A route key holding `r`** (`12r3`) is read as route `12` with retries `3`, since `Saved` reuses the world name's
   parser. Only a hand edit writes one, and the route that comes out is checked against the chart (SAVE-1).
4. **`RWWorld.Ensure` returns null both for "not ready, try next frame" and for "this route leaves the chart"**, so
   whatever waits on it waits for good on the second. SAVE-1 closes the one source found (the save); the vote and the
   boat only offer linked columns. Telling the two apart in `Ensure` would cover any future source; not done here.
5. **The chart's maps follow the pool.** The chart's rows and links are drawn before its maps (`RWChart.Generate`), so a
   saved route stays valid if the base game adds or drops a Gameplan, but the maps on its nodes can change, the map
   being replayed included. For Dimitri: keep it (a game update reshuffles maps under a running campaign) or save each
   sailed node's Gameplan with the route.

## 2. Check 5: the Scope Editor and the scope

- **The editor holds no edits.** `ArtifactData list scopes` on https://claude.ai/artifact/5KAGb5wdqiNV1bEoxNMgEo:
  no documents, so Dimitri has saved nothing in it.
- **The editor equals his project-files copy, byte for byte, in all 17 files; both are behind the repository in 7**
  (`02`, `03`, `04`, `07`, `08`, `12`, `16`). Every difference is Claude's: status lines and `/~ \` notes from PRs #79 and
  #83, and this PR's note in `12`; none of his wording. Left for his word, since overwriting under `/mnt/project-files`
  needs it (§5, item 1). Its `README.md` there says 34 sheets carry the New chip; `build.py` lists 47 (34, less the 5 the overlap
  check removed, plus the 18 of the roguelikes round). Every name on that list is in scope `11` or `12`.
- **The rename left nothing behind.** "Vessel Cardinal" and "Team Cardinal" appear only where they are kept on purpose:
  the history in scope `02`, the naming note in `12`, save-compatibility comments in the mode's code and its old-save
  check, the kit's dated notes, `docs/mode/MECHANIC_IDEAS.md`'s header, `CLAUDE.md`'s rule, a RogueLab comment. "15
  maps" and the mode's "Swell" appear nowhere outside dated reports and the PLAN's history (Argo's water part is named
  `Swell`, which is its own thing).
- **Every sheet follows its template.** All 55 Direction sheets in `11` and all 16 sheets in `12` have their template's
  slots in its order and end with `By:`; 49 Directions, 11 Trinkets and 4 Cardinals are Claude's, as Part C says.
- **Fixed:** scope `12`'s note and `17_Cardinals.cs`'s header said Cardinals past the 8th "are counted in the run line";
  the run line carries only the objective (`20_Vote.cs` `Line`), as `SCOPE_CODE_AUDIT_2026-11-04.md` row 3 found. Both
  now say they still work but show nowhere yet; which one changes is that row's question.

## 3. Check 6: art and sprites

Nothing to fix.

- **Every file is listed and every listed file exists.** Each part's `Art/Temporary/README.md` against its
  `game_size/`, `aseprite/` and `preview_8x/`: the mode's 39 icons and sheets, Yossele's 3, Spool's 6, and the minion
  folders' READMEs. `RogueWaves_Icons.aseprite` has the 15 frames of 12 x 12 its README says. Every path in
  `docs/TEMP_ART.md`'s table and generator list exists (the project-files ones included).
- **Every sprite the code asks for exists.** The mode's `RWAssets.Sprite` and `Like` names, and `Direction_<name>` for all
  22 built Directions (Yossele 3, Spool 2, Argo 2, Uloe 4, the mode's 11), the two spelling aliases included.
  Yossele's and Spool's `FileArt` files are the size the code draws (cloud preview of the builders: 192 x 256, 128 x 128,
  240 x 96, 160 x 64), so the builder doesn't skip one; all 80 minion versions are the size of the sheet they would
  replace (Naut 96 x 64 against `NautSheet`'s 6 x 4 cells of 16).
- **Every `.aseprite` copy under a `Temporary` folder (174) has a "TEMP" layer.**
- **Nothing private or from before the scope.** No image in the repository is named for a base vessel, a stock foe or a
  mod before the scope; the minion generators draw every body from scratch (the Snoble only gives the size, measured).
  The pre-scope folders that remain (Riptide, RogueLab, the kit's Mummer and Sprigg examples) hold code and notes, no
  art, and are kept by PR #78's rule. Markdown image links and backtick image paths in the docs all resolve (the rest
  name files the builders make or the PC holds).

## 4. Check 7: `docs/PLAN.md` Part C and its Log

| # | Found | Done |
| --- | --- | --- |
| P1 | "Now" says every dated entry is in the Log, but four merged PRs had no Log entry: #78 (the pre-scope clean-up), #82 (held Cardinals on later maps), #83 (the quality pass), #85 (the best-practices setup) | Log entries added, dated, from "Now", the PRs' commits and their reports |
| P2 | "Now" doesn't say that PR #82's test round stopped before #80's test list, or that the PC's `TDS_Mod` is older than PR #80 | Said in this PR's "Now" entry |
| P3 | "Now" names the code review (#84) only as the PR that repaired the build; its 54 unfixed bugs and its PC list are only in the Log | Said in this PR's "Now" entry |
| P4 | Open questions: row 4 sat after row 6 | Moved |
| P5 | Open questions had nothing for the decisions PRs #83 and #84 left him | Rows 11 and 12 added; row 13 for this report |
| P6 | The Decisions of 2026-10-09 to 2026-10-11 still say 15 maps, Swell, Team and Vessel Cardinals | Kept: each is dated and the later Decisions supersede it (2026-10-09 20:48Z, 2026-11-04 rename). History, not state |

Checked and true: every repository path Part C cites exists; kit chapter 25 and pitfall #151 are the next free numbers;
`Shared` equals the kit's `kit/Shared`; every part's version is the one "Now" names; "Phase 5 pieces" matches the
reports; the `.claude/settings.json` item (C10) is still open (`apt-get update` and the UnityPy install are not allowed
there).

## 5. Left for Dimitri

1. **Sync his copy and the Scope Editor:** say "sync" and the 7 files in `/mnt/project-files/scope/` and the editor
   are brought up to the repository (Claude's lines only), and the editor's README count corrected.
2. **The chart's maps under a running campaign** (§1.4, item 5): keep, or save each sailed map's Gameplan.
3. On the next PC round: the mode's self-test should read 155 with F1-F7 passing, and the key check in §1.4, item 1.

## 6. What was compiled and what ran

- `python3 ../Tools/cloud-check/check_mod.py RogueWavesMode/Code Shared RogueWavesMode ""`, then with
  `ROGUEWAVESMODE_SELFTEST` and with `ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_GAPS`: 0 errors each (the warnings are the
  base's own: `92_Gaps.cs:786`, `96_Flow.cs:311`). Compiled here.
- `python3 ../Tools/cloud-check/save_fuzz.py`: 7 of 7 pass. Ran here, on a stand-in save.
- Nothing ran on the PC.
