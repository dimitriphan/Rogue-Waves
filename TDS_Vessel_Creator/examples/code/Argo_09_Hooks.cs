// EXAMPLE, frozen: Argo 1.x code from ModProjects, kept because the kit docs cite it. The live Argo 2.0 is
// TDS_Mod/ModSource/Argo (and Assets/ArgoMod/Editor); copy from there, not from here.
// OnHurt's "A copy" comment is half wrong: projectiles give each hit its own copy of Data; some AIs do reuse one
// dictionary across hits (pitfall #30).
// Damage modifiers that ride on the game's own hit event. Helper_Entitytools.Interacthurt hands each hit to the
// target's Oninteract chain; Entity_Ai_Mob_General (or the tower's AI) applies it there. Putting Argo's handler first
// in that chain lets it scale the hit before anyone applies it, whoever dealt it:
//   Pinged   (SONAR)   enemies take 20% more damage from every source, teammates' towers included.
//   Bulkhead (Nauts)   a tuned tower takes 25% less.
// Helper_Entitytools.Updateinteract rebuilds the chain whenever an upgrade or effect lands on the entity, so the
// handler is checked and moved back to the front a few times a second while it is needed.
public static class ArgoHooks
{
    static readonly Dictionary<Entity_Master, float> pinged = new Dictionary<Entity_Master, float>();
    static readonly Dictionary<Entity_Master, List<ArgoNautBrain>> armored = new Dictionary<Entity_Master, List<ArgoNautBrain>>();
    static readonly Dictionary<Entity_Master, float> armorAmount = new Dictionary<Entity_Master, float>();
    static readonly Entity_Master.Oninteracts handler = OnHurt;
    static float nextCheck;
    public static int Boosted, Softened;

    public static void Reset()
    {
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

    public static void Armor(Entity_Master tower, ArgoNautBrain by, float amount)
    {
        List<ArgoNautBrain> list;
        if (!armored.TryGetValue(tower, out list))
            armored[tower] = list = new List<ArgoNautBrain>();
        if (!list.Contains(by))
            list.Add(by);
        float old;
        armorAmount[tower] = Mathf.Max(armorAmount.TryGetValue(tower, out old) ? old : 0f, Mathf.Clamp(amount, 0f, 0.9f));
        First(tower);
    }

    public static void Unarmor(Entity_Master tower, ArgoNautBrain by)
    {
        List<ArgoNautBrain> list;
        if (tower == null || !armored.TryGetValue(tower, out list))
            return;
        list.Remove(by);
        if (list.Count == 0)
        {
            armored.Remove(tower);
            armorAmount.Remove(tower);
        }
    }

    public static void Update()
    {
        if (Time.time < nextCheck)
            return;
        nextCheck = Time.time + 0.1f;
        List<Entity_Master> drop = null;
        foreach (KeyValuePair<Entity_Master, float> kv in pinged)
        {
            if (kv.Key == null || Time.time >= kv.Value || !ArgoUtil.Alive(kv.Key))
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
        foreach (Entity_Master t in armored.Keys)
        {
            if (t == null || !ArgoUtil.Alive(t))
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

    // Moves Argo's handler to the front of the entity's Oninteract chain (adding it if Updateinteract dropped it).
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
            float scale = 1f;
            if (IsPinged(hit.To))
            {
                scale *= 1f + ArgoData.PingBonus;
                Boosted++;
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
            // A copy: projectiles and some AIs reuse one dictionary for several hits.
            hit.Data = new Dictionary<string, float>(hit.Data);
            hit.Data["Damagemulti"] = multi * scale;
        }
        catch (Exception e)
        {
            ArgoLog.Warn("hit hook: " + e.Message);
        }
    }
}
