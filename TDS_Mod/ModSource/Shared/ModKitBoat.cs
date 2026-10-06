// Kit 1.4 (2026-10-04). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared vessel-mod kit: the boat. Every mod vessel needs a spot on the boat's vessel select and an unlocked button.
// The boat lays its vessels out on Boat/Charselect/Spots, one child per vessel (Populatecharselect runs each time the
// select opens and puts vessel i on spot i). The shipped boat has six spots, all named "Spot", for five vessels. A click
// picks the nearest vessel box under the mouse (Boatclick: one Physics.Raycast on the UI layer), so a vessel whose box
// is covered by its neighbours' boxes can't be picked with the mouse at all.
//
// The select camera looks straight across the deck (along the spots' +x, level with the vessels), so vessels at the same
// spot along the deck hide each other whatever their depth. And the game's click box (0.4 cube, centre 0.2 below the
// spot) sits under the sprite: it runs from the eyes to below the feet, so the head isn't clickable at all.
//
// Kits 1.0-1.3 left the shipped spots where they were and put each extra spot in the middle of a gap, 0.14 to one side:
// by ten vessels the boxes covered each other and Crackpot couldn't be clicked (measured in game, 2026-10-04). Kit 1.4
// keeps the shipped layout while the vessels fit on it; past that, every vessel, shipped ones too, stands in staggered
// rows, evenly spaced in spot order from a little past one end of the shipped spots to the other, at their heights along
// the deck, each row behind standing a little higher than its neighbours in front so its heads show over them; the
// click boxes are moved onto the sprites and trimmed to their bodies. Past 16 vessels they also shrink a little. Every
// mod computes this from the same numbers: the shipped spots' first positions, which whichever mod sees the boat first
// copies to Charselect/Spots_Home. Extra spots are named like the shipped ones, so kit 1.0-1.3 copies still running in
// older builds (they only move spots named differently from the first) leave every spot alone.
public static class ModKitBoat
{
    // The layout. Every mod must use the same values: change them only in a kit release, or spots flicker between two.
    const string HomeName = "Spots_Home";   // never put the kit's type prefix in here: the build scripts rename it
    const float RowSpread = 0.28f;          // from the front row to the back row, across the deck
    const float RowRise = 0.06f;            // each row stands this much above the deck under its neighbours
    const float EndRoom = 0.2f;             // deck used beyond the outer shipped spots, in mean shipped gaps (the hull
                                            // slopes up past them: 0.5 put the end vessels in the hull, 0.2 doesn't)
    const int MaxRows = 3;
    const float MinScale = 0.6f;

    // A crowded boat's click boxes, in the button's own units: the game's 0.4 box lifted 0.16 onto the sprite (feet to
    // the top of the head) and trimmed to 0.25 along the deck (the camera's left-right), so a click lands on the vessel
    // drawn there.
    static readonly Vector3 BoxCentre = new Vector3(0f, -0.04f, 0f), BoxSize = new Vector3(0.4f, 0.4f, 0.25f);

    // Self-test hooks: Hold stops the layout (a test places the spots itself); TestVessels adds spots for that many;
    // GameBoxes leaves the game's click boxes as they are.
    public static bool Hold, GameBoxes;
    public static int TestVessels;

    static bool missingLogged, layoutLogged;
    static int spotsLogged;

    public static void LateUpdate(string heroId)
    {
        Macro_Worldvisual world = Macro_Worldvisual.Instance;
        if (world == null || world.Boat == null)
            return;
        Transform spots = world.Boat.transform.Find("Charselect/Spots");
        if (spots == null || spots.childCount == 0)
        {
            if (!missingLogged)
                Debug.LogWarning("[ModKit] boat: Boat/Charselect/Spots not found; the mod vessel can't get a spot");
            missingLogged = true;
            return;
        }
        Transform home = Home(spots);
        int vessels = Mathf.Max(Vessels(), TestVessels);
        if (vessels > 0)
        {
            EnsureSpots(spots, vessels);
            if (!Hold)
                Arrange(spots, home, vessels, 0, RowSpread, EndRoom, RowRise);
        }
        // The buttons only exist while the select is open (Charselect is inactive otherwise), so only look for them then.
        if (spots.parent.gameObject.activeInHierarchy)
        {
            if (!GameBoxes && vessels > home.childCount)
                FitBoxes(spots, vessels);
            Unlock(heroId);
        }
    }

    static void FitBoxes(Transform spots, int n)
    {
        for (int i = 0; i < n && i < spots.childCount; i++)
            foreach (Transform child in spots.GetChild(i))
            {
                BoxCollider box = child.GetComponent<BoxCollider>();
                if (box != null && child.GetComponent<Macro_Characterselect_Click>() != null && (box.center != BoxCentre || box.size != BoxSize))
                {
                    box.center = BoxCentre;
                    box.size = BoxSize;
                }
            }
    }

    static int Vessels()
    {
        Dictionary<string, UnityEngine.Object> heroes;
        Tds_Masterdatabase db = Tds_Masterdatabase.Instance;
        if (db == null || db.Items == null || !db.Items.TryGetValue("Hero", out heroes))
            return 0;
        return heroes.Count;
    }

    // The shipped spots' first transforms, one child each, in spot order. Made once per boat, before any spot is added
    // or moved: the shipped spots are the leading children named like the first one (older kits name extras Spot_Mod<n>).
    public static Transform Home(Transform spots)
    {
        Transform select = spots.parent;
        Transform home = select.Find(HomeName);
        if (home != null)
            return home;
        GameObject go = new GameObject(HomeName);
        go.SetActive(false);
        home = go.transform;
        home.SetParent(select, false);
        string shipped = spots.GetChild(0).name;
        for (int i = 0; i < spots.childCount && spots.GetChild(i).name == shipped; i++)
        {
            Transform s = spots.GetChild(i);
            Transform h = new GameObject("Home" + i).transform;
            h.SetParent(home, false);
            h.localPosition = s.localPosition;
            h.localRotation = s.localRotation;
            h.localScale = s.localScale;
        }
        return home;
    }

    static void EnsureSpots(Transform spots, int count)
    {
        string shipped = spots.GetChild(0).name;
        bool added = false;
        while (spots.childCount < count)
        {
            Transform last = spots.GetChild(spots.childCount - 1);
            Transform extra = UnityEngine.Object.Instantiate(last, spots);
            foreach (Transform copied in extra)
                UnityEngine.Object.Destroy(copied.gameObject);
            extra.localRotation = last.localRotation;
            extra.localScale = last.localScale;
            added = true;
        }
        for (int i = 1; i < spots.childCount; i++)
            if (spots.GetChild(i).name != shipped)
                spots.GetChild(i).name = shipped;
        if (added && spots.childCount != spotsLogged)
        {
            spotsLogged = spots.childCount;
            Debug.Log("[ModKit] boat: " + spots.childCount + " spots for " + count + " vessels");
        }
    }

    // Places the first n spots. While they fit on the shipped spots they stand there; otherwise in rows (0: as many as
    // the spacing needs) 'spread' deep, from 'endRoom' mean gaps beyond one end of the shipped spots to as far beyond
    // the other. Spot i stands i steps along the deck in row i % rows (row 0 nearest the camera), so neighbours
    // alternate rows; a spot in row r > 0 stands r * 'rise' above the highest deck under it and its two neighbours.
    public static void Arrange(Transform spots, Transform home, int n, int rows, float spread, float endRoom, float rise)
    {
        int shipped = home.childCount;
        n = Mathf.Min(n, spots.childCount);
        if (shipped < 2)
            return;
        if (n <= shipped)
        {
            for (int i = 0; i < shipped && i < spots.childCount; i++)
                Put(spots.GetChild(i), home.GetChild(i).localPosition, home.GetChild(i).localScale);
            return;
        }
        if (home != deckHome)
        {
            deck = new Deck(home);
            deckHome = home;
        }
        Deck d = deck;
        if (d.Length < 0.01f)
            return;
        float gap = d.Length / (shipped - 1);
        float room = endRoom * gap;
        float step = (d.Length + 2f * room) / (n - 1);
        if (rows <= 0)
            rows = Mathf.Clamp(Mathf.CeilToInt(gap / step - 0.001f), 2, MaxRows);
        float scale = Mathf.Clamp(rows * step / gap, MinScale, 1f);
        float depth = Mathf.Max(spread, d.Depth);
        bool forward = home.GetChild(shipped - 1).localPosition.z >= home.GetChild(0).localPosition.z;
        Vector3 size = home.GetChild(0).localScale * scale;
        float start = forward ? d.ZMin - room : d.ZMax + room, along = forward ? step : -step;
        for (int i = 0; i < n; i++)
        {
            float z = start + along * i;
            int row = i % rows;
            float across = rows > 1 ? depth * ((float)row / (rows - 1) - 0.5f) : 0f;
            float y = d.Height(z);
            if (row > 0)
                y = Mathf.Max(y, Mathf.Max(d.Height(start + along * Mathf.Max(i - 1, 0)), d.Height(start + along * Mathf.Min(i + 1, n - 1)))) + rise * row * scale;
            Put(spots.GetChild(i), new Vector3(d.LineX(z) + across, y, z), size);
        }
        if (!layoutLogged && !Hold)
        {
            layoutLogged = true;
            Debug.Log("[ModKit] boat: " + n + " vessels in " + rows + " rows " + depth.ToString("F2") + " deep, " + step.ToString("F3")
                + " apart, rising " + rise.ToString("F2") + ", scale " + scale.ToString("F2") + "; " + Describe(spots));
        }
    }

    static void Put(Transform spot, Vector3 p, Vector3 scale)
    {
        if ((spot.localPosition - p).sqrMagnitude > 1e-6f)
            spot.localPosition = p;
        if ((spot.localScale - scale).sqrMagnitude > 1e-6f)
            spot.localScale = scale;
    }

    // The deck as the shipped spots describe it: their run along local z, the line they follow across it (x), how far
    // they already stand off that line (Depth), and their heights along the deck (the hull rises at both ends), which a
    // spot between two shipped ones takes from the straight line between theirs.
    static Transform deckHome;
    static Deck deck;

    struct Deck
    {
        public float ZMin, ZMax, Length, Depth;
        float zMean, xMean, xSlope;
        float[] zs, ys;

        public Deck(Transform home)
        {
            int n = home.childCount;
            zs = new float[n];
            ys = new float[n];
            ZMin = float.MaxValue; ZMax = float.MinValue;
            zMean = 0f; xMean = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = home.GetChild(i).localPosition;
                ZMin = Mathf.Min(ZMin, p.z); ZMax = Mathf.Max(ZMax, p.z);
                zMean += p.z / n; xMean += p.x / n;
                // kept sorted by z (insertion sort)
                int j = i;
                while (j > 0 && zs[j - 1] > p.z) { zs[j] = zs[j - 1]; ys[j] = ys[j - 1]; j--; }
                zs[j] = p.z; ys[j] = p.y;
            }
            Length = ZMax - ZMin;
            float szz = 0f, szx = 0f;
            foreach (Transform h in home)
            {
                float dz = h.localPosition.z - zMean;
                szz += dz * dz; szx += dz * (h.localPosition.x - xMean);
            }
            xSlope = szz > 1e-6f ? szx / szz : 0f;
            float off = 0f, on = 0f;
            foreach (Transform h in home)
            {
                float r = h.localPosition.x - (xMean + xSlope * (h.localPosition.z - zMean));
                off = Mathf.Min(off, r); on = Mathf.Max(on, r);
            }
            Depth = on - off;
        }

        public float LineX(float z) { return xMean + xSlope * (z - zMean); }

        public float Height(float z)
        {
            int n = zs.Length;
            if (z <= zs[0]) return ys[0];
            if (z >= zs[n - 1]) return ys[n - 1];
            int i = 1;
            while (zs[i] < z) i++;
            float span = zs[i] - zs[i - 1];
            return span > 1e-5f ? Mathf.Lerp(ys[i - 1], ys[i], (z - zs[i - 1]) / span) : Mathf.Max(ys[i - 1], ys[i]);
        }
    }

    // One line with what decides the layout: the shipped spots, the spots' frame, the click box and, while the select
    // is open, the camera as seen from the spots.
    public static string Describe(Transform spots)
    {
        List<string> l = new List<string>();
        Transform home = spots.parent.Find(HomeName);
        if (home != null)
            foreach (Transform h in home) l.Add(h.localPosition.ToString("F2"));
        string s = "shipped " + l.Count + ": " + string.Join(" ", l.ToArray())
            + "; spots rot " + spots.localRotation.eulerAngles.ToString("F0") + " scale " + spots.lossyScale.ToString("F2")
            + "; spot rot " + spots.GetChild(0).localRotation.eulerAngles.ToString("F0") + " scale " + spots.GetChild(0).localScale.ToString("F2");
        Tds_Masterdatabase db = Tds_Masterdatabase.Instance;
        GameObject click = db != null ? db.Getitem("Vfx", "Heroselectclick") as GameObject : null;
        Collider box = click != null ? click.GetComponent<Collider>() : null;
        if (box is BoxCollider)
            s += "; click box centre " + ((BoxCollider)box).center.ToString("F2") + " size " + ((BoxCollider)box).size.ToString("F2");
        else if (box is SphereCollider)
            s += "; click sphere centre " + ((SphereCollider)box).center.ToString("F2") + " radius " + ((SphereCollider)box).radius.ToString("F2");
        else if (box is CapsuleCollider)
            s += "; click capsule centre " + ((CapsuleCollider)box).center.ToString("F2") + " radius " + ((CapsuleCollider)box).radius.ToString("F2")
                + " height " + ((CapsuleCollider)box).height.ToString("F2") + " axis " + ((CapsuleCollider)box).direction;
        else
            s += "; click collider " + (box != null ? box.GetType().Name : click != null ? "none on the prefab" : "prefab not found");
        Camera cam = Camera.main;
        if (cam != null && spots.parent.gameObject.activeInHierarchy)
            s += "; camera at " + spots.InverseTransformPoint(cam.transform.position).ToString("F2") + " looking "
                + spots.InverseTransformDirection(cam.transform.forward).ToString("F2") + (cam.orthographic ? " ortho " + cam.orthographicSize.ToString("F2") : " fov " + cam.fieldOfView.ToString("F0"))
                + " screen " + Screen.width + "x" + Screen.height;
        return s;
    }

    // Self-test helper: how much of each vessel on the open select a mouse click reaches, tested the way the game tests
    // a click (Boatclick: Physics.Raycast from Camera.main, 100 m, UI layer, the nearest box wins). Each button's box is
    // sampled on a 9x9 grid over its screen rectangle; a point counts when the ray meets that box at all, and is
    // clickable when that box is the nearest hit. 'unreachable' counts unlocked vessels with no clickable point.
    public static string ClickReport(out int unreachable)
    {
        unreachable = 0;
        Camera cam = Camera.main;
        Macro_Worldvisual world = Macro_Worldvisual.Instance;
        Transform spots = world != null && world.Boat != null ? world.Boat.transform.Find("Charselect/Spots") : null;
        if (cam == null || spots == null)
            return "no camera or boat";
        int mask = LayerMask.GetMask("UI");
        List<string> parts = new List<string>();
        int count = 0;
        foreach (Transform spot in spots)
            foreach (Transform child in spot)
            {
                Macro_Characterselect_Click button = child.GetComponent<Macro_Characterselect_Click>();
                if (button == null || !child.gameObject.activeInHierarchy)
                    continue;
                count++;
                Collider box = child.GetComponent<Collider>();
                int seen = 0, won = 0;
                if (box != null && box.enabled)
                {
                    Bounds b = box.bounds;
                    float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                    for (int c = 0; c < 8; c++)
                    {
                        Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((c & 1) == 0 ? -1f : 1f, (c & 2) == 0 ? -1f : 1f, (c & 4) == 0 ? -1f : 1f));
                        Vector3 p = cam.WorldToScreenPoint(corner);
                        x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y);
                    }
                    for (int gx = 0; gx < 9; gx++)
                        for (int gy = 0; gy < 9; gy++)
                        {
                            Vector3 p = new Vector3(Mathf.Lerp(x0, x1, (gx + 0.5f) / 9f), Mathf.Lerp(y0, y1, (gy + 0.5f) / 9f), 0f);
                            if (p.x < 0f || p.y < 0f || p.x >= Screen.width || p.y >= Screen.height)
                                continue;
                            Ray ray = cam.ScreenPointToRay(p);
                            RaycastHit own, hit;
                            if (!box.Raycast(ray, out own, 100f))
                                continue;
                            seen++;
                            if (Physics.Raycast(ray, out hit, 100f, mask) && hit.collider == box)
                                won++;
                        }
                }
                if (won == 0 && !button.Locked)
                    unreachable++;
                string name = button.Character.StartsWith("Hero_") ? button.Character.Substring(5) : button.Character;
                parts.Add(name + " " + (seen > 0 ? Mathf.RoundToInt(100f * won / seen) : 0) + "%" + (box == null ? " (no box)" : "") + (button.Locked ? " (locked)" : ""));
            }
        return count + " vessels, " + unreachable + " can't be clicked: " + string.Join(", ", parts.ToArray());
    }

    static void Unlock(string heroId)
    {
        foreach (Macro_Characterselect_Click button in UnityEngine.Object.FindObjectsOfType<Macro_Characterselect_Click>())
            if (button.Character == heroId && button.Locked)
            {
                button.Locked = false;
                // Populatecharselect marks vessels missing from the save as locked, and the button's Start may already
                // have passed that to its VFX ("Locked"), which then draws only the shadow. Start again sends Locked=false.
                button.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
                Debug.Log("[ModKit] boat: unlocked " + heroId);
            }
    }
}
