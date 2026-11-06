// Kit 1.15 (2026-11-06). Versions and compatibility rules: TDS_Vessel_Creator\kit\README.md
// Shared kit: the Rogue Waves pack registry (docs/PACK.md sections 2-4). Every part of the pack (each vessel, the mode)
// is its own prefab with its own compiled copy of this file, and parts load in no fixed order. They meet on one
// DontDestroyOnLoad GameObject, "RogueWaves_Registry", which the first part to boot creates: each part adds one child
// named "<Part> <version> kit <kit>". Nothing else is shared, so no part needs another's types.
//   Join(part, version)     registers the part, logs "[RogueWaves:<Part>] v<version> kit <kit>", warns on a mismatch
//   Has(part)               another part is loaded (it joined the registry)
//   Taken(heroId)           a vessel id already exists in the game's data: a standalone vessel mod got there first, so
//                           the pack's part stands down (PACK section 2.3) and Notice() tells the player once
//   Mark(name)              a one-per-pack job (ModKitFocus): true for the first part to ask, false for the rest
public static class ModKitPack
{
    public const string Kit = "1.15";
    public const string RegistryName = "RogueWaves_Registry";
    static GameObject registry;

    public static GameObject Registry
    {
        get
        {
            if (registry != null)
                return registry;
            registry = GameObject.Find(RegistryName);
            if (registry == null)
            {
                registry = new GameObject(RegistryName);
                UnityEngine.Object.DontDestroyOnLoad(registry);
            }
            return registry;
        }
    }

    public static void Join(string part, string version)
    {
        Transform root = Registry.transform;
        string name = part + " " + version + " kit " + Kit;
        bool there = false;
        foreach (Transform c in root)
            if (c.name.StartsWith(part + " ")) { c.name = name; there = true; }
        if (!there)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(root, false);
        }
        Debug.Log("[RogueWaves:" + part + "] v" + version + " kit " + Kit);
        string mismatch = Mismatch();
        if (mismatch != "")
            Debug.LogWarning("[RogueWaves:" + part + "] parts disagree: " + mismatch);
    }

    // "" when every registered part has the same pack version and kit; otherwise the list of parts.
    public static string Mismatch()
    {
        string version = null, kit = null;
        bool differ = false;
        List<string> all = new List<string>();
        foreach (Transform c in Registry.transform)
        {
            string[] p = c.name.Split(' ');
            if (p.Length < 4 || p[2] != "kit")
                continue;
            all.Add(c.name);
            if (version == null) { version = p[1]; kit = p[3]; }
            else if (p[1] != version || p[3] != kit) differ = true;
        }
        return differ ? string.Join(", ", all.ToArray()) : "";
    }

    public static bool Has(string part)
    {
        foreach (Transform c in Registry.transform)
            if (c.name.StartsWith(part + " "))
                return true;
        return false;
    }

    public static bool Taken(string heroId)
    {
        Tds_Masterdatabase db = Tds_Masterdatabase.Instance;
        Dictionary<string, UnityEngine.Object> heroes;
        return db != null && db.Items != null && db.Items.TryGetValue("Hero", out heroes) && heroes != null && heroes.ContainsKey(heroId);
    }

    public static bool Mark(string job)
    {
        Transform root = Registry.transform;
        if (root.Find(job) != null)
            return false;
        GameObject child = new GameObject(job);
        child.transform.SetParent(root, false);
        return true;
    }

    // A warning on screen once the game can show one (the main menu), retried for a minute, and in the log at once.
    public static void Notice(MonoBehaviour host, string part, string text)
    {
        Debug.LogError("[RogueWaves:" + part + "] " + text);
        if (host != null)
            host.StartCoroutine(Show(text));
    }

    static IEnumerator Show(string text)
    {
        for (float waited = 0f; waited < 60f; waited += 2f)
        {
            yield return new WaitForSecondsRealtime(2f);
            bool shown = false;
            try
            {
                Helper_Uitools.Warning(text, "", 10f);
                shown = true;
            }
            catch (Exception) { }
            if (shown)
                yield break;
        }
    }
}
