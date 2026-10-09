# Round 5 captures: art review (step 4, 2026-10-08)

The 19 PNGs `ARGO_SHOTS` took in PC round 5 (138/0, Thehunt), copied from the PC's
`Desktop\ClaudeProjects\handoff\selftest_logs\Argo_round5_shots`. Checked against `DESIGN.md` "Art" and `docs/STYLE.md`
(vessels look as the user's concept art draws them; Argo keeps its brass-and-sea look).

| Capture | What it shows | Verdict |
| --- | --- | --- |
| `shot_fx`, `shot_built` | Five build options with their text (Deep Water third, Miner's Base fifth); every Argo building on the hill | as designed |
| `close_water`, `close_argo` | Deep Water and a FLOOD pool: stone frame, brass corner caps, small pump dome with its spout, light water | as designed |
| `close_aim` | Abyss and Trench pools (Trench's dark whirlpool reads), the Tremor's dust over the pools | as designed; the Tremor ring itself is hard to read in a still, judge it in hand play |
| `close_reel` | The Harpoon Gun's brass line to a reeled foe | as designed |
| `close_towers`, `close_tuning`, `close_steep_*` | Scatter, Harpoon (turret aimed down the slope), Drill Rig, Miner's Base, a Naut tuning | as designed |
| `close_range_*` | The game's range sphere on Scatter (4.5), Harpoon (6.5) and Drill Rig (AIM reach 5), centred | as designed |
| `shot_journal_*` | Argo's page with FATHOMS, the Miner's Base and a Naut page, Flippers' upgrade page | as designed |
| `shot_journal_..._Trap_Argowater` | Deep Water's page: **its 3D view is a thin line** | **fixed, seen in round 6** |
| `shot_boat` | Argo on the boat's vessel select with its five buildings | Deep Water is a line here too; left as is |

**Deep Water reads as a line in 3D UI views.** The pool is 0.075 high and 0.8 wide, and both UI views look at a model
nearly level. The journal's view is a `UIObject3D` fed by `Logitems.Image3D.Source`, which the kit sets from the
building's `Model`, and it spins the model about its vertical axis. Fix (seen in round 6): the journal gets
`ArgoAssets.JournalModel`, which for every `Trap_Argowater*` model builds a "coin" once: two pools back to back, tipped
40° toward the view, so a pool face shows whichever way it spins. Round 6's capture (`../round6/`) shows a small
tipped pool, framed small by the view's own scale but readable.

The boat's vessel select (`Macro_Setupui`, `Entry.Towerdisplay`) instantiates each building's in-game `Visprefab`
level, so the only fix there is tilting the in-game model, which would break it on the map. Left as is.

Not captured, still unseen in game: the new upgrade models one by one (Ironclad, Tether, Reel, Undertow, Bedrock).
Deep Water's bob was added after round 5 and measured by round 6's self-test.
