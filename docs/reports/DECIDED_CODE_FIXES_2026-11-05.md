# The decided code fixes (2026-11-05)

Dimitri answered `OPEN_DECISIONS_2026-11-05b.md` at 16:04Z with "Default for all". This builds the code items of that
page's hand-off table: 2a, 2b, 2c, 2f and 2g (the seven behaviour questions of `BUILD_UNBUILT_2026-11-05.md` §2, d and
e left as they are), 5 (Spare Reel), 6 (the leave vote's line) and 9 (British spelling). Base `fb208b8` (kit 1.13).
Compiled here, every part, release and self-test builds, 0 errors; nothing ran on the PC.

## 1. What changed

| Item | Change | Where |
| --- | --- | --- |
| 2a | `ModKitUtil.Front` leaves a handler alone when it already stands among the mod handlers ahead of the game's own (the game's are the ones whose target is an `Entity_Ai_Base`; `Updateinteract` adds only those, read from its IL). Two parts' handlers on one thing no longer swap order on every call. Kit 1.14, pitfall #168 | `Shared/ModKitUtil.cs` (and the kit copy) |
| 2b | Searchlight's slow goes through `ModKitUtil.SlowOn`, so it never takes a foe under 10% of its speed. A slow cut short by the floor is made again at full strength once the other slows leave room (found by the code review) | `Uloe/Code/07_Building.cs` |
| 2c | Argo's Cleave (the hit that carries `Argocleave`) is not the vessel's own hit: `RWAny.ExtraHit` skips it with the pack's own extra hits, so Notched Oar neither counts nor doubles it, Fish Hook doesn't roll on it, Gutting Knife doesn't sharpen it, and the attack counts (Galley Kettle, Chain Shot) start from the swing itself | `RogueWavesMode/Code/22_AnyDirections.cs`, `24_Trinkets.cs`, `23_BaseDirections.cs` (Long Leash reads the same test) |
| 2f | Tar Bucket marks a hit with Mason's Trowel's hook (`RWUtil.Hit`, put back four times a second), so a hit carrying its own Damage counts; `Lasthittime` is no longer read | `01_Util.cs`, `23_BaseDirections.cs`, `24_Trinkets.cs` |
| 2g | Blurry Photo copies: every "the player's things" test uses one `RWRun.IsCopy`, which reads the tags the game adds at run time. A Hero-tagged spawn (Boutbro, a copy) is reached once it has its stats, when a copy shows as one. Pitfall #169 | `14_Run.cs`, `16_Directions.cs`, `17_Cardinals.cs`, `22_AnyDirections.cs`, `23_BaseDirections.cs` |
| 2g | The Cardinals' paired lookups (`ClashHere`, `PriceMine`, `MaxedHere`) read the id a held stack puts on the vessel's panel, the copy or the mark, through the same rule as `Grant` | `17_Cardinals.cs` |
| 2g | `HasMinions`: checked, no change to what it does (§2) | `16_Directions.cs` (comment) |
| 5 | Boutman's Direction is shown as **Spare Reel**. Its id stays `Longline`, so saves and its icon (`Direction_Longline`) keep working | `16_Directions.cs:89`, scope `11`, the mode's temporary-art index |
| 6 | The leave vote: "Most votes win. Nobody leaves right before their demise" (no full stop, like the dialog's other lines) | `20_Vote.cs:662`, the gaps test's copy in `92_Gaps.cs` |
| 9 | British spelling in player text: one word was American, Argo's Reel "toward" (now "towards"), in the game line, its journal line, scope `09` and Argo's `DESIGN.md`. Everything else already reads British (Colour, Armour) or is a name kept as Dimitri wrote it: FERTILIZE and its resource Fertilizer, Woolen Lining, Aging, the game's own DEFENSE | `Argo/Code/02_Data.cs:318-319` |

## 2. Readings

- **`HasMinions` filters nothing among the nine vessels because all nine have minions.** Each vessel's scope has a
  (Minion) block: Smacky's miners, Sno Sno's Snobles, Boutman's Boutbro and fish, Fie's knights, Buster's dogs, Argo's
  Nauts, Yossele's Cordyfriends, Spool's Crafted, Uloe's Photons. A filter that really filters still lets all nine
  through, and it already leaves out a vessel the pack doesn't know (another mod's). So the finding was a reading, not a
  slip. The comment now says so, and nothing else changed.
- **Blurry Photo copies were reached because the old tests read the wrong tags.** `Entity_Ai_Effect_Artifact_Blurryphoto`
  adds "Clone" and "Prespawn" with `Helper_Entitytools.Tag` after the spawn, into the thing's own list. The being's data
  tags never carry them. Every test that read the data tags (the Directions' and Trinkets' Reach, `RWAny.Owned`) let a
  copy through. That is how Made of Tougher Stuff could reach a copy of the vessel. Copies are vessels, never towers, so
  for Wound Spring, Oil Can, Spring Tide and Tidal Mend's towers nothing changes in play. The one test is now the same
  everywhere, and it leaves copies out, as each sweep's comment always said. Tidal Mend's heal still mends anything
  friendly within its reach, copies included, as a heal does.
- **The Front order is stable between rebuilds, not fixed.** After `Updateinteract` rebuilds a thing's chain, the parts
  line up in the order their sweeps call `Front`. So Made of Tougher Stuff III with Odd Socks III on a Crafted can still
  differ from one upgrade landing to the next, but no longer every 0.05-0.1 s. A fixed order needs a rank per part, the
  large kit change the clean-up named.
- **"Most votes win" is the code's rule:** a player who doesn't answer counts as a stay, and a tie stays.
- **No self-test was added.** Each change is listed for the next PC round instead (§4).

## 3. The code review (`/code-review high`, 10 findings)

| Finding | What happened |
| --- | --- |
| A copy fires the pack's Onspawn before the game tags it | Fixed: Hero-tagged spawns are reached once they have stats |
| Searchlight's cut slow stays cut after Fog Horn ends | Fixed: made again at full strength when there is room |
| `Grant` and the new panel-id rule picked copy or mark by different tests | Fixed: one rule (`OnPanel`) |
| `IsCopy` also read data tags, Long Leash kept its own extra-hit test, Argo's key unexplained | Fixed: run-time tags only, `ExtraHit` in Long Leash, a comment at Argo's cleave |
| Kit README row, pitfalls, Part C missing | Written |
| "Most votes win" vs. the tie and non-answers | Kept: Dimitri's wording, and it states the rule (§2) |
| Front order still depends on who calls first after a rebuild | Kept: what decision 2a took (§2) |
| Tar Bucket fronts its hook on every tower four times a second and never takes it off | Kept: the same as Mason's Trowel; a hook left on a tower only looks up one dictionary, and the next rebuild drops it |

## 4. For the next PC round

1. Two parts' hooks on one thing: Made of Tougher Stuff III with Odd Socks III on a Crafted, and Spring Tide III with
   Yossele's Blockade. Each blow's result should hold steady between upgrades (kit 1.14).
2. A foe under Fog Horn walking into a Searchlight: it stops at 10% of its speed, and slows 20% again once Fog Horn's
   reach is left.
3. Argo with Notched Oar, Fish Hook and Gutting Knife: Cleave's second foe takes no doubling, no snag and no Knife bonus,
   and every swing counts once.
4. Tar Bucket on a tower hit by a foe whose hit carries its own Damage: it doesn't mend until 4 s after the last hit.
5. A lobby Blurry Photo with Made of Tougher Stuff: the copy doesn't take it, Boutbro does.
6. Spare Reel on Boutman's Direction card; the leave vote's new line fits its dialog (the gaps build's dialog check).
