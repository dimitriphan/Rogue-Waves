// RogueWavesFoes: the pack's enemy modifiers (docs/PACK.md: prefab RogueWaves_Foes_Default, dev mod RogueWavesFoesMod; docs/scope/13;
// kit docs/13 §6, docs/reports/SCOPE_FIT_PLAN_2026-10-28.md §5). Each modifier is an upgrade on the foe, named
// RW_Modifier_<Name>_Upgradeobject, that the mode part writes into its Tide rows (Wavecontent.Upgrades) by name: parts meet
// through the game's load tables, not through types. The upgrade carries no script: this part's host brain finds foes
// holding it and does the work; every machine reads the foe's synced Upgradelist and draws its effect. No new skins
// (docs/scope/13): each shows as an effect, in a colour none of the stock elites use (docs/STYLE.md "Enemies", kit docs/18 §5.5).
//   Cursed    an aura: nearby vessels, towers and units deal less damage while they stay in it (RogueLab R6's held loop, proven)
//   Berserk   damage and speed rise as its health falls
//   Aging     30% slower, health and damage up to +100% over 50 s (RogueLab A1: the health fraction is kept)
//   Dazzling  an aura: nearby vessels, towers and units are Blinded (they deal less) and Illuminated (they take more):
//             the stock timed Barrel_Boost and the stock Merchut mark on pack numbers (RogueLab R1; docs/scope/10)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Blockage.Address;

public class RogueWavesFoesMod : MonoBehaviour
{
    public static RogueWavesFoesMod Instance;
    public static bool Ready;

    void Start()
    {
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        ModKitPack.Join(RWFData.Part, RWFData.Version);
        RWFLog.Info("loaded v" + RWFData.Version);
        StartCoroutine(Boot());
    }

    IEnumerator Boot()
    {
        while (Tds_Masterdatabase.Instance == null || Bla_Addressables.Instance == null || !Tds_Masterdatabase.Instance.Items.ContainsKey("Hero"))
            yield return new WaitForSecondsRealtime(0.5f);
        Heroentryobject smacky = null;
        Dictionary<string, UnityEngine.Object> heroes = Tds_Masterdatabase.Instance.Items["Hero"];
        UnityEngine.Object found;
        if (heroes.TryGetValue("Hero_Smacky", out found)) smacky = found as Heroentryobject;
        if (smacky != null && smacky.Heroloads != null)
        {
            Bl_Address.Loadgroup g = null;
            try { g = Bl_Address.Load(smacky.Heroloads.Objectname); }
            catch (Exception e) { RWFLog.Warn("loading " + smacky.Heroloads.Objectname + ": " + e.Message); }
            float waited = 0f;
            while (g != null && !g.Loaded && waited < 180f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        try { RWFModifiers.Build(); }
        catch (Exception e) { RWFLog.Warn("modifiers: " + e); }
#if ROGUEWAVESFOES_SELFTEST
        RWFTest.Boot();
#endif
        Ready = true;
        RWFLog.Info("ready, " + RWFModifiers.Built + " modifiers");
        RWFLog.Info("icons: none");   // Run-SelfTests.ps1 -BootCheck waits for this line
    }

    void Update()
    {
        if (!Ready)
            return;
        try { RWFModifiers.Update(); } catch (Exception e) { RWFLog.Once("modifiers", "modifiers: " + e); }
        try { RWFLooks.Update(); } catch (Exception e) { RWFLog.Once("looks", "looks: " + e); }
#if ROGUEWAVESFOES_SELFTEST
        try { RWFTest.Update(); } catch (Exception e) { RWFLog.Once("test", "self-test: " + e); }
#endif
    }
}

public static class RWFData
{
    public const string Part = "Foes";
    public const string Version = "0.1.0-dev";    // the pack version plus -dev (docs/PACK.md §4)
    public const string Prefix = "RW_Modifier_", Suffix = "_Upgradeobject";
    public const float CursedRange = 3f, CursedDamage = -0.25f;                 // docs/scope/13: defaults
    public const float BerserkDamage = 1f, BerserkSpeed = 0.5f;                 // at no health left
    public const float AgingSpeed = -0.3f, AgingMax = 1f, AgingSeconds = 50f;   // the scope's numbers
    // Dazzling: Blind and Illuminate at Light level 1 (docs/scope/13, Dimitri's answer 11 of 2026-11-05: 15%, 3 s after leaving).
    public const float DazzlingRange = 3f, BlindDamage = -0.15f, IlluminateTaken = 0.15f, DazzleSeconds = 3f;
}

public static class RWFLog
{
    const string Tag = "[RogueWavesFoes] ";
    static readonly List<string> said = new List<string>();
    public static void Info(string message) { Debug.Log(Tag + message); }
    public static void Warn(string message) { Debug.LogWarning(Tag + message); }
    public static void Once(string key, string message)
    {
        if (said.Contains(key)) return;
        said.Add(key);
        Warn(message);
    }
}

public static class RWFModifiers
{
    public const string Cursed = "Cursed", Berserk = "Berserk", Aging = "Aging", Dazzling = "Dazzling";
    public static readonly string[] All = { Cursed, Berserk, Aging, Dazzling };
    public static int Built;
    public static Upgradeproperties Blind, Illuminate;
    static Bl_Address.Loadlist list;
    static readonly Dictionary<string, UnityEngine.Object> carriers = new Dictionary<string, UnityEngine.Object>();

    public static string Id(string name) { return RWFData.Prefix + name + RWFData.Suffix; }

    public static void Build()
    {
        Bl_Address.Loadgroup group = new Bl_Address.Loadgroup();
        group.Items = new List<Bl_Address.Loadlist>();
        group.Loaded = true;
        group.Percent = 100;
        list = new Bl_Address.Loadlist();
        list.Loaded = true;
        list.Percent = 100;
        list.Fromgroup = group;
        group.Items.Add(list);
        List<Bl_Address.Loadlist> labelLists;
        if (!Bl_Address.Groups.TryGetValue("Upgrade", out labelLists))
        {
            labelLists = new List<Bl_Address.Loadlist>();
            Bl_Address.Groups["Upgrade"] = labelLists;
        }
        labelLists.Add(list);
        Bl_Address.Loadedfolders["RW_Foes"] = group;
        Upgradeproperties template = null;
        foreach (Bl_Address.Loadlist l in labelLists)
            foreach (UnityEngine.Object o in l.Items.Values)
                if (template == null && o is Upgradeproperties)
                    template = (Upgradeproperties)o;
        if (template == null) { RWFLog.Warn("no upgrade to copy: no modifiers"); return; }
        Make(template, Id(Cursed), "Cursed", "A curse on this foe weakens the attack of everything nearby");
        Make(template, Id(Berserk), "Berserk", "Stronger and faster the more it is hurt");
        Make(template, Id(Aging), "Aging", "Slow, but its health and damage grow the longer it lives");
        Make(template, Id(Dazzling), "Dazzling", "Blinds and Illuminates everything nearby");
        Built = 4;
        Blind = Make(template, "RW_Effect_Blind_Upgradeobject", "Blinded", "Deals less damage", "Entity_Ai_Effect_Barrel_Boost",
            "Health_Pmulti", 0f, "Speed_Multi", 0f, "Attackspeed_Multi", 0f, "Damage_Multi", RWFData.BlindDamage, "Duration_Base", RWFData.DazzleSeconds);
        Illuminate = Make(template, "RW_Effect_Illuminate_Upgradeobject", "Illuminated", "Takes more damage", "Entity_Ai_Effect_Tower_Merchut", "Effect_Multi", RWFData.IlluminateTaken);
    }

    static Upgradeproperties Make(Upgradeproperties template, string id, string display, string desc, string script = null, params object[] keyValue)
    {
        Upgradeproperties u = UnityEngine.Object.Instantiate(template);
        u.name = id;
        u.Objectname = id;
        u.Id = (id.ToLowerInvariant() + "-rw").Length > 60 ? (id.ToLowerInvariant() + "-rw").Substring(0, 60) : id.ToLowerInvariant() + "-rw";
        u.Notes = "";
        u.Display_Name = display;
        u.Display_Desc = desc;
        u.Priority = -3;
        u.Costs = new List<Link_Resourceset>();
        u.Defaultparam = new List<Upgradeproperties.Param>();
        u.Defaultparams = new Dictionary<string, float>();
        u.Aiscripts = new List<UnityEngine.Object>();
        u.Ent_Visual = null;
        u.Upgrades = new List<Upgradeproperties>();
        if (script != null)
        {
            UnityEngine.Object carrier;
            if (!carriers.TryGetValue(script, out carrier) || carrier == null)
            {
                carrier = new TextAsset("");
                carrier.name = script;
                carrier.hideFlags = HideFlags.DontUnloadUnusedAsset;
                carriers[script] = carrier;
            }
            u.Aiscripts.Add(carrier);
        }
        for (int i = 0; i + 1 < keyValue.Length; i += 2)
        {
            Upgradeproperties.Param p = new Upgradeproperties.Param();
            p.Name = (string)keyValue[i];
            p.Value = Convert.ToSingle(keyValue[i + 1]);
            u.Defaultparam.Add(p);
            u.Defaultparams[p.Name] = p.Value;
        }
        list.Items[u.Objectname] = u;
        list.Index.Add(u);
        FixedString64Bytes uid = new FixedString64Bytes(u.Id);
        list.Uids[uid] = u;
        u.Loadsource = "RW_Foes";
        Bl_Address.Groupsname[new FixedString64Bytes(u.Objectname)] = u;
        Bl_Address.Groupsids[uid] = u;
        return u;
    }

    // ---------------------------------------------------------------- the host's brain

    class Foe
    {
        public Entity_Master Master;
        public bool Cursed, Berserk, Aging, Dazzling;
        public float Born;
        // Each modifier its own handles: a foe with Aging and Berserk kept only Aging's damage when they shared one (FOES-3).
        public Link_Modifier AgingDamage, AgingHealth, BerserkSpeed, BerserkDamage;
    }

    static readonly Dictionary<Entity_Master, Foe> foes = new Dictionary<Entity_Master, Foe>();
    static readonly Dictionary<Entity_Master, Link_Modifier> cursedHeld = new Dictionary<Entity_Master, Link_Modifier>();
    static readonly List<Entity_Master> illuminated = new List<Entity_Master>();
    static readonly Dictionary<Entity_Master, float> leftAt = new Dictionary<Entity_Master, float>();   // when it left every Dazzling aura
    static float nextTick, nextAura;
    static object seenMap;
    public static int Tagged;

    public static bool Has(Entity_Master e, string name)
    {
        return e != null && e.Upgradelistserver != null && e.Upgradelistserver.ContainsKey(Id(name));
    }

    static bool Alive(Entity_Master e) { return e != null && e.Beingserver != null && !e.Beingserver.Dead; }

    static float Stat(Entity_Master e, string name)
    {
        Link_Stats s;
        return e.Statsserver != null && e.Statsserver.TryGetValue(name, out s) ? s.Value_Calc : -1f;
    }

    // Host, in a map.
    public static void Update()
    {
        Game_Entity ge = Game_Entity.Instance;
        if (ge == null || !ge.IsServer || Game_Mapvars.Instance == null)
            return;
        if (!ReferenceEquals(seenMap, Game_Mapvars.Instance))
        {
            seenMap = Game_Mapvars.Instance;
            foes.Clear();
            cursedHeld.Clear();
            illuminated.Clear();
            leftAt.Clear();
        }
        if (Time.time < nextTick)
            return;
        nextTick = Time.time + 0.25f;
        Find(ge);
        foreach (Foe f in new List<Foe>(foes.Values))
        {
            if (!Alive(f.Master)) { foes.Remove(f.Master); continue; }
            if (f.Berserk) BerserkTick(f);
            if (f.Aging) AgingTick(f);
        }
        if (Time.time >= nextAura)
        {
            nextAura = Time.time + 1f;
            CursedTick(ge);
            DazzlingTick(ge);
        }
    }

    static void Find(Game_Entity ge)
    {
        List<Entity_Master> enemies;
        if (!ge.Entityclienttags.TryGetValue("Enemy", out enemies))
            return;
        foreach (Entity_Master e in enemies)
        {
            if (!Alive(e) || foes.ContainsKey(e) || e.Upgradelistserver == null)
                continue;
            bool c = Has(e, Cursed), b = Has(e, Berserk), a = Has(e, Aging), d = Has(e, Dazzling);
            if (!c && !b && !a && !d)
                continue;
            Foe f = new Foe { Master = e, Cursed = c, Berserk = b, Aging = a, Dazzling = d, Born = Time.time };
            if (a)
            {
                Helper_Entitytools.Addmod(e, "Speed", RWFData.AgingSpeed, Helper_Entitytools.Modtypes.Multi, 0f);
                f.AgingDamage = Helper_Entitytools.Addmod(e, "Damage", 0f, Helper_Entitytools.Modtypes.Multi, 0f);
                f.AgingHealth = Helper_Entitytools.Addmod(e, "Health", 0f, Helper_Entitytools.Modtypes.Multi, 0f);
            }
            if (b)
            {
                f.BerserkSpeed = Helper_Entitytools.Addmod(e, "Speed", 0f, Helper_Entitytools.Modtypes.Multi, 0f);
                f.BerserkDamage = Helper_Entitytools.Addmod(e, "Damage", 0f, Helper_Entitytools.Modtypes.Multi, 0f);
            }
            foes[e] = f;
            Tagged++;
        }
    }

    static void BerserkTick(Foe f)
    {
        float hp = Helper_Damagetools.Getpercenthealth(f.Master);
        float missing = Mathf.Clamp01(1f - hp);
        if (f.BerserkDamage != null) f.BerserkDamage.Value_Multi = RWFData.BerserkDamage * missing;
        if (f.BerserkSpeed != null) f.BerserkSpeed.Value_Multi = RWFData.BerserkSpeed * missing;
    }

    static void AgingTick(Foe f)
    {
        float share = Mathf.Clamp01((Time.time - f.Born) / RWFData.AgingSeconds) * RWFData.AgingMax;
        if (f.AgingDamage != null) f.AgingDamage.Value_Multi = share;
        if (f.AgingHealth != null) f.AgingHealth.Value_Multi = share;
    }

    static List<Entity_Master> Targets(Game_Entity ge)
    {
        List<Entity_Master> all = new List<Entity_Master>();
        foreach (string tag in new[] { "Tower", "Mob", "Hero" })
        {
            List<Entity_Master> some;
            if (ge.Entitiesserver.TryGetValue(tag, out some) && some != null)
                foreach (Entity_Master e in some)
                    // Never the Bouthouse: it hands what it holds on to Boutman, where nothing takes it off (pitfall #85, FOES-2).
                    if (Alive(e) && !all.Contains(e) && e.Entitydata.Value.Team != 0 && !ModKitUtil.HasTag(e, "Bouthouse")) all.Add(e);
        }
        return all;
    }

    static bool Near(Entity_Master t, bool cursed, float range)
    {
        foreach (Foe f in foes.Values)
            if ((cursed ? f.Cursed : f.Dazzling) && Alive(f.Master) && Flat(t.Entityposition.Value, f.Master.Entityposition.Value) <= range)
                return true;
        return false;
    }

    static float Flat(Vector3 a, Vector3 b) { return new Vector2(a.x - b.x, a.z - b.z).magnitude; }

    // A held Damage modifier while in range (a unit with no Damage stat, a wall, is left alone).
    static void CursedTick(Game_Entity ge)
    {
        foreach (Entity_Master t in Targets(ge))
        {
            bool inRange = Near(t, true, RWFData.CursedRange) && Stat(t, "Damage") > 0f;
            Link_Modifier m;
            bool has = cursedHeld.TryGetValue(t, out m);
            if (inRange && !has) cursedHeld[t] = Helper_Entitytools.Addmod(t, "Damage", RWFData.CursedDamage, Helper_Entitytools.Modtypes.Multi, 0f);
            if (!inRange && has) { if (m != null) m.Remove(); cursedHeld.Remove(t); }
        }
        foreach (Entity_Master t in new List<Entity_Master>(cursedHeld.Keys))
            if (!Alive(t)) cursedHeld.Remove(t);
    }

    static void DazzlingTick(Game_Entity ge)
    {
        if (Blind == null || Illuminate == null)
            return;
        foreach (Entity_Master t in Targets(ge))
        {
            bool inRange = Near(t, false, RWFData.DazzlingRange);
            if (inRange)
            {
                // The stock timed boost removes itself; while in range it is simply laid on again.
                if (Stat(t, "Damage") > 0f && t.Upgradelistserver != null && !t.Upgradelistserver.ContainsKey(Blind.Objectname))
                    Helper_Entitytools.Upgrade(t, Blind, Helper_Entitytools.Upgradecheck.Forced, null);
                if (t.Upgradelistserver != null && !t.Upgradelistserver.ContainsKey(Illuminate.Objectname))
                {
                    Helper_Entitytools.Upgrade(t, Illuminate, Helper_Entitytools.Upgradecheck.Forced, null);
                    illuminated.Add(t);
                }
            }
            else if (illuminated.Contains(t))
            {
                // Illuminate lingers 3 s after the thing leaves the aura, as the timed Blind does (docs/scope/13).
                float left;
                if (!leftAt.TryGetValue(t, out left))
                    leftAt[t] = Time.time;
                else if (Time.time - left >= RWFData.DazzleSeconds)
                {
                    illuminated.Remove(t);
                    leftAt.Remove(t);
                    Helper_Entitytools.Removeupgrade(t, Illuminate);
                }
                continue;
            }
            leftAt.Remove(t);
        }
        illuminated.RemoveAll(t => t == null);
        foreach (Entity_Master t in new List<Entity_Master>(leftAt.Keys))
            if (t == null || !illuminated.Contains(t)) leftAt.Remove(t);
    }
}

// Every machine: each marked foe wears its modifier's effect, a pulsing ring of soft motes the game's own sprite shader draws,
// built in code so it needs no stock effect the map may not load (pitfall #114). Colours: docs/STYLE.md "Enemies".
public static class RWFLooks
{
    static readonly Dictionary<Entity_Master, GameObject> worn = new Dictionary<Entity_Master, GameObject>();
    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    static float nextLook;
    public static int Worn;

    static Color Colour(string name)
    {
        switch (name)
        {
            case RWFModifiers.Cursed: return new Color(0.45f, 0.33f, 0.85f, 0.85f);    // indigo
            case RWFModifiers.Berserk: return new Color(1f, 0.32f, 0.62f, 0.85f);      // hot pink
            case RWFModifiers.Aging: return new Color(0.55f, 0.82f, 0.86f, 0.85f);     // barnacle blue
            default: return new Color(1f, 0.92f, 0.35f, 0.9f);                          // bright yellow
        }
    }

    static Sprite Ring(string name)
    {
        Sprite s;
        if (sprites.TryGetValue(name, out s) && s != null)
            return s;
        const int size = 32;
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Point;
        Color32[] px = new Color32[size * size];
        Color c = Colour(name);
        bool aura = name == RWFModifiers.Cursed || name == RWFModifiers.Dazzling;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - 15.5f, dy = y - 15.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
                bool on = aura ? r > 13f && r < 15.5f && ((Mathf.FloorToInt(Mathf.Atan2(dy, dx) * 8f / Mathf.PI) & 1) == 0)
                    : r > 12f && r < 14f && ((Mathf.FloorToInt(Mathf.Atan2(dy, dx) * 6f / Mathf.PI) & 1) == 0);
                px[y * size + x] = on ? (Color32)c : new Color32(0, 0, 0, 0);
            }
        t.SetPixels32(px);
        t.Apply(false);
        s = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 20f, 0, SpriteMeshType.FullRect);
        s.name = "RW_Modifier_" + name;
        sprites[name] = s;
        return s;
    }

    public static string Holds(Entity_Master e)
    {
        if (e == null || e.Upgradelist == null)
            return "";
        foreach (FixedString64Bytes u in e.Upgradelist)
        {
            string n = u.ToString();
            if (n.StartsWith(RWFData.Prefix)) return n.Substring(RWFData.Prefix.Length, n.Length - RWFData.Prefix.Length - RWFData.Suffix.Length);
        }
        return "";
    }

    public static void Update()
    {
        if (Time.unscaledTime >= nextLook)
        {
            nextLook = Time.unscaledTime + 0.25f;
            List<Entity_Master> enemies;
            if (Game_Entity.Instance != null && Game_Entity.Instance.Entityclienttags != null && Game_Entity.Instance.Entityclienttags.TryGetValue("Enemy", out enemies))
                foreach (Entity_Master e in enemies)
                    if (e != null && !worn.ContainsKey(e))
                    {
                        string name = Holds(e);
                        if (name != "") Wear(e, name);
                    }
        }
        if (worn.Count == 0)
            return;
        List<Entity_Master> gone = null;
        float t = Time.time;
        foreach (KeyValuePair<Entity_Master, GameObject> kv in worn)
        {
            if (kv.Key == null || kv.Value == null)
            {
                if (gone == null) gone = new List<Entity_Master>();
                gone.Add(kv.Key);
                continue;
            }
            float pulse = 1f + 0.12f * Mathf.Sin(t * 4f);
            kv.Value.transform.localScale = new Vector3(pulse, pulse, pulse);
            kv.Value.transform.Rotate(0f, 0f, 60f * Time.deltaTime, Space.Self);
        }
        if (gone != null)
            foreach (Entity_Master e in gone) { if (worn.ContainsKey(e) && worn[e] != null) UnityEngine.Object.Destroy(worn[e]); worn.Remove(e); }
    }

    static void Wear(Entity_Master e, string name)
    {
        SpriteRenderer body = e.GetComponentInChildren<SpriteRenderer>(true);
        if (body == null)
            return;
        GameObject g = new GameObject("RW_Look_" + name);
        g.transform.SetParent(body.transform, false);
        g.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        SpriteRenderer r = g.AddComponent<SpriteRenderer>();
        r.sprite = Ring(name);
        r.sharedMaterial = body.sharedMaterial;
        r.sortingLayerID = body.sortingLayerID;
        r.sortingOrder = body.sortingOrder + 1;
        r.color = Colour(name);
        worn[e] = g;
        Worn++;
    }
}
