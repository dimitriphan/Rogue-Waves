# Expansion scope: what Rogue Waves needs beyond vessels

Status 2026-10-08: two research rounds done (`docs/14`, rows 4 and 15; `docs/15`, Phase 1: rows 3, 6, 7, 9, 10). This page lists the pieces an
expansion-sized DLC for TDS has, what the kit already knows about each (from the game's code, read with Cecil and the
cloud decompiler in `TDS_Vessel_Creator/docs/13` §11), and what a research session would still have to establish. Pick
the rows that Rogue Waves should include; each approved row becomes a kit chapter (`docs/14+`) once studied, with its
pitfalls in `docs/09`.

Legend for "Kit knows": **done** = verified in a shipping mod; **read** = read from the game's code, not tried;
**none** = not looked at.

| # | Piece of an expansion | Kit knows | What is left to learn | Risk |
| --- | --- | --- | --- | --- |
| 1 | **Vessels** (heroes, 4 buildables, upgrade trees, units, abilities, journal, boat, online) | done | nothing new; overhaul pass on the six existing ones | low |
| 2 | **New enemies and elites** | read (`docs/13`) | the 7 open questions of `docs/13` §10: stock enemy data, elite values, Tide tables, when enemies load, enemy art format, enemy journal pages, flying | medium: needs one PC research round |
| 3 | **Tide tables and game modes** (`Modeobject`, `Wavesobject`, `Mode_Normal/Storm/Drought/Tsunami/Final/Tutorial`, `Clientmode_*`) | read (`docs/13` §3, `docs/15` §2-4): mod mode = stock scripts by name + host brain; three-map chain proven in game (RogueLab round 1) | whether a mod can register a new mode entry or edit a `Wavesobject` on every machine; how the Gameplan picks mode, soundtrack and medals; how to add a mode button to the menu | medium |
| 4 | **New maps** (`Mapobject`, `Mapentryobject`, `Mapworlddatabaseobject`, `Game_Grid.Generatemap`, `Map_Generationedit`, `Specialspots`, lightmaps) | read, container side (`docs/14` §2, §6); maps ship as `Map` prefabs with `Mods_Mapsettings` (`docs/15` §10) | the map prefab format and tiles, how a map joins the world/boat list, ore nodes, enemy cores, paths, lightmap data in a mod bundle, the medal and leaderboard tie-in | high: likely the biggest piece, and ModTool's bundle may not carry scenes |
| 5 | **Bosses and the final mode** (`Mode_Final`, `Clientmode_Final`) | none | how the final fight is scripted, whether a boss is just a big `Beingobject` with its own AIs, phases, banners | medium-high |
| 6 | **Artifacts and relics** (`Entity_Ai_Artifact_*`, `Wavebreaker`, `Teamapply`, scaling on `Totalwaves`) | read (`docs/15` §7): artifacts = Cardinals, data a mod can add (proven in game, RogueLab round 1) | what an artifact is, how it's granted, whether a mod can add one | medium |
| 7 | **Enemy-side buildings and shops** (`Entity_Ai_Tower_Fieshop`, `Genitem`, `Entity_Command_Itemselect_Base`) | read (`docs/15` §5, §6): no shop in the game; Merchut contracts hardcoded; `Itemselect` is a target picker | the item system behind Fie's shop: if generic, a source of expansion-level mechanics (items, consumables) | medium |
| 8 | **Music and stings** (`Clientmode_*`, `Gameplan.Soundtrack`) | none (vessel SFX are done, `docs/03` §6) | how a soundtrack is chosen per map or mode, whether a mod can supply tracks, loudness targets for music | low-medium |
| 9 | **Unlocks and progression** (`Unlock`, `Unlockentryobject`, `Unlock_Tracker_*`, `Macro_Unlocklink_*`, `Macro_Boat_Unlocks`) | read (`docs/15` §8): unlocks are data in `Items["Unlock"]` | whether an expansion can gate its content behind in-game unlocks like the base game (Pig, Kippy, Bird trackers) and store them in the save without breaking it | medium |
| 10 | **Save compatibility** (`Bl_Save`, `Bl_Savedata`, `Player_Heroselect`) | read (`docs/15` §8): the live save is `Bla_Data`; a mod folder of its own survives | what else a mod may write, what happens to a save when the mod is removed, version migration | medium; a mistake costs the user's save |
| 11 | **Steam achievements and leaderboards** (`Bla_Data_Steamachievements`, `Bla_Leaderboard`, `Game_Endscreen.Validate`) | read (mods disable leaderboard validation) | nothing a mod can add; document the limits for players | low |
| 12 | **Localization** (`Bla_Localization`, `Bla_Localization_Text`) | none (vessel text is English strings) | whether mod text can go through the game's localization table, and which languages the game ships | low |
| 13 | **Tutorial and tips** (`Mode_Tutorial`, `Tds_Tutorialprompts`, `Tipthreshold`) | none | whether new mechanics can add tips, and how the base game teaches a vessel | low |
| 14 | **Photo mode, cosmetics, skins** (`Tds_Photomode`, skins on beings) | partly read (enemy elite skins, boat visuals) | alternate vessel skins as an expansion extra | low |
| 15 | **Packaging one expansion** (one or many mods, load order, shared kit, version, player README, Steam Workshop or zip) | decided, pending the Phase 2 gate (`docs/PACK.md`): one `RogueWavesMod` bundle, a prefab per part, no load order, one pack version | whether one bundle can carry many vessels plus enemies and maps, or each stays its own `<Name>Mod`; a single installer; keeping mods of the pack in step; whether TDS has a Workshop at all | medium |
| 16 | **Balance across a whole roster** (`docs/02` §7 is per vessel) | drafted, pending the Phase 2 gate (`docs/STYLE.md`) | a pass that pits every vessel against every mode and map, with the self-test harness running the roster; telemetry from the self-test | medium |
| 17 | **Online with many new pieces** (`docs/07` covers vessel sync) | done for vessels | sync rules for new enemies, modes and maps; a lobby check that every player has the whole pack | medium |
| 18 | **Art direction of a pack** (`docs/03` is per vessel) | partly done | a shared theme (what "Rogue Waves" looks like: palette, motifs, naming), a style sheet every vessel, enemy and map follows, a reference strip so the pack reads as one | low, but decides the fantasy |

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
