# Self-test map snapshots

Every self-test build (kit 1.5) plays the pinned map `ModKitTest.TestPlan` (`3B_Thehunt`) and logs its ground once as
`TEST MAP` lines. `Run-SelfTests.ps1` saves them as `<Name>_<time>_map.txt` beside the run's summary. Bring one here:

```bash
python3 TDS_Mod/Tools/cloud-check/map_view.py save <the _map.txt or a Player.log> TDS_Vessel_Creator/reference/maps
python3 TDS_Mod/Tools/cloud-check/map_view.py near TDS_Vessel_Creator/reference/maps/3B_Thehunt.txt <x,y,z> 8
python3 TDS_Mod/Tools/cloud-check/map_view.py spots TDS_Vessel_Creator/reference/maps/3B_Thehunt.txt <x,y,z> 3 8
```

`near` draws the ground around a tile (where a test would build or spawn), `spots` lists the tiles `ModKitTest.Spot`
would try in order. The map's prefab is fixed (`Game_Grid.Generatemap` has no randomness), so one snapshot holds until
the game updates the map.

No snapshot committed yet: every kit 1.5+ self-test run (Argo 2.0, RogueLab, RogueWavesMode) writes one; save one
here when a placement problem needs it.
