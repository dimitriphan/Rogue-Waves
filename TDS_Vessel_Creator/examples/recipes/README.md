# Recipes: compiled sketches for the non-vessel parts of a pack

Small static classes that turn chapters `19`-`21` and `23` into code a mode, a map or the pack's UI can copy. Each was written
from the decompiled game (2026-10-14, `docs/13` §11) and **compiled with cloud-check; none has run in game**. Copy what
you need into a part's `Code/` folder, rename the class to the part's prefix, and give it a self-test check before
trusting it.

| File | What | Chapter |
| --- | --- | --- |
| `RecipeNet.cs` | client id of a player, a durable name, a carrier every map has (the Basin), a message to one machine (`Pongload_ClientRpc`), late-join catch-up (`Pingpongserver`), the shared clock (`Time_Synced`), a load watchdog against the 120 s kick | `19` §5-6 |
| `RecipeUi.cs` | the choice dialog opened safely (forced menu, wait, `Runprompt(..., false)`), a timed close, the host's tally per `Sessionid`, a line under the Tide counter, reading this player's own command | `20` §2, §5 |
| `RecipeSound.cs` | the sound table read-only (`Pair`, `Exists`, `Group`), stock sounds here or everywhere, stock music crossfades through a `Bla_Ambient`, the pack's own looping clip on the music mixer group | `21` §1.4, §2 |
| `RecipeText.cs` | the pack's translations written into `Bla_Localization.translations` and re-applied after each language load | `21` §3.4 |
| `RecipeSave.cs` | one folder, `RW_` keys, typed writes, a schema number with read-only for newer saves, reload on slot change, per-player rows keyed by name | `23` §2 |

Not here, because a mod already proved it: the vote's command half (host writes offers into the `Goalstring` and
`Goalvalue` of its SALVAGE `Button` command, the pick returns through `Command_ServerRpc`; a `None` command is the same
channel, just not drawn) is `TDS_Mod/ModSource/Riptide/Code/06_Salvage.cs`; reopening the lobby for a rejoin is
`TDS_Mod/ModSource/RogueWavesMode/Code/09_Rejoin.cs`.

## Check

The recipes need no kit file, so compile them alone (no `Shared`):

```bash
cd TDS_Mod/ModSource
python3 ../Tools/cloud-check/check_mod.py ../../TDS_Vessel_Creator/examples/recipes none Recipe ""
```

`RecipeNet.Tell` needs `ClientRpcParams.Send.TargetClientIds`, added to the cloud-check stubs on 2026-10-14
(`stubs/ThirdParty.cs`); rebuild the stubs once (`build_stubs.sh`) if a container set them up earlier.
