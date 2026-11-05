// Mirror: a wall that Light empowers. Its rules ride on the game's own hit event (Yossele's, Argo's and Spool's pattern):
// Helper_Entitytools.Interacthurt hands each hit to the target's Oninteract chain, and the Mirror's handler goes first.
//   Lit             a foe whose hit lands is Blinded at the level the Mirror's Light gives; no Light, nothing.
//   Silver Backing  that Blind one level stronger.
//   Vanity Lights   Illuminates what hits it, in place of Blind.
//   Glare           a hit from a foe with Blind level N has an N x 20% chance to do nothing.
//   Bounce Card     a share of the Light it receives goes on to your buildings around it, never to another Bounce Card.
//   Tempered Glass  more health (a stat Pmulti) and 20% less damage from each hit.
//   Hall of Mirrors the mark it gives also lands on every foe near the attacker.
//   Reflection      a share of what each hit does goes back at the attacker, more in bright Light.
//   Heliostat       once a second it beams the nearest foe for Light x 1.5 and gives it its mark.
// Helper_Entitytools.Updateinteract rebuilds the chain whenever an upgrade or effect lands, so the handler is moved back to the
// front ten times a second.
public class UloeMirrorBrain : UloeBuilding
{
    public int Flashes, Nullified, Hits, Spread, Reflected, Heliobeams;
    public float Taken, Cut, ReflectedDamage;
    public Entity_Master LastAttacker;
    public static float ForceRoll = -1f;       // the self-test's dice; below 0 the real ones
    readonly UloeLight relay = new UloeLight();
    Entity_Master.Oninteracts handler;
    float nextFront, nextFeed, nextHelio, lastFlashVis;

    // The strength its Light gives: 0 unlit, then levels 1 to 3.
    public int Level
    {
        get
        {
            float l = Light;
            return l <= 0f ? 0 : l >= UloeData.MirrorLevel3 ? 3 : l >= UloeData.MirrorLevel2 ? 2 : 1;
        }
    }

    public float RelayNow { get { return Has("Bounce") ? Light * Param("Relay_Base", 0.5f) : 0f; } }

    // The mark it gives at a level: Illuminate after Vanity Lights, else Blind (one stronger with Silver Backing).
    public int MarkLevel(int level)
    {
        return Has("Vanity") ? level : Mathf.Min(UloeData.BlindMulti.Length, level + Mathf.RoundToInt(Param("Blindup_Base", 0f)));
    }

    void Mark(Entity_Master foe, int level)
    {
        if (Has("Vanity"))
            UloeMarks.Illuminate(Master, foe, MarkLevel(level));
        else
            UloeMarks.Blind(Master, foe, MarkLevel(level));
    }

    public float ReflectShare { get { return Has("Reflection") ? (Light >= UloeData.MirrorLevel3 ? Param("Reflectlit_Base", 0.6f) : Param("Reflect_Base", 0.3f)) : 0f; } }

    protected override void Started2()
    {
        handler = OnHit;
        ModKitUtil.Front(Master, handler);
    }

    protected override void Tick()
    {
        if (Time.time >= nextFront)
        {
            nextFront = Time.time + 0.1f;
            ModKitUtil.Front(Master, handler);
        }
        if (Time.time >= nextFeed && Has("Bounce"))
        {
            nextFeed = Time.time + UloeData.LightTick;
            UloeServer.Shine(relay, Master, Pos, Param("Relayradius_Base", 3f), RelayNow, Master, true);
        }
        if (Time.time >= nextHelio && Has("Heliostat"))
        {
            nextHelio = Time.time + UloeData.HeliostatEvery;
            Heliostat();
        }
    }

    // The nearest foe in its reach takes Light x Heliostat_Base and the mark its Light gives; unlit it does nothing.
    void Heliostat()
    {
        int level = Level;
        if (level <= 0)
            return;
        List<Entity_Master> foes = Foes(Param("Heliorange_Base", 4f));
        if (foes.Count == 0)
            return;
        Entity_Master foe = foes[0];
        ModKitCombat.Hurt(Master, foe, Light * Param("Heliostat_Base", 1.5f), 1f, null, null);
        if (ModKitUtil.Alive(foe))
            Mark(foe, level);
        Heliobeams++;
        ModKitUtil.Vis(Master, "Uloe_Line", ModKitUtil.Pos(foe), 1f + 0.5f * level);
    }

    void OnHit(Link_Interact hit)
    {
        try
        {
            float multi, damage;
            if (hit == null || hit.To != Master || hit.Data == null || hit.Type != "Hurt" || !hit.Data.TryGetValue("Damagemulti", out multi) || !hit.Data.TryGetValue("Damage", out damage))
                return;
            float total = damage * multi;
            if (total <= 0f)
                return;
            Taken += total;
            Hits++;
            Entity_Master from = hit.From;
            if (from == null || from == Master || !ModKitUtil.IsFoe(from, Team))
                return;
            LastAttacker = from;
            // Glare reads the Blind already on the foe, so the hit that first blinds it still lands.
            bool nullified = false;
            if (Has("Glare"))
            {
                int blind = UloeMarks.BlindLevel(from);
                float chance = blind * Param("Nullify_Base", 0.2f);
                float roll = ForceRoll >= 0f ? ForceRoll : UnityEngine.Random.value;
                if (blind > 0 && roll < chance)
                {
                    hit.Data = new Dictionary<string, float>(hit.Data);
                    hit.Data["Damage"] = 0f;
                    hit.Data["Damagemulti"] = 0f;
                    nullified = true;
                    Nullified++;
                    ModKitUtil.Vis(Master, "Uloe_Nullify", Pos, 0f);
                }
            }
            // Tempered Glass cuts the hit on its own copy of the data (a caller may share one Data among targets).
            float dealt = nullified ? 0f : total;
            if (!nullified && Has("Tempered"))
            {
                float cut = Mathf.Clamp01(Param("Tempered_Base", 0.2f));
                hit.Data = new Dictionary<string, float>(hit.Data);
                hit.Data["Damagemulti"] = multi * (1f - cut);
                Cut += total * cut;
                dealt = total * (1f - cut);
            }
            // Reflection: a share of what the hit does, back at the attacker, from the Mirror.
            float share = ReflectShare;
            if (share > 0f && dealt > 0f && ModKitUtil.Alive(from))
            {
                ModKitCombat.Hurt(Master, from, dealt * share, 1f, null, null);
                Reflected++;
                ReflectedDamage += dealt * share;
            }
            int level = Level;
            if (level <= 0 || !ModKitUtil.Alive(from))
                return;
            Mark(from, level);
            Flashes++;
            // Hall of Mirrors: the same mark on every foe near the attacker.
            if (Has("Hall"))
                foreach (Entity_Master f in ModKitUtil.FoesNear(Master, ModKitUtil.Pos(from), Param("Hall_Base", 2f)))
                    if (f != from && ModKitUtil.Alive(f))
                    {
                        Mark(f, level);
                        Spread++;
                    }
            if (Time.time - lastFlashVis > 0.3f)
            {
                lastFlashVis = Time.time;
                ModKitUtil.Vis(Master, "Uloe_Flash", ModKitUtil.Pos(from), level);
            }
        }
        catch (Exception e)
        {
            UloeLog.Warn("mirror hit: " + e.Message);
        }
    }
}
