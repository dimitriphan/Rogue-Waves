# 10 · Checklists

Copy the relevant list into your notes and tick it honestly.

## Design review (before code)

- [ ] Name (one capitalised word), title, difficulty; silhouette distinct from all vessels on the boat.
- [ ] Concept's fantasy kept; every change listed with its reason.
- [ ] Hero stats inside the bands; one stock attack AI; Display_Desc pattern.
- [ ] Special rule as a one-line comma phrase; lore 1-2 lines of story.
- [ ] Ability (if any): keys 6/7, picker if targeted, cooldown visible, server-validated.
- [ ] Exactly 4 buildables with distinct jobs; cost colour matches job; base cost 10-60.
- [ ] Each tree: 7+ upgrades, 2 roots, crossing branches, one early end, one capstone that changes play; tier costs on
      the curve; names punny, descs short, no numbers in text.
- [ ] Unit-stat params don't reuse the building's own stat names.
- [ ] Every mechanic has a visible, honest effect on every screen.
- [ ] Custom resource (if any): earn/spend/cap/loss clear, per player, doesn't punish Tide 1.
- [ ] Test plan: one section per mechanic.
- [ ] Units (if any): `docs/12` §7 rules: weak alone, respawn 5-10 s, +1 unit at tier 2, at least one player order,
      capstone changes behaviour, ≤ ~6 per building.
- [ ] Towers that shoot turn toward their target; working towers show the work (`docs/03` §4a).

## Art review (previews and in-game captures)

- [ ] Sprites: matched to the chosen base vessel (named in DESIGN.md) and the shared conventions: chibi proportions,
      no black outline, opaque edges in darker tones (a soft rim only when Smacky is the reference), dark dot eyes,
      pastel palette with one or two accents, one signature feature, live idle.
- [ ] All sheets at the exact sizes/layouts; back variants drawn, not mirrored.
- [ ] UI: 12×12 ready-up, 128 line-art splash, 32 end icons, 16 px mini, 32 white line tab icon; rows checked.
- [ ] Towers: low-poly sleek like Sno Sno's/Buster's, never voxel (no exceptions); 0.8-1.45 tall, inside the tile; details on front/top; no spindly parts;
      every upgrade adds one clear feature; whole-model disabled renderer on Vis; moving parts named and pivoted the
      same on every upgrade model, their swing inside the tile.
- [ ] Effects: honest radius; dome bursts point up; debuff looks clear tall sprites; ground effects at ground level.
- [ ] Sounds at the game's levels on its mixer group.
- [ ] Library holders saved inactive.
- [ ] Side by side with shipped vessels in the boat capture: nothing looks out of place.

## Code review

- [ ] Every id/tag/icon/effect/sound/class carries the vessel name; ids ≤ 60 chars.
- [ ] No System.IO/Reflection/Core/JsonUtility; build script passes.
- [ ] Brains only on the server; try/catch per brain tick; logs once per kind.
- [ ] Upgrades go through the game's own `Entity_Ai_General_Upgrade` on every building that upgrades (costs, throttle,
  Forced/Normal, Upgradeborder, Upgraded stat, Voice in a Bottle; `05` §14), or through the template's mirror of it.
- [ ] Button state via `Updatecommand(..., false, false)`; commands validated on the host.
- [ ] Debuffs with `Beingref.Value`; projectiles get every param; hit data copied before edits.
- [ ] Icons added at boot, checked 8 s later with stock fallbacks.
- [ ] Journal built after content, inside try/catch; tabs synced and ticked.
- [ ] Boat: `ModKitBoat.LateUpdate(heroId)` in LateUpdate.
- [ ] Online checklist (`docs/07`) done.

## Release

- [ ] Self-test: 0 failures; captures reviewed (boat, buildings, vessel, every effect, journal pages).
- [ ] Rebuilt **without** defines; no `#define` in the code file.
- [ ] Build Mod Prefab (log OK, today), Park, Export (both passes done), Restore (every mod's file count back,
      `_Parked` gone).
- [ ] `_isEnabled: true` for every installed vessel mod.
- [ ] Boot check: every vessel mod `ready` + own icons, 0 exceptions; game closed.
- [ ] Version equal in Data, ModPrefab and zip name (a Rogue Waves part: the pack version, `docs/PACK.md` §4).
- [ ] Zip: `README - <Name>.txt` + `Mods/<Name>Mod/...` (a Rogue Waves part: the pack's zip, `docs/PACK.md` §5); sent to
      the user.
- [ ] `IMPLEMENTATION_NOTES.md` + `DESIGN.md` up to date; new lessons added to `docs/09_PITFALLS.md`.

## Hand-off / final report to the user

- What the vessel is and does (short), what changed from the concept and why.
- Test results with numbers (checks passed, launches made), what was verified only by compiling.
- Known issues and anything found in other mods (don't fix other sessions' mods unasked; report them).
- Side effects: the save's selected vessel changed; files installed; zip location.
