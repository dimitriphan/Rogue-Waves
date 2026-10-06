// Runs on every player (host included). Listens to the Vis RPCs the server sends through Mummer's entities and plays
// Mummer's own sounds and moving effects (thread lines, Scraps flying to a Loom), and shows owner-only messages. The
// Marked, Held and Tangled looks are not here: the game attaches those itself (Vfxdebuffcheck by name).
public static class MummerClient
{
    static readonly List<Entity_Master_Client> hooked = new List<Entity_Master_Client>();
    static Game_Entity seen;
    static float nextScan;
    static Material threadMat;

    public static readonly Color ThreadColor = new Color(0.86f, 0.78f, 0.95f, 1f);

    public static void Update()
    {
        Game_Entity ge = Game_Entity.Instance;
        if (ge != seen)
        {
            seen = ge;
            hooked.Clear();
        }
        if (ge == null || ge.Entityclient == null || Time.unscaledTime < nextScan)
            return;
        nextScan = Time.unscaledTime + 0.25f;
        hooked.RemoveAll(c => c == null);
        foreach (Entity_Master master in ge.Entityclient)
        {
            if (master == null || master.Client == null || hooked.Contains(master.Client))
                continue;
            string being = master.Being.Value.ToString();
            if (being != MummerData.Hero && MummerData.BuildFor(being) == null && MummerData.PuppetFor(being) == null)
                continue;
            Entity_Master m = master;
            master.Client.Vis += (type, position, number) => OnVis(m, type, position, number);
            hooked.Add(master.Client);
        }
    }

    static void OnVis(Entity_Master master, string type, Vector3 position, float number)
    {
        if (type == null)
            return;
        try
        {
            Vector3 at = master != null ? MummerUtil.Pos(master) : position;
            string being = master != null ? master.Being.Value.ToString() : "";
            // The stock attack AIs announce each swing with "Attack" and each shot with "Shoot".
            if (type == "Attack")
            {
                MummerAssets.Play("Mummer_Peekaboo", at, being == MummerData.Hero ? 1f : 0.5f);
                return;
            }
            if (type == "Shoot")
            {
                MummerAssets.Play("Mummer_Bolt", at);
                return;
            }
            if (!type.StartsWith("Mummer_"))
                return;
            switch (type)
            {
                case "Mummer_Hook":
                    MummerAssets.Play("Mummer_Hookthrow", at);
                    Animate(master, "Hook");
                    Run(ThreadRun(master, (ulong)number, position, 0.15f, 0.75f, true));
                    break;
                case "Mummer_Scrap":
                    Run(ScrapRun(position, master, Mathf.RoundToInt(number)));
                    break;
                case "Mummer_Markpaid":
                    MummerAssets.Play("Mummer_Bell", position, 0.8f);
                    Run(ScrapRun(position, master, Mathf.RoundToInt(number)));
                    break;
                case "Mummer_Spin":
                    MummerAssets.Play("Mummer_Loom", at);
                    Run(LineRun(at + Vector3.up * 0.55f, position + Vector3.up * 0.1f, 0.35f, 0.02f));
                    break;
                case "Mummer_Stitch":
                    MummerAssets.Play("Mummer_Stitch", at);
                    MummerAssets.Effect("World_Mummer_Stitch", at + Vector3.up * 0.3f, 1.5f);
                    break;
                case "Mummer_Puppetpop":
                    MummerAssets.Play("Mummer_Puppetpop", at);
                    MummerAssets.Effect("World_Mummer_Scrappop", at, 1.2f);
                    break;
                case "Mummer_Puppetdown":
                    MummerAssets.Play("Mummer_Puppetdown", position);
                    break;
                case "Mummer_Finale":
                    MummerAssets.Play("Mummer_Puppetdown", position, 1.4f);
                    MummerAssets.Effect("World_Mummer_Finale", Ground(position), 2f);
                    break;
                case "Mummer_Grab":
                    MummerAssets.Play("Mummer_Grab", at);
                    Run(ThreadRun(master, (ulong)number, position, 0f, 0.6f, false));
                    break;
                case "Mummer_Flail":
                    MummerAssets.Effect("World_Mummer_Flail", position + Vector3.up * 0.2f, 1f);
                    break;
                case "Mummer_Drop":
                    MummerAssets.Play("Mummer_Bell", position, 0.5f);
                    break;
                case "Mummer_Curtain":
                    MummerAssets.Play("Mummer_Curtain", at);
                    MummerAssets.Effect("World_Mummer_Velvet", position, 1.2f);
                    break;
                case "Mummer_Finalact":
                    MummerAssets.Play("Mummer_Finalact", at);
                    GameObject ring = MummerAssets.Effect("World_Mummer_Finalact", Ground(at), 2.5f);
                    if (ring != null) ring.transform.localScale = Vector3.one * Mathf.Max(1f, number);
                    break;
                case "Mummer_Costume":
                    MummerAssets.Play("Mummer_Costume", at);
                    MummerAssets.Effect("World_Mummer_Confetti", at + Vector3.up * 0.6f, 2f);
                    break;
                case "Mummer_Costumechange":
                    MummerAssets.Play("Mummer_Stitch", at, 0.7f);
                    MummerAssets.Effect("World_Mummer_Confetti", at + Vector3.up * 0.6f, 2f);
                    break;
                case "Mummer_Unravel":
                    MummerAssets.Play("Mummer_Unravel", at);
                    MummerAssets.Effect("World_Mummer_Unravel", at + Vector3.up * 0.4f, 2f);
                    break;
                case "Mummer_Encore":
                case "Mummer_Patchwork":
                    MummerAssets.Play("Mummer_Stitch", position, 0.8f);
                    MummerAssets.Effect("World_Mummer_Stitch", position + Vector3.up * 0.3f, 1.5f);
                    break;
                case "Mummer_Built":
                    MummerAssets.Effect("World_Mummer_Scrappop", at, 1.2f);
                    MummerAssets.Play("Mummer_Stitch", at, 0.6f);
                    break;
                case "Mummer_Upgrade":
                    MummerAssets.Play("Mummer_Stitch", at, 0.8f);
                    break;
                case "Mummer_Towerdown":
                    MummerAssets.Effect("World_Mummer_Stuffing", at, 2f);
                    MummerAssets.Play("Mummer_Puppetdown", at, 1.3f);
                    break;
                case "Mummer_Herodeath":
                    MummerAssets.Effect("World_Hero_Mummer_Death", at, 2.5f);
                    MummerAssets.Play("Mummer_Death", at);
                    break;
                case "Mummer_Warn":
                    int n = Mathf.RoundToInt(number), code = n % 100, value = n / 100;
                    if (MummerUtil.OwnedLocally(master) && code > 0 && code < MummerData.WarnText.Length)
                        Helper_Uitools.Warning(MummerData.WarnText[code] + (value > 0 ? " (" + value + ")" : ""), "", 2f);
                    break;
            }
        }
        catch (Exception e)
        {
            MummerLog.Warn("client vis " + type + ": " + e.Message);
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
        if (MummerMod.Instance != null)
            MummerMod.Instance.StartCoroutine(routine);
    }

    static Vector3 Ground(Vector3 entityPosition) { return entityPosition + Vector3.down * 0.48f; }

    // ---------------------------------------------------------------- thread lines

    static Material ThreadMaterial()
    {
        if (threadMat != null)
            return threadMat;
        GameObject fx = MummerAssets.Vfx("World_Mummer_Stitch");
        ParticleSystemRenderer r = fx != null ? fx.GetComponent<ParticleSystemRenderer>() : null;
        threadMat = r != null && r.sharedMaterial != null ? new Material(r.sharedMaterial) : new Material(Shader.Find("Sprites/Default"));
        threadMat.name = "Mummer_Thread";
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
        LineRenderer l = Line("Mummer_Spinthread", width);
        for (float e = 0f; e < seconds && l != null; e += Time.deltaTime)
        {
            Draw(l, a, b, 0.12f * (1f - e / seconds));
            yield return null;
        }
        if (l != null) UnityEngine.Object.Destroy(l.gameObject);
    }

    // HOOK and the Cross: the thread flies out from the caster, catches the foe and stays on it while it is reeled.
    static IEnumerator ThreadRun(Entity_Master from, ulong targetId, Vector3 fallback, float delay, float hold, bool barb)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        Entity_Master target = MummerUtil.ById(targetId);
        LineRenderer l = Line("Mummer_Thread", 0.035f);
        GameObject tip = barb ? MummerAssets.Effect("World_Mummer_Barb", fallback, 0f) : null;
        const float fly = 0.16f;
        float total = fly + hold;
        for (float e = 0f; e < total && l != null; e += Time.deltaTime)
        {
            Vector3 a = (from != null ? MummerUtil.Pos(from) : fallback) + Vector3.up * (barb ? 0.45f : 1.25f);
            Vector3 b = (target != null && MummerUtil.Pos(target) != Vector3.zero ? MummerUtil.Pos(target) : fallback) + Vector3.up * 0.25f;
            Vector3 end = e < fly ? Vector3.Lerp(a, b, e / fly) : b;
            Draw(l, a, end, e < fly ? 0.05f : 0.02f);
            if (tip != null) tip.transform.position = end;
            yield return null;
        }
        if (barb && target != null)
            MummerAssets.Play("Mummer_Reel", MummerUtil.Pos(target));
        if (l != null) UnityEngine.Object.Destroy(l.gameObject);
        if (tip != null) UnityEngine.Object.Destroy(tip);
    }

    // A Scrap pops off a felled foe and flies to the Loom (or vessel) that spun it.
    static IEnumerator ScrapRun(Vector3 from, Entity_Master to, int count)
    {
        MummerAssets.Effect("World_Mummer_Scrappop", Ground(from) + Vector3.up * 0.2f, 1.2f);
        GameObject fly = MummerAssets.Effect("World_Mummer_Scrapfly", from + Vector3.up * 0.3f, 0f);
        if (fly == null)
            yield break;
        if (count >= 3) fly.transform.localScale = Vector3.one * 1.5f;
        const float time = 0.55f;
        for (float e = 0f; e < time && fly != null; e += Time.deltaTime)
        {
            float k = e / time;
            Vector3 dest = to != null ? MummerUtil.Pos(to) + Vector3.up * 0.5f : from;
            fly.transform.position = Vector3.Lerp(from + Vector3.up * 0.3f, dest, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.8f;
            yield return null;
        }
        if (to != null)
            MummerAssets.Play("Mummer_Scrap", MummerUtil.Pos(to));
        if (fly != null) UnityEngine.Object.Destroy(fly, 0.3f);
    }
}
