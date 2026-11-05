// Runs on every player (host included). Listens to the Vis RPCs the server sends through Spool's entities and plays
// Spool's own sounds and moving effects: ground Thread as small tufts (drawn here, not networked entities), pickups
// flying to their carrier, drop-offs flying into the Yarn Stash, PATCH's thread, and owner-only messages.
public static class SpoolClient
{
    static readonly List<Entity_Master_Client> hooked = new List<Entity_Master_Client>();
    static readonly Dictionary<int, Tuft> tufts = new Dictionary<int, Tuft>();
    static Game_Entity seen;
    static float nextScan;
    static Material threadMat, spriteMat;
    static Sprite tuftSprite;
    public static int TuftsShown, Flights;

    public static readonly Color ThreadColor = new Color(0.435f, 0.682f, 0.494f, 1f);   // sage 6FAE7E (lilac 945DAD until 2026-11-05)

    class Tuft
    {
        public GameObject Go;
        public SpriteRenderer Sprite;
        public Vector3 At;
        public int Amount;
        public float Until, Seed;
    }

    // Temporary art (ModSource/Spool/Art/Temporary/README.md): a coiled loop of sage thread (lilac until 2026-11-05) with its end trailing off
    // (Dimitri's pick of 2026-11-03), lit from the upper left (12x12, drawn here so ground Thread needs nothing from the
    // asset pack).
    static readonly string[] TuftPixels =
    {
        "............", "....jjjx....", "...jx...xx..", "..jx.jjx.xX.", "..j.jx..x.X.", "..j.x.jx.xX.",
        "..x.xX..XX..", "..xX.xXXX.X.", "...xX....X..", "....XXXXX...", "..........X.", "...........X",
    };

    public static void Update()
    {
        Game_Entity ge = Game_Entity.Instance;
        if (ge != seen)
        {
            seen = ge;
            hooked.Clear();
            ClearTufts();
        }
        if (tufts.Count > 0)
            AnimateTufts();
        if (ge == null || ge.Entityclient == null || Time.unscaledTime < nextScan)
            return;
        nextScan = Time.unscaledTime + 0.25f;
        hooked.RemoveAll(c => c == null);
        QuietPins(ge);
        foreach (Entity_Master master in ge.Entityclient)
        {
            if (master == null || master.Client == null || hooked.Contains(master.Client))
                continue;
            string being = master.Being.Value.ToString();
            if (being != SpoolData.Hero && SpoolData.BuildFor(being) == null && SpoolData.UnitFor(being) == null)
                continue;
            Entity_Master m = master;
            master.Client.Vis += (type, position, number) => OnVis(m, type, position, number);
            hooked.Add(master.Client);
        }
    }

    // A Cookie Tin's TARGET pin is a marker, never something to click: the pointer takes the nearest collider it meets, so the
    // pin under a Stitched took its clicks (round 3, 2026-11-05: 0 of 3 and 2 of 3). Its hit colliders go off on every
    // machine (the host's server-side hit object, a client's own copy); the pin has no upgrades, so they are built once.
    public static int PinsQuieted;   // self-test
    static readonly List<Entity_Master> quiet = new List<Entity_Master>();

    static void QuietPins(Game_Entity ge)
    {
        List<Entity_Master> pins;
        quiet.RemoveAll(m => m == null);
        if (ge.Entityclienttags == null || !ge.Entityclienttags.TryGetValue("Spoolpin", out pins) || pins == null)
            return;
        foreach (Entity_Master pin in pins)
        {
            if (pin == null || quiet.Contains(pin))
                continue;
            GameObject server = pin.Currenthit, own = pin.Client != null ? pin.Client.Currentvishit : null;
            // Done only once the one this machine's pointer reads is built: the host's server-side one, a client's own copy.
            if (ge.IsServer ? server == null : own == null)
                continue;
            foreach (GameObject h in new[] { server, own })
                if (h != null)
                    foreach (Collider c in h.GetComponentsInChildren<Collider>(true))
                        c.enabled = false;
            quiet.Add(pin);
            PinsQuieted++;
        }
    }

    static void OnVis(Entity_Master master, string type, Vector3 position, float number)
    {
        if (type == null)
            return;
        try
        {
            Vector3 at = master != null ? ModKitUtil.Pos(master) : position;
            string being = master != null ? master.Being.Value.ToString() : "";
            // The stock attack AIs announce each swing with "Attack" and each shot with "Shoot".
            if (type == "Attack")
            {
                SpoolAssets.Play("Spool_Swat", at, being == SpoolData.Hero ? 1f : 0.5f);
                return;
            }
            if (type == "Shoot")
            {
                SpoolAssets.Play("Spool_Needle", at);
                return;
            }
            if (!type.StartsWith("Spool_"))
                return;
            int n = Mathf.RoundToInt(number);
            switch (type)
            {
                case "Spool_Tuft":
                    SetTuft(n >> 4, position, n & 15, true);
                    break;
                case "Spool_Take":
                    Run(Fly(position, master, false));
                    if (n >> 4 > 0)
                        SetTuft(n >> 4, position, n & 15, false);
                    SpoolAssets.Play("Spool_Tuft", position);
                    break;
                case "Spool_Deliver":
                    Run(Fly(at, null, true, position));
                    break;
                case "Spool_Spin":
                    SpoolAssets.Play("Spool_Spin", at);
                    ModKitAssets.Effect("World_Spool_Stitch", at + Vector3.up * 0.6f, 1.2f);
                    if (n == 1 && master != null)
                        Run(LineRun(at + Vector3.up * 0.5f, at + Vector3.up * 1.4f, 0.35f, 0.025f));
                    break;
                case "Spool_Strike":
                    SpoolAssets.Play("Spool_Swat", position, 0.4f);
                    break;
                case "Spool_Stitch":
                    SpoolAssets.Play("Spool_Stitch", at);
                    ModKitAssets.Effect("World_Spool_Stitch", at + Vector3.up * 0.4f, 1.5f);
                    break;
                // Merit Badge: a Crafted drowned another foe (number: how many).
                case "Spool_Merit":
                    SpoolAssets.Play("Spool_Pop", position, 0.6f);
                    ModKitAssets.Effect("World_Spool_Stitch", position + Vector3.up * 0.4f, 1f);
                    break;
                case "Spool_Pop":
                    SpoolAssets.Play("Spool_Pop", at);
                    ModKitAssets.Effect("World_Spool_Stuffing", at, 1.2f);
                    break;
                case "Spool_Down":
                    SpoolAssets.Play("Spool_Down", position, n == 1 ? 1.3f : 1f);
                    ModKitAssets.Effect("World_Spool_Stuffing", position, 1.5f);
                    break;
                case "Spool_Unstitch":
                    SpoolAssets.Play("Spool_Stitch", position, 0.6f);
                    ModKitAssets.Effect("World_Spool_Stuffing", position, 1.2f);
                    break;
                case "Spool_Burst":
                    SpoolAssets.Play("Spool_Burst", position);
                    GameObject ring = ModKitAssets.Effect("World_Spool_Burst", Ground(position), 2f);
                    if (ring != null) ring.transform.localScale = Vector3.one * Mathf.Max(0.8f, number);
                    break;
                case "Spool_Leap":
                    SpoolAssets.Play("Spool_Pop", at, 1.2f);
                    break;
                case "Spool_Prick":
                    SpoolAssets.Play("Spool_Prick", position);
                    ModKitAssets.Effect("World_Spool_Prick", position + Vector3.up * 0.3f, 1f);
                    break;
                case "Spool_Target":
                    if (ModKitUtil.OwnedLocally(master))
                    {
                        SpoolAssets.Play("Spool_Prick", position, 0.7f);
                        Run(LineRun(at + Vector3.up * 0.4f, position + Vector3.up * 0.1f, 0.6f, 0.02f));
                    }
                    break;
                case "Spool_Patch":
                    SpoolAssets.Play("Spool_Patch", at);
                    Animate(master, "Patch");
                    Run(ThreadRun(master, (ulong)number, position, 0.1f, 0.6f));
                    break;
                case "Spool_Mend":
                    ModKitAssets.Effect("World_Spool_Mend", position + Vector3.up * 0.4f, 1f);
                    break;
                case "Spool_Built":
                    ModKitAssets.Effect("World_Spool_Stuffing", at, 1.2f);
                    SpoolAssets.Play("Spool_Stitch", at, 0.6f);
                    break;
                case "Spool_Upgrade":
                    SpoolAssets.Play("Spool_Stitch", at, 0.8f);
                    break;
                case "Spool_Herodeath":
                    ModKitAssets.Effect("World_Hero_Spool_Death", at, 2.5f);
                    SpoolAssets.Play("Spool_Death", at);
                    break;
                case "Spool_Warn":
                    int code = n % 100, value = n / 100;
                    if (ModKitUtil.OwnedLocally(master) && code > 0 && code < SpoolData.WarnText.Length)
                        Helper_Uitools.Warning(SpoolData.WarnText[code] + (value > 0 ? " (" + value + ")" : ""), "", 2f);
                    break;
            }
        }
        catch (Exception e)
        {
            SpoolLog.Warn("client vis " + type + ": " + e.Message);
        }
    }

    // Same call the game's Entity_Vis_Mob_Attack makes for "Attack": Force restarts the animation and takes its priority.
    static void Animate(Entity_Master master, string anim)
    {
        if (master == null)
            return;
        foreach (Entity_Vis_Animator_Base a in master.GetComponentsInChildren<Entity_Vis_Animator_Base>(true))
            a.Animplay(anim, Game_Sprite.Animationtype.Force);
    }

    static void Run(IEnumerator routine)
    {
        if (SpoolMod.Instance != null)
            SpoolMod.Instance.StartCoroutine(routine);
    }

    static Vector3 Ground(Vector3 entityPosition) { return entityPosition + Vector3.down * 0.48f; }

    // ---------------------------------------------------------------- ground Thread

    // The sprite material Spool's own units are drawn with, so a tuft is lit like them.
    static Material SpriteMaterial()
    {
        if (spriteMat != null)
            return spriteMat;
        foreach (Entity_Master_Client c in hooked)
        {
            SpriteRenderer r = c != null ? c.GetComponentInChildren<SpriteRenderer>(true) : null;
            if (r != null && r.sharedMaterial != null)
                return spriteMat = r.sharedMaterial;
        }
        return null;
    }

    static GameObject TuftObject(string name, out SpriteRenderer r)
    {
        if (tuftSprite == null)
            tuftSprite = SpoolAssets.Pixel(TuftPixels, "Spool_Tuft");
        GameObject go = new GameObject(name);
        r = go.AddComponent<SpriteRenderer>();
        r.sprite = tuftSprite;
        Material m = SpriteMaterial();
        if (m != null) r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    // A new tuft, a merged one (fresh time and size), or one a carrier took from (smaller or gone).
    static void SetTuft(int id, Vector3 at, int amount, bool fresh)
    {
        Tuft t;
        if (!tufts.TryGetValue(id, out t))
        {
            if (amount <= 0)
                return;
            SpriteRenderer r;
            GameObject go = TuftObject("Spool_Tuft_" + id, out r);
            t = new Tuft { Go = go, Sprite = r, At = Ground(at) + Vector3.up * 0.2f, Seed = (id % 17) * 0.37f };
            tufts[id] = t;
            TuftsShown++;
        }
        if (amount <= 0)
        {
            RemoveTuft(id);
            return;
        }
        t.Amount = amount;
        if (fresh)
            t.Until = Time.time + SpoolData.DropLife;
        float size = Mathf.Lerp(0.7f, 1.3f, Mathf.Clamp01((amount - 1) / 6f));   // 0.42 to 0.78 wide: the 8 px tuft was 0.32 to 0.64 and hard to see
        t.Go.transform.localScale = Vector3.one * size;
    }

    static void RemoveTuft(int id)
    {
        Tuft t;
        if (!tufts.TryGetValue(id, out t))
            return;
        if (t.Go != null)
            UnityEngine.Object.Destroy(t.Go);
        tufts.Remove(id);
    }

    static void ClearTufts()
    {
        foreach (Tuft t in tufts.Values)
            if (t.Go != null)
                UnityEngine.Object.Destroy(t.Go);
        tufts.Clear();
    }

    // Tufts bob, face the camera, and fade in their last two seconds; each player times them out on its own.
    static readonly List<int> gone = new List<int>();

    static void AnimateTufts()
    {
        Camera cam = Camera.main;
        float now = Time.time;
        gone.Clear();
        foreach (KeyValuePair<int, Tuft> kv in tufts)
        {
            Tuft t = kv.Value;
            if (t.Go == null || now >= t.Until)
            {
                gone.Add(kv.Key);
                continue;
            }
            t.Go.transform.position = t.At + Vector3.up * (Mathf.Sin(now * 3f + t.Seed) * 0.04f);
            if (cam != null)
                t.Go.transform.rotation = cam.transform.rotation;
            float left = t.Until - now;
            Color c = t.Sprite.color;
            c.a = left < 2f ? Mathf.Clamp01(left / 2f) * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(now * 8f))) : 1f;
            t.Sprite.color = c;
        }
        foreach (int id in gone)
            RemoveTuft(id);
    }

    // A tuft hops from one point to a carrier (a pickup) or from a carrier into the Yarn Stash (a drop-off).
    static IEnumerator Fly(Vector3 from, Entity_Master to, bool deliver, Vector3 stash = default(Vector3))
    {
        SpriteRenderer r;
        GameObject go = TuftObject("Spool_Tuftfly", out r);
        Flights++;
        Camera cam = Camera.main;
        Vector3 a = Ground(from) + Vector3.up * 0.3f;
        const float time = 0.45f;
        for (float e = 0f; e < time && go != null; e += Time.deltaTime)
        {
            float k = e / time;
            Vector3 dest = deliver ? stash + Vector3.up * 0.3f : to != null ? ModKitUtil.Pos(to) + Vector3.up * 0.2f : a;
            go.transform.position = Vector3.Lerp(a, dest, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.7f;
            if (cam != null) go.transform.rotation = cam.transform.rotation;
            yield return null;
        }
        if (deliver)
        {
            SpoolAssets.Play("Spool_Deliver", stash);
            ModKitAssets.Effect("World_Spool_Stitch", stash + Vector3.up * 0.4f, 1.2f);
        }
        if (go != null) UnityEngine.Object.Destroy(go);
    }

    // ---------------------------------------------------------------- thread lines

    static Material ThreadMaterial()
    {
        if (threadMat != null)
            return threadMat;
        GameObject fx = ModKitAssets.Vfx("World_Spool_Stitch");
        ParticleSystemRenderer r = fx != null ? fx.GetComponent<ParticleSystemRenderer>() : null;
        threadMat = r != null && r.sharedMaterial != null ? new Material(r.sharedMaterial) : new Material(Shader.Find("Sprites/Default"));
        threadMat.name = "Spool_Thread";
        return threadMat;
    }

    static LineRenderer Line(string name, float width)
    {
        GameObject go = new GameObject(name);
        LineRenderer l = go.AddComponent<LineRenderer>();
        l.sharedMaterial = ThreadMaterial();
        l.positionCount = 10;
        l.startWidth = width;
        l.endWidth = width;
        l.startColor = ThreadColor;
        l.endColor = ThreadColor;
        l.useWorldSpace = true;
        l.numCapVertices = 0;
        l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return l;
    }

    // A slightly sagging thread between two points.
    static void Draw(LineRenderer l, Vector3 a, Vector3 b, float sag)
    {
        for (int i = 0; i < l.positionCount; i++)
        {
            float t = i / (float)(l.positionCount - 1);
            l.SetPosition(i, Vector3.Lerp(a, b, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * sag);
        }
    }

    static IEnumerator LineRun(Vector3 a, Vector3 b, float seconds, float width)
    {
        LineRenderer l = Line("Spool_Spinthread", width);
        for (float e = 0f; e < seconds && l != null; e += Time.deltaTime)
        {
            Draw(l, a, b, 0.12f * (1f - e / seconds));
            yield return null;
        }
        if (l != null) UnityEngine.Object.Destroy(l.gameObject);
    }

    // PATCH: a thread runs from Spool to what it mends and stitches it.
    static IEnumerator ThreadRun(Entity_Master from, ulong targetId, Vector3 fallback, float delay, float hold)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        Entity_Master target = ModKitUtil.ById(targetId);
        LineRenderer l = Line("Spool_Patchthread", 0.035f);
        const float fly = 0.16f;
        float total = fly + hold;
        for (float e = 0f; e < total && l != null; e += Time.deltaTime)
        {
            Vector3 a = (from != null ? ModKitUtil.Pos(from) : fallback) + Vector3.up * 0.45f;
            Vector3 b = (target != null && ModKitUtil.Pos(target) != Vector3.zero ? ModKitUtil.Pos(target) : fallback) + Vector3.up * 0.3f;
            Vector3 end = e < fly ? Vector3.Lerp(a, b, e / fly) : b;
            Draw(l, a, end, e < fly ? 0.05f : 0.02f);
            yield return null;
        }
        Vector3 at = target != null ? ModKitUtil.Pos(target) : fallback;
        ModKitAssets.Effect("World_Spool_Stitch", at + Vector3.up * 0.4f, 1.5f);
        SpoolAssets.Play("Spool_Stitch", at, 0.8f);
        if (l != null) UnityEngine.Object.Destroy(l.gameObject);
    }
}
