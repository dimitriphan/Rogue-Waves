# 21 · Music, sound and text (soundtracks, stings, the sound table, translations)

Expansion rows 8 (music and stings) and 12 (localization) of the Rogue Waves repository's `docs/EXPANSION_SCOPE.md`.
Everything here was **read from the game's code** (decompiled as in `13` §11) on 2026-10-14, or from RogueLab's round
logs (`reference/roguelab_round1-3.txt`, marked *log*). Nothing here was tried in game; §6 is the PC list. Compiled
sketches are in `examples/recipes/RecipeSound.cs` and `RecipeText.cs`. Vessel sound design (levels, the library
prefab) stays in `03` §6.

## 0. Verdict

| Need | Can a mod? | How |
| --- | --- | --- |
| Play a **stock** sound by key, here or on every client | yes | `Helper_Vfxtools.Playsound(pos, key)` / `Playsoundui(key)`; host: `Tds_Multiplayer_Radio.Instance.Sfx_ClientRpc` / `Sfxui_ClientRpc` (§2.2) |
| Add its **own** sound key to the game's table | no | own `AudioSource` on the game's mixer group (§2.1) |
| Pick a **stock soundtrack** for its mode or map | yes | the Gameplan's `Soundtrack`, or the host writes `Tds_Multiplayerinfo.Soundtrack` before the load (§1.4) |
| Play **its own music** | yes, beside the game's system | own looping `AudioSource` on the music mixer group, stock "Gamemusic" stopped (§1.4) |
| Change music **at run time** (sea weather, a boss) | yes, client side on every machine | a `Bla_Ambient` helper or its own player; the trigger must reach every client |
| Its own client mode script | no | `Type.GetType` never sees mod types (as `15` §2 for `Modescript`) |
| **Translate** its text | yes, in memory, per machine | rows in `Bla_Localization.translations`, re-added after each language change (§3.4) |

## 1. Music

### 1.1 The data

- A **Gameplan** names its soundtrack (`Gameplandatabaseobject.Soundtrack`, a `Soundtrackentryobject` that holds only
  `Musicloads`, the group to load). Mode and map entries have no music field.
- A `Soundtrackobject` is a list of groups (`Intro`, `1`, `2`, `3`, `4`, `Stinger`), each a list of **sound keys** of
  the game's table, not clips; only the first key of each group is ever played (Soundtrackobject.cs:10-28;
  Clientmode_Normal.cs:75-146).
- Six ship (*log*): `Soundtrack_Blue` (14 plans: Ulu, Vultures, Kong, Blasthead, Brittle, Split…), `_Yellow` (10:
  Maybeth, Gridhead, Opal, Skyhook, Chrono, the tutorial), `_Cyan` (9: Snobound, Crescent, Hold, the final map…),
  `_Red` (9: Artrio, Thehunt, Mercs, Oroboro, Mini, the custom slot), `_Main` (7: Monogrove, Galbass, Woozy, Manafold,
  Hydroplane, Crimson…), `_Mainoriginal` (2). The full table by plan is in `reference/roguelab_round2.txt`.
- The boat's music is the world's `Display_Soundtrack`, played once, not looped (Macro_Unlock.cs:1099-1104).

### 1.2 How a match plays it

The leader's pick writes `Tds_Multiplayerinfo.Soundtrack` (synced); every machine loads it the same way
(Game_Init.cs:100-121; Game_Client_Init.cs:97-122). The **client mode script** plays it on every machine:

| Moment | What plays (Clientmode_Normal, Mode_Normal) |
| --- | --- |
| Map start | `Intro` looped on tag "Gamemusic": silent 15 s, then a 15 s fade-in |
| One Tide before a stage | `Stinger` once, when the banner reads "TIDE APPROACHES" and `Wave == Musicwave(n) - 1` |
| Tide = `Musicwave(n)` | stage `n` looped, hard cut; ambience ducked to -5 dB |
| Breaks ("TIDE CLEARED", "TIDE APPROACHES", "TIDE APPROACHING") | music muffled: -2.5 dB and a 700 Hz filter over 3 s |
| Tide end / break end (host to all) | `Tide_Complete` or `Tide_Completetime`, `Special_Tideintro`, `Special_Tideexplosion` |
| Win or lose | "Gamemusic" fades over 5 s; no sting in code |
| Final map | a waterfall loop instead of music; its `Soundtrack_Cyan` loads but never plays |

`Musicwave1-4` per mode (*log*): Normal 1, 10, 16, 25; Drought 1, 5, 10, 15; Storm 1, 8, 99, 99 (stages 3-4 never
come); Tsunami 1, 2, 3, 4. A mod's `Modeobject` sets them in `Defaultparam`.

- **Stages are exact and ordered:** a stage plays only at `Wave == Musicwave(n)` with the previous stage reached
  (Clientmode_Normal.cs:22-71). A skipped Tide freezes the music; **a rejoining client starts at stage 0 and keeps
  the Intro for the rest of the map** (predicted).
- **The music listens to English banner words** (Clientmode_Normal.cs:73, 84): a mode that writes its own break text
  loses the muffle and the stinger. Keep the stock words, or drive the music yourself.
- One "Gamemusic" track at a time, a stinger on top, a filter in breaks: **no intensity layers**.
- Game loops sit under a `DontDestroyOnLoad` container (Bl_Sound.cs:86-151): music survives scene loads until its tag
  is stopped.

### 1.3 The player

`Bl_Sound` keeps **tags** ("Gamemusic", "Musicsting", "Ambient_Music", "Introgustywind", "Outeffect", …) to playing
sources. A play with `Override` stops the tag first; the fade argument means 0 = cut, negative = crossfade over |f|,
positive = fade the old one out over f, then the new one in over f (Bl_Sound.cs:360-441).

### 1.4 What a mod can do

- **Pick a stock track:** give the twin Gameplan a stock `Soundtrack` (RogueWavesMode's twins keep each map's music),
  or the host writes `Tds_Multiplayerinfo.Instance.Soundtrack.Value` before the load (proven by RogueLab's chain,
  `15` §11). Per sea weather, the host picks the soundtrack with the map; every client follows with no client code.
- **Its own soundtrack from stock keys** (inferred): `Instantiate` a loaded stock `Soundtrackobject` (its `OnEnable`
  rebuilds `Data`), point groups at other stock keys, register it and a `Soundtrackentryobject` on every machine like a
  mode (`15` §2). Every key must exist.
- **Its own clip:** not through a soundtrack (keys only). Play a looping `AudioSource` on the music mixer group (the
  group of a stock music key's `Soundpair`) and stop "Gamemusic" through a `Bla_Ambient` helper (`Turnoff()`). On that
  group it obeys the volume slider and the break muffle. Leave `Musicwave*` out of the mode's params so stock stage
  changes don't cut in. Stop it on `Gamecomplete` and when the scene ends.
- **Run-time change:** on every client, a `Bla_Ambient` on tag "Gamemusic" crossfades to a stock key (5 s); the trigger
  is a synced value or a `Vis` event (`19` §6).

## 2. Sound effects

### 2.1 The table

- `Bla_Audio.Instance.Database` (public) holds groups of `Soundpair {key, val (clips), group (AudioMixerGroup),
  Volume, Pitchvar}`. The player `Blockage.Sound.Bl_Sound` is an **internal static class**: mod code cannot call it.
  Its dictionary is built **once**, on the first play, before mods load (Bl_Sound.cs:13, 86-121): **the table never
  takes a mod key**.
- **An unknown, empty or null key throws** (`KeyNotFoundException`, Bl_Sound.cs:124-138) wherever the game plays one:
  `Playsound`, `Vfx_Sound`, `Sfxstart`/`Sfxend`, projectile sounds, a soundtrack entry, a world's `Display_Sound`
  (played with no empty check, Macro_Worldui.cs:633). Check every key the pack writes at boot
  (`RecipeSound.Exists`).
- **The last variant of a key never plays:** `Random.Range(0, Count - 1)` with the int overload excludes the top
  (Bl_Sound.cs:129). Replace a `Soundpair.val` list, never append to it.
- Pools: 50 world and 10 UI sources, growing without limit; positional sources use linear rolloff, `Mindist` 0 and
  `Maxdist` 100 by code default (Bla_Audio.cs:98-104, 447-478).

### 2.2 Public ways to play a stock key

| Call | Plays | Where it runs |
| --- | --- | --- |
| `Helper_Vfxtools.Playsound(pos, key)` / `Playsoundui(key)` | positional / 2D | this machine |
| `Tds_Multiplayer_Radio.Instance.Sfx_ClientRpc(pos, key)` / `Sfxui_ClientRpc(key)` | the two above | host calls, every client plays; not throttled |
| `Visualproperties` entry `Sfxstart` / `Sfxend` | at spawn / at a real death | every machine (`01` §10 omits these two fields) |
| `Spritecont.Sound` | on that animation frame | every machine (the kit's footstep) |
| `Bla_Soundoneshot`, `Vfx_Sound`, `Vfx_Soundui` | when enabled, inside an effect | where enabled |

Stock keys seen in code include `Commandstart`, `Notenough`, `Mob_Select`, `Resource_Gain`, `Timertick`, `Warning`,
`Journal_Page`, `Menu_Tab`, `Button_Clickenter`, `Equip_Artifact`, `Unlock_General`, `Bounty_Accept`, `General_Hit`,
`Tide_Complete`, `Special_Tideintro`, `Ambient_Wind_Heavy`, `Ambient_Sea_Calm`. The full list is data (§6).

### 2.3 Mixer and loudness

- One `AudioMixer` (`Bla_Audio.Instance.Mixer`); each key plays on its `Soundpair.group`. The settings sliders write
  exposed parameters as `40 · log10(v + 0.01)` dB (Bl_Settings.cs:111-114). The game itself tweens `Music_Main`,
  `Music_Main_Freq`, `Music_Main_Freqwet` and `General_Ambient`; a mod writing those is overwritten by the running
  tween.
- **A mod source with no mixer group ignores the sliders** (it still obeys the quick mute, which sets
  `AudioListener.volume`). Route vessel sounds to a stock key's group (Argo uses the `Upgrade` key's).
- The game normalises nothing: a clip plays at `Soundpair.Volume` × its own level. `03` §6's method (match
  `rms × volume` to stock clips) is right; for music, measure stock tracks offline (§6).

## 3. Text and translations

### 3.1 Where strings live

- One CSV, `Resources.Load<TextAsset>("Localization")`: row 0 is the language names, the `English` column is the
  key, the player's column is the value (Bla_Localization.cs:225-343). The language is the save's
  `Settings/Language`, else Unity's `SystemLanguage` name ("French", "ChineseSimplified"…). Which languages ship is
  data (§6).
- **The English text is the key, and unknown keys display unchanged**: a mod's raw English shows in every language. An
  English string equal to a stock key gets the stock translation.
- **The kit's `<Name>_Localization.csv` is never read by the game** (only `Resources/Localization` and the dev file
  below are). It is a translator's sheet; translations need code (§3.4).
- If the scene's `Bla_Localization.dev_mode` is on (the code default is true; the shipped value is data), every unknown
  string is appended, one file write each, to `persistentDataPath/Localization.csv`. **Never use dynamic text as a
  key** ("Map 3 of 6", a name plus a number): use a `{0}` key with `GetText(key, n)`.

### 3.2 What is translated at display time

Being, building, upgrade and Cardinal names and descriptions, stat icon names, map and mode titles, the banner and
note, warning popups, end-screen tips. Networked texts travel as English and **each client translates them in its own
language**. A banner ending in a number is split, so "TIDE 5" looks up "TIDE {0}" (Game_Ui.cs:1520-1538).

### 3.3 Placeholders

`GetText(key, values)` replaces `{n}` with `values[n]` (each looked up as a key itself); `{word}` with no digits is
dropped; a backslash is dropped and turns placeholders off for the rest of the string (Bla_Localization.cs:475-540).

### 3.4 Adding translations

`Bla_Localization.translations` is a `public static Dictionary<string, string>` holding the current language only,
**replaced on every language load** (Bla_Localization.cs:33, 283-301). A pack writes its rows at boot and again from
`Bl_Settings.Settingschanged`, reading the language from `Bl_Settings.Toapplysettings.Language`; it never overwrites a
stock key. Labels the game refreshed before the hook ran keep English until the pack sets them again. Recipe:
`examples/recipes/RecipeText.cs`.

## 4. Pitfalls (predicted from the code; move to `09` once met)

1. An unknown sound key throws wherever the game plays it (§2.1).
2. The sound table never grows; appended variants never play (§2.1).
3. A `Soundtrackobject` or `Modeobject` made with `CreateInstance` has empty dictionaries: both build them in
   `OnEnable` from their lists. `Instantiate` a stock one, or fill both.
4. A soundtrack missing `Intro`, `Stinger` or a reached stage throws inside the client mode script; a null soundtrack
   stops the match load (Tds_Multiplayer_Remote.cs:674; Game_Init.cs:119).
5. `Bla_Ambient` traps: null `Oneshots` throws every frame; `Pan` defaults true (pans music); disabling it stops its
   whole tag, stock music included; the end screen and main menu shut every `Bla_Ambient` off.
6. A chained scene change skips the client mode's end: ambience stays at -5 dB and music plays on until the next
   Intro. Stop mod loops yourself.
7. `Blu4_Tooltip` with `Localize = false` registers the prefab's old label and reverts on a language change.
8. A pure-sound effect prefab sent through `Vfxoneshotcheck` never returns to the pool (its duration comes only from a
   `VisualEffect`'s "Lifetime", Vfx_Base.cs:57-83). Keep mod sounds on `Vis` events.

## 5. Corrections this chapter makes

- `01` §10, `03` §6: "mods play their own `AudioSource`s" is half the story: stock keys play from mod code through
  the public calls of §2.2, and `Visualproperties` entries carry `Sfxstart`/`Sfxend`.
- `03` §6 and the builders: the localization CSV is not read by the game (§3.1).
- `15` §18.2/§18.3: music stages are exact and ordered; the final map's soundtrack never plays (§1.2).

## 6. PC list (one probe build, logs only)

1. Every `Soundpair` in `Bla_Audio.Instance.Database`: key, group name, `Volume`, `Pitchvar`, variant count, clip
   names and lengths.
2. The six `Soundtrackobject`s' groups and keys.
3. `Bla_Settings.Instance.Audiomixers` and each one's value; the mixer's group names; live `Mindist`/`Maxdist`.
4. `Bla_Settings.Instance.Language`; the CSV's header line and row count; `dev_mode`; whether
   `persistentDataPath/Localization.csv` exists.
5. A stock TMP label's font and fallback count (which scripts it covers).
6. Offline: integrated loudness and true peak of two or three stock tracks, as the pack's music target.
7. Try a mod music clip on the music group, heard on host and client.

## 7. Sources

The decompiled `Assembly-CSharp.dll` (Blockage.Sound.*, Bla_Ambient, Bla_Audio, Vfx_Sound, Soundtrackobject,
Clientmode_*, Mode_Normal, Game_Init, Game_Client_Init, Bl_Localization, Bla_Localization, Bl_Settings), read on
2026-10-14 by a parallel reader and spot-checked by hand (`Bl_Sound` internal, the variant pick, `translations`);
`reference/roguelab_round1-2.txt` for the soundtrack names and `Musicwave` values.
