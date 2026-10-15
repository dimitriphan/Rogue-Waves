// EXAMPLE, frozen: Sprigg 1.x code from ModProjects (Sprigg itself waits there for its overhaul), kept because the
// kit docs cite it.
// Server-side brain for the vessel: build options, WATER, Sap and blooming over the run. It plays the part of a
// vessel's own AI (Entity_Ai_Hero_<Name>). Attacking is left to the stock Entity_Ai_Mob_Ranged, exactly like Sno Sno:
// this brain only names the projectile it fires. Movement, building and selling are the engine's generic hero AIs.
public class SpriggHeroBrain : MonoBehaviour
{
    public Entity_Master Master;
    public int Sap;
    public string Stage = "";
    bool started, armed;
    Link_Modifier sapCounter;

    public static readonly string[] WarnText =
    {
        "",
        "Not enough Sap",
        "Nothing thirsty nearby",
        "Needs more Rings to upgrade",
        "Watered",
        "Already watered this Tide",
        "Too far away to water",
        "Fully grown",
    };

    void Update()
    {
        if (Master == null)
        {
            Destroy(this);
            return;
        }
        if (!SpriggUtil.IsServer)
            return;
        try
        {
            if (!started)
            {
                if (Master.Statsserver == null || Master.Statsserver.Count == 0 || Master.Commandlistserver == null || Master.Radio == null)
                    return;
                Begin();
            }
            if (!armed)
                Arm();
        }
        catch (Exception e)
        {
            SpriggLog.Warn("hero brain: " + e.Message);
            enabled = false;
        }
    }

    void Begin()
    {
        started = true;
        SpriggServer.Heroes.Add(this);
        for (int i = 0; i < SpriggData.Towers.Length; i++)
        {
            string link = "Buildoption" + (i + 1);
            Link_Command c = Helper_Entitytools.Createcommand(Master, "Command_" + link, link, false, true);
            c.Goalstring = SpriggData.Towers[i];
            c.Goalvalue = 1f;
            Helper_Entitytools.Updatecommand(Master, link, "", true, true);
        }
        if (SpriggContent.Water != null)
        {
            Helper_Entitytools.Createcommand(Master, SpriggData.WaterCommand, SpriggData.WaterLink, false, true);
            Helper_Entitytools.Updatecommand(Master, SpriggData.WaterLink, "", true, true);
        }
        Sap = SpriggData.SapStart;
        sapCounter = SpriggUtil.Counter(Master, "Sap", Sap);
        Master.Command += OnCommand;
        SetStage(SpriggServer.Tide);
        SpriggLog.Info("Sprigg landed for player " + SpriggUtil.Owner(Master));
    }

    // Entity_Ai_Mob_Ranged creates the Attack command when it spawns; give it Sprigg's seed (Hero_Snosno.Starting does
    // the same with its snowball).
    void Arm()
    {
        Link_Command attack;
        if (!Master.Commandlistserver.TryGetValue("Attack", out attack) || attack == null)
            return;
        attack.Goalstring = SpriggData.HeroSeed;
        Entity_Ai_Mob_Ranged ranged = Master.GetComponent<Entity_Ai_Mob_Ranged>();
        if (ranged != null && ranged.Hurtdict != null)
            ranged.Hurtdict["Seed"] = 1f;
        armed = true;
    }

    // ---------------------------------------------------------------- Sap and blooming

    public void AddSap(int amount)
    {
        Sap = Mathf.Max(0, Sap + amount);
        SpriggUtil.SetCounter(Master, sapCounter, "Sap", Sap);
    }

    public void OnTide(int tide)
    {
        if (Master == null)
            return;
        AddSap(SpriggData.SapPerTide);
        SetStage(tide);
    }

    void SetStage(int tide)
    {
        string stage = tide >= SpriggData.BloomTide ? "Bloom" : tide >= SpriggData.BudTide ? "Bud" : tide >= SpriggData.LeafyTide ? "Leafy" : "";
        if (stage == Stage)
            return;
        Stage = stage;
        SpriggUtil.Skin(Master, stage);
        if (stage != "")
            SpriggUtil.Vis(Master, "Sprigg_Bloomstage", SpriggUtil.Pos(Master), stage == "Bloom" ? 3 : stage == "Bud" ? 2 : 1);
    }

    // ---------------------------------------------------------------- WATER

    void OnCommand(string command, string json, bool server)
    {
        if (command != SpriggData.WaterLink)
            return;
        try { Water(Picked(json)); }
        catch (Exception e) { SpriggLog.Warn("water: " + e.Message); }
    }

    // The plant the player clicked (SpriggWaterSelect sends its NetworkObjectId as {"Ref":id}); null if none.
    static SpriggPlantBrain Picked(string json)
    {
        int at = json != null ? json.IndexOf("\"Ref\":") : -1;
        if (at < 0)
            return null;
        int i = at + 6, j = i;
        while (j < json.Length && char.IsDigit(json[j])) j++;
        ulong id;
        if (j == i || !ulong.TryParse(json.Substring(i, j - i), out id))
            return null;
        foreach (SpriggPlantBrain p in SpriggServer.Plants)
            if (p != null && p.Master != null && p.Master.NetworkObjectId == id)
                return p;
        return null;
    }

    public void Water() { Water(null); }

    // One plant per press: the one the player picked, or (from the hotkey with nothing under the cursor) the nearest
    // thirsty one. At full Bloom every thirsty plant in range drinks at once.
    public void Water(SpriggPlantBrain picked)
    {
        if (Sap < 1)
        {
            SpriggUtil.Warn(Master, 1);
            return;
        }
        bool bloom = Stage == "Bloom";
        float range = bloom ? SpriggData.BloomWaterRange : SpriggData.WaterRange;
        Vector3 pos = SpriggUtil.Pos(Master);
        long owner = SpriggUtil.Owner(Master);
        if (picked != null && !bloom)
        {
            if (!picked.Started || SpriggUtil.Owner(picked.Master) != owner)
                return;
            if (Vector3.Distance(SpriggUtil.Pos(picked.Master), pos) > range)
            {
                SpriggUtil.Warn(Master, 6);
                return;
            }
            if (!picked.Thirsty)
            {
                SpriggUtil.Warn(Master, picked.Rings >= picked.Cap ? 7 : 5);
                return;
            }
        }
        List<SpriggPlantBrain> thirsty = new List<SpriggPlantBrain>();
        if (picked != null && !bloom)
            thirsty.Add(picked);
        else
            foreach (SpriggPlantBrain p in SpriggServer.Plants)
                if (p != null && p.Started && p.Thirsty && SpriggUtil.Owner(p.Master) == owner && Vector3.Distance(SpriggUtil.Pos(p.Master), pos) <= range)
                    thirsty.Add(p);
        if (thirsty.Count == 0)
        {
            SpriggUtil.Warn(Master, 2);
            return;
        }
        thirsty.Sort((a, b) => Vector3.Distance(SpriggUtil.Pos(a.Master), pos).CompareTo(Vector3.Distance(SpriggUtil.Pos(b.Master), pos)));
        if (picked != null && thirsty.Remove(picked))
            thirsty.Insert(0, picked);
        int watered = 0;
        foreach (SpriggPlantBrain p in thirsty)
        {
            if (Sap < 1)
                break;
            // Clients play the can, then pour an arc of water onto this plant (numbered so Bloom's arcs fan out in turn).
            float delay = watered * 0.1f;
            Master.Radio.Vis_ClientRpc("Sprigg_Wateranim", SpriggUtil.Pos(p.Master), delay);
            p.Water(delay);
            AddSap(-1);
            watered++;
            if (!bloom)
                break;
        }
    }
}