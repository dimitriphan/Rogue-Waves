// A campaign's per-player things (docs/scope/06, 12; kit docs/15 §3, §14.1), on the host's mod object: Pure Aura, the
// Directions and Vessel Cardinals each player holds, their vessel, and the team's Cardinals. In a map they show through
// what already syncs: a Pure Aura counter on each vessel (ModKitCounter), the upgrades on it, and three None-style commands
// on each vessel (kit docs/20 §3.1), which draw nothing:
//   RW_Ask    host to its owner: the question open for that player (Goalstring what, Goalvalue the round)
//   RW_Run    host to its owner: the run line's text
//   RW_Order  owner to host: a pick, sent with Command_ServerRpc, which only the vessel's owner may send (kit docs/15 §6)
// Saved under the host's save for Continue, keyed by the GUID the pack keeps for each player (RWIdentity, kit docs/19 §5),
// and only at a checkpoint: a map's start and its win. A lost map replays from the last save, so what it earned goes.
public class RWPlayer
{
    public string Key = "", Name = "", Hero = "";
    public int Pure;
    public readonly List<string> Directions = new List<string>();   // every Direction upgrade held, each rank its own id
    public readonly List<string> Cardinals = new List<string>();    // Vessel Cardinals, each stack its own numbered id
    public bool Owed;                                               // a Direction pick waits for this player
    public float Sent;                                              // base aura sent in this break (Fund Transfer)
    public long Session = -1;                                       // in this host session
}

public static class RWRun
{
    public const string PureStat = "Pure_Aura";
    public const string OrderLink = "RW_Order", AskLink = "RW_Ask", RunLink = "RW_Run";
    public const string CountKey = "RW_Run_Count", PlayerKey = "RW_Run_Player_", TeamKey = "RW_Run_Team";
    public const int PerTide = 3, PerBonusTide = 2;   // docs/scope/06 [Pure Aura]
    public static readonly Dictionary<string, RWPlayer> Players = new Dictionary<string, RWPlayer>();
    public static readonly List<string> Team = new List<string>();        // Team Cardinals held, each stack its own id
    public static bool InCampaignMap;                                     // host: this map belongs to the campaign, a staging map too
    static readonly List<Entity_Master> listening = new List<Entity_Master>();
    static float nextTick;
    public static event Action<Entity_Master, RWPlayer, string> Order;    // a vessel's owner sent json

    // ---------------------------------------------------------------- who is who

    // The key that outlives a host session: the player's kept GUID, or their Steam name if the host doesn't know one.
    public static string KeyOf(long session)
    {
        Blm_Multiplayer mp = Blm_Multiplayer.Instance;
        if (mp != null && mp.Sessionids != null)
            foreach (KeyValuePair<string, long> kv in mp.Sessionids)
                if (kv.Value == session) return kv.Key;
        Blm_Player p;
        return Blm_Server.Instance != null && Blm_Server.Instance.Sessionidsplayer.TryGetValue(session, out p) && p != null ? "name:" + p.Displayname.Value : "session:" + session;
    }

    public static RWPlayer For(long session)
    {
        string key = KeyOf(session);
        RWPlayer p;
        if (!Players.TryGetValue(key, out p))
        {
            p = new RWPlayer();
            p.Key = key;
            Players[key] = p;
        }
        p.Session = session;
        Blm_Player bp;
        if (Blm_Server.Instance != null && Blm_Server.Instance.Sessionidsplayer.TryGetValue(session, out bp) && bp != null)
        {
            string name = bp.Displayname.Value.ToString();
            if (name != "") p.Name = name;
        }
        return p;
    }

    public static List<long> Sessions()
    {
        List<long> list = new List<long>();
        if (Blm_Server.Instance != null)
            list.AddRange(Blm_Server.Instance.Sessionidsplayer.Keys);
        list.Sort();
        return list;
    }

    // A player's own vessel in this map (the being they picked), alive or not: minions tagged Hero don't count.
    public static Entity_Master Vessel(long session)
    {
        List<Entity_Master> heroes;
        if (Game_Entity.Instance == null || !Game_Entity.Instance.Entityclienttags.TryGetValue("Hero", out heroes))
            return null;
        Blm_Player bp;
        Tds_Clientinfo info = Blm_Server.Instance != null && Blm_Server.Instance.Sessionidsplayer.TryGetValue(session, out bp) && bp != null ? bp.GetComponent<Tds_Clientinfo>() : null;
        Beingobject being = info != null ? Bl_Address.Getload("Hero", info.Hero.Value.ToString()) as Beingobject : null;
        Entity_Master any = null;
        foreach (Entity_Master e in heroes)
        {
            if (e == null || e.Entitydata.Value.Owner != session)
                continue;
            if (being != null && e.Being.Value.ToString() == being.Objectname)
                return e;
            if (any == null) any = e;
        }
        return any;
    }

    // ---------------------------------------------------------------- the save

    public static void Clear()
    {
        Players.Clear();
        Team.Clear();
    }

    public static void Save()
    {
        int n = 0;
        foreach (RWPlayer p in Players.Values)
        {
            RWUtil.WriteString(RWData.SaveFolder, PlayerKey + n, Encode(p));
            n++;
        }
        RWUtil.WriteString(RWData.SaveFolder, CountKey, n.ToString());
        RWUtil.WriteString(RWData.SaveFolder, TeamKey, string.Join(",", Team.ToArray()));
        RWUtil.Flush();
    }

    public static void Load()
    {
        Clear();
        int n = RWUtil.ReadInt(RWData.SaveFolder, CountKey, 0);
        for (int i = 0; i < n; i++)
        {
            RWPlayer p = Decode(RWUtil.ReadString(RWData.SaveFolder, PlayerKey + i));
            if (p != null && p.Key != "") Players[p.Key] = p;
        }
        string team = RWUtil.ReadString(RWData.SaveFolder, TeamKey);
        if (team != "") Team.AddRange(team.Split(','));
    }

    public static void Forget()
    {
        int n = RWUtil.ReadInt(RWData.SaveFolder, CountKey, 0);
        for (int i = 0; i < n; i++)
            if (RWUtil.Has(RWData.SaveFolder, PlayerKey + i)) RWUtil.Remove(RWData.SaveFolder, PlayerKey + i);
        foreach (string key in new[] { CountKey, TeamKey })
            if (RWUtil.Has(RWData.SaveFolder, key)) RWUtil.Remove(RWData.SaveFolder, key);
        Clear();
    }

    // Tabs split the fields: a Steam name may hold anything but a tab, and a tab can't be typed into one.
    public static string Encode(RWPlayer p)
    {
        return string.Join("\t", new[] { p.Key, p.Name.Replace("\t", " "), p.Hero, p.Pure.ToString(), string.Join(",", p.Directions.ToArray()), string.Join(",", p.Cardinals.ToArray()), p.Owed ? "1" : "0" });
    }

    public static RWPlayer Decode(string line)
    {
        string[] f = (line ?? "").Split('\t');
        if (f.Length < 7)
            return null;
        RWPlayer p = new RWPlayer();
        p.Key = f[0];
        p.Name = f[1];
        p.Hero = f[2];
        int.TryParse(f[3], out p.Pure);
        if (f[4] != "") p.Directions.AddRange(f[4].Split(','));
        if (f[5] != "") p.Cardinals.AddRange(f[5].Split(','));
        p.Owed = f[6] == "1";
        return p;
    }

    // Continue's card: who sailed, from the save (kit docs/20: a card's hover text).
    public static string SavedCrew()
    {
        List<string> crew = new List<string>();
        int n = RWUtil.ReadInt(RWData.SaveFolder, CountKey, 0);
        for (int i = 0; i < n; i++)
        {
            RWPlayer p = Decode(RWUtil.ReadString(RWData.SaveFolder, PlayerKey + i));
            if (p != null && p.Hero != "") crew.Add(p.Name != "" ? p.Name + " (" + HeroName(p.Hero) + ")" : HeroName(p.Hero));
        }
        return string.Join(", ", crew.ToArray());
    }

    public static string HeroName(string hero)
    {
        Heroentryobject e = RWUtil.Item<Heroentryobject>("Hero", hero);
        return e != null && !string.IsNullOrEmpty(e.Displayname) ? Bla_Localization.GetText(e.Displayname) : hero.Replace("Hero_", "");
    }

    // ---------------------------------------------------------------- the map (host)

    public static void MapStart()
    {
        listening.Clear();
        InCampaignMap = RWCampaign.Live && Tds_Multiplayerinfo.Instance != null && Tds_Multiplayerinfo.Instance.Gameplan.Value.ToString().StartsWith(RWData.PlanPrefix);
        if (!InCampaignMap)
            return;
        foreach (long s in Sessions())
        {
            RWPlayer p = For(s);
            Blm_Player bp = Blm_Server.Instance.Sessionidsplayer[s];
            Tds_Clientinfo info = bp != null ? bp.GetComponent<Tds_Clientinfo>() : null;
            if (info != null && p.Hero == "") p.Hero = info.Hero.Value.ToString();   // the vessel the campaign locks in
            p.Sent = 0f;
        }
        Save();    // the checkpoint a lost map goes back to
        nextTick = 0f;
    }

    public static void MapEnd(string result)
    {
        InCampaignMap = false;
    }

    // The Tide before this break is cleared: every player present is paid (a bonus Tide less; the Tide in progress when
    // a map ends pays nothing, because this is its break).
    public static void PayTide(int wave)
    {
        int pay = (wave <= RWCampaign.WinTide ? PerTide : PerBonusTide) + (RWWeather.Current != RWChart.Calm ? RWWeather.PureAuraPerTide : 0);
        foreach (long s in Sessions())
            For(s).Pure += pay;
        RWLog.Info("Tide " + wave + " paid " + pay + " Pure Aura each");
        nextTick = 0f;
    }

    public static void Pay(int amount, string why)
    {
        foreach (long s in Sessions())
            For(s).Pure += amount;
        RWLog.Info("paid " + amount + " Pure Aura each: " + why);
        nextTick = 0f;
    }

    // Host, every frame of a campaign map: every vessel carries the three commands and the counter.
    public static void Tick()
    {
        if (!InCampaignMap || !RWUtil.IsServer || Time.time < nextTick)
            return;
        nextTick = Time.time + 0.5f;
        foreach (long s in Sessions())
        {
            Entity_Master v = Vessel(s);
            if (v == null || v.Commandlistserver == null || v.Beingserver == null || v.Beingserver.Dead)
                continue;   // the game skips updates on a dead entity: a fallen vessel catches up when it stands again
            Carry(v, For(s));
        }
    }

    static void Carry(Entity_Master v, RWPlayer p)
    {
        if (RWContent.OrderCommand == null)
            return;
        foreach (string link in new[] { OrderLink, AskLink, RunLink })
            if (!v.Commandlistserver.ContainsKey(link))
                Helper_Entitytools.Createcommand(v, RWContent.CommandFor(link), link, false, link == OrderLink);
        if (!listening.Contains(v))
        {
            listening.RemoveAll(h => h == null);
            listening.Add(v);
            Entity_Master me = v;
            v.Command += (command, json, server) => OnCommand(me, command, json);
        }
        if (Mathf.Abs(ModKitCounter.Get(v, PureStat) - p.Pure) > 0.01f || !v.Statsserver.ContainsKey(PureStat))
            ModKitCounter.Set(v, PureStat, p.Pure);
        RWDirections.Grant(v, p);
        RWCardinals.Grant(v, p);
    }

    static void OnCommand(Entity_Master v, string command, string json)
    {
        if (command != OrderLink || v == null || Game_Entity.Instance == null || !Game_Entity.Instance.IsServer || Order == null)
            return;
        RWPlayer p = For(v.Entitydata.Value.Owner);
        foreach (Delegate d in Order.GetInvocationList())
        {
            try { ((Action<Entity_Master, RWPlayer, string>)d)(v, p, json); }
            catch (Exception e) { RWLog.Warn("order " + json + ": " + e); }
        }
    }

    // ---------------------------------------------------------------- host to one player

    public static void Ask(long session, string what, float round)
    {
        Entity_Master v = Vessel(session);
        Link_Command link;
        if (v == null || v.Commandlistserver == null || !v.Commandlistserver.TryGetValue(AskLink, out link))
            return;
        if ((link.Goalstring ?? "") == what && link.Goalvalue == round)
            return;
        link.Goalstring = what;
        link.Goalvalue = round;
        Helper_Entitytools.Updatecommand(v, AskLink, "", false, false);
    }

    public static void RunLine(long session, string text)
    {
        Entity_Master v = Vessel(session);
        Link_Command link;
        if (v == null || v.Commandlistserver == null || !v.Commandlistserver.TryGetValue(RunLink, out link))
            return;
        if (text.Length > 60) text = text.Substring(0, 60);
        if ((link.Goalstring ?? "") == text)
            return;
        link.Goalstring = text;
        Helper_Entitytools.Updatecommand(v, RunLink, "", false, false);
    }

    // ---------------------------------------------------------------- orders' little JSON (written by this part alone)

    public static string Json(string kind, int pick, string text)
    {
        return "{\"Kind\":\"" + kind + "\",\"Pick\":" + pick + ",\"Text\":\"" + (text ?? "") + "\"}";
    }

    public static string JsonText(string json, string key)
    {
        string k = "\"" + key + "\":\"";
        int at = json != null ? json.IndexOf(k) : -1;
        if (at < 0)
            return "";
        int start = at + k.Length, end = json.IndexOf('"', start);
        return end > start ? json.Substring(start, end - start) : "";
    }

    public static int JsonInt(string json, string key)
    {
        string k = "\"" + key + "\":";
        int at = json != null ? json.IndexOf(k) : -1;
        if (at < 0)
            return -1;
        int start = at + k.Length, end = start;
        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
        int v;
        return int.TryParse(json.Substring(start, end - start), out v) ? v : -1;
    }
}

// The same, as each machine sees it: this player's vessel and what the host wrote on it. Reads only what syncs.
public static class RWMine
{
    public static long Session { get { return Blm_Server.Instance != null && Blm_Server.Instance.Localplayer != null ? Blm_Server.Instance.Localplayer.Sessionid.Value : -1; } }

    public static Entity_Master Vessel()
    {
        long me = Session;
        if (me < 0 || Game_Entity.Instance == null || Game_Entity.Instance.Entityclient == null)
            return null;
        Entity_Master best = null;
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
        {
            if (e == null || e.Commandlist == null || e.Entitydata.Value.Owner != me)
                continue;
            foreach (Link_Commandref c in e.Commandlist)
                if (c.Name.ToString() == RWRun.OrderLink)
                    best = e;
        }
        return best;
    }

    // The last entry of a name is the newest: an update removes and adds it (kit docs/19 §2).
    public static bool Command(Entity_Master v, string link, out Link_Commandref found)
    {
        found = default(Link_Commandref);
        bool any = false;
        if (v == null || v.Commandlist == null)
            return false;
        foreach (Link_Commandref c in v.Commandlist)
            if (c.Name.ToString() == link)
            {
                found = c;
                any = true;
            }
        return any;
    }

    public static float Pure()
    {
        Entity_Master v = Vessel();
        if (v == null || v.Stats == null)
            return 0f;
        foreach (Link_Statsref s in v.Stats)
            if (s.Name.ToString() == RWRun.PureStat)
                return s.Value;
        return 0f;
    }

    public static void Send(string kind, int pick, string text)
    {
        Entity_Master v = Vessel();
        Entity_Master_Remote remote = v != null ? v.GetComponent<Entity_Master_Remote>() : null;
        if (remote != null)
            remote.Command_ServerRpc(RWRun.OrderLink, RWRun.Json(kind, pick, text), default(Unity.Netcode.ServerRpcParams));
    }

    public static void Popup(string text, string icon)
    {
        try { Helper_Uitools.Warning(text, icon, 3f); }
        catch (Exception e) { RWLog.Once("popup", "popup: " + e.Message); }
    }
}

// A line under the Tide counter for this player: Pure Aura, then what the host writes (kit docs/20 §5 "Run line").
public static class RWRunLabel
{
    static TMPro.TMP_Text label;
    static float nextLook;
    public static string Shown = "";    // the self-test reads it

    public static void Update()
    {
        if (Time.unscaledTime < nextLook)
            return;
        nextLook = Time.unscaledTime + 0.25f;
        Entity_Master v = RWMine.Vessel();
        if (v == null || Game_Ui.Instance == null || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game")
            return;
        if (label == null && !Make())
            return;
        Link_Commandref line;
        string text = "Pure Aura " + Mathf.RoundToInt(RWMine.Pure());
        if (RWMine.Command(v, RWRun.RunLink, out line) && line.Goalstring.ToString() != "")
            text += RWCard.Separator + line.Goalstring;
        if (label.text != text)
            label.text = text;
        Shown = text;
    }

    static bool Make()
    {
        Blockage.Menu.Blu2_Menu_Handler main;
        GameObject wave;
        if (Blockage.Menu.Bl_Menus.Menus == null || !Blockage.Menu.Bl_Menus.Menus.TryGetValue("Main", out main) || main == null
            || main.Items == null || !main.Items.TryGetValue("Wave", out wave) || wave == null)
            return false;
        TMPro.TMP_Text stock = wave.GetComponentInChildren<TMPro.TMP_Text>();
        if (stock == null)
            return false;
        GameObject o = new GameObject("RW_RunLine", typeof(RectTransform));
        o.transform.SetParent(wave.transform.parent, false);
        TMPro.TextMeshProUGUI t = o.AddComponent<TMPro.TextMeshProUGUI>();
        t.font = stock.font;
        t.fontSharedMaterial = stock.fontSharedMaterial;
        t.fontSize = stock.fontSize * 0.6f;
        t.alignment = stock.alignment;
        t.color = new Color(0.957f, 0.933f, 0.973f, 1f);     // Pearl, docs/STYLE.md
        t.raycastTarget = false;
        t.enableWordWrapping = false;
        t.overflowMode = TMPro.TextOverflowModes.Overflow;
        RectTransform r = o.transform as RectTransform, w = wave.transform as RectTransform;
        if (r != null && w != null)
        {
            r.anchorMin = w.anchorMin;
            r.anchorMax = w.anchorMax;
            r.pivot = w.pivot;
            r.sizeDelta = w.sizeDelta;
            r.anchoredPosition = w.anchoredPosition + new Vector2(0f, -w.rect.height * 1.9f);
        }
        label = t;
        return true;
    }
}
