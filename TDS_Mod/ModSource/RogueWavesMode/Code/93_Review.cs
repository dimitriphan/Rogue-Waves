#if ROGUEWAVESMODE_TWOPC
// Debug build only, the two-player build with "-rwshort -rwreview": after the first rejoin, the code review's online PC checks
// (docs/reports/CODE_REVIEW_2026-11-04.md §6: MODE-D4, B1, B3, B4). A finding's reading is a line "TEST INFO FINDING <id>: SEEN|not seen (...)":
// it says the same before and after a fix, and fails nothing.
public static partial class RWTest
{
    static void Reading(string id, bool seen, string detail) { Line("INFO FINDING " + id + ": " + (seen ? "SEEN" : "not seen") + " (" + detail + ")"); }

    static string Speeds()
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        return "Time.timeScale " + RWUtil.F(Time.timeScale) + ", Time_Scale " + (vars != null ? RWUtil.F(vars.Time_Scale.Value) : "-") + ", Time_Speedup " + (vars != null ? vars.Time_Speedup.Value.ToString() : "-");
    }

    // The Basin holds through the test's big fight: its 60 extra foes aren't a balance check, and a map lost there made every rejoin
    // check after it fail (round 2, a Spool host with nothing built).
    static void HoldBasin()
    {
        Entity_Master basin = RWUtil.PlayerCore();
        Link_Stats s;
        if (basin == null || basin.Statsserver == null) return;
        if (basin.Statsserver.TryGetValue("Health", out s)) s.Value = s.Value_Calc;
        if (basin.Statsserver.TryGetValue("Healthresource", out s)) s.Value = s.Value_Calc;
    }

    // The fight's last part on the host: looks sent by the host's own code, to see whether they reach the friend past the game's
    // throttle, and how many foes a slow really holds on the host, to tell "no slow happened" from "the look was lost".
    static IEnumerator BigFightHost(long friend)
    {
        float t = 0f, sendAt = 20f;
        int sent = 0, mostSlowed = 0, mostStack = 0;
        // A top-up each frame isn't enough: 60 foes spawned together can strike in one frame (round 3, an Argo host: the map lost in seconds).
        Entity_Master held = RWUtil.PlayerCore();
        if (held != null && held.Statsserver != null && held.Statsserver.ContainsKey("Health")) Helper_Entitytools.Addmod(held, "Health", 100000f, Helper_Entitytools.Modtypes.Add, 45f);
        while (t < 37f)
        {
            HoldBasin();
            int slowed = 0;
            List<Entity_Master> foes;
            if (Game_Entity.Instance.Entityclienttags.TryGetValue("Enemy", out foes))
                foreach (Entity_Master e in new List<Entity_Master>(foes))
                {
                    Link_Stats s;
                    if (e != null && e.Beingserver != null && !e.Beingserver.Dead && e.Statsserver != null && e.Statsserver.TryGetValue("Speed", out s) && s.Value_Multi < 0.99f) slowed++;
                    if (t >= sendAt && sent < 40 && e != null && e.Beingserver != null && !e.Beingserver.Dead && !e.Taglistserver.Contains("Enemycore") && Time.frameCount % 30 == 0)
                    {
                        Entity_Master by = RWRun.Vessel(friend) ?? RWRun.Vessel(RWMine.Session);
                        if (by != null) { ModKitUtil.Debuff(by, e, "Debuff_Slow", 6f); sent++; }
                    }
                }
            if (slowed > mostSlowed) mostSlowed = slowed;
            if (Game_Mapvars.Instance != null && Game_Mapvars.Instance.Clientsendstack > mostStack) mostStack = Game_Mapvars.Instance.Clientsendstack;
            t += Time.deltaTime;
            yield return null;
        }
        Info("the big fight on the host", "foes slowed at once, at most " + mostSlowed + "; the game's look queue at most " + mostStack + " of 50; looks the test sent from 20 s on: " + sent + "; " + RWVote.Foes() + " foes up, the map " + Game_Mapvars.Instance.Gamecomplete.Value);
    }

    // MODE-B2's reading on a machine that has just rejoined: a client half holding the same visual script twice, a being listed twice.
    static string Doubles(out int doubled)
    {
        doubled = 0;
        int halves = 0, twice = 0;
        List<string> names = new List<string>();
        foreach (Entity_Master_Client c in UnityEngine.Object.FindObjectsOfType<Entity_Master_Client>())
        {
            halves++;
            List<string> seen = new List<string>();
            bool hit = false;
            foreach (MonoBehaviour b in c.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (b == null) continue;
                string t = b.GetType().Name;
                if (!t.StartsWith("Entity_Vis")) continue;
                if (seen.Contains(t)) { hit = true; if (names.Count < 6) names.Add(c.name + ":" + t); }
                else seen.Add(t);
            }
            if (hit) doubled++;
        }
        List<Entity_Master> listed = new List<Entity_Master>();
        foreach (Entity_Master e in Game_Entity.Instance.Entityclient)
        {
            if (listed.Contains(e)) twice++;
            else listed.Add(e);
        }
        doubled += twice;
        return halves + " client halves, " + doubled + " with a double, " + twice + " beings listed twice: " + string.Join(", ", names.ToArray());
    }

    static string CardinalRow()
    {
        List<string> row = new List<string>();
        if (Tds_Multiplayerinfo.Instance != null)
            foreach (FixedString64Bytes a in Tds_Multiplayerinfo.Instance.Artifacts) row.Add(a.ToString());
        return row.Count + " [" + string.Join(",", row.ToArray()) + "]";
    }

    // ---------------------------------------------------------------- after the first rejoin: the host

    static IEnumerator ReviewHost(long me, long friend)
    {
        Game_Mapvars vars = Game_Mapvars.Instance;
        vars.Time_Scale.Value = 1f;
        // MODE-C9 and C10: after the rejoin, one key for the friend, and the Cardinal row as the host has it.
        Reading("MODE-C9", RWRun.For(friend).Key != RWTwo.SecondGuid, "the friend's key after the rejoin: " + RWRun.For(friend).Key + ", " + RWRun.Players.Count + " players held");
        Info("MODE-C10 the host's row", CardinalRow());

        // MODE-D4: the friend's vessel goes down in the last seconds of Tide 15; the vote to leave opens while it is down.
        yield return Sync("d4");
        ClearFoes();
        vars.Entry.Value = 2 * RWCampaign.WinTide - 1;
        vars.Wave.Value = RWCampaign.WinTide - 1;
        yield return new WaitForSeconds(0.5f);
        int breaks = RWVote.Breaks;
        freeze = false;
        forcing = false;
        vars.Time_Wave.Value = 0.1f;
        yield return Until(() => vars.Wave.Value >= RWCampaign.WinTide, 30f);
        yield return new WaitForSeconds(1f);
        // The vote opens on an empty field: a foe stays up until the break is here and the vessel is down, so it opens as it falls.
        List<Entity_Master> enemyCores = RWUtil.EnemyCores();
        Entity_Master keeper = enemyCores.Count > 0 ? RogueWavesModeKitTest.SpawnFoe("Enemy_Dagger", enemyCores[0].Entityposition.Value, 2f, 10f, null) : null;
        forcing = true;
        float t = 0f;
        while (RWVote.Breaks == breaks && t < 60f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        forcing = false;
        vars.Time_Wave.Value = RWMode.BreakSeconds;
        Entity_Master hisVessel = RWRun.Vessel(friend);
        if (hisVessel != null && hisVessel.Beingserver != null) Game_Entity.Instance.Despawn(hisVessel.Beingserver, true, "RogueWaves");
        yield return null;
        ClearFoes();
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 20f);
        bool down = RWRun.Vessel(friend) == null;
        if (RWAsk.Open == RWVote.LeaveKind) yield return Say("d4open");
        if (RWAsk.Open == RWVote.LeaveKind)
        {
            yield return WaitDialog(8f);
            ClickCard("Leave");
        }
        else Setup("MODE-D4", "the vote never opened: state '" + RWVote.State + "', won " + RWCampaign.WonThisMap);
        yield return Until(() => RWVote.State == "", RWVote.LeaveSeconds + 15f);
        Info("MODE-D4 on the host", "the friend's vessel down as the vote opened: " + down + "; the vote: " + RWVote.LastVote + ", departing " + RWVote.Departing);
        yield return Until(() => RWRun.Vessel(friend) != null, 70f);
        freeze = true;
        yield return Say("d4over");
        yield return Until(() => RWTwo.OtherSaid("d4read"), 120f);

        // MODE-D7: the host answers the vote, opens SHOP; the other player's answer closes the question, and the shop should stay.
        yield return ToBreakAfter(vars.Wave.Value + 1);
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 20f);
        if (RWAsk.Open != RWVote.LeaveKind) Setup("MODE-D7", "no vote to leave: state '" + RWVote.State + "'");
        else
        {
            yield return WaitDialog(8f);
            ClickCard("Stay");
            yield return new WaitForSeconds(0.6f);
            RWShop.Open("Shop");
            yield return WaitDialog(6f);
            bool shopUp = RWDialog.Up();
            yield return Say("d7");
            yield return Until(() => RWVote.State == "", 30f);
            yield return new WaitForSeconds(1f);
            Reading("MODE-D7", shopUp && !RWDialog.Up(), "SHOP up as the other player answered: " + shopUp + "; a second on: " + RWDialog.Up() + "; the vote: " + RWVote.LastVote);
            if (RWDialog.Up()) ClickCard("Back");
        }
        yield return Say("d7over");
        yield return new WaitForSeconds(1f);

        // MODE-B4 and B3: O5's mid-map join, which then joins on the boat for the next map.
        Line("DRIVER KILL");
        yield return Until(() => !Blm_Server.Instance.Sessionids.ContainsKey(friend), 40f);
        yield return new WaitForSeconds(2f);
        int turned = RWRejoin.Turned;
        Line("DRIVER START stranger");
        yield return Until(() => RWRejoin.Turned > turned, 300f);
        float turnedAt = Time.realtimeSinceStartup;
        Info("MODE-B3", "the stranger was turned away, " + RWRejoin.Turned + " so far; the party goes home");
        RWCampaign.Choice = RWCampaign.Open()[0].Col;     // a real leave's map vote has picked it by now
        yield return Win();
        yield return new WaitForSeconds(5f);
        Macro_Main old = Macro_Main.Instance;
        Tds_Multiplayer_Remote.Instance.Returnmap_ServerRpc(true, default(Unity.Netcode.ServerRpcParams));
        yield return Until(() => Macro_Main.Instance != null && Macro_Main.Instance != old && Macro_Main.Instance.Ready, 150f);
        Macro_Main boat = Macro_Main.Instance;
        yield return Until(() => Here(), 40f);
        yield return Until(() => PlayersIn() >= 2, 150f);
        Info("MODE-B3 on the boat", PlayerList() + ", " + RWUtil.F(Time.realtimeSinceStartup - turnedAt) + " s after the turn-away");
        RWCampaign.Choice = RWCampaign.Open()[0].Col;
        yield return Until(() => boat.Selected.Value.ToString() != "" && Macro_Worldvisual.Instance.Insetup, 60f);
        yield return OneWay("mapb3");
        boat.Playgame(true);
        yield return Until(() => LoadedHere() && RWRun.InCampaignMap && RWCampaign.Playing >= 0, 240f);
        Info("MODE-B3 the next map is up", RWUtil.F(Time.realtimeSinceStartup - turnedAt) + " s after the turn-away");
        yield return Until(() => RWTwo.OtherSaid("b3read"), 120f);
    }

    // ---------------------------------------------------------------- the second player

    static IEnumerator ReviewSecond()
    {
        yield return Sync("d4");
        bool opened = false, cameBack = false;
        Entity_Master first = RWMine.Vessel();
        float t = 0f;
        float backAt = -1f;
        while (!RWTwo.OtherSaid("d4over") && t < 150f)
        {
            Entity_Master now = RWMine.Vessel();
            if (now != null && now != first && backAt < 0f) { cameBack = true; backAt = t; }
            if (RWAsk.Open == RWVote.LeaveKind && !opened)
            {
                opened = true;
                yield return WaitDialog(5f);
                ClickCard("Stay");      // the host leaves, this player stays: the party stays for what comes next
            }
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(2f);
        if (!RWTwo.OtherSaid("d4open")) Setup("MODE-D4", "the host's vote never opened");
        else Reading("MODE-D4", !opened, "the vote to leave's dialog opened on this machine: " + opened + "; the respawned vessel stood after " + RWUtil.F(backAt) + " s: " + cameBack + "; RWAsk.Open now '" + RWAsk.Open + "'");
        yield return Say("d4read");
        // MODE-D7: this player answers once the host's shop is up.
        yield return Until(() => RWAsk.Open == RWVote.LeaveKind, 150f);
        if (RWAsk.Open == RWVote.LeaveKind)
        {
            yield return WaitDialog(8f);
            yield return Until(() => RWTwo.OtherSaid("d7"), 40f);
            ClickCard("Stay");
        }
        yield return new WaitForSecondsRealtime(600f);     // the host kills this copy next
    }

    // The stranger of phase "stranger": turned away once, its main menu read, then back every few seconds until the host's party is on the boat.
    static IEnumerator ReviewStranger()
    {
        int loadAt = -1, rejoinAt = -1, index = 0;
        Application.LogCallback watch = (text, stack, type) =>
        {
            index++;
            if (text == null) return;
            if (text.Contains("Loading 80%") && loadAt < 0) loadAt = index;
            if (text.Contains("rejoin: map loaded here") && rejoinAt < 0) rejoinAt = index;
        };
        Application.logMessageReceived += watch;
        for (int i = 1; i <= 30; i++)
        {
            yield return RWTwo.Join();
            yield return Until(() => Unity.Netcode.NetworkManager.Singleton.IsConnectedClient, 30f);
            yield return new WaitForSecondsRealtime(4f);
            yield return Until(() => UnityEngine.Object.FindObjectOfType<Mainmenu_Main>() != null || (Macro_Main.Instance != null && Macro_Main.Instance.Ready), 90f);
            if (Macro_Main.Instance != null && Macro_Main.Instance.Ready) break;
            yield return new WaitForSecondsRealtime(3f);
            string box = ErrorBox();
            Reading("MODE-B4 try " + i, box.Contains("Lost the map"), "the turned-away player's error box: '" + box + "'");
            if (i == 1)
            {
                Mainmenu_Main menu = UnityEngine.Object.FindObjectOfType<Mainmenu_Main>();
                if (menu != null)
                {
                    menu.SendMessage("Transitiontomain", SendMessageOptions.DontRequireReceiver);
                    yield return new WaitForSecondsRealtime(4f);
                    Blockage.Menu.Bl_Menus.Changemenu("Main", true);
                    yield return new WaitForSecondsRealtime(2f);
                    Info("MODE-B4 on the main menu", ErrorBox());
                }
            }
            yield return new WaitForSecondsRealtime(2f);
        }
        Macro_Main boat = Macro_Main.Instance;
        if (boat == null || !boat.Ready) { Setup("MODE-B3", "never reached the boat"); yield break; }
        yield return new WaitForSecondsRealtime(2f);
        boat.Selectcharacter(RWTwo.SecondHero);
        yield return OneWay("mapb3");
        loadAt = rejoinAt = -1;     // a turned-away join's lines don't count: only the next map's
        boat.Playgame(true);
        yield return Until(() => LoadedHere(), 240f);
        yield return new WaitForSecondsRealtime(25f);
        Application.logMessageReceived -= watch;
        Reading("MODE-B3", rejoinAt >= 0 && (loadAt < 0 || rejoinAt < loadAt), "on the next map: the 'rejoin: map loaded here' line at log line " + rejoinAt + ", 'Loading 80%' at " + loadAt + " (-1: none)");
        yield return Say("b3read");
        yield return new WaitForSecondsRealtime(600f);
    }
}
#endif
