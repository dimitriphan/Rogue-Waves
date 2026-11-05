# RogueWavesFoes: the pack's enemies part

The enemy modifiers of `docs/scope/13` as a part of the pack (`docs/PACK.md`): prefab `RogueWaves_Foes_Default`, built and self-tested as
the dev mod `RogueWavesFoesMod`. Each is an upgrade, `RW_Modifier_<Name>_Upgradeobject`, that shows as a pixel ring around the foe in a
colour no stock elite uses (`docs/STYLE.md` "Enemies"). The mode's `RWDifficulty` draws them per campaign. A foe with one keeps its own skin.

| Modifier | Does | Numbers (`RWFData`) |
| --- | --- | --- |
| Cursed | weakens the attack of everything nearby, vessels too | `CursedRange` 3, `CursedDamage` -25% (his answer 10 of 2026-11-05) |
| Berserk | stronger and faster the more it is hurt | at no health left, damage +100%, speed +50%: defaults |
| Aging | slow, health and damage grow the longer it lives | speed -30%, up to +100%, over 50 s: the scope's |
| Dazzling | Blinds and Illuminates everything nearby, in the stock upgrades' way, not Uloe's Light | within 3 tiles, damage -15%, taken +15%, both lingering 3 s after leaving (his answer 11 of 2026-11-05) |

Self-test in game: 12 pass, 0 fail (`-Define ROGUEWAVESFOES_SELFTEST`); 13 checks since 2026-11-05 (Illuminate's linger), compiled, not yet run. The look is by self-test only: a player's eye on the rings is left.
Build: `Build-RogueWavesFoesMod.ps1`, then Tools > ModKit > Build Queue. The editor half is `Assets/RogueWavesFoesMod/Editor`.

2026-11-05 (compiled, not yet run in game; `docs/reports/CODE_REVIEW_FIXES_2026-11-05.md`): the auras skip the Bouthouse, which hands what it holds on to
Boutman (FOES-2, pitfall #85), and a foe with Aging and Berserk keeps both modifiers' handles (FOES-3).

No journal pages yet: the stock enemy pages list a foe type, and a modifier isn't one (a sheet for them waits on a decision of the journal's layout).