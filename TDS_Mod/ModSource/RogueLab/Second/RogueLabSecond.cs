// RogueLab round 2, A9 of docs/RESEARCH_GAPS.md: the code of RogueLab's second prefab, RogueLabMod_Second_Default, in
// the same mod bundle as RogueLabMod_Default. The game compiles each prefab's code on its own (docs/15 §10); this
// checks that a second Default prefab in one bundle starts at all, which the pack's layout (docs/PACK.md §1) rests on.
// It leaves a GameObject named RogueLab_SecondAlive (RogueLabData.SecondMarker) that the first prefab looks for.
using UnityEngine;

public class RogueLabSecond : MonoBehaviour
{
    void Start()
    {
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        GameObject marker = new GameObject("RogueLab_SecondAlive");
        DontDestroyOnLoad(marker);
        Debug.Log("[RogueLab] TEST INFO second prefab: RogueLabMod_Second_Default started");
    }
}
