// Damage modifiers that ride on the game's own hit event. Helper_Entitytools.Interacthurt hands each hit to the
// target's Oninteract chain; Entity_Ai_Mob_General (or the tower's AI) applies it there. Putting Argo's handler first
// in that chain lets it scale the hit before anyone applies it, whoever dealt it:
//   Pinged   (SONAR)   enemies take 20% more damage from every source, teammates' towers included.
//   Bulkhead (Nauts)   a tuned tower takes 25% less.
//   Soak     (2.0)     a foe in Crushing Depth water takes 15% more from every source.
//   Chum     (Chum Bucket, a Direction) a Slowed or Pinned foe takes 8 / 15 / 25% more from the holder's buildings.
// Helper_Entitytools.Updateinteract rebuilds the chain whenever an upgrade or effect lands on the entity, so the
// handler is checked and moved back to the front a few times a second while it is needed.
public static class ArgoHooks
{
    static readonly Dictionary<Entity_Master, float> pinged = new Dictionary<Entity_Master, float>();
    // Bulkhead: the tower's cut and the Naut tuning it (one Naut per tower, ArgoTowerBrain.CrewOrder).
    static readonly Dictionary<Entity_Master, ArgoNautBrain> armored = new Dictionary<Entity_Master, ArgoNautBrain>();
    static readonly Dictionary<Entity_Master, float> armorAmount = new Dictionary<Entity_Master, float>();
    static readonly Dictionary<Entity_Master, Vector2> soaked = new Dictionary<Entity_Master, Vector2>();   // (amount, until)
    static readonly Entity_Master.Oninteracts handler = OnHurt;
    static float nextCheck;
    public static int Boosted, Softened, Crushed, Chummed;

    public static void Reset()
    {
        soaked.Clear();
        pinged.Clear();
        armored.Clear();
        armorAmount.Clear();
    }

    public static bool IsPinged(Entity_Master e)
    {
        float until;
        return e != null && pinged.TryGetValue(e, out until) && Time.time < until;
    }

    public static float ArmorOf(Entity_Master e)
    {
        float a;
        return e != null && armorAmount.TryGetValue(e, out a) ? a : 0f;
    }

    public static void Ping(Entity_Master e, float seconds)
    {
        if (e == null)
            return;
        float until;
        pinged[e] = Mathf.Max(pinged.TryGetValue(e, out until) ? until : 0f, Time.time + seconds);
        First(e);
    }

    public static float SoakOf(Entity_Master e)
    {
        Vector2 s;
        return e != null && soaked.TryGetValue(e, out s) && Time.time < s.y ? s.x : 0f;
    }

    public static void Soak(Entity_Master e, float amount, float seconds)
    {
        if (e == null || amount <= 0f)
            return;
        soaked[e] = new Vector2(Mathf.Max(amount, SoakOf(e)), Time.time + seconds);
        First(e);
    }

    public static void Armor(Entity_Master tower, ArgoNautBrain by, float amount)
    {
        armored[tower] = by;
        armorAmount[tower] = Mathf.Clamp(amount, 0f, 0.9f);
        First(tower);
    }

    // Only the Naut that tunes the tower takes its cut off.
    public static void Unarmor(Entity_Master tower, ArgoNautBrain by)
    {
        ArgoNautBrain tuner;
        if (tower == null || !armored.TryGetValue(tower, out tuner) || tuner != by)
            return;
        armored.Remove(tower);
        armorAmount.Remove(tower);
    }

    public static void Update()
    {
        if (Time.time < nextCheck)
            return;
        nextCheck = Time.time + 0.1f;
        List<Entity_Master> drop = null;
        foreach (KeyValuePair<Entity_Master, float> kv in pinged)
        {
            if (kv.Key == null || Time.time >= kv.Value || !ModKitUtil.Alive(kv.Key))
            {
                if (drop == null) drop = new List<Entity_Master>();
                drop.Add(kv.Key);
            }
            else
                First(kv.Key);
        }
        if (drop != null)
            foreach (Entity_Master e in drop)
                pinged.Remove(e);
        drop = null;
        foreach (KeyValuePair<Entity_Master, Vector2> kv in soaked)
        {
            if (kv.Key == null || Time.time >= kv.Value.y || !ModKitUtil.Alive(kv.Key))
            {
                if (drop == null) drop = new List<Entity_Master>();
                drop.Add(kv.Key);
            }
            else
                First(kv.Key);
        }
        if (drop != null)
            foreach (Entity_Master e in drop)
                soaked.Remove(e);
        drop = null;
        if (ArgoServer.AnyChum())
            foreach (Entity_Master foe in ArgoStatus.Debuffees())
                First(foe);
        foreach (Entity_Master t in armored.Keys)
        {
            if (t == null || !ModKitUtil.Alive(t))
            {
                if (drop == null) drop = new List<Entity_Master>();
                drop.Add(t);
            }
            else
                First(t);
        }
        if (drop != null)
            foreach (Entity_Master t in drop)
            {
                armored.Remove(t);
                armorAmount.Remove(t);
            }
    }

    // Chum Bucket: a foe that was just Slowed or Pinned gets the handler at once, and Update keeps it there while held.
    public static void WatchDebuffed(Entity_Master foe)
    {
        if (foe != null && ArgoServer.AnyChum())
            First(foe);
    }

    // Moves Argo's handler to the front of the entity's Oninteract chain (adding it if Updateinteract dropped it).
    static void First(Entity_Master e) { ModKitUtil.Front(e, handler); }

    // What Chum Bucket adds to this hit: the holder's rank, when one of their buildings hits a Slowed or Pinned foe.
    static float ChumOf(Link_Interact hit)
    {
        if (hit.From == null || hit.From.GetComponent<ArgoTowerBrain>() == null || !ArgoStatus.DebuffedBefore(hit.To))
            return 0f;
        int rank = ArgoServer.RanksOf(ModKitUtil.Owner(hit.From)).Chumbucket;
        return rank > 0 ? ArgoData.Chumbuckets[rank - 1].Bonus : 0f;
    }

    static void OnHurt(Link_Interact hit)
    {
        try
        {
            if (hit == null || hit.Type != "Hurt" || hit.To == null || hit.Data == null)
                return;
            float scale = 1f;
            if (IsPinged(hit.To))
            {
                scale *= 1f + ArgoData.PingBonus;
                Boosted++;
            }
            float soak = SoakOf(hit.To);
            if (soak > 0f)
            {
                scale *= 1f + soak;
                Crushed++;
            }
            float chum = ChumOf(hit);
            if (chum > 0f)
            {
                scale *= 1f + chum;
                Chummed++;
            }
            float armor = ArgoHooks.ArmorOf(hit.To);
            if (armor > 0f)
            {
                scale *= 1f - armor;
                Softened++;
            }
            float multi;
            if (Mathf.Approximately(scale, 1f) || !hit.Data.TryGetValue("Damagemulti", out multi))
                return;
            // A copy: some AIs reuse one dictionary for several hits (pitfall #30).
            hit.Data = new Dictionary<string, float>(hit.Data);
            hit.Data["Damagemulti"] = multi * scale;
        }
        catch (Exception e)
        {
            ArgoLog.Warn("hit hook: " + e.Message);
        }
    }
}
