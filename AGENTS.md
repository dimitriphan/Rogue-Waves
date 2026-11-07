# Rogue Waves

Dimitri's fan expansion for *TDS - Tower Defense Strategy*: a roguelike campaign mode, four vessels, foes, Directions,
Trinkets and Cardinals, as C# mod code built with the game's mod tools. `CLAUDE.md` is the map of the repository;
`docs/PLAN.md` Part A holds the working rules and Part C "Now" the state. Claude writes the pull requests; Codex
reviews them (Dimitri, 2026-11-06). When Codex has not reviewed a PR's head two hours on, a Claude reviewer stands in
under the same rules (Dimitri, 2026-11-07). Pull requests base on `claude/phase-1-6lfnar`.

## Code Review Rules

These rules bind whoever reviews: Codex, or the Claude reviewer standing in for it. Review for real defects and rule
breaks. Leave out style, wording and formatting, and don't flag compile errors:
the PR's thread compiles every changed part with `TDS_Mod/Tools/cloud-check` and says so in the PR.

Flag as P0:

- A file that must never be committed: the game's binaries (`Assembly-CSharp.dll`, `.bundle`, `.unitypackage`), any
  sprite or texture taken from the game itself (the base vessels', foes', units' sheets), anything copied from
  `/mnt/project-files`, an API key or token.
- Mod code (`TDS_Mod/ModSource/**/*.cs`) using `System.IO` or `System.Reflection`, or needing `System.dll` or
  `System.Core` (the game compiles mods with Roslyn 3.8 without them): the game refuses to load it.
- Game state changed on a client: the host decides and the engine syncs; clients only draw (kit
  `TDS_Vessel_Creator/docs/07_ONLINE.md`). Data read from another player's message or from a save used without a
  bounds or null check.
- `TDS_Mod/ModSource/Shared` and `TDS_Vessel_Creator/kit/Shared` no longer equal, or a kit change without its version
  bumped in `TDS_Vessel_Creator/kit/README.md` and the file headers.
- A known pitfall reintroduced: check the changed code against `TDS_Vessel_Creator/docs/09_PITFALLS.md` by symptom.

Flag as P1:

- A game number (damage, cost, range, cooldown, health, chance) that differs from `docs/scope/` or the part's
  `DESIGN.md` without the PR saying Dimitri asked for it. Values in `~tildes~` in the scope are Claude's defaults and
  may change; plain values are his.
- An id without its vessel's name (`Hero_<Name>`, `Tower_<Name>x`, `Mob_<Name>x`, `Debuff_<Name>x`).
- `docs/PLAN.md` Part C "Now": the PR must add one entry of at most three lines at the end, after the base's entries,
  and must not reorder or drop the base's.
- A doc that cites a path, section, pitfall or kit number that does not exist in the PR's tree.
- Upgrade or item text that carries numbers; the old names "Vessel Cardinals" or "Team Cardinals" outside save keys
  (they are Trinkets and Cardinals); mentions of the mods from before the scope outside dated reports, the PLAN Log and
  version histories.
- Art (`*.png` under `TDS_Mod/ModSource`): half-transparent pixels, pure black `000000`, or a saturated hue between 270
  and 320 degrees on anything that is not a foe (`docs/STYLE.md`, `docs/art/CHATGPT_BRIEF.md`).
- A PR body that claims a test ran on the PC with no report or self-test count, or calls a clean compile a test.

When nothing above applies and the change does what its PR says, say so plainly in one line.
