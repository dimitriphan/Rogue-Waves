// Round 4 (0.4.0), map 1: the mechanics Dimitri's 2026-10-09 scope adds that the game's code alone can't settle
// (docs/MECHANICS_CHECK.md in the Rogue Waves repository). Solo host; one line per question, as rounds 1-3.
//   break   V2 a button added to the vessel mid-match shows on the selected vessel's bar, and goes again
//           V1 a neutral stand-in for the shopkeeper VENDCINT: spawned near the core with owner -1 and every player as a
//              subowner, its button shows for the local player, its sprites take the vessels' fall-in (_Offset), then
//              it jumps up and is removed
//           F1 Fund Transfer's moves: take and give each base aura on the host
//           O1 a minion spawned from the vessel belongs to the player and carries the Mob tag (aura fruit pays owners)
//   Tide 1  K1 an enemy's death reaches Ondespawn with its place, and who hit it last (Fertilizer counts kills nearby)
//           D1 the vessel's Damagedone after a hit, and a heal back (Vampire Dentures)
//           A1 Aging: raise a hurt enemy's max health, speed and damage at runtime; is the health fraction kept
//           E1 a look on the enemy through Vfxdebuffcheck (modifiers show as an effect, not a skin)
public static class RogueLabRound4
{
    const string FertilizeLink = "RogueLab_Fertilize";
    const string ShopLink = "RogueLab_Shop";
    static string buttonCommand = "";                       // a stock Button-style command the vessel already uses
    static readonly List<string> deaths = new List<string>();
    static Entity_Master watched;
    static bool watchedDied;
    static string watchedLasthit = "", watchedPlace = "";

    public static IEnumerator Map1()
    {
        RogueLabMatch.freeze = true;                        // hold the opening break
        yield return new WaitForSeconds(2f);
        yield return Guarded("V2 vessel button added mid-match", VesselButton());
        yield return Guarded("V1 shopkeeper stand-in", Shop());
        RogueLabMatch.Guard("F1 Fund Transfer moves", Fund);
        yield return Guarded("O1 minion owner and tags", MinionOwner());
        yield return Guarded("Tide 1 probes", TideProbes());
    }

    // A probe that throws is one FAIL line, and the round goes on.
    static IEnumerator Guarded(string name, IEnumerator probe)
    {
        while (true)
        {
            object current;
            try
            {
                if (!probe.MoveNext()) yield break;
                current = probe.Current;
            }
            catch (Exception e) { RogueLabProbe.Check(name, false, e.ToString()); yield break; }
            yield return current;
        }
    }

    // ---- V2: Yossele's Fertilize button appears only once a Direction unlocks it.
    static IEnumerator VesselButton()
    {
        Entity_Master hero = RogueLabUtil.MainHero();
        if (hero == null || Game_Selection.Instance == null || Game_Ui.Instance == null) { RogueLabProbe.Setup("V2 vessel button added mid-match", "hero " + (hero != null) + ", selection " + (Game_Selection.Instance != null)); yield break; }
        List<string> seen = new List<string>();
        foreach (KeyValuePair<string, Link_Command> c in hero.Commandlistserver)
        {
            seen.Add(c.Key + "=" + (c.Value.Ref != null ? c.Value.Ref.Objectname + "/" + c.Value.Ref.Style : "none"));
            if (buttonCommand == "" && c.Value.Ref != null && c.Value.Ref.Style == Commandobject.Styles.Button) buttonCommand = c.Value.Ref.Objectname;
        }
        RogueLabProbe.Info("V2 vessel commands", string.Join(", ", seen.ToArray()));
        if (buttonCommand == "") { RogueLabProbe.Setup("V2 vessel button added mid-match", "the vessel has no Button-style command to borrow"); yield break; }
        Select(hero);
        yield return new WaitForSeconds(1f);
        bool before = UiHas(FertilizeLink);
        Helper_Entitytools.Createcommand(hero, buttonCommand, FertilizeLink, false, true);
        yield return new WaitForSeconds(1.5f);
        bool shown = UiHas(FertilizeLink);
        Helper_Entitytools.Removecommand(hero, FertilizeLink);
        yield return new WaitForSeconds(1.5f);
        bool gone = !UiHas(FertilizeLink);
        RogueLabProbe.Check("V2 vessel button added mid-match shows on the selected vessel's bar, and goes when removed", !before && shown && gone,
            "borrowed " + buttonCommand + "; before " + before + ", after Createcommand " + shown + ", after Removecommand gone " + gone);
        Game_Selection.Instance.Selected = null;
    }

    // ---- V1: VENDCINT, played by a Boutbro stand-in.
    static IEnumerator Shop()
    {
        Entity_Master hero = RogueLabUtil.MainHero();
        Entity_Master core = RogueLabMatch.PlayerCore();
        Beingobject being = RogueLabUtil.Load<Beingobject>("Mob", RogueLabData.Bro);
        if (hero == null || core == null || being == null) { RogueLabProbe.Setup("V1 shopkeeper stand-in", "hero " + (hero != null) + ", core " + (core != null) + ", being " + (being != null)); yield break; }
        Genitem tile = Tile(being, core.Entityposition.Value, hero.Entitydata.Value.Team);
        if (tile == null) { RogueLabProbe.Setup("V1 shopkeeper stand-in", "no tile near the core"); yield break; }
        Link_Spawn spawn = new Link_Spawn();
        spawn.Being = being; spawn.Genadd = tile; spawn.Skin = ""; spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        spawn.Owner = -1; spawn.Team = hero.Entitydata.Value.Team;
        foreach (long id in Blm_Server.Instance.Sessionids.Keys) spawn.Subowners.Add(id);
        Link_Being made = Helper_Entitytools.Spawn(spawn, tile.Position, null, false, null);
        yield return new WaitForSeconds(1f);
        Entity_Master shop = made != null ? made.Worldobject : null;
        if (shop == null) { RogueLabProbe.Check("V1 shop spawned with owner -1 and every player as subowner", false, "Spawn returned no entity"); yield break; }
        long[] subs = shop.Entitydata.Value.Subowner;
        bool sub = subs != null && Array.IndexOf(subs, RogueLabUtil.LocalId) >= 0;
        RogueLabProbe.Check("V1 shop spawned with owner -1 and every player as subowner", RogueLabUtil.Owner(shop) == -1 && sub,
            "owner " + RogueLabUtil.Owner(shop) + ", team " + shop.Entitydata.Value.Team + ", subowners [" + Join(subs) + "], local " + RogueLabUtil.LocalId
            + ", in the F list " + RogueLabUtil.Heroes().Contains(shop) + ", " + RogueLabUtil.F(Vector3.Distance(shop.Entityposition.Value, core.Entityposition.Value)) + " from the core");

        // Its button for a subowner.
        if (buttonCommand != "")
        {
            Helper_Entitytools.Createcommand(shop, buttonCommand, ShopLink, false, true);
            Select(shop);
            yield return new WaitForSeconds(1.5f);
            RogueLabProbe.Check("V1 shop button shows for a subowner who selects it", UiHas(ShopLink), "borrowed " + buttonCommand);
            Game_Selection.Instance.Selected = null;
        }

        // The fall-in, copied from Entity_Vis_Hero.Landing (client side; here the host is the client).
        List<Material> mats = OffsetMaterials(shop.gameObject);
        int renderers = shop.GetComponentsInChildren<SpriteRenderer>(true).Length;
        string anims = Animations(shop.gameObject), heroAnims = Animations(hero.gameObject);
        Play(shop.gameObject, "Falling");
        float t = 0f, mid = -1f;
        while (t < 3f)
        {
            t += Time.deltaTime;
            Vector4 v = Vector4.Lerp(new Vector4(-10f, 200f, -10f, 0f), Vector4.zero, t / 3f);
            foreach (Material m in mats) m.SetVector("_Offset", v);
            if (mid < 0f && t > 1.5f && mats.Count > 0) mid = mats[0].GetVector("_Offset").y;
            yield return null;
        }
        foreach (Material m in mats) m.SetVector("_Offset", Vector4.zero);
        Play(shop.gameObject, "Land");
        RogueLabProbe.Check("V1 shop sprites take the vessels' fall-in offset", mats.Count > 0 && mid > 0f,
            mats.Count + " of " + renderers + " sprite materials have _Offset, y halfway " + RogueLabUtil.F(mid) + "; stand-in animations [" + anims + "]; vessel animations [" + heroAnims + "]");

        // The jump: the same offset upwards, then removed without a death.
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            foreach (Material m in mats) m.SetVector("_Offset", Vector4.Lerp(Vector4.zero, new Vector4(10f, 200f, 10f, 0f), t));
            yield return null;
        }
        Helper_Entitytools.Despawn(shop.Beingserver, false, "RogueLab");
        yield return new WaitForSeconds(1f);
        RogueLabProbe.Check("V1 shop removed after its jump", shop == null || !RogueLabUtil.Alive(shop), "");
    }

    // ---- F1: what a Fund Transfer does on the host, per colour (solo: the same player gives and takes).
    static void Fund()
    {
        long me = RogueLabUtil.LocalId;
        Tds_Clientinfo info;
        if (Game_Players.Instance == null || !Game_Players.Instance.Prevsessionlist.TryGetValue(me, out info)) { RogueLabProbe.Setup("F1 Fund Transfer moves", "the local player is not in Prevsessionlist"); return; }
        Game_Mapvars vars = Game_Mapvars.Instance;
        float gain = vars.Resourcegainmultiplier, loss = vars.Resourcelossmultiplier;
        vars.Resourcegainmultiplier = 1f; vars.Resourcelossmultiplier = 1f;
        string[] colours = { "Redaura", "Blueaura", "Yellowaura" };
        List<string> lines = new List<string>();
        bool ok = true;
        foreach (string c in colours)
        {
            Helper_Resources.Giveresources(new List<Link_Resourceset> { new Link_Resourceset { Name = c, Amount = 100f } }, me, Helper_Resources.Styles.Default);
            float a = Amount(info, c);
            bool took = Helper_Resources.Useresources(new List<Link_Resourceset> { new Link_Resourceset { Name = c, Amount = 40f } }, me, 1f);
            float b = Amount(info, c);
            Helper_Resources.Giveresources(new List<Link_Resourceset> { new Link_Resourceset { Name = c, Amount = 40f } }, me, Helper_Resources.Styles.Default);
            float d = Amount(info, c);
            ok &= took && Mathf.Abs(a - b - 40f) < 0.01f && Mathf.Abs(d - a) < 0.01f;
            lines.Add(c + " " + RogueLabUtil.F(a) + " -> " + RogueLabUtil.F(b) + " -> " + RogueLabUtil.F(d));
        }
        vars.Resourcegainmultiplier = gain; vars.Resourcelossmultiplier = loss;
        RogueLabProbe.Check("F1 Fund Transfer takes and gives each base aura exactly", ok, string.Join("; ", lines.ToArray()) + "; multipliers were gain " + RogueLabUtil.F(gain) + ", loss "
            + RogueLabUtil.F(loss) + "; Wave " + vars.Wave.Value + " in the opening break, so 50 x Wave is " + (50 * vars.Wave.Value));
    }

    // ---- O1: a minion made by the vessel itself: whose is it (aura fruit pays the picker's owner).
    static IEnumerator MinionOwner()
    {
        Entity_Master hero = RogueLabUtil.MainHero();
        Beingobject being = RogueLabUtil.Load<Beingobject>("Mob", RogueLabData.Bro);
        if (hero == null || being == null) { RogueLabProbe.Setup("O1 minion owner and tags", "hero " + (hero != null) + ", being " + (being != null)); yield break; }
        Genitem tile = Tile(being, hero.Entityposition.Value, hero.Entitydata.Value.Team);
        if (tile == null) { RogueLabProbe.Setup("O1 minion owner and tags", "no tile near the vessel"); yield break; }
        Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(hero.Beingserver);
        spawn.Being = being; spawn.Genadd = tile; spawn.Skin = ""; spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
        Link_Being made = Helper_Entitytools.Spawn(spawn, tile.Position, hero.Beingserver, true, null);
        yield return new WaitForSeconds(1f);
        Entity_Master minion = made != null ? made.Worldobject : null;
        if (minion == null) { RogueLabProbe.Check("O1 minion from the vessel belongs to the player", false, "Spawn returned no entity"); yield break; }
        RogueLabProbe.Check("O1 minion from the vessel belongs to the player and is tagged Mob", RogueLabUtil.Owner(minion) == RogueLabUtil.LocalId && minion.Taglistserver.Contains("Mob"),
            "owner " + RogueLabUtil.Owner(minion) + " (local " + RogueLabUtil.LocalId + "), tags [" + string.Join(",", minion.Taglistserver.ToArray()) + "]; vessel tags ["
            + string.Join(",", hero.Taglistserver.ToArray()) + "]");
        Helper_Entitytools.Despawn(minion.Beingserver, false, "RogueLab");
    }

    // ---- Tide 1: K1, D1, A1, E1 on the first foes.
    static IEnumerator TideProbes()
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        RogueLabMatch.freeze = false;
        RogueLabMatch.forcing = true;
        yield return RogueLabMatch.Until(() => vars.Wave.Value >= 1, 60f);
        RogueLabMatch.forcing = false;
        int entry = vars.Entry.Value;
        yield return RogueLabMatch.Until(() => Foes().Count >= 2, 90f);
        List<Entity_Master> foes = Foes();
        if (foes.Count < 2) { RogueLabProbe.Setup("Tide 1 probes", "fewer than two foes in Tide 1 (" + foes.Count + ")"); yield break; }
        Entity_Master hero = RogueLabUtil.MainHero();
        if (hero == null) { RogueLabProbe.Setup("Tide 1 probes", "no vessel"); yield break; }
        Entity_Master a = foes[0], b = foes[1];
        Game_Entity.Instance.Ondespawn += OnDespawn;
        try
        {
            // D1: the vessel's own hit, as its attack sends it (no Damage in the data: the vessel's Damage stat is used).
            float dd0 = hero.Damagedone, h0 = Health(a);
            Helper_Entitytools.Interacthurt(hero, a, null);
            float dd1 = hero.Damagedone, h1 = Health(a);
            Helper_Entitytools.Interacthurt(b, hero, new Dictionary<string, float> { { "Damage", 5f }, { "Damagemulti", 1f } });
            float hh0 = Health(hero);
            Helper_Entitytools.Interactheal(hero, hero, new Dictionary<string, float> { { "Heal", 3f } });
            float hh1 = Health(hero);
            RogueLabProbe.Check("D1 the vessel's Damagedone counts its hit", dd1 - dd0 > 0f, "Damagedone " + RogueLabUtil.F(dd0) + " -> " + RogueLabUtil.F(dd1) + ", foe health "
                + RogueLabUtil.F(h0) + " -> " + RogueLabUtil.F(h1) + ", vessel Damage stat " + RogueLabUtil.F(Stat(hero, "Damage")));
            RogueLabProbe.Info("D1 heal on the vessel", "after a 5 hit " + RogueLabUtil.F(hh0) + ", after Interactheal Heal 3 " + RogueLabUtil.F(hh1));

            // A1 and E1 on foe b.
            Aging(b);
            Look(b);
        }
        catch (Exception e) { RogueLabProbe.Check("Tide 1 probes", false, e.ToString()); }

        // K1: kill foe a with the vessel's own hits, and see what Ondespawn gets.
        watched = a;
        for (int i = 0; i < 200 && RogueLabUtil.Alive(a); i++)
        {
            try { Helper_Entitytools.Interacthurt(hero, a, null); } catch (Exception) { break; }
            if (i % 10 == 9) yield return null;
        }
        yield return new WaitForSeconds(1f);
        RogueLabProbe.Check("K1 an enemy's death reaches Ondespawn with its place and its last hitter", watchedDied && watchedLasthit == RogueLabUtil.BeingName(hero),
            "died " + watchedDied + " at " + watchedPlace + ", Lasthit " + watchedLasthit + "; vessel Killed " + hero.Killed + "; other deaths this Tide [" + string.Join("; ", deaths.ToArray()) + "]");
        Game_Entity.Instance.Ondespawn -= OnDespawn;
        RogueLabMatch.ClearFoes();
        yield return RogueLabMatch.EndTide(entry);
        RogueLabMatch.freeze = true;
    }

    static void OnDespawn(Entity_Master master, bool killed, string reason)
    {
        if (master == null || master.Entitydata.Value.Team != 0) return;
        string place = master.Entityposition.Value.ToString("F1");
        string hit = master.Lasthit != null ? RogueLabUtil.BeingName(master.Lasthit) : "none";
        if (master == watched) { watchedDied = killed; watchedLasthit = hit; watchedPlace = place; return; }
        if (deaths.Count < 8) deaths.Add(RogueLabUtil.BeingName(master) + " killed " + killed + " by " + hit + " (" + reason + ")");
    }

    // A1: Aging's numbers at runtime. Health is a stat: Value now, Value_Calc the max.
    static void Aging(Entity_Master foe)
    {
        Link_Stats hp;
        if (!foe.Statsserver.TryGetValue("Health", out hp)) { RogueLabProbe.Setup("A1 Aging", "no Health stat"); return; }
        string type = hp.Value_Type.ToString();
        Helper_Entitytools.Interacthurt(foe, foe, new Dictionary<string, float> { { "Damage", hp.Value_Calc * 0.5f }, { "Damagemulti", 1f } });
        float v0 = hp.Value, m0 = hp.Value_Calc, f0 = m0 > 0f ? v0 / m0 : 0f;
        Helper_Entitytools.Addmod(foe, "Health", 1f, Helper_Entitytools.Modtypes.Multi, 0f);
        float v1 = hp.Value, m1 = hp.Value_Calc, f1 = m1 > 0f ? v1 / m1 : 0f;
        bool kept = Mathf.Abs(f1 - f0) < 0.02f;
        if (!kept) hp.Value = f0 * m1;                      // the mod's fix if the game doesn't keep it
        float v2 = hp.Value;
        float s0 = Stat(foe, "Speed"), d0 = Stat(foe, "Damage");
        Helper_Entitytools.Addmod(foe, "Speed", -0.3f, Helper_Entitytools.Modtypes.Multi, 0f);
        Helper_Entitytools.Addmod(foe, "Damage", 1f, Helper_Entitytools.Modtypes.Multi, 0f);
        float s1 = Stat(foe, "Speed"), d1 = Stat(foe, "Damage");
        RogueLabProbe.Check("A1 Aging doubles max health at runtime", Mathf.Abs(m1 - 2f * m0) < 0.5f, "health type " + type + ": " + RogueLabUtil.F(v0) + "/" + RogueLabUtil.F(m0)
            + " -> " + RogueLabUtil.F(v1) + "/" + RogueLabUtil.F(m1) + ", fraction kept by the game " + kept + (kept ? "" : ", set by the mod to " + RogueLabUtil.F(v2)));
        RogueLabProbe.Check("A1 Aging slows by 30% and doubles damage at runtime", s0 > 0f && Mathf.Abs(s1 / s0 - 0.7f) < 0.02f && (d0 <= 0f || Mathf.Abs(d1 / d0 - 2f) < 0.02f),
            "Speed " + RogueLabUtil.F(s0) + " -> " + RogueLabUtil.F(s1) + ", Damage " + RogueLabUtil.F(d0) + " -> " + RogueLabUtil.F(d1));
    }

    // E1: a look on the enemy through the game's debuff effects (Boutman's red glow stands in for a modifier's look).
    static void Look(Entity_Master foe)
    {
        Entity_Master_Client client = foe.GetComponent<Entity_Master_Client>();
        if (client == null || client.Visobj == null || foe.Radio == null) { RogueLabProbe.Setup("E1 modifier look", "client " + (client != null) + ", radio " + (foe.Radio != null)); return; }
        int before = client.Visobj.transform.childCount;
        foe.Radio.Vfxdebuffcheck("Debuff_Boutman_Red", foe.Beingref.Value, 5f);
        RogueLabMod.Instance.StartCoroutine(LookAfter(client, before));
    }

    static IEnumerator LookAfter(Entity_Master_Client client, int before)
    {
        yield return new WaitForSeconds(0.5f);
        int after = client != null && client.Visobj != null ? client.Visobj.transform.childCount : -1;
        RogueLabProbe.Check("E1 a debuff look attaches to the enemy", after > before, "Vis children " + before + " -> " + after);
    }

    // ---- helpers
    static void Select(Entity_Master master)
    {
        Game_Selection.Instance.Selected = master;
        Game_Selection.Instance.Select();
    }

    static bool UiHas(string link)
    {
        Blockage.Menu.Blu2_Menu_Handler main;
        if (!Blockage.Menu.Bl_Menus.Menus.TryGetValue("Main", out main) || main == null) return false;
        foreach (Transform t in main.GetComponentsInChildren<Transform>(true))
            if (t.name == link) return true;
        return false;
    }

    static Genitem Tile(Beingobject being, Vector3 near, int team)
    {
        Link_Being probe = new Link_Being { Being = being, Entity = being.Entity, Block = being.Block, Team = team };
        foreach (Genitem g in ModKitTest.Ground(near, 4f, 1, 1))
        {
            Genitem st = null;
            try { st = Helper_Gridtools.Checkifstandable(g, probe); } catch (Exception) { }
            if (st != null && ModKitTest.Flat(g.Position, near) > 1f) return g;
        }
        return null;
    }

    static List<Material> OffsetMaterials(GameObject go)
    {
        List<Material> list = new List<Material>();
        foreach (SpriteRenderer r in go.GetComponentsInChildren<SpriteRenderer>(true))
            foreach (Material m in r.materials)
                if (m != null && m.HasProperty("_Offset")) list.Add(m);
        return list;
    }

    static string Animations(GameObject go)
    {
        List<string> names = new List<string>();
        foreach (Entity_Vis_Animator_Base a in go.GetComponentsInChildren<Entity_Vis_Animator_Base>(true))
            if (a.Spriteclone != null && a.Spriteclone.Items != null)
                foreach (string k in a.Spriteclone.Items.Keys)
                    if (!names.Contains(k)) names.Add(k);
        return string.Join(",", names.ToArray());
    }

    static void Play(GameObject go, string anim)
    {
        foreach (Entity_Vis_Animator_Base a in go.GetComponentsInChildren<Entity_Vis_Animator_Base>(true))
            a.Animplay(anim, Game_Sprite.Animationtype.Force);
    }

    static List<Entity_Master> Foes()
    {
        List<Entity_Master> found = new List<Entity_Master>();
        List<Entity_Master> list;
        if (Game_Entity.Instance == null || !Game_Entity.Instance.Entityclienttags.TryGetValue("Enemy", out list)) return found;
        foreach (Entity_Master e in list)
            if (RogueLabUtil.Alive(e) && e.Entitydata.Value.Team == 0 && !e.Taglistserver.Contains("Enemycore") && e.Statsserver.ContainsKey("Health"))
                found.Add(e);
        return found;
    }

    static float Health(Entity_Master m) { Link_Stats s; return m != null && m.Statsserver.TryGetValue("Health", out s) ? s.Value : -1f; }
    static float Stat(Entity_Master m, string name) { Link_Stats s; return m != null && m.Statsserver.TryGetValue(name, out s) ? s.Value_Calc : -1f; }

    static float Amount(Tds_Clientinfo info, string colour)
    {
        foreach (Link_Resource r in info.Resources)
            if (r.Name.ToString() == colour) return r.Amount;
        return 0f;
    }

    static string Join(long[] ids)
    {
        if (ids == null) return "";
        List<string> s = new List<string>();
        foreach (long id in ids) s.Add(id.ToString());
        return string.Join(",", s.ToArray());
    }
}
