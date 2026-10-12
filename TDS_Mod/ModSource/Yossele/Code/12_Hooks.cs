// Damage modifiers that ride on the game's own hit event (Argo's pattern, verified in game: Bulkhead, Crushing Depth).
// Helper_Entitytools.Interacthurt hands each hit to the target's Oninteract chain; putting Yossele's handler first lets
// it scale the hit before anyone applies it, whoever dealt it:
//   Stonehenge armour   10% less damage per Growth stage.
//   Vine Blockade       a barrier soaks hits until it is spent (no healing past it; a new one each Tide).
//   Connected Roots     65% of a hit on one of your buildings within a healthy Sapling's Range goes to the Sapling.
// Helper_Entitytools.Updateinteract rebuilds the chain whenever an upgrade or effect lands on the entity, so the handler
// is checked and moved back to the front ten times a second while it is needed.
public static class YosseleHooks
{
    static readonly Dictionary<Entity_Master, float> armor = new Dictionary<Entity_Master, float>();
    static readonly Dictionary<Entity_Master, float> shield = new Dictionary<Entity_Master, float>();
    static readonly Dictionary<Entity_Master, YosselePachiraBrain> guarded = new Dictionary<Entity_Master, YosselePachiraBrain>();
    static readonly Entity_Master.Oninteracts handler = OnHurt;
    static float nextCheck;
    public static int Softened, Soaked, Shared, Skipped;
    public static float SharedDamage, SoakedDamage;

    public static void Reset()
    {
        armor.Clear();
        shield.Clear();
        guarded.Clear();
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
    }

    public static void Update(List<YosseleBuilding> buildings)
    {
        if (Time.time < nextCheck)
            return;
        nextCheck = Time.time + 0.1f;
        // Connected Roots: every building of the Sapling's team within its Range (not another Sapling: two would pass
        // one hit back and forth).
        guarded.Clear();
        foreach (YosseleBuilding b in buildings)
        {
            YosselePachiraBrain p = b as YosselePachiraBrain;
            if (p == null || !p.Started || p.Master == null || !YosseleUtil.Alive(p.Master) || !p.Has("Connected"))
                continue;
            float range = p.Stat("Range", 2f);
            foreach (Entity_Master t in YosseleUtil.Area(p.Pos, range + 0.3f, YosseleUtil.Tagged("Tower")))
                if (t != null && t != p.Master && YosseleUtil.Alive(t) && YosseleUtil.Team(t) == p.Team && t.GetComponent<YosselePachiraBrain>() == null
                    && YosseleUtil.Flat(YosseleUtil.Pos(t), p.Pos) <= range + 0.3f && !guarded.ContainsKey(t))
                    guarded[t] = p;
        }
        Keep(armor);
        Keep(shield);
        foreach (Entity_Master t in guarded.Keys)
            First(t);
    }

    static void Keep(Dictionary<Entity_Master, float> table)
    {
        List<Entity_Master> drop = null;
        foreach (Entity_Master e in table.Keys)
        {
            if (e == null || !YosseleUtil.Alive(e))
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
    static void First(Entity_Master e)
    {
        Entity_Master.Oninteracts chain = e.Oninteract;
        if (chain != null)
        {
            Delegate[] list = chain.GetInvocationList();
            if (list.Length > 0 && list[0].Equals(handler))
                return;
        }
        Delegate rest = Delegate.Remove(chain, handler);
        e.Oninteract = (Entity_Master.Oninteracts)Delegate.Combine(handler, rest);
    }

    static void OnHurt(Link_Interact hit)
    {
        try
        {
            if (hit == null || hit.Type != "Hurt" || hit.To == null || hit.Data == null)
                return;
            float multi, damage;
            if (!hit.Data.TryGetValue("Damagemulti", out multi) || !hit.Data.TryGetValue("Damage", out damage))
            {
                if (armor.ContainsKey(hit.To) || shield.ContainsKey(hit.To) || guarded.ContainsKey(hit.To)) Skipped++;
                return;
            }
            float total = damage * multi;
            if (total <= 0f)
                return;
            float scale = 1f;
            YosselePachiraBrain guard;
            if (guarded.TryGetValue(hit.To, out guard) && guard != null && guard.Master != null && YosseleUtil.Alive(guard.Master)
                && YosseleUtil.Health(guard.Master) > guard.Param("Connected_Above", 0.5f) * YosseleUtil.MaxHealth(guard.Master))
            {
                float share = Mathf.Clamp01(guard.Param("Connected_Share", 0.65f));
                scale *= 1f - share;
                Shared++;
                SharedDamage += total * share;
                ModKitCombat.Hurt(hit.From ?? guard.Master, guard.Master, total * share, 1f, hit.From, null);
            }
            float a;
            if (armor.TryGetValue(hit.To, out a) && a > 0f)
            {
                scale *= 1f - a;
                Softened++;
            }
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
                    YosseleUtil.RemoveDebuff(hit.To, hit.To, YosseleData.VfxBlockade);
                    YosseleUtil.Vis(hit.To, "Yossele_Blockadebreak", YosseleUtil.Pos(hit.To), 0f);
                }
                else
                    shield[hit.To] = s;
            }
            if (Mathf.Approximately(scale, 1f))
                return;
            // A copy: projectiles and some AIs reuse one dictionary for several hits.
            hit.Data = new Dictionary<string, float>(hit.Data);
            hit.Data["Damagemulti"] = multi * Mathf.Max(0f, scale);
        }
        catch (Exception e)
        {
            YosseleLog.Warn("hit hook: " + e.Message);
        }
    }
}
