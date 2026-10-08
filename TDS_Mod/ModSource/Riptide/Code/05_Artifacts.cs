// Salvaged artifacts: the game's own relics, granted mid-run.
//   Host:   applies the artifact's Coreupgrades to every Basin core, as Helper_Entityai.Applyartifacts does at the core's
//           spawn, plus "catch-up" for the kinds that only reach what spawns after them; then appends the artifact to
//           the synced lobby list Tds_Multiplayerinfo.Artifacts and announces it with the artifact's own icon.
//   Every machine (host too): adds what is new in that list to its own Game_Mapvars.Artifactobjects and redraws the
//           HUD's artifact row. A NetworkList replays to players who join mid-run, so they see the same row.
//   Host, back on the world map: takes the salvaged names out of the lobby list again.
public static class RiptideArtifacts
{
    public static readonly List<string> Salvaged = new List<string>();
    static int seenCount = -1;
    static float loadedAt = -1f;
    static readonly RiptideFaults faults = new RiptideFaults();
    static bool stopped;

    public static Artifactentryobject Entry(string id) { return RiptideUtil.Entry<Artifactentryobject>("Artifact", id); }

    public static Artifactobject Object(string id)
    {
        Artifactentryobject e = Entry(id);
        return e != null && e.Artifactloads != null ? RiptideUtil.Load<Artifactobject>("Artifact", e.Artifactloads.Objectname) : null;
    }

    // The effect scripts on an artifact's core upgrades ("Entity_Ai_Artifact_Coin+..."), which say what kind it is.
    public static string Kind(Artifactobject a)
    {
        List<string> kinds = new List<string>();
        if (a != null && a.Coreupgrades != null)
            foreach (Upgradeproperties u in a.Coreupgrades)
                if (u != null)
                    kinds.Add(RiptideUtil.ScriptNames(u.Aiscripts));
        return string.Join(" | ", kinds.ToArray());
    }

    public static bool Held(string id)
    {
        Tds_Multiplayerinfo info = Tds_Multiplayerinfo.Instance;
        if (info == null)
            return false;
        FixedString64Bytes key = new FixedString64Bytes(id);
        return info.Artifacts.Contains(key) || info.Mixedart.Value == key;
    }

    public static void BeginRun()
    {
        Salvaged.Clear();
    }

    // ---------------------------------------------------------------- host

    // Grants an artifact to the team. Returns "" when it went through, else why not.
    public static string Grant(string id)
    {
        if (!RiptideUtil.InRun || !Game_Entity.Instance.IsServer)
            return "not the host of a Riptide match";
        if (Held(id))
            return "already held";
        Artifactobject art = Object(id);
        if (art == null)
            return "not loaded";
        List<Entity_Master> cores = RiptideUtil.Tagged("Core");
        if (cores.Count == 0)
            return "no Basin core";
        foreach (Entity_Master core in cores)
            foreach (Upgradeproperties up in art.Coreupgrades)
                if (up != null)
                    Helper_Entitytools.Upgrade(core, up, Helper_Entitytools.Upgradecheck.Forced, art);
        CatchUp(art, cores[0]);
        Tds_Multiplayerinfo.Instance.Artifacts.Add(new FixedString64Bytes(id));
        Salvaged.Add(id);
        Artifactentryobject entry = Entry(id);
        string name = entry != null ? Bla_Localization.GetText(entry.Display_Name).ToUpper() : id;
        if (cores[0].Radio != null)
            cores[0].Radio.Uiwarning_ClientRpc(name + RiptideData.SalvagedWarning, id, 3f);
        RiptideLog.Info("salvaged " + id + " (" + Kind(art) + ")");
        return "";
    }

    // The apply-kinds hook Game_Entity.Onspawn and only reach what spawns afterwards (Heroapply: a vessel respawning at
    // the core). Mid-run the director gives the same upgrades to what already stands, with the same filters the game's
    // scripts use. Returns how many entities got something.
    public static int CatchUp(Artifactobject art, Entity_Master core)
    {
        int reached = 0;
        foreach (Upgradeproperties up in art.Coreupgrades)
        {
            if (up == null || up.Aiscripts == null || up.Defaultparams == null)
                continue;
            string kinds = RiptideUtil.ScriptNames(up.Aiscripts);
            List<Upgradeproperties> give = new List<Upgradeproperties>();
            foreach (KeyValuePair<string, float> p in up.Defaultparams)
            {
                Upgradeproperties u = RiptideUtil.Load<Upgradeproperties>("Upgrade", p.Key);
                if (u != null)
                    give.Add(u);
            }
            if (give.Count == 0)
                continue;
            int team = RiptideUtil.Team(core);
            foreach (Entity_Master e in new List<Entity_Master>(Game_Entity.Instance.Entityclient))
            {
                if (!RiptideUtil.Alive(e) || e.Beingserver.Entity == null)
                    continue;
                List<string> tags = e.Beingserver.Entity.Tags;
                List<Upgradeproperties> these = null;
                if (kinds.Contains(RiptideData.KindTower) && tags.Contains("Tower") && RiptideUtil.Team(e) == team)
                    these = give;
                else if (kinds.Contains(RiptideData.KindMob) && tags.Contains("Mob"))
                    these = give;   // the game's Mobapply compares its own team with itself: every Mob gets it
                else if (kinds.Contains(RiptideData.KindTeam) && e.Parent != null && RiptideUtil.HasTag(e.Parent, "Hero")
                    && (tags.Contains("Tower") || tags.Contains("Mob")))
                    these = new List<Upgradeproperties> { give[0] };   // Teamapply uses its first upgrade only
                else if (kinds.Contains(RiptideData.KindHero) && tags.Contains("Hero"))
                    these = give;
                if (these == null)
                    continue;
                bool got = false;
                foreach (Upgradeproperties u in these)
                    if (e.Upgradelistserver == null || !e.Upgradelistserver.ContainsKey(u.Objectname))
                    {
                        Helper_Entitytools.Upgrade(e, u, Helper_Entitytools.Upgradecheck.Forced, null);
                        got = true;
                    }
                if (got)
                    reached++;
            }
        }
        return reached;
    }

    // Back on the world map: the lobby list returns to what the players picked in Setup.
    public static void RestoreLobby()
    {
        if (Salvaged.Count == 0 || Tds_Multiplayerinfo.Instance == null || !Tds_Multiplayerinfo.Instance.IsServer)
            return;
        foreach (string id in Salvaged)
            Tds_Multiplayerinfo.Instance.Artifacts.Remove(new FixedString64Bytes(id));
        RiptideLog.Info("lobby artifacts restored (" + Salvaged.Count + " salvaged ones taken out)");
        Salvaged.Clear();
    }

    // ---------------------------------------------------------------- every machine

    public static void Tick()
    {
        if (stopped)
            return;
        try { Sync(); }
        catch (Exception e) { stopped = faults.Fail("artifact row", e); }
    }

    static void Sync()
    {
        if (!RiptideUtil.InRun || Tds_Multiplayerinfo.Instance == null)
        {
            seenCount = -1;
            loadedAt = -1f;
            return;
        }
        // Wait until this machine has loaded the match (the game fills the row itself at load).
        if (loadedAt < 0f)
        {
            Blm_Server server = Blm_Server.Instance;
            Tds_Clientinfo me = server != null && server.Localplayer != null ? server.Localplayer.GetComponent<Tds_Clientinfo>() : null;
            if (me == null || !me.Loaded.Value)
                return;
            loadedAt = Time.realtimeSinceStartup;
        }
        if (Time.realtimeSinceStartup - loadedAt < 2f)
            return;
        int count = Tds_Multiplayerinfo.Instance.Artifacts.Count;
        if (count == seenCount)
            return;
        seenCount = count;
        int added = 0;
        foreach (FixedString64Bytes name in Tds_Multiplayerinfo.Instance.Artifacts)
        {
            Artifactobject art = Object(name.ToString());
            if (art != null && !Game_Mapvars.Instance.Artifactobjects.Contains(art))
            {
                Game_Mapvars.Instance.Artifactobjects.Add(art);
                added++;
            }
        }
        if (added > 0)
            Redraw();
    }

    public static void Redraw()
    {
        Game_Artifacts row = UnityEngine.Object.FindObjectOfType<Game_Artifacts>();
        if (row != null)
            row.SendMessage("Updateartifactlist", SendMessageOptions.DontRequireReceiver);
    }
}
