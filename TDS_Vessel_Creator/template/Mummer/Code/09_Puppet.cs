// Server-side brain for an Understudy. Fighting is the game's own Entity_Ai_Mob_Combat / Entity_Ai_Mob_Ranged; this
// brain only decides where the Understudy walks, the way the shipped units' AIs do it (Boutman's fish set the Move
// command's Goalobject to a foe, or its Goalposition to a spot, and Updatecommand it): step out to meet foes that come
// within reach of its Stage, prefer Marked foes, and walk back to the Stage when nothing is near.
public class MummerPuppetBrain : MonoBehaviour
{
    public Entity_Master Master;
    public MummerData.Puppet Def;
    public MummerStageBrain Stage;
    public bool Started;
    public Entity_Master Target;
    public int Orders;

    Vector3 home;
    float nextThink, nextHeal;
    int slot;

    void Update()
    {
        if (Master == null)
        {
            Destroy(this);
            return;
        }
        if (!MummerUtil.IsServer)
            return;
        try
        {
            if (!Started)
            {
                if (Master.Statsserver == null || Master.Statsserver.Count == 0 || Master.Commandlistserver == null || !Master.Commandlistserver.ContainsKey("Move"))
                    return;
                Begin();
            }
            if (Def.Ranged)
                Arm();
            if (Time.time >= nextThink)
            {
                nextThink = Time.time + 0.4f;
                Think();
            }
            if (Time.time >= nextHeal)
            {
                nextHeal = Time.time + 1f;
                Heal();
            }
        }
        catch (Exception e)
        {
            MummerLog.Warn(Def.Id + " brain: " + e.Message);
            enabled = false;
        }
    }

    void Begin()
    {
        Started = true;
        if (Stage != null && Stage.Master != null)
        {
            slot = Stage.Stitched;
            float a = slot * 2.4f;
            home = MummerUtil.Pos(Stage.Master) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.1f;
            float hp = Stage.Param("Puppethealth_Multi", 0f), dmg = Stage.Param("Puppetdamage_Multi", 0f), spd = Stage.Param("Puppetspeed_Multi", 0f);
            if (hp != 0f) { Helper_Entitytools.Addmod(Master, "Health", hp, Helper_Entitytools.Modtypes.Multi, 0f); Fill(); }
            if (dmg != 0f) Helper_Entitytools.Addmod(Master, "Damage", dmg, Helper_Entitytools.Modtypes.Multi, 0f);
            if (spd != 0f) Helper_Entitytools.Addmod(Master, "Speed", spd, Helper_Entitytools.Modtypes.Multi, 0f);
        }
        else
            home = MummerUtil.Pos(Master);
        MummerUtil.Vis(Master, "Mummer_Puppetpop", MummerUtil.Pos(Master), (float)Def.Costume);
    }

    // A raised maximum starts full.
    void Fill()
    {
        Link_Stats s;
        if (Master.Statsserver.TryGetValue("Health", out s))
            s.Value = s.Value_Calc;
    }

    // Entity_Ai_Mob_Ranged creates the Attack command when it spawns; give it the felt bolt (Sno Sno's AI names its
    // snowball the same way).
    bool armed;
    void Arm()
    {
        if (armed)
            return;
        Link_Command attack;
        if (!Master.Commandlistserver.TryGetValue("Attack", out attack) || attack == null)
            return;
        attack.Goalstring = MummerData.PuppetBolt;
        Entity_Ai_Mob_Ranged ranged = Master.GetComponent<Entity_Ai_Mob_Ranged>();
        if (ranged != null && ranged.Hurtdict != null)
            ranged.Hurtdict["Bolt"] = 1f;
        armed = true;
    }

    Vector3 Anchor { get { return Stage != null && Stage.Master != null ? MummerUtil.Pos(Stage.Master) : home; } }
    float Leash { get { return Stage != null && Stage.Master != null ? Stage.Leash : 4f; } }

    void Think()
    {
        Vector3 pos = MummerUtil.Pos(Master);
        Vector3 anchor = Anchor;
        float leash = Leash;
        float range = MummerUtil.Stat(Master, "Range", 1.5f);
        List<Entity_Master> foes = MummerUtil.FoesNear(Master, pos, Mathf.Max(MummerData.PuppetEngage, range + 0.5f));
        foes.RemoveAll(f => Vector3.Distance(MummerUtil.Pos(f), anchor) > leash + 0.5f);
        Entity_Master target = MummerServer.Pick(foes);
        string state = Master.State.Value.ToString();
        if (Vector3.Distance(pos, anchor) > leash + 1f)
            target = null;   // strayed past its strings: back to the stage first
        if (target != null)
        {
            float d = Vector3.Distance(MummerUtil.Pos(target), pos);
            if (Def.Ranged)
            {
                if (d > range - 0.3f)
                    MoveTo(null, MummerUtil.Pos(target) + (pos - MummerUtil.Pos(target)).normalized * Mathf.Max(1f, range - 1f));
                else if (state == "Move")
                    Stop();
            }
            else if (d > range - 0.1f && (Target != target || state != "Move"))
                MoveTo(target, Vector3.zero);
            Target = target;
            return;
        }
        Target = null;
        if (Vector3.Distance(pos, home) > 1.2f && state != "Move" && state != "Attack")
            MoveTo(null, home);
        else if (Vector3.Distance(pos, home) > 1.2f && Vector3.Distance(pos, anchor) > leash + 1f)
            MoveTo(null, home);
    }

    void MoveTo(Entity_Master target, Vector3 position)
    {
        Link_Command move = Master.Commandlistserver["Move"];
        move.Goalobject = target;
        move.Goalposition = target != null ? Vector3.one * -1f : position;
        move.Goalvalue = 0f;
        move.Goalstring = "Idle";   // what to do on arrival (the Mining Hut sends its miners the same way)
        Helper_Entitytools.Updatecommand(Master, "Move", "", true, true);
        Orders++;
    }

    void Stop() { Helper_Entitytools.Updatecommand(Master, "Idle", "", true, true); }

    // The Aegis costume patches itself up when left alone (like the Aegis it copies); Standing Ovation and a Trapdoor
    // Curtain heal any Understudy near them.
    void Heal()
    {
        float amount = 0f;
        if (Def.Costume == MummerData.Costume.Aegis && Time.time - Master.Lasthittime > MummerData.AegisRegenDelay)
            amount += MummerData.AegisRegen;
        if (Stage != null && Stage.Master != null && Stage.Has("Ovation") && Vector3.Distance(MummerUtil.Pos(Master), Anchor) <= Leash + 0.5f)
            amount += Stage.Param("Regen_Base", 2f);
        foreach (MummerBuilding b in MummerServer.Buildings)
            if (b is MummerCurtainBrain && b.Has("Trapdoor") && b.Master != null && MummerUtil.Owner(b.Master) == MummerUtil.Owner(Master)
                && Vector3.Distance(MummerUtil.Pos(b.Master), MummerUtil.Pos(Master)) <= 2.5f)
                amount += b.Param("Heal_Base", 3f);
        Link_Stats hp;
        if (amount > 0f && Master.Statsserver.TryGetValue("Health", out hp) && hp.Value < hp.Value_Calc - 0.01f)
            MummerUtil.Heal(Master, Master, amount);
    }

    // Grand Finale: the Understudy bursts into stuffing that hurts every foe around it.
    public void Fell()
    {
        Vector3 pos = MummerUtil.Pos(Master);
        Entity_Master radio = Stage != null && Stage.Master != null ? Stage.Master : Master;
        if (Stage != null && Stage.Master != null && Stage.Has("Finale"))
        {
            float burst = Stage.Param("Burst_Base", 8f);
            foreach (Entity_Master foe in MummerUtil.FoesNear(Stage.Master, pos, 1.6f))
                MummerUtil.Hurt(Stage.Master, foe, burst);
            MummerUtil.Vis(radio, "Mummer_Finale", pos, burst);
        }
        MummerUtil.Vis(radio, "Mummer_Puppetdown", pos, (float)Def.Costume);
    }
}
