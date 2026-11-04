// Mirror: a wall that Light empowers. Its rules ride on the game's own hit event (Yossele's, Argo's and Spool's pattern):
// Helper_Entitytools.Interacthurt hands each hit to the target's Oninteract chain, and the Mirror's handler goes first.
//   Lit             a foe whose hit lands is Blinded at the level the Mirror's Light gives; no Light, nothing.
//   Silver Backing  that Blind one level stronger.
//   Vanity Lights   Illuminates what hits it, in place of Blind.
//   Glare           a hit from a foe with Blind level N has an N x 20% chance to do nothing.
//   Bounce Card     a share of the Light it receives goes on to your buildings around it, never to another Bounce Card.
// Helper_Entitytools.Updateinteract rebuilds the chain whenever an upgrade or effect lands, so the handler is moved back to the
// front ten times a second.
public class UloeMirrorBrain : UloeBuilding
{
    public int Flashes, Nullified, Hits;
    public float Taken;
    public Entity_Master LastAttacker;
    public static float ForceRoll = -1f;       // the self-test's dice; below 0 the real ones
    readonly UloeLight relay = new UloeLight();
    Entity_Master.Oninteracts handler;
    float nextFront, nextFeed, lastFlashVis;

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
                    Nullified++;
                    ModKitUtil.Vis(Master, "Uloe_Nullify", Pos, 0f);
                }
            }
            int level = Level;
            if (level <= 0)
                return;
            if (Has("Vanity"))
                UloeMarks.Illuminate(Master, from, level);
            else
                UloeMarks.Blind(Master, from, Mathf.Min(UloeData.BlindMulti.Length, level + Mathf.RoundToInt(Param("Blindup_Base", 0f))));
            Flashes++;
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
