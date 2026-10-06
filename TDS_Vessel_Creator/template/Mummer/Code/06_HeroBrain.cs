// Server-side brain for the vessel: build options, the Thread counter and HOOK. It plays the part of a vessel's own
// AI (Entity_Ai_Hero_<Name>). Attacking is left to the stock Entity_Ai_Mob_Combat, exactly like Smacky; movement,
// building and selling are the engine's generic hero AIs.
public class MummerHeroBrain : MonoBehaviour
{
    public Entity_Master Master;
    public MummerPlayer Player;
    public int Hooks;
    public float HookReady;
    public string LastReel = "";
    bool started, hookOff;
    Link_Modifier threadCounter;
    Link_Command hookCommand;
    Entity_Master reeling;
    Vector3 reelTo;
    float reelUntil;

    void Update()
    {
        if (Master == null)
        {
            Destroy(this);
            return;
        }
        if (!MummerUtil.IsServer)
            return;
        if (started)
        {
            if (hookOff && Time.time >= HookReady)
                SetHookButton(false);
            if (reeling != null)
                KeepReeling();
            return;
        }
        try
        {
            if (Master.Statsserver == null || Master.Statsserver.Count == 0 || Master.Commandlistserver == null || Master.Radio == null)
                return;
            Begin();
        }
        catch (Exception e)
        {
            MummerLog.Warn("hero brain: " + e.Message);
            enabled = false;
        }
    }

    void Begin()
    {
        started = true;
        MummerServer.Heroes.Add(this);
        for (int i = 0; i < MummerData.Towers.Length; i++)
        {
            string link = "Buildoption" + (i + 1);
            Link_Command c = Helper_Entitytools.Createcommand(Master, "Command_" + link, link, false, true);
            c.Goalstring = MummerData.Towers[i];
            c.Goalvalue = 1f;
            Helper_Entitytools.Updatecommand(Master, link, "", true, true);
        }
        if (MummerContent.Hook != null)
        {
            hookCommand = Helper_Entitytools.Createcommand(Master, MummerData.HookCommand, MummerData.HookLink, false, true);
            Helper_Entitytools.Updatecommand(Master, MummerData.HookLink, "", true, true);
        }
        Player = MummerServer.Player(MummerUtil.Owner(Master));
        Player.Hero = this;
        threadCounter = MummerUtil.Counter(Master, "Thread", Player.Thread);
        Master.Command += OnCommand;
        MummerLog.Info("Mummer landed for player " + MummerUtil.Owner(Master));
    }

    public void ShowThread()
    {
        if (Master != null && threadCounter != null)
            MummerUtil.SetCounter(Master, threadCounter, "Thread", Player.Thread);
    }

    // ---------------------------------------------------------------- HOOK

    // The button greys out while HOOK recoils: its Goalstring is "Off", synced through Commandlist (Run=false, so the
    // command is not cast again).
    void SetHookButton(bool off)
    {
        hookOff = off;
        if (hookCommand == null)
            return;
        hookCommand.Goalstring = off ? "Off" : "";
        Helper_Entitytools.Updatecommand(Master, MummerData.HookLink, "", false, false);
    }

    void OnCommand(string command, string json, bool server)
    {
        if (command != MummerData.HookLink)
            return;
        try { Hook(Picked(json)); }
        catch (Exception e) { MummerLog.Warn("hook: " + e.Message); }
    }

    // The foe the player clicked (MummerHookSelect sends its NetworkObjectId as {"Ref":id}); null if none.
    static Entity_Master Picked(string json)
    {
        int at = json != null ? json.IndexOf("\"Ref\":") : -1;
        if (at < 0)
            return null;
        int i = at + 6, j = i;
        while (j < json.Length && char.IsDigit(json[j])) j++;
        ulong id;
        if (j == i || !ulong.TryParse(json.Substring(i, j - i), out id))
            return null;
        return MummerUtil.ById(id);
    }

    // Casts a thread at the picked foe (or, from the hotkey with nothing picked, the nearest one): it is reeled up to
    // two tiles toward Mummer, held for a moment and Marked. Foes as heavy as a boss are Marked but not moved.
    public void Hook(Entity_Master foe)
    {
        if (Time.time < HookReady)
        {
            MummerUtil.Warn(Master, MummerData.WarnRecoil);
            return;
        }
        Vector3 pos = MummerUtil.Pos(Master);
        int team = MummerUtil.Team(Master);
        if (foe != null && !MummerUtil.IsFoe(foe, team))
            foe = null;
        if (foe == null)
        {
            List<Entity_Master> near = MummerUtil.FoesNear(Master, pos, MummerData.HookRange);
            foreach (Entity_Master f in near)
                if (foe == null && !MummerServer.IsMarked(f)) foe = f;
            if (foe == null && near.Count > 0) foe = near[0];
        }
        if (foe == null)
        {
            MummerUtil.Warn(Master, MummerData.WarnNoFoe);
            return;
        }
        if (Vector3.Distance(MummerUtil.Pos(foe), pos) > MummerData.HookRange + 0.6f)
        {
            MummerUtil.Warn(Master, MummerData.WarnFar);
            return;
        }
        HookReady = Time.time + MummerData.HookCooldown;
        Hooks++;
        SetHookButton(true);
        Vector3 dir = MummerUtil.Pos(foe) - pos;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            Master.Entitydirection.Value = dir.normalized;
        bool heavy = MummerServer.TooHeavy(foe);
        Vector3 to = heavy ? MummerUtil.Pos(foe) : Reel(foe, pos);
        // Clients: Mummer plays its Hook animation and a thread line runs from Mummer to the foe while it is reeled.
        Master.Radio.Vis_ClientRpc("Mummer_Hook", MummerUtil.Pos(foe), (float)foe.NetworkObjectId);
        MummerServer.Mark(foe, MummerUtil.Owner(Master), MummerData.MarkDuration, Master);
        Entity_Master caught = foe;
        MummerUtil.Later(this, heavy ? 0.1f : MummerData.HookReelTime, () => MummerUtil.Hold(Master, caught, MummerData.HookRoot, false, null));
        if (heavy)
            MummerUtil.Warn(Master, MummerData.WarnHeavy);
    }

    // The foe walks itself up to HookReel tiles toward Mummer on its own movement (boosted for a moment), so the
    // reel is smooth on every screen and the foe's AI picks its path up again from where it lands.
    Vector3 Reel(Entity_Master foe, Vector3 hero)
    {
        Link_Being being = foe.Beingserver;
        if (being == null || being.Currentblock == null || foe.Commandlistserver == null || !foe.Commandlistserver.ContainsKey("Move"))
        {
            LastReel = "no Move command";
            return MummerUtil.Pos(foe);
        }
        Vector3 from = being.Currentblock.Position;
        Vector3 dir = hero - from;
        dir.y = 0f;
        if (dir.magnitude < 1.4f)
        {
            LastReel = "already close";
            return MummerUtil.Pos(foe);
        }
        dir.Normalize();
        string tried = "";
        for (int k = MummerData.HookReel; k >= 1; k--)
        {
            Genitem block = Tile(from + dir * k);
            if (block == null || block == being.Currentblock) { tried += " k" + k + ":no tile"; continue; }
            Genitem stand = null;
            try { stand = Helper_Gridtools.Checkifstandable(block, being); } catch (Exception) { }
            if (stand == null) { tried += " k" + k + ":not standable"; continue; }
            if (Vector3.Distance(stand.Position, hero) < 0.9f) { tried += " k" + k + ":on Mummer"; continue; }
            Link_Path path = Helper_Navtools.Getpath(being.Currentblock, stand, being, false);
            if (!path.Valid || path.Length > k + 2) { tried += " k" + k + ":path " + (path.Valid ? path.Length.ToString() : "none"); continue; }
            reeling = foe;
            reelTo = stand.Position;
            reelUntil = Time.time + MummerData.HookReelTime;
            Helper_Entitytools.Addmod(foe, "Speed", 5f, Helper_Entitytools.Modtypes.Multi, MummerData.HookReelTime);
            Steer(foe);
            LastReel = "k" + k + " to " + reelTo.ToString("F1") + " (path " + path.Length + ")" + tried;
            return stand.Position;
        }
        LastReel = "no tile:" + tried;
        return MummerUtil.Pos(foe);
    }

    // The walkable tile at a point: the grid is looked up by exact tile position (Getblock rounds to the hundredth), so
    // the point snaps to the tile centre first, at the foe's level or one step down or up (the ground's Surface entity
    // carries the tile a foe stands on).
    static Genitem Tile(Vector3 at)
    {
        Link_Entityquery surface = MummerUtil.Tagged("Surface");
        foreach (float dy in new[] { 0f, -1f, 1f })
        {
            Vector3 p = new Vector3(Mathf.Round(at.x), Mathf.Round(at.y) + dy, Mathf.Round(at.z));
            Entity_Master s = Helper_Entityquerytools.Entitynearest(p, 0.5f, surface);
            if (s != null && s.Beingserver != null && s.Beingserver.Currentblock != null)
                return s.Beingserver.Currentblock;
        }
        return null;
    }

    void Steer(Entity_Master foe)
    {
        Link_Command move = foe.Commandlistserver["Move"];
        move.Goalobject = null;
        move.Goalposition = reelTo;
        move.Goalvalue = 0f;
        move.Goalstring = "Idle";
        Helper_Entitytools.Updatecommand(foe, "Move", "", true, true);
    }

    // A foe's own combat AI re-targets every slow tick (a foe in melee would turn straight back to its fight), so for
    // the moment of the reel the thread wins: any order the foe gives itself is replaced by the walk to the reel spot.
    void KeepReeling()
    {
        Entity_Master foe = reeling;
        Vector3 at = foe != null && MummerUtil.Alive(foe) ? MummerUtil.Pos(foe) : reelTo;
        Vector2 gap = new Vector2(at.x - reelTo.x, at.z - reelTo.z);
        if (foe == null || !MummerUtil.Alive(foe) || Time.time > reelUntil || gap.sqrMagnitude < 0.06f)
        {
            reeling = null;
            return;
        }
        Link_Command move = foe.Commandlistserver["Move"];
        if (foe.State.Value.ToString() != "Move" || move.Goalobject != null || (move.Goalposition - reelTo).sqrMagnitude > 0.01f)
            Steer(foe);
    }
}
