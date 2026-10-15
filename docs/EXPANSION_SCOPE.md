# Expansion scope: what Rogue Waves needs beyond vessels

Status 2026-10-08, rows updated to 2026-10-14: two research rounds done (`docs/14`, rows 4 and 15; `docs/15`, Phase 1: rows 3, 6, 7, 9, 10), then a full decompile (kit `docs/19`-`24`, rows 2, 4, 5, 8-13, 17). This page lists the pieces an
expansion-sized DLC for TDS has, what the kit already knows about each (from the game's code, read with Cecil and the
cloud decompiler in `TDS_Vessel_Creator/docs/13` §11), and what a research session would still have to establish. Pick
the rows that Rogue Waves should include; each approved row becomes a kit chapter (`docs/14+`) once studied, with its
pitfalls in `docs/09`. What the scope itself still needs researched is in `docs/RESEARCH_GAPS.md`.

Legend for "Kit knows": **done** = verified in a shipping mod; **read** = read from the game's code, not tried;
**none** = not looked at.

| # | Piece of an expansion | Kit knows | What is left to learn | Risk |
| --- | --- | --- | --- | --- |
| 1 | **Vessels** (heroes, 4 buildables, upgrade trees, units, abilities, journal, boat, online) | done | nothing new; overhaul pass on the six existing ones | low |
| 2 | **New enemies and elites** | read (`docs/13`; art format, flying and movement per kind from code in `docs/24` §1-3) | the numbers only a dump gives (frame sizes, `Defaulthit` sizes, `docs/24` §8); stock data, elite values, Tide tables, enemy loading and journal pages answered by RogueLab rounds 1-3 and `docs/RESEARCH_GAPS.md` C1-C4 | medium |
| 3 | **Tide tables and game modes** (`Modeobject`, `Wavesobject`, `Mode_Normal/Storm/Drought/Tsunami/Final/Tutorial`, `Clientmode_*`) | read (`docs/13` §3, `docs/15` §2-4): mod mode = stock scripts by name + host brain; three-map chain proven in game (RogueLab round 1) | how the Gameplan picks soundtrack and medals (a mod mode entry, live Tide edits and a main-menu button are proven, `docs/15` §1, §13, §14.8) | medium |
| 4 | **New maps** (`Mapobject`, `Mapentryobject`, `Mapworlddatabaseobject`, `Game_Grid.Generatemap`, `Map_Generationedit`, `Specialspots`, lightmaps) | read (`docs/14` §2, §6; the grid, paths, map objects and nodes, `docs/16`); the `Mapdata` prefab, its children and the pack's own registration route (not the custom-map slot) read 2026-10-14 (`docs/22`) | a stock map's prefab dumped on the PC (`docs/22` §7); one test map built and loaded through the pack's own entry | high: likely the biggest piece |
| 5 | **Bosses and the final mode** (`Mode_Final`, `Clientmode_Final`) | read (`docs/15` §18.3): the final map is a 6-minute survival with no boss; a boss is the pack's own enemy, a big `Beingobject` with its own brain (plan with phases, banners and the Tide around it: `docs/24` §4-6) | a boss mock in a probe round (`docs/24` §8) | medium-high |
| 6 | **Artifacts and relics** (`Entity_Ai_Artifact_*`, `Wavebreaker`, `Teamapply`, scaling on `Totalwaves`) | read (`docs/15` §7): artifacts = Cardinals, data a mod can add (proven in game, RogueLab round 1) | new Cardinals' effects and how many (the system is proven) | medium |
| 7 | **Enemy-side buildings and shops** (`Entity_Ai_Tower_Fieshop`, `Genitem`, `Entity_Command_Itemselect_Base`) | read (`docs/15` §5, §6): no shop in the game; Merchut contracts hardcoded; `Itemselect` is a target picker; Fie's Rest Stop is a timed-buff thrower, not an item system (`docs/15` §18.1) | whether any stock code gives a generic consumable or item to reuse (Fie's shop is not one) | medium |
| 8 | **Music and stings** (`Clientmode_*`, `Gameplan.Soundtrack`) | read (`docs/21` §1-2): the Gameplan picks a stock soundtrack; stages follow `Musicwave1..4`; the table never takes a mod key, own music plays beside it on the music mixer group | stock track loudness and the shipped key list (`docs/21` §6) | low-medium |
| 9 | **Unlocks and progression** (`Unlock`, `Unlockentryobject`, `Unlock_Tracker_*`, `Macro_Unlocklink_*`, `Macro_Boat_Unlocks`) | read (`docs/15` §8, §18.4; `docs/23` §3): unlocks are data in `Items["Unlock"]` with trackers, earned at the next boat visit; only vessel and Cardinal unlocks are announced; vessel locks are cosmetic | a vessel unlock tried in game (`docs/23` §3.4) | medium |
| 10 | **Save compatibility** (`Bl_Save`, `Bl_Savedata`, `Player_Heroselect`) | read (`docs/15` §8, `docs/23` §1-2): the live save is `Bla_Data` (`Bl_Save` is dead code); what a mod may write, the pack's removal and a schema version are written up | the save format and an uninstall on the PC (`docs/23` §8) | medium; a mistake costs the user's save |
| 11 | **Steam achievements and leaderboards** (`Bla_Data_Steamachievements`, `Bla_Leaderboard`, `Game_Endscreen.Validate`) | read (`docs/23` §5): one modded player turns records off for the lobby; stats, unlocks, medals and achievements still count | nothing a mod can add; document the limits for players | low |
| 12 | **Localization** (`Bla_Localization`, `Bla_Localization_Text`) | read (`docs/21` §3): English is the key; a mod adds rows to the live table per machine after each language load; the kit's CSV is never read | which languages the game ships (data) | low |
| 13 | **Tutorial and tips** (`Mode_Tutorial`, `Tds_Tutorialprompts`, `Tipthreshold`) | read (`docs/15` §18.2): warning popup and banner note reach every client; tips only on the end screen; tutorial prompts are client-local and exist in ordinary matches (`docs/20` §2) | whether new mechanics can add tips, and how the base game teaches a vessel | low |
| 14 | **Photo mode, cosmetics, skins** (`Tds_Photomode`, skins on beings) | partly read (enemy elite skins, boat visuals) | alternate vessel skins as an expansion extra | low |
| 15 | **Packaging one expansion** (one or many mods, load order, shared kit, version, player README, Steam Workshop or zip) | decided at the Phase 2 gate (`docs/PACK.md`): one `RogueWavesMod` bundle, a prefab per part, no load order, one pack version | enemies and maps as parts of the bundle (several `Default` prefabs in one bundle are proven, `docs/15` §15; there is no Workshop, `docs/14` §4) | medium |
| 16 | **Balance across a whole roster** (`docs/02` §7 is per vessel) | partly done | a pass that pits every vessel against every mode and map, with the self-test harness running the roster; telemetry from the self-test | medium |
| 17 | **Online with many new pieces** (`docs/07` covers vessel sync) | done for vessels; read for the rest (`docs/19`: every RPC and synced field, the load and its 120 s kick, late join, identity, the effect throttle, rules for mode code) | the online evening (`docs/19` §8); a lobby check that every player has the whole pack (none in the game) | medium |
| 18 | **Art direction of a pack** (`docs/03` is per vessel) | approved at the Phase 2 gate (`docs/STYLE.md`) | a shared theme (what "Rogue Waves" looks like: palette, motifs, naming), a style sheet every vessel, enemy and map follows, a reference strip so the pack reads as one | low, but decides the fantasy |

## How a research session works here

The game's code can be read in this container without the PC: `TDS_Vessel_Creator/reference/api_assembly_csharp.txt`
is the API dump, `tools/cecil/` reads IL, callers and string literals on the PC, and `docs/13` §11 describes the cloud
decompiler setup (ICSharpCode.Decompiler from NuGet) that produced chapter 13. Facts that only a running game can give
(asset contents, data values, Addressables groups) go on a PC research list like `docs/13` §10 and come back through
Sprigg-style research builds (`examples/code/Sprigg_95_Research.cs`).

## Suggested order, if everything is approved

1. Rows 18 and 15 first (theme and packaging), since every other row depends on them.
2. Row 2, then 3 (enemies, then Tide tables and modes): the kit already has the reading, one PC round answers §10.
3. Rows 4 and 5 (maps, bosses) as the long pole, after a feasibility read of what ModTool's bundle can carry.
4. Rows 6 to 14 as the pack's extras, each a short read.
5. Rows 16 and 17 last, on the finished roster.
