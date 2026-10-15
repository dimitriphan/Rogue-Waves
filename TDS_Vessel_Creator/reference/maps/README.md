# Self-test map snapshots

Every vessel self-test built on the template (kit 1.5+) plays the pinned map `ModKitTest.TestPlan` (`3B_Thehunt`) and
logs its ground once as `TEST MAP` lines (`ModKitTest.MapDump`). `Run-SelfTests.ps1` saves them as
`<Name>_<time>_map.txt` beside the run's summary. Bring one here:

```bash
python3 TDS_Mod/Tools/cloud-check/map_view.py save <the _map.txt or a Player.log> TDS_Vessel_Creator/reference/maps
python3 TDS_Mod/Tools/cloud-check/map_view.py near TDS_Vessel_Creator/reference/maps/3B_Thehunt.txt <x,y,z> 8
python3 TDS_Mod/Tools/cloud-check/map_view.py spots TDS_Vessel_Creator/reference/maps/3B_Thehunt.txt <x,y,z> 3 8
```

`near` draws the ground around a tile (where a test would build or spawn), `spots` lists the tiles `ModKitTest.Spot`
would try in order. The map's prefab is fixed (`Game_Grid.Generatemap` has no randomness), so one snapshot holds until
the game updates the map.

No snapshot committed yet: every vessel self-test run (Argo 2.0, Yossele) writes one; RogueLab's and RogueWavesMode's
don't call `MapDump`. Save one here when a placement problem needs it.
