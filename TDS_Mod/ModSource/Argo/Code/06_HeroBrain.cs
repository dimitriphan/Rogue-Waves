// Server-side brain for the vessel: build options (2.0: five, the fifth the Miner's Base, gone while one stands), SONAR
// (switched off in 2.0) and the second hit of each swing. It plays the part of a
// vessel's own AI (Entity_Ai_Hero_<Name>). Swinging is left to the stock Entity_Ai_Mob_Combat, exactly like Smacky:
// this brain listens to the vessel's own hit event (Entity_Master.Interact) and carries each swing on to a neighbour.
public class ArgoHeroBrain : MonoBehaviour
{
    public Entity_Master Master;
    public float SonarReadyAt;
    public int Sonars, Cleaves, BaseHidden, BaseShown;
    bool started;
    float nextBase;
    static readonly List<string> missingOptions = new List<string>();
    Link_Command sonarCommand;
    bool sonarOff;
    float nextHookCheck;
    Entity_Master.Interacts hitHandler;
    readonly ArgoFaults faults = new ArgoFaults();

    public static readonly string[] WarnText =
    {
        "",
        "SONAR is recharging",
        "{0} can't reach that tower",
        "{0} is already on that tower",
        "Pick a tower",
        "Sent {0}",
        "{0} is heading home",
        "{0} is swimming back to the Miner's Base",
        "That tower belongs to the other team",
        "Nauts tune towers, never traps",
        "Out of this Drill Rig's reach",
        "FLOOD a tile next to this Deep Water or one of its pools",
    };

    void Update()
    {
        if (Master == null)
        {
            Destroy(this);
            return;
        }
        if (!ArgoUtil.IsServer)
            return;
        try
        {
            if (!started)
            {
                if (Master.Statsserver == null || Master.Statsserver.Count == 0 || Master.Commandlistserver == null || Master.Radio == null)
                    return;
                Begin();
            }
            if (sonarOff && Time.time >= SonarReadyAt)
                SetSonarButton(false);
            if (Time.time >= nextBase)
            {
                CheckBase();
                CheckDirections();
            }
            // Helper_Entitytools.Updateinteract rebuilds Interact whenever an upgrade or effect lands on the vessel.
            if (Time.time >= nextHookCheck)
            {
                nextHookCheck = Time.time + 0.25f;
                if (Master.Interact == null || Array.IndexOf(Master.Interact.GetInvocationList(), hitHandler) < 0)
                    Master.Interact += hitHandler;
            }
        }
        catch (Exception e)
        {
            if (faults.Fail("hero brain", e))
                enabled = false;
        }
    }

    void Begin()
    {
        started = true;
        ArgoServer.Heroes.Add(this);
        for (int i = 0; i < ArgoData.Towers.Length; i++)
            BuildOption(i);
        CheckBase();
        CheckDirections();
        if (ArgoData.SonarEnabled && ArgoContent.Commands.ContainsKey(ArgoData.SonarLink))
        {
            sonarCommand = Helper_Entitytools.Createcommand(Master, ArgoData.SonarCommand, ArgoData.SonarLink, false, true);
            Helper_Entitytools.Updatecommand(Master, ArgoData.SonarLink, "", true, true);
        }
        // Subscribed after the Updatecommand calls above, which run each command once.
        Master.Command += OnCommand;
        hitHandler = OnHit;
        Master.Interact += hitHandler;
        ArgoLog.Info("Argo landed for player " + ArgoUtil.Owner(Master));
    }

    // Build option i+1 builds Towers[i]. The fifth needs Command_Buildoption5 (ArgoContent registers it when the game
    // has none); without it Argo keeps four buttons and logs why, rather than a button with no command behind it.
    void BuildOption(int i)
    {
        string link = "Buildoption" + (i + 1);
        // Quiet lookup: CheckBase asks every half second, and Getload logs each miss.
        if (ArgoUtil.Load<Commandobject>("Command", "Command_" + link) == null)
        {
            if (!missingOptions.Contains(link))
            {
                missingOptions.Add(link);
                ArgoLog.Warn("no Command_" + link + "; " + ArgoData.Towers[i] + " has no build button");
            }
            return;
        }
        Link_Command c = Helper_Entitytools.Createcommand(Master, "Command_" + link, link, false, true);
        c.Goalstring = ArgoData.Towers[i];
        c.Goalvalue = 1f;
        Helper_Entitytools.Updatecommand(Master, link, "", true, true);
    }

    // The Miner's Base limit: its button goes while this player's Base stands and comes back when it is gone
    // (Entity_Ai_Hero_Boutman.Checkbouthouses removes and recreates Buildoption1 the same way). The server checks build
    // commands against the vessel's own build options, so a removed button can't be built from either.
    public void CheckBase()
    {
        nextBase = Time.time + 0.5f;
        if (!started || Master == null || Master.Commandlistserver == null)
            return;
        bool has = ArgoServer.HasBase(ArgoUtil.Owner(Master));
        bool shown = Master.Commandlistserver.ContainsKey(ArgoData.BaseOption);
        if (has && shown)
        {
            Helper_Entitytools.Removecommand(Master, ArgoData.BaseOption);
            BaseHidden++;
        }
        else if (!has && !shown)
        {
            BuildOption(Array.IndexOf(ArgoData.Towers, ArgoData.Bell));
            if (Master.Commandlistserver.ContainsKey(ArgoData.BaseOption))
                BaseShown++;
        }
    }

    // The Directions this vessel holds, for what its player's Deep Water and Harpoon Guns do (kit 1.7).
    void CheckDirections()
    {
        ArgoServer.Ranks r = ArgoServer.RanksOf(ArgoUtil.Owner(Master));
        r.Deepcurrent = ArgoServer.DirectionRank(Master, ArgoData.Deepcurrents[0].Name);
        r.Heavysalvo = ArgoServer.DirectionRank(Master, ArgoData.Heavysalvos[0].Name);
    }

    // The button's look follows the command's Goalstring ("Off" while SONAR recharges), synced through Commandlist.
    void SetSonarButton(bool off)
    {
        sonarOff = off;
        if (sonarCommand == null)
            return;
        sonarCommand.Goalstring = off ? "Off" : "";
        // run = false: only sync the look. With true the game re-runs the command, so SONAR fired its own "recharging"
        // warning on every use (Argo 1.0).
        Helper_Entitytools.Updatecommand(Master, ArgoData.SonarLink, "", false, false);
    }

    void OnCommand(string command, string json, bool server)
    {
        if (command != ArgoData.SonarLink)
            return;
        try { Sonar(); }
        catch (Exception e) { ArgoLog.Warn("sonar: " + e.Message); }
    }

    // ---------------------------------------------------------------- SONAR

    public bool Sonar()
    {
        if (Time.time < SonarReadyAt)
        {
            ArgoUtil.Warn(Master, 1);
            return false;
        }
        SonarReadyAt = Time.time + ArgoData.SonarCooldown;
        SetSonarButton(true);
        Sonars++;
        Vector3 pos = ArgoUtil.Pos(Master);
        // Clients play the animation and the ring, and mark every enemy inside the same radius.
        ArgoUtil.Vis(Master, "Argo_Sonar", pos, ArgoData.SonarRadius);
        foreach (Entity_Master e in ArgoUtil.Area(pos, ArgoData.SonarRadius, ArgoUtil.Enemies(Master)))
            if (e != null && ArgoUtil.Alive(e) && !ArgoUtil.HasTag(e, "Core") && !ArgoUtil.HasTag(e, "Enemycore"))
                ArgoHooks.Ping(e, ArgoData.PingDuration);
        // Every Drill Rig on Argo's team inside the ping quakes.
        foreach (ArgoTowerBrain t in ArgoServer.Towers)
            if (t != null && t.IsDrill && t.Started && ArgoUtil.Team(t.Master) == ArgoUtil.Team(Master)
                && ArgoUtil.Flat(ArgoUtil.Pos(t.Master), pos) <= ArgoData.SonarRadius)
                t.Quake(ArgoData.QuakeRoot);
        return true;
    }

    // ---------------------------------------------------------------- the second hit

    void OnHit(Link_Interact hit)
    {
        try
        {
            if (hit == null || hit.Type != "Hurt" || hit.To == null || hit.To == Master || hit.Data == null || hit.Data.ContainsKey("Argocleave"))
                return;
            Entity_Master first = hit.To;
            if (ArgoUtil.Team(first) == ArgoUtil.Team(Master) || !(ArgoUtil.HasTag(first, "Mob") || ArgoUtil.HasTag(first, "Tower")))
                return;
            float damage, multi;
            if (!hit.Data.TryGetValue("Damage", out damage) || !hit.Data.TryGetValue("Damagemulti", out multi))
                return;
            Entity_Master second = null;
            float best = float.MaxValue;
            Vector3 at = ArgoUtil.Pos(first);
            foreach (Entity_Master e in ArgoUtil.Area(at, ArgoData.CleaveReach, ArgoUtil.Enemies(Master)))
            {
                if (e == null || e == first || !ArgoUtil.Alive(e))
                    continue;
                float d = ArgoUtil.Flat(ArgoUtil.Pos(e), at);
                if (d < best && ArgoUtil.Flat(ArgoUtil.Pos(e), ArgoUtil.Pos(Master)) <= ArgoUtil.Stat(Master, "Range", 1.75f) + 1f)
                {
                    best = d;
                    second = e;
                }
            }
            if (second == null)
                return;
            // Recorded on the second foe like the swing itself (ModKitCombat): kill credit, and no Aegis regeneration.
            ModKitCombat.Hurt(Master, second, damage * multi * ArgoData.CleaveMulti, 1f, null, ArgoUtil.Data("Argocleave", 1f));
            Cleaves++;
            ArgoUtil.Vis(Master, "Argo_Clang", ArgoUtil.Pos(second), 0);
        }
        catch (Exception e)
        {
            ArgoLog.Warn("cleave: " + e.Message);
        }
    }
}
