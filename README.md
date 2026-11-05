# Rogue Waves

**Rogue Waves** is an expansion-sized fan mod for *TDS - Tower Defense Strategy* (Blockage), built around roguelike
play. It adds a new campaign mode, four new vessels, new enemy modifiers, new run modifiers and a few new economy
mechanics, and plays online with 2 to 4 players. It ships as one mod pack, `RogueWavesMod`, with every part inside it
and no load order; each release adds parts to the same pack, so players update one mod.

The design is Dimitri's scope in `docs/scope/` (one file per category, `00_Read_Me.txt` lists them).

## The mode

Players pick a vessel, are locked into it, and sail a **campaign of 10 maps** in a row, each harder than the last. Between
maps they vote on the next one on a **sea chart** (a node map of linked maps, with sea weather), and the run is saved
between maps, so a campaign can be played over several sessions; a player who drops out can rejoin a running map.

- **Pure Aura**, a new currency earned each tide and from optional objectives, buys the run's upgrades.
- **Directions** are upgrade paths picked before each map; their ranks are bought at **VENDCINT**, the shop, which
  also sells **Trinkets** (run modifiers for one player's things) and **Cardinals** (run modifiers for the whole team).
- **Fund Transfer** lets players share aura between tides.
- **Optional objectives**, **enemy modifiers** (Cursed, Berserk, Aging, Dazzling) and the sea weather change each
  campaign. A campaign has 3 retries; finishing one is meant to unlock a harder
  difficulty for the next (Depth, on hold).

The whole run is in `docs/scope/04_Rogue_Waves_Mode.txt` and step by step in `05_Gameplay_Rundown.txt`.

## The vessels

| Vessel | Title | How it plays | Scope |
| --- | --- | --- | --- |
| **Yossele** | The Patient Golem | Sturdy ranged clay golem with a green mouth laser; towers it keeps alive grow stronger | `07` |
| **Spool** | Crafty Crafted Crafter | Melee plush who commands a disposable army of stitched minions | `08` |
| **Argo** | Deep Sea Diver | Slow melee diver with no walls: slows, pins and reels foes in | `09` |
| **Uloe** | Light Personified | Frail ranged light bulb who powers clusters of towers with Light and wandering Photons | `10` |

## Where it stands

Every part is built with temporary art and passes its self-test in the game: the mode (the whole campaign, from the
**Rogue Waves** button on the main menu to the end screen), the foes, Yossele, Spool, Argo and Uloe. A full ten-map
campaign has been won, two players have played it together, and the parts boot as one pack. The first release is
**Yossele alone**, waiting on Dimitri's own sprites and play test; the mode and the other vessels follow. Live state:
`docs/PLAN.md` Part C "Now".

## What is in this repository

| Path | What |
| --- | --- |
| `docs/scope/` | The scope: the highest truth after Dimitri's newest words |
| `docs/` | The living project pages: the plan and its state (`PLAN.md`), how the pack ships (`PACK.md`), the style sheet (`STYLE.md`), the expansion's reach (`EXPANSION_SCOPE.md`), the mode's design pages (`mode/`), vessel concept pages (`concepts/`) and dated reports (`reports/`) |
| `TDS_Mod/` | The Unity mod project: each part's code and design in `ModSource/<Part>`, its Unity editor code in `Assets/<Part>Mod`, the shared kit, the pack list, and the tools that compile and preview here and run rounds on the PC (`TDS_Mod/README.md`) |
| `TDS_Vessel_Creator/` | The kit: every rule learned about how TDS works and how a vessel is made, built, tested and shipped (`README.md`, then `docs/`) |
| `concept-tools/` | Python for concept pages and art previews, and the sea chart generator |
| `.claude/` | Path-scoped rule notes and the session permissions |

## Working on it

Start with `CLAUDE.md` (the map and reading order). Work lands on the branch `claude/phase-1-6lfnar`
(`docs/PROJECT_SETUP.md` §1). The game, Unity and the mod tools run only on Dimitri's Windows PC; a cloud session
designs, writes code and checks it with `TDS_Mod/Tools/cloud-check` (one-time `setup.sh` per container), and a PC round
builds and tests it in the game (`TDS_Mod/Tools/pc/README.md`). The game's `Assembly-CSharp.dll` and the PC lock
`pc/QUEUE.md` live in the Claude Project's files, never in git.
