# 06 · The journal

Every vessel must have journal pages as complete as Smacky's, reachable from the vessel tabs on every vessel page.
The shared kit (`kit/Shared/ModKitJournal.cs`, compiled into each mod as `<Name>KitVesselJournal` /
`<Name>KitVesselTabs`) does all of it from a plain description. Verified in game with 8 vessels (5 shipped + Sprigg,
Argo, Mummer): every vessel page had exactly one tab per vessel, in the same order.

## 1. How the game's journal works

- Pages are `Logobject`s (`Title`, `Maindesc`, `Thumbnail`, `Parent`, `Unlockconditions`, `Elements`). `Elements` is a
  `[SerializeReference]` list of `Logitems`: `Text`, `Image`, `Image3D` (a live 3D model view), `TextImageBeing`,
  `Stats`, `Drops`, `Link`, `LinkImage`, `LinkUpgrade`, `TextUpgradeParam`, and `List`/`Grid` groups.
- A vessel's root page is `Heroentryobject.Log` (`Log_Hero_<Name>`). Shipped tree: `Log_Main` → `Log_Hero_X` →
  `Log_Hero_X_Tower_Y` → `Log_Tower_Y_KeyN_Upgradeobject`; units `Log_Mob_Z`.
- The **vessel tabs** are a `Grid` of `Link {Sidelink = true}` on every vessel page; each tab shows the target page's
  `Thumbnail` (32×32 white 1 px line icon, the game's Vesselicons style).
- Open in game (for captures): `Bl_Menus.Changemenu("Journal")` then `Loghandler.Log_Current = page`.

## 2. What the kit builds

| Page | Content (cloned from the template vessel's matching page, then edited) |
|---|---|
| `Log_Hero_<Name>` | idle portrait, name, the special rule as a banner (like Sno Sno's), stats, lore, TOWERS & UNITS links to each building, vessel tabs |
| `Log_Hero_<Name>_<BuildingId>` | the building's 3D model, stats, cost, NOTABLE UPGRADES (the tier-2 roots), RELATED ITEMS (its units) |
| `Log_<UpgradeId>` | the upgrade's model, banner, EFFECTS (one line per param, values formatted by the game), NEXT UPGRADES |
| `Log_<UnitId>` | the unit's sprite, stats, lore |

Pages are clones because `Object.Instantiate` deep-copies a page's element objects, so layout, fonts and colours stay
the game's. Brand-new `Link` + `Image3D` items did **not** draw; the kit reuses the template's items and deep-copies
extra ones from a scratch clone (`Fresh<T>`), destroying the scratch page so it never appears as a stray page.

**EFFECTS** labels: the kit knows the game's stats; your own params need labels in `spec.Labels` (`"Scraprange" →
"Scrap Range"`; `""` hides a param, e.g. on/off flags). Display style comes from the suffix: `_Multi`, `_Pmulti`,
`_Percent` → percent; `_Base` → float; `_Max`, `_Every`, `_Count` → integer; others → "+x".

## 3. Feeding it (`12_Journal.cs`)

```csharp
ModKitJournalSpec spec = new ModKitJournalSpec
{
    HeroId = MummerData.Hero, Template = template /* Smacky's entry */, Hero = heroEntity,
    Rule = heroEntity.Upgradesinit.Count > 0 ? heroEntity.Upgradesinit[0] : null,   // the tooltip upgrade
    Title = MummerData.Displayname, Lore = MummerData.Lore, Origin = "Mummer mod",
    Portrait = MummerAssets.Frames("Mummer-Sheet", 32, new Vector2(0.5f, 0.5f))[0],  // an idle frame
    Tab = MummerAssets.Single("Mummertabicon", 32), Accent = new Color(0.70f, 0.57f, 0.84f, 1f),
};
spec.Labels["Scraprange"] = "Scrap Range";
foreach (building) {
    var jb = new ModKitJournalBuilding { Id, Title, Desc = lore, Entity = being.Entity, Model = MummerAssets.Model(id) };
    // one ModKitJournalUpgrade per upgrade (Id, Title, Desc, Upgrade, Model), Next links as in the data, tier-2s in jb.Roots
    // units: jb.Units.Add(new ModKitJournalUnit { Id, Title, Desc, Entity, Look = first sprite frame })
    spec.Buildings.Add(jb);
}
ModKitVesselJournal journal = new ModKitVesselJournal(spec);
MummerContent.Entry.Log = journal.Build();      // the vessel's root page
ModKitVesselTabs.Sync();                        // tabs on every vessel page, now
// and every frame from Update: ModKitVesselTabs.Tick() (re-syncs every 3 s and when a vessel appears)
```

Build the journal **after** every being, upgrade and the entry exist (end of `Content.Build`), inside try/catch: a
journal failure must not stop the vessel loading.

## 4. Tab order (all mods must agree)

Shipped vessels first in the game's order (`Hero_Smacky`, `Hero_Snosno`, `Hero_Buster`, `Hero_Boutman`, `Hero_Fie`),
then every other vessel sorted by id (ordinal). The sync only adds missing tabs, removes duplicates and sorts, so it is
idempotent and any number of mods can run it in any load order. Argo 1.0 has its own copy (`VesselTabs`) with the same
rule.

Why it's needed: `Instantiate` remaps a page's references to itself, so a vessel page cloned from Smacky's turns
Smacky's own tab into a tab pointing at the clone (Sprigg 1.3 shipped with Smacky's tab missing).

## 5. Checks (the self-test's journal section)

- the vessel has its own page (`Entry.Log.name == "Log_Hero_<Name>"`) and it links every building page;
- a building page links its upgrade pages; a unit building's page links its unit pages;
- **every** vessel page (shipped and modded) has exactly one tab per vessel in the Hero folder;
- captures of the vessel page, a building page, an upgrade page, a unit page and another vessel's page, looked at.

## 6. Art for the journal

- Portrait: an idle frame facing the camera (like `Smacky4-Sheet_0`).
- Tab icon: 32×32, white, 1 px lines, open interior, the vessel's silhouette and one signature feature.
- Tower models: a disabled renderer of the whole model on the prefab's `Vis` so the 3D view frames the whole tower.
- Lore texts: building `Lore` (2 sentences, what it is and does), unit lore, upgrade desc (the same short phrase the
  upgrade panel shows).
