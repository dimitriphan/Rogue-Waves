#if ROGUEWAVESFOES_SELFTEST
// The four modifiers on a solo host: each on a real foe, read back from its stats and what stands near it. One launch.
public static class RWFTest
{
    public static int Pass, Fail, Setups, errors;
    static bool done, freeze;
    static readonly List<Vector3> used = new List<Vector3>();

    public static void Boot()
    {
        Application.logMessageReceived += OnLog;
        ModKitTest.Log = Line;
        RogueWavesFoesMod.Instance.StartCoroutine(Guarded());
    }

    static void Line(string message) { RWFLog.Info("TEST " + message); }
    static void Info(string name, string detail) { Line("INFO " + name + ": " + detail); }
    static void Setup(string name, string why) { Setups++; Line("SETUP " + name + ": " + why); }
    static bool Check(string name, bool ok, string detail = "")
    {
        if (ok) Pass++; else Fail++;
        Line((ok ? "PASS " : "FAIL ") + name + (detail != "" ? ": " + detail : ""));
        return ok;
    }

    static void OnLog(string condition, string stack, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error) return;
        errors++;
        if (errors <= 20) Info("error seen", (condition ?? "").Split('\n')[0]);
    }

    public static void Update()
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        if (vars != null && freeze && !done && Game_Entity.Instance != null && Game_Entity.Instance.IsServer && vars.Time_Wave.Value < 600f)
            vars.Time_Wave.Value = 900f;
    }

    static IEnumerator Until(Func<bool> ok, float timeout)
    {
        float t = 0f;
        while (!ok() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
    }

    static IEnumerator Guarded()
    {
        IEnumerator run = Run();
        while (true)
        {
            object step;
            try
            {
                if (!run.MoveNext()) break;
                step = run.Current;
            }
            catch (Exception e) { Check("self-test ran to the end", false, e.ToString()); break; }
            yield return step;
        }
        done = true;
        Line("DONE pass " + Pass + ", fail " + Fail + ", setup " + Setups + ", errors logged " + errors);
    }

    static float Stat(Entity_Master e, string name)
    {
        Link_Stats s;
        return e != null && e.Statsserver != null && e.Statsserver.TryGetValue(name, out s) ? s.Value_Calc : -1f;
    }

    static float F(float v) { return Mathf.Round(v * 100f) / 100f; }

    static Entity_Master Marked(string being, string modifier, Vector3 near, float min, float max)
    {
        Entity_Master foe = ModKitTest.SpawnFoe(being, near, min, max, used);
        if (foe == null) return null;
        Upgradeproperties u = Bl_Address.Getload("Upgrade", RWFModifiers.Id(modifier)) as Upgradeproperties;
        if (u != null) Helper_Entitytools.Upgrade(foe, u, Helper_Entitytools.Upgradecheck.Forced, null);
        return foe;
    }

    static IEnumerator Run()
    {
        yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null, 120f);
        yield return new WaitForSecondsRealtime(8f);
        Mainmenu_Main menu = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
        if (menu != null) menu.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance.Ready, 120f);
        if (Macro_Main.Instance == null) { Setup("boat", "no boat"); yield break; }
        yield return new WaitForSecondsRealtime(4f);
        foreach (string m in RWFModifiers.All)
            Check(m + " is registered for the mode to name", Bl_Address.Getload("Upgrade", RWFModifiers.Id(m)) is Upgradeproperties, RWFModifiers.Id(m));
        Macro_Main.Instance.Selectcharacter("Hero_Smacky");
        string why;
        string node = ModKitTest.PickNode(out why);
        Line("picked Smacky and map node '" + node + "' " + why);
        if (node == "") { Setup("map", why); yield break; }
        Tds_Multiplayer_Remote.Instance.Selectmap_ServerRpc(node, false, default(Unity.Netcode.ServerRpcParams));
        yield return new WaitForSecondsRealtime(3f);
        Macro_Main.Instance.Playgame(true);
        yield return Until(() => Game_Mapvars.Instance != null && Game_Mapvars.Instance.Maploaded.Value && Game_Rounds.Instance != null, 200f);
        if (Game_Mapvars.Instance == null) { Setup("map", "no map"); yield break; }
        // The vessels' start runs on the Tide clock: nothing holds it until one stands.
        Entity_Master hero = null;
        List<Entity_Master> heroes = null;
        yield return Until(() => Game_Entity.Instance.Entityclienttags.TryGetValue("Hero", out heroes) && heroes.Count > 0, 90f);
        if (heroes != null && heroes.Count > 0) hero = heroes[0];
        freeze = true;
        yield return new WaitForSeconds(4f);
        Entity_Master core = ModKitTest.Core();
        if (core == null) { Setup("map", "no core"); yield break; }
        Vector3 c = core.Entityposition.Value;

        // A tower of the player's, to feel the auras.
        Beingobject archer = Bl_Address.Getload("Mob", "Tower_Smackyarcher") as Beingobject;
        Entity_Master tower = null;
        if (hero != null && archer != null)
        {
            Vector3 at = ModKitTest.Spot(hero.Entityposition.Value, archer, hero.Entitydata.Value.Team, 3f, 8f, used, "tower");
            if (at != Vector3.zero)
            {
                Link_Spawn spawn = Helper_Entitytools.Createlinkspawnparent(hero.Beingserver);
                spawn.Being = archer;
                spawn.Genadd = ModKitTest.Surface(at);
                spawn.Skin = "";
                spawn.Spawnstyle = Link_Spawn.Spawnstyles.Forced;
                Link_Being made = Helper_Entitytools.Spawn(spawn, at, hero.Beingserver, true, null);
                tower = made != null ? made.Worldobject : null;
            }
        }
        yield return new WaitForSeconds(1.5f);
        if (tower == null) { Setup("auras", "no tower"); }

        // Aging: at once 30% slower; grows.
        Entity_Master aging = Marked("Enemy_Dagger", RWFModifiers.Aging, c, 12f, 30f);
        if (aging == null) { Setup("Aging", "no foe"); yield break; }
        float agingSpeed0 = -1f;
        Entity_Master plain = ModKitTest.SpawnFoe("Enemy_Dagger", c, 12f, 30f, used);
        yield return new WaitForSeconds(1.5f);
        float baseSpeed = Stat(plain, "Speed"), baseDamage = Stat(plain, "Damage"), baseHealth = Stat(plain, "Health");
        agingSpeed0 = Stat(aging, "Speed");
        Check("Aging: 30% slower from its spawn", baseSpeed > 0f && Mathf.Abs(agingSpeed0 / baseSpeed - 0.7f) < 0.03f, F(agingSpeed0) + " against " + F(baseSpeed));
        yield return new WaitForSeconds(5f);
        float d1 = Stat(aging, "Damage"), h1 = Stat(aging, "Health");
        Check("Aging: health and damage grow as it lives", d1 > baseDamage && h1 > baseHealth, "after about 6.5 s: damage " + F(d1) + " of " + F(baseDamage) + ", health " + F(h1) + " of " + F(baseHealth));
#if ROGUEWAVESFOES_SHOTS
        yield return Shot("aging");
#endif

        // Berserk: stronger as it hurts.
        Entity_Master berserk = Marked("Enemy_Dagger", RWFModifiers.Berserk, c, 12f, 30f);
        yield return new WaitForSeconds(1f);
        float b0 = Stat(berserk, "Damage"), s0 = Stat(berserk, "Speed");
        Helper_Entitytools.Interacthurt(berserk, berserk, new Dictionary<string, float> { { "Damage", Stat(berserk, "Health") * 0.5f }, { "Damagemulti", 1f } });
        yield return new WaitForSeconds(1f);
        float b1 = Stat(berserk, "Damage"), s1 = Stat(berserk, "Speed");
        Check("Berserk: at half health, more damage and speed", b1 > b0 * 1.3f && s1 > s0 * 1.1f, "damage " + F(b0) + " -> " + F(b1) + ", speed " + F(s0) + " -> " + F(s1));
        Check("Berserk: the effect is on the foe, its look worn on this machine", RWFLooks.Holds(berserk) == RWFModifiers.Berserk && RWFLooks.Worn >= 1, "worn " + RWFLooks.Worn);

        // Cursed and Dazzling, beside the tower.
        if (tower != null)
        {
            float td0 = Stat(tower, "Damage");
            Entity_Master cursed = Marked("Enemy_Dagger", RWFModifiers.Cursed, tower.Entityposition.Value, 1f, 2.8f);
            yield return new WaitForSeconds(2.5f);
            float td1 = Stat(tower, "Damage");
            bool near = cursed != null && ModKitTest.Flat(cursed.Entityposition.Value, tower.Entityposition.Value) <= RWFData.CursedRange;
            Check("Cursed: a tower nearby deals less while it stands", near && Mathf.Abs(td1 / td0 - (1f + RWFData.CursedDamage)) < 0.05f, F(td0) + " -> " + F(td1) + (cursed != null ? ", " + F(ModKitTest.Flat(cursed.Entityposition.Value, tower.Entityposition.Value)) + " away" : ""));
#if ROGUEWAVESFOES_SHOTS
            yield return Shot("cursed");
#endif
            if (cursed != null) Game_Entity.Instance.Despawn(cursed.Beingserver, false, "RogueWaves");
            yield return new WaitForSeconds(2f);
            float td2 = Stat(tower, "Damage");
            Check("Cursed: and gets it back once the foe is gone", Mathf.Abs(td2 - td0) < 0.05f, F(td1) + " -> " + F(td2));

            int before = errors;
            Entity_Master dazzling = Marked("Enemy_Dagger", RWFModifiers.Dazzling, tower.Entityposition.Value, 1f, 2.8f);
            yield return new WaitForSeconds(2.5f);
            float td3 = Stat(tower, "Damage");
            bool lit = tower.Upgradelistserver.ContainsKey(RWFModifiers.Illuminate.Objectname);
            Check("Dazzling: a tower nearby is Blinded and Illuminated", td3 < td0 && lit, "damage " + F(td0) + " -> " + F(td3) + ", illuminated " + lit);
#if ROGUEWAVESFOES_SHOTS
            yield return Shot("dazzling");
#endif
            if (hero != null)
            {
                Helper_Entitytools.Interacthurt(hero, tower, new Dictionary<string, float> { { "Damage", 1f }, { "Damagemulti", 1f } });
                yield return new WaitForSeconds(0.2f);
            }
            if (dazzling != null) Game_Entity.Instance.Despawn(dazzling.Beingserver, false, "RogueWaves");
            yield return new WaitForSeconds(5f);
            Check("Dazzling: the Illuminate mark goes once the foe is gone, with no exception", !tower.Upgradelistserver.ContainsKey(RWFModifiers.Illuminate.Objectname) && errors == before, "errors " + (errors - before));
        }
    }

#if ROGUEWAVESFOES_SHOTS
    static IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        int w = Screen.width, h = Screen.height;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        int k = Mathf.Max(1, Mathf.CeilToInt(w / 1280f));
        int ow = w / k, oh = h / k;
        Color32[] src = tex.GetPixels32();
        UnityEngine.Object.Destroy(tex);
        byte[] bytes = new byte[ow * oh * 4];
        for (int y = 0; y < oh; y++)
            for (int x = 0; x < ow; x++)
            {
                Color32 c = src[(y * k) * w + x * k];
                int i = (y * ow + x) * 4;
                bytes[i] = c.r; bytes[i + 1] = c.g; bytes[i + 2] = c.b; bytes[i + 3] = 255;
            }
        string b64 = Convert.ToBase64String(bytes);
        const int Chunk = 8000;
        int parts = (b64.Length + Chunk - 1) / Chunk;
        for (int p = 0; p < parts; p++)
            Debug.Log("[RogueWavesFoesDump] shot_" + name + " " + ow + "x" + oh + " " + (p + 1) + "/" + parts + " " + b64.Substring(p * Chunk, Math.Min(Chunk, b64.Length - p * Chunk)));
    }
#endif
}
#endif
