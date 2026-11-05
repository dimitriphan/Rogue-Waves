// Damage modifiers that ride on the game's own hit event (Argo's pattern, verified in game: Bulkhead, Crushing Depth).
// Helper_Entitytools.Interacthurt hands each hit to the target's Oninteract chain; putting Yossele's handler first lets
// it scale the hit before anyone applies it, whoever dealt it:
//   Stonehenge armour   10% less damage per Growth stage, 20% more with Monument.
//   Fortifying Roots    30% less damage for its team's buildings within the Sapling's Range (the strongest aura
//                       counts; it adds to a building's own armour, every reduction capped at 90%).
//   Vine Blockade       a barrier soaks hits until it is spent (no healing past it; a new one each Tide).
//   Connected Roots     65% of a hit on a Tower-tagged building of the Sapling's owner within a healthy Sapling's
//                       Range goes to the Sapling.
//   Easy Target         a marked foe takes 15% more from every hit (the handler sits on the foe).
//   Sap Bucket III      a sapped foe takes 10% more from the sapper's buildings while the sap lasts (17_Directions.cs).
//   Monument            the attacker's Interact runs before the target's Oninteract (Helper_Entitytools.Interacthurt),
//                       so a foe's debuff (the Bulwark's stun: Override modifiers and the Disabled look) is already on
//                       the stones when the hit reaches this handler, which strips it; a sweep ten times a second
//                       catches debuffs that come without a hit.
// Helper_Entitytools.Updateinteract rebuilds the chain whenever an upgrade or effect lands on the entity, so the handler
// is checked and moved back to the front ten times a second while it is needed.
public static class YosseleHooks
{
    static readonly Dictionary<Entity_Master, float> armor = new Dictionary<Entity_Master, float>();
    static readonly Dictionary<Entity_Master, float> shield = new Dictionary<Entity_Master, float>();
    static readonly Dictionary<Entity_Master, YosselePachiraBrain> guarded = new Dictionary<Entity_Master, YosselePachiraBrain>();
    static readonly Dictionary<Entity_Master, float> aura = new Dictionary<Entity_Master, float>();
    static readonly Dictionary<Entity_Master, float> markBonus = new Dictionary<Entity_Master, float>();
    static readonly Dictionary<Entity_Master, float> markUntil = new Dictionary<Entity_Master, float>();
    class SapMark { public float Bonus, Until; public long Owner; }
    static readonly Dictionary<Entity_Master, SapMark> sapped = new Dictionary<Entity_Master, SapMark>();
    static readonly List<Entity_Master> monuments = new List<Entity_Master>();
    static readonly Entity_Master.Oninteracts handler = OnHurt;
    static float nextCheck;
    public static int Softened, Soaked, Shared, Skipped, Marked, Cleansed;
    public static float SharedDamage, SoakedDamage;

    public static void Reset()
    {
        armor.Clear();
        shield.Clear();
        guarded.Clear();
        aura.Clear();
        markBonus.Clear();
        markUntil.Clear();
        sapped.Clear();
        monuments.Clear();
        watched.Clear();
    }

    public static void Armor(Entity_Master e, float amount)
    {
        if (e == null)
            return;
        if (amount <= 0f) armor.Remove(e);
        else
        {
            armor[e] = Mathf.Clamp(amount, 0f, 0.9f);
            First(e);
        }
    }

    public static void Shield(Entity_Master e, float amount)
    {
        if (e == null || amount <= 0f)
            return;
        shield[e] = amount;
        First(e);
    }

    public static float ShieldOf(Entity_Master e)
    {
        float s;
        return e != null && shield.TryGetValue(e, out s) ? s : 0f;
    }

    public static float ArmorOf(Entity_Master e)
    {
        float a;
        return e != null && armor.TryGetValue(e, out a) ? a : 0f;
    }

    public static float AuraOf(Entity_Master e)
    {
        float a;
        return e != null && aura.TryGetValue(e, out a) ? a : 0f;
    }

    // A building's whole reduction: its own armour and the strongest aura on it, added, capped.
    public static float Reduction(Entity_Master e) { return Mathf.Clamp(ArmorOf(e) + AuraOf(e), 0f, YosseleData.ArmorCap); }

    // Easy Target: the foe takes 'bonus' more from every hit for 'seconds' (renewed while it stays on the vines).
    public static void Mark(Entity_Master by, Entity_Master foe, float bonus, float seconds)
    {
        if (foe == null || !ModKitUtil.Alive(foe) || bonus <= 0f)
            return;
        float had;
        bool fresh = !markUntil.TryGetValue(foe, out had) || had < Time.time;
        markBonus[foe] = markBonus.ContainsKey(foe) && !fresh ? Mathf.Max(markBonus[foe], bonus) : bonus;
        markUntil[foe] = Mathf.Max(had, Time.time + seconds);
        First(foe);
        if (fresh)
        {
            Marked++;
            ModKitUtil.Debuff(by, foe, YosseleData.VfxMarked, seconds);
        }
        else
            ModKitUtil.Debuff(by, foe, YosseleData.VfxMarked, markUntil[foe] - Time.time);
    }

    // Sap Bucket III: the foe takes 'bonus' more from 'owner''s buildings for 'seconds' (renewed by each laser hit; the
    // last sapper's).
    public static void Sap(Entity_Master foe, long owner, float bonus, float seconds)
    {
        if (foe == null || !ModKitUtil.Alive(foe) || bonus <= 0f)
            return;
        sapped[foe] = new SapMark { Bonus = bonus, Until = Time.time + seconds, Owner = owner };
        First(foe);
    }

    // The Sap Bucket bonus a hit from 'from' gets on 'foe' (0: none).
    public static float SapOf(Entity_Master foe, Entity_Master from)
    {
        SapMark s;
        if (foe == null || from == null || !sapped.TryGetValue(foe, out s) || s.Until <= Time.time)
            return 0f;
        YosseleBuilding b = from.GetComponent<YosseleBuilding>();
        return b != null && b.Owner == s.Owner ? s.Bonus : 0f;
    }

    public static float MarkOf(Entity_Master foe)
    {
        float until, bonus;
        return foe != null && markUntil.TryGetValue(foe, out until) && until > Time.time && markBonus.TryGetValue(foe, out bonus) ? bonus : 0f;
    }

    public static void Monument(Entity_Master e, bool on)
    {
        if (e == null)
            return;
        if (on && !monuments.Contains(e)) { monuments.Add(e); Adopt(e); }
        else if (!on) monuments.Remove(e);
        if (on) First(e);
    }

    // A Stonehenge's own modifiers, never cleansed once it is a Monument (YOS-2, Dimitri's answer 17 of 2026-11-05, B9).
    // Each Stonehenge is watched from the moment it stands (Watch): a modifier is its own when it first shows within a
    // half-second of the building standing or of an upgrade landing on it that isn't a foe's (a stock Cardinal's trade-off,
    // Paper Tower's negative Multi, a Direction). A foe's debuff (the Bulwark's stun on a hit, a cursed foe's aura, the
    // pack's Blind) comes with no upgrade of the building's own, so it is stripped. An Override modifier is never
    // adopted: the game uses them only for stuns (Bulwark, Aurabeast, Computer, Snare). Upgrades named Enemy_ or
    // RW_Modifier_, and the foe part's Blind and Illuminate, are foes' (kit docs/13 §4).
    // One watched Stonehenge: its own modifiers, every one it has shown, its own upgrades' count, and when the window shuts.
    sealed class Watched
    {
        public readonly List<Link_Modifier> Own = new List<Link_Modifier>(), Seen = new List<Link_Modifier>();
        public int Upgrades;
        public float Until;
    }
    static readonly Dictionary<Entity_Master, Watched> watched = new Dictionary<Entity_Master, Watched>();
    public const float AdoptSeconds = 0.5f;

    static bool FoeUpgrade(string key)
    {
        return key.StartsWith("Enemy_", StringComparison.Ordinal) || key.StartsWith("RW_Modifier_", StringComparison.Ordinal)
            || key == "RW_Effect_Blind_Upgradeobject" || key == "RW_Effect_Illuminate_Upgradeobject";
    }

    static bool Stun(Link_Modifier m) { return m.Value_Override_On > 0 || m.Value_Override != 0f; }

    static int OwnUpgrades(Entity_Master e)
    {
        int mine = 0;
        if (e.Upgradelistserver != null)
            foreach (string key in e.Upgradelistserver.Keys)
                if (!FoeUpgrade(key)) mine++;
        return mine;
    }

    // From Started2: what lands in the building's first half-second is its own.
    public static void Watch(Entity_Master e)
    {
        if (e == null || watched.ContainsKey(e))
            return;
        watched[e] = new Watched { Until = Time.time + AdoptSeconds, Upgrades = OwnUpgrades(e) };
        Adopt(e);
    }

    // Takes in the modifiers that are new since the last look, if the window is open; every look notes what it saw.
    static void Adopt(Entity_Master e)
    {
        Watched w;
        if (e == null || e.Modifiers == null || !watched.TryGetValue(e, out w))
            return;
        int count = OwnUpgrades(e);
        if (w.Upgrades != count)
            w.Until = Time.time + AdoptSeconds;
        w.Upgrades = count;
        bool open = Time.time <= w.Until;
        w.Own.RemoveAll(m => m == null || m.Dead);
        w.Seen.RemoveAll(m => m == null || m.Dead);
        foreach (KeyValuePair<string, List<Link_Modifier>> kv in e.Modifiers)
            if (kv.Value != null)
                foreach (Link_Modifier m in kv.Value)
                    if (m != null && !m.Dead && !w.Seen.Contains(m))
                    {
                        w.Seen.Add(m);
                        if (open && !Stun(m)) w.Own.Add(m);
                    }
    }

    public static bool IsOwn(Entity_Master e, Link_Modifier m) { Watched w; return e != null && watched.TryGetValue(e, out w) && w.Own.Contains(m); }

    public static bool IsMonument(Entity_Master e) { return e != null && monuments.Contains(e); }

    // Strips every foe's debuff from a Monument: Override modifiers (the Bulwark's stun freezes Damage, Speed and
    // Attackspeed with them) and any negative Multi or Pmulti, then the Disabled look; never one of its own (Adopt).
    public static int Cleanse(Entity_Master e)
    {
        if (e == null || e.Modifiers == null)
            return 0;
        Adopt(e);
        Watched w;
        List<Link_Modifier> mine = watched.TryGetValue(e, out w) ? w.Own : null;
        int n = 0;
        foreach (KeyValuePair<string, List<Link_Modifier>> kv in new List<KeyValuePair<string, List<Link_Modifier>>>(e.Modifiers))
        {
            if (kv.Value == null)
                continue;
            foreach (Link_Modifier m in new List<Link_Modifier>(kv.Value))
                if (m != null && !m.Dead && (mine == null || !mine.Contains(m)) && (Stun(m) || m.Value_Multi < 0f || m.Value_Pmulti < 0f))
                {
                    ModKitUtil.Unmod(m);
                    n++;
                }
        }
        if (n > 0)
        {
            Cleansed += n;
            ModKitUtil.RemoveDebuff(e, e, YosseleData.VfxDisabled);
            ModKitUtil.Vis(e, "Yossele_Cleanse", ModKitUtil.Pos(e), n);
        }
        return n;
    }

    public static YosselePachiraBrain GuardOf(Entity_Master e)
    {
        YosselePachiraBrain p;
        return e != null && guarded.TryGetValue(e, out p) ? p : null;
    }

    public static void Forget(Entity_Master e)
    {
        armor.Remove(e);
        shield.Remove(e);
        guarded.Remove(e);
        aura.Remove(e);
        monuments.Remove(e);
        watched.Remove(e);
    }

    public static void ForgetFoe(Entity_Master foe)
    {
        markBonus.Remove(foe);
        markUntil.Remove(foe);
        sapped.Remove(foe);
    }

    public static void Update(List<YosseleBuilding> buildings)
    {
        if (Time.time < nextCheck)
            return;
        nextCheck = Time.time + 0.1f;
        // Connected Roots: every building of the Sapling's owner within its Range (the sheet's "your buildings"; not
        // another Sapling: two would pass one hit back and forth).
        guarded.Clear();
        foreach (YosseleBuilding b in buildings)
        {
            YosselePachiraBrain p = b as YosselePachiraBrain;
            if (p == null || !p.Started || p.Master == null || !ModKitUtil.Alive(p.Master) || !p.Has("Connected"))
                continue;
            float range = p.Stat("Range", 2f);
            foreach (Entity_Master t in ModKitUtil.Area(p.Pos, range + 0.3f, ModKitUtil.Tagged("Tower")))
                if (t != null && t != p.Master && ModKitUtil.Alive(t) && ModKitUtil.Owner(t) == p.Owner && t.GetComponent<YosselePachiraBrain>() == null
                    && ModKitUtil.Flat(ModKitUtil.Pos(t), p.Pos) <= range + 0.3f && !guarded.ContainsKey(t))
                    guarded[t] = p;
        }
        // Fortifying Roots: the strongest aura on each building of the Sapling's team within its Range, itself included.
        aura.Clear();
        foreach (YosseleBuilding b in buildings)
        {
            YosselePachiraBrain p = b as YosselePachiraBrain;
            if (p == null || !p.Started || p.Master == null || !ModKitUtil.Alive(p.Master) || !p.Has("Fortifying"))
                continue;
            float range = p.Stat("Range", 2.5f), value = p.Param("Fortify_Armor", 0.3f), had;
            foreach (Entity_Master t in ModKitUtil.Area(p.Pos, range + 0.3f, ModKitUtil.Tagged("Tower")))
                if (t != null && ModKitUtil.Alive(t) && ModKitUtil.Team(t) == p.Team && ModKitUtil.Flat(ModKitUtil.Pos(t), p.Pos) <= range + 0.3f)
                    aura[t] = aura.TryGetValue(t, out had) ? Mathf.Max(had, value) : value;
        }
        Keep(armor);
        Keep(shield);
        foreach (Entity_Master t in guarded.Keys)
            First(t);
        foreach (Entity_Master t in aura.Keys)
            First(t);
        monuments.RemoveAll(m => m == null || !ModKitUtil.Alive(m));
        foreach (Entity_Master m in new List<Entity_Master>(watched.Keys))
            if (m == null || !ModKitUtil.Alive(m)) watched.Remove(m);
            else if (!monuments.Contains(m)) Adopt(m);
        foreach (Entity_Master m in monuments)
        {
            First(m);
            Cleanse(m);
        }
        List<Entity_Master> gone = null;
        foreach (KeyValuePair<Entity_Master, float> kv in markUntil)
            if (kv.Key == null || !ModKitUtil.Alive(kv.Key) || kv.Value < Time.time - 5f)
            {
                if (gone == null) gone = new List<Entity_Master>();
                gone.Add(kv.Key);
            }
            else if (kv.Value > Time.time)
                First(kv.Key);
        if (gone != null)
            foreach (Entity_Master e in gone)
            {
                markBonus.Remove(e);
                markUntil.Remove(e);
            }
        gone = null;
        foreach (KeyValuePair<Entity_Master, SapMark> kv in sapped)
            if (kv.Key == null || !ModKitUtil.Alive(kv.Key) || kv.Value.Until < Time.time)
            {
                if (gone == null) gone = new List<Entity_Master>();
                gone.Add(kv.Key);
            }
            else
                First(kv.Key);
        if (gone != null)
            foreach (Entity_Master e in gone)
                sapped.Remove(e);
    }

    static void Keep(Dictionary<Entity_Master, float> table)
    {
        List<Entity_Master> drop = null;
        foreach (Entity_Master e in table.Keys)
        {
            if (e == null || !ModKitUtil.Alive(e))
            {
                if (drop == null) drop = new List<Entity_Master>();
                drop.Add(e);
            }
            else
                First(e);
        }
        if (drop != null)
            foreach (Entity_Master e in drop)
                table.Remove(e);
    }

    // Moves Yossele's handler to the front of the entity's Oninteract chain (adding it if Updateinteract dropped it).
    static void First(Entity_Master e) { ModKitUtil.Front(e, handler); }

    static void OnHurt(Link_Interact hit)
    {
        try
        {
            if (hit == null || hit.Type != "Hurt" || hit.To == null || hit.Data == null)
                return;
            if (hit.From != null && monuments.Contains(hit.To) && ModKitUtil.IsFoe(hit.From, ModKitUtil.Team(hit.To)))
                Cleanse(hit.To);
            float multi, damage;
            if (!hit.Data.TryGetValue("Damagemulti", out multi) || !hit.Data.TryGetValue("Damage", out damage))
            {
                if (armor.ContainsKey(hit.To) || shield.ContainsKey(hit.To) || guarded.ContainsKey(hit.To) || aura.ContainsKey(hit.To)) Skipped++;
                return;
            }
            float total = damage * multi;
            if (total <= 0f)
                return;
            float scale = 1f;
            YosselePachiraBrain guard;
            if (guarded.TryGetValue(hit.To, out guard) && guard != null && guard.Master != null && ModKitUtil.Alive(guard.Master)
                && ModKitUtil.Health(guard.Master) > guard.Param("Connected_Above", 0.5f) * ModKitUtil.MaxHealth(guard.Master))
            {
                float share = Mathf.Clamp01(guard.Param("Connected_Share", 0.65f));
                scale *= 1f - share;
                Shared++;
                SharedDamage += total * share;
                // A shooter destroyed while its shot flew is a destroyed reference, which ?? keeps and Hurt drops (MAX P1-2).
                Entity_Master by = hit.From != null ? hit.From : null;
                ModKitCombat.Hurt(by ?? guard.Master, guard.Master, total * share, 1f, by, null);
            }
            float a = Reduction(hit.To);
            if (a > 0f)
            {
                scale *= 1f - a;
                Softened++;
            }
            float mark = MarkOf(hit.To);
            if (mark > 0f)
                scale *= 1f + mark;
            float sap = SapOf(hit.To, hit.From);
            if (sap > 0f)
                scale *= 1f + sap;
            float s;
            if (shield.TryGetValue(hit.To, out s) && s > 0f)
            {
                float left = total * scale;
                float soak = Mathf.Min(s, left);
                s -= soak;
                scale = total > 0f ? (left - soak) / total : 0f;
                Soaked++;
                SoakedDamage += soak;
                if (s <= 0.01f)
                {
                    shield.Remove(hit.To);
                    ModKitUtil.RemoveDebuff(hit.To, hit.To, YosseleData.VfxBlockade);
                    ModKitUtil.Vis(hit.To, "Yossele_Blockadebreak", ModKitUtil.Pos(hit.To), 0f);
                }
                else
                    shield[hit.To] = s;
            }
            if (Mathf.Approximately(scale, 1f))
                return;
            ModKitCombat.SetMulti(hit, multi * Mathf.Max(0f, scale));
        }
        catch (Exception e)
        {
            YosseleLog.Warn("hit hook: " + e.Message);
        }
    }
}
