// Server-side brain for the vessel: the build menu, its own Light, PLUG IN and the Directions. It plays the part of a
// vessel's own AI (Entity_Ai_Hero_<Name>). Attacking is the stock Entity_Ai_Mob_Ranged, exactly like Sno Sno: the brain only
// names its shot (the spark); movement, building and selling are the engine's generic hero AIs.
public class UloeHeroBrain : MonoBehaviour
{
    public Entity_Master Master;
    public UloePlayer Player;
    public Entity_Master Plug;      // what the cord is in; null when unplugged
    Vector3 plugAt;                 // where it was last seen, for the unplug effects once it's gone
    public float PlugReady, LightNow, RadiusNow;
    public int Plugs, Unplugs, Snaps, Refused;
    public bool Armed;
    public string LastPlug = "";
    readonly UloeLight light = new UloeLight();
    Link_Command plugCommand;
    bool started, off;
    float nextFeed, nextCheck, nextMenu, nextLink;
    Entity_Master.Interacts dealt;

    public long Owner { get { return ModKitUtil.Owner(Master); } }
    public float Reach { get { return UloeData.PlugRange + UloeData.ExtensionReach[Player != null ? Player.Extension : 0]; } }

    void Update()
    {
        if (Master == null)
        {
            Destroy(this);
            return;
        }
        if (!ModKitUtil.IsServer)
            return;
        try
        {
            if (!started)
            {
                if (Master.Statsserver == null || Master.Statsserver.Count == 0 || Master.Commandlistserver == null || Master.Radio == null)
                    return;
                Begin();
            }
            if (!Armed)
                Arm();
            if (off && Time.time >= PlugReady)
                SetButton();
            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + 0.2f;
                CheckCord();
            }
            if (Time.time >= nextMenu)
            {
                nextMenu = Time.time + 0.5f;
                CheckDirections();
                ModKitUtil.Listen(Master, dealt);
            }
            if (Time.time >= nextFeed)
            {
                nextFeed = Time.time + UloeData.LightTick;
                Feed();
            }
            if (Plug != null && Time.time >= nextLink)
                ShowCord();
        }
        catch (Exception e)
        {
            UloeLog.Warn("hero brain: " + e.Message);
            enabled = false;
        }
    }

    void Begin()
    {
        started = true;
        UloeServer.Heroes.Add(this);
        Player = UloeServer.Player(Owner);
        Player.Hero = this;
        for (int i = 0; i < UloeData.Towers.Length; i++)
        {
            string link = "Buildoption" + (i + 1);
            Link_Command c = Helper_Entitytools.Createcommand(Master, "Command_" + link, link, false, true);
            c.Goalstring = UloeData.Towers[i];
            c.Goalvalue = 1f;
            Helper_Entitytools.Updatecommand(Master, link, "", true, true);
        }
        CheckDirections();
        if (UloeContent.Plug != null)
        {
            plugCommand = Helper_Entitytools.Createcommand(Master, UloeData.PlugCommand, UloeData.PlugLink, false, true);
            Helper_Entitytools.Updatecommand(Master, UloeData.PlugLink, "", true, true);
        }
        dealt = OnDealt;
        // Subscribed after the Updatecommand calls above, which run each command once.
        Master.Command += OnCommand;
        UloeLog.Info("Uloe landed for player " + Owner);
    }

    // Entity_Ai_Mob_Ranged creates the Attack command when it spawns; give it the spark (Hero_Snosno.Starting does the same
    // with its snowball). The spark's hits carry no Damage, so the game fills in Uloe's own.
    void Arm()
    {
        Link_Command attack;
        if (!Master.Commandlistserver.TryGetValue("Attack", out attack) || attack == null)
            return;
        attack.Goalstring = UloeContent.SparkReady ? UloeData.Spark : UloeData.SparkFallback;
        Armed = true;
    }

    // The Directions this vessel holds (kit 1.7), for what its player's Power Banks and Plug In read.
    void CheckDirections()
    {
        Player.Extension = ModKitDirections.Rank(Master, UloeData.Part, UloeData.Extension);
        Player.Bright = ModKitDirections.Rank(Master, UloeData.Part, UloeData.Bright);
        Player.Batteries = ModKitDirections.Rank(Master, UloeData.Part, UloeData.Batteries);
        Player.Flash = ModKitDirections.Rank(Master, UloeData.Part, UloeData.Flash);
    }

    // Flash Bulb: a spark that lands marks the foe, by rank.
    void OnDealt(Link_Interact hit)
    {
        try
        {
            if (Player == null || Player.Flash <= 0 || hit == null || hit.Type != "Hurt" || !ModKitUtil.IsFoe(hit.To, ModKitUtil.Team(Master)))
                return;
            int lit = UloeData.FlashLit[Player.Flash], blind = UloeData.FlashBlind[Player.Flash];
            if (lit > 0) UloeMarks.Illuminate(Master, hit.To, lit);
            if (blind > 0) UloeMarks.Blind(Master, hit.To, blind);
        }
        catch (Exception e)
        {
            UloeLog.Warn("flash bulb: " + e.Message);
        }
    }

    public string Menu()
    {
        List<string> l = new List<string>();
        for (int i = 1; i <= 4; i++)
        {
            Link_Command c;
            if (Master.Commandlistserver.TryGetValue("Buildoption" + i, out c) && c != null)
                l.Add(c.Goalstring);
        }
        return ModKitUtil.Join(l);
    }

    // ---------------------------------------------------------------- Light

    // Light a node gives Plug In: from its total aura; a Power Bank's: from its upgrades.
    public static float PlugLight(Entity_Master target)
    {
        UloeBankBrain bank = target != null ? target.GetComponent<UloeBankBrain>() : null;
        if (bank != null)
            return UloeData.BankBase + UloeData.BankPerUpgrade * bank.UpgradeCount;
        return Mathf.Min(UloeData.NodeMax, UloeData.NodeBase + UloeData.NodePerAura * UloeUtil.NodeAura(target));
    }

    void Feed()
    {
        if (!ModKitUtil.Alive(Master))
            return;
        LightNow = (Plug != null ? PlugLight(Plug) : UloeData.HeroLight) + UloeData.BrightLight[Player.Bright];
        RadiusNow = Plug != null ? UloeData.PlugRadius : UloeData.HeroRadius;
        UloeServer.Shine(light, Master, ModKitUtil.Pos(Master), RadiusNow, LightNow, null);
    }

    // ---------------------------------------------------------------- commands

    void OnCommand(string command, string json, bool server)
    {
        if (command != UloeData.PlugLink)
            return;
        try
        {
            if (json != null && json.IndexOf("\"Unplug\"", StringComparison.Ordinal) >= 0)
                Unplug(false);
            else
                PlugIn(UloeUtil.Picked(json));
        }
        catch (Exception e)
        {
            UloeLog.Warn("command " + command + ": " + e.Message);
        }
    }

    // One of this player's Power Banks, or a node.
    public bool Pluggable(Entity_Master m)
    {
        if (!ModKitUtil.Alive(m))
            return false;
        UloeBankBrain bank = m.GetComponent<UloeBankBrain>();
        if (bank != null)
            return bank.Started && bank.Owner == Owner;
        return UloeUtil.IsNode(m);
    }

    // The nearest node or Power Bank of this player's in the cord's reach.
    Entity_Master Nearest(Vector3 pos)
    {
        Entity_Master best = UloeUtil.NearestNode(pos, Reach);
        float bestD = best != null ? ModKitUtil.Flat(ModKitUtil.Pos(best), pos) : Reach;
        foreach (UloeBankBrain b in UloeServer.Owned<UloeBankBrain>(Owner))
        {
            float d = ModKitUtil.Flat(b.Pos, pos);
            if (d <= bestD)
            {
                best = b.Master;
                bestD = d;
            }
        }
        return best;
    }

    // Plugs into what was picked, or (the hotkey with nothing under the cursor) the nearest in reach. Pressed while plugged
    // into something else, the cord moves. The server checks every pick.
    public void PlugIn(Entity_Master target)
    {
        if (Time.time < PlugReady)
        {
            Refused++;
            LastPlug = "cooling down";
            UloeUtil.Warn(Master, UloeData.WarnCooldown);
            return;
        }
        Vector3 pos = ModKitUtil.Pos(Master);
        if (target != null && !Pluggable(target))
            target = null;
        if (target == null)
            target = Nearest(pos);
        if (target == null)
        {
            Refused++;
            LastPlug = "nothing in reach";
            UloeUtil.Warn(Master, UloeData.WarnNoPlug);
            return;
        }
        if (ModKitUtil.Flat(ModKitUtil.Pos(target), pos) > Reach + UloeData.PlugSlack)
        {
            Refused++;
            LastPlug = "too far";
            UloeUtil.Warn(Master, UloeData.WarnFar);
            return;
        }
        Plug = target;
        plugAt = ModKitUtil.Pos(target);
        Plugs++;
        LastPlug = ModKitUtil.BeingName(target) + " Light " + PlugLight(target).ToString("0.##");
        ModKitUtil.Skin(Master, UloeData.Plugged);
        SetButton();
        Vector3 dir = ModKitUtil.Pos(target) - pos;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            Master.Entitydirection.Value = dir.normalized;
        ModKitUtil.Vis(Master, "Uloe_Plugin", ModKitUtil.Pos(target), 0f);
        ShowCord();
        nextFeed = 0f;
    }

    // A sold or destroyed Bank still holds its reference here but reads == null (Unity's ==), so the tests below ask the
    // reference itself, or the cord would stay in it for good (ULO-1).
    public void Unplug(bool snapped)
    {
        if ((object)Plug == null)
            return;
        Vector3 at = ModKitUtil.Alive(Plug) ? ModKitUtil.Pos(Plug) : plugAt;
        Plug = null;
        Unplugs++;
        PlugReady = Time.time + UloeData.PlugCooldown;
        if (snapped)
        {
            Snaps++;
            UloeUtil.Warn(Master, UloeData.WarnSnapped);
        }
        ModKitUtil.Skin(Master, "");
        SetButton();
        ModKitUtil.Vis(Master, "Uloe_Plugcord", at, -1f);
        ModKitUtil.Vis(Master, "Uloe_Ring", at, -1f);
        ModKitUtil.Vis(Master, "Uloe_Unplug", at, snapped ? 1f : 0f);
        nextFeed = 0f;
    }

    // The cord: gone with what it was in, or snapped when Uloe walks past its length.
    void CheckCord()
    {
        if ((object)Plug == null)
            return;
        if (ModKitUtil.Alive(Plug))
            plugAt = ModKitUtil.Pos(Plug);
        if (!ModKitUtil.Alive(Master) || !ModKitUtil.Alive(Plug) || ModKitUtil.Flat(ModKitUtil.Pos(Plug), ModKitUtil.Pos(Master)) > Reach + UloeData.PlugSlack)
            Unplug(true);
    }

    // Told again every few seconds, so a player who joins late still sees the cord; a client lets it go if it stops hearing.
    void ShowCord()
    {
        nextLink = Time.time + UloeData.LinkResend;
        if (Plug == null)
            return;
        ModKitUtil.Vis(Master, "Uloe_Plugcord", ModKitUtil.Pos(Plug), 1f);
        ModKitUtil.Vis(Master, "Uloe_Ring", ModKitUtil.Pos(Master), UloeData.PlugRadius);
    }

    // The button: "On" while plugged (it reads UNPLUG), "Off" while cooling (grey), both synced through Commandlist with
    // Run false, so the command is not cast again.
    void SetButton()
    {
        off = Plug == null && Time.time < PlugReady;
        if (plugCommand == null)
            return;
        plugCommand.Goalstring = Plug != null ? "On" : off ? "Off" : "";
        Helper_Entitytools.Updatecommand(Master, UloeData.PlugLink, "", false, false);
    }
}
