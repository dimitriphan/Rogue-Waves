# Uloe: temporary art drawn as files

**Everything in this folder is temporary** (`docs/TEMP_ART.md`). Uloe's art in game is drawn by code
(`Assets/UloeMod/Editor`, `ModSource/Uloe/Previews/`), except the Photon below.

| Folder | What | In game |
| --- | --- | --- |
| `game_size/Uloephoton-Sheet.png` (with `aseprite/`, `preview_8x/`) | 160 x 64, 16 px cells, bottom-centre pivot; rows v0-v3, columns 0-5 Move (0 = Idle), 6-9 Attack. The Photon: a small yellow four-point star that turns as it drifts, flashing from a plus to a cross (v04 Star, Dimitri's pick, 2026-11-05) | wired: `UloeBuilder.FileArt` takes it over the code's sheet; not yet built in Unity or seen in game |
| `minions_2026-11-04/photon/` | ten Snoble-sized versions of the Photon (2026-11-04), same name, size and layout as the code's `Uloephoton-Sheet` | v04 is copied to `game_size/` above; the rest are reference |
