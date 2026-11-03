// Kit 1.7 (2026-11-03). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared kit: the scope's F-key fix (docs/15 section 9, RogueLab's RogueLabFocusFix). After a respawn the game's
// Focus (F) can select a minion tagged "Hero" (a Boutbro) instead of the vessel: the new vessel entity is appended to
// Entityclienttags["Hero"] behind it. On the first F after the local vessel respawned, a handler added after the
// game's own selects the vessel and moves it to the front of that list, so later presses cycle from it. Client-side.
// Every vessel part runs it; the registry (ModKitPack.Mark) lets only the first part install it, so it runs once.
public static class ModKitFocus
{
    public static bool Respawned;
    public static int Fixes;
    static bool installed, mine;
    static Entity_Master lastHero;
    static float nextLook;

    // Call every frame (any machine).
    public static void Update()
    {
        if (!installed)
        {
            installed = true;
            try
            {
                // The build scripts rename the kit's prefix inside strings too: this mark's name must not hold it (kit 1.7).
                if (!ModKitPack.Mark("Focusfix"))
                    return;
                Blockage.Input focus = Blockage.Bl_Inputs.Actions["Focus"];
                focus.Function = (Blockage.Input.Pressfunct)Delegate.Combine(focus.Function, new Blockage.Input.Pressfunct(After));
                mine = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[RogueWaves] F-key fix not installed: " + e.Message);
            }
        }
        if (!mine || Time.unscaledTime < nextLook)
            return;
        nextLook = Time.unscaledTime + 0.5f;
        Entity_Master hero = MainHero();
        if (hero != null && lastHero != null && hero != lastHero)
            Respawned = true;
        if (hero != null)
            lastHero = hero;
    }

    static void After(string action)
    {
        if (action != "Onpressed" || !Respawned)
            return;
        try
        {
            Entity_Master hero = MainHero();
            List<Entity_Master> list;
            if (hero == null || Game_Selection.Instance == null || !Game_Entity.Instance.Entityclienttags.TryGetValue("Hero", out list))
                return;
            Respawned = false;
            list.Remove(hero);
            list.Insert(0, hero);
            Game_Selection.Instance.Selected = hero;
            Game_Selection.Instance.Select();
            Fixes++;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[RogueWaves] F-key fix: " + e.Message);
        }
    }

    // The local player's own vessel: its being is the one the player picked (Tds_Clientinfo.Hero).
    public static Entity_Master MainHero()
    {
        Blm_Server server = Blm_Server.Instance;
        if (server == null || server.Localplayer == null || Game_Entity.Instance == null)
            return null;
        Tds_Clientinfo info = server.Localplayer.GetComponent<Tds_Clientinfo>();
        List<Entity_Master> list;
        if (info == null || !Game_Entity.Instance.Entityclienttags.TryGetValue("Hero", out list))
            return null;
        Beingobject being = Blockage.Address.Bl_Address.Getload("Hero", info.Hero.Value.ToString()) as Beingobject;
        long me = server.Localplayer.Sessionid.Value;
        foreach (Entity_Master e in list)
            if (e != null && being != null && e.Entitydata.Value.Owner == me && e.Being.Value.ToString() == being.Objectname)
                return e;
        return null;
    }
}
