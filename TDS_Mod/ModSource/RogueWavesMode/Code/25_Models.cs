using System.Collections.Generic;
using UnityEngine;

// The mode part's model library (2026-11-05, open decisions 11-05b item 10): low-poly models built by Tools >
// RogueWavesMode > Build All Assets into RWAssets_Library/Models (Assets/RogueWavesModeMod/Editor/RogueWavesModeModels.cs),
// coloured from the mode's palette texture. A model is lit like the game's own by moving every renderer onto a copy of a
// game material that uses the "General Tower" shader graph (the stock ore's, or Smacky's archer tower's), with the
// palette as its texture: the way the vessel parts light their towers (YosseleAssets.ApplyTowerMaterial), and the shader
// Entity_Vis_Resource flashes when a node is hit. Temporary art (docs/TEMP_ART.md). The vessel parts each carry the
// same lighting step (YosseleAssets.ApplyTowerMaterial and its twins); a kit helper for all of them is left to a kit
// change.
public static class RWModels
{
    public const string Meteor = "RW_Meteor";     // Mind the Meteor's node, a white crystal on a fallen stone (scope 14)

    static readonly List<string> lit = new List<string>();    // no HashSet: the game's compiler has no System.Core
    static Material tower;

    // The library model of this name, lit with a copy of 'like' (a game renderer's material) once; null when the
    // library doesn't hold it (a build from before 2026-11-05) or no game tower material could be found yet (then the
    // caller keeps the stock look, and the next call tries again): an unlit model has no hit flash.
    public static GameObject Get(string name, Material like)
    {
        if (!RWAssets.Find())
            return null;
        Transform t = RWAssets.Library.Find("Models/" + name);
        if (t == null)
            return null;
        if (!lit.Contains(name))
        {
            if (!Light(t.gameObject, like))
                return null;
            lit.Add(name);
        }
        return t.gameObject;
    }

    // Whether every renderer of this look draws with the game's tower shader (what the hit flash needs).
    public static bool IsLit(GameObject look)
    {
        if (look == null)
            return false;
        MeshRenderer[] all = look.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer r in all)
            if (r.sharedMaterial == null || r.sharedMaterial.shader == null || r.sharedMaterial.shader.name != TowerShader)
                return false;
        return all.Length > 0;
    }

    // The first material of a game model's renderers, or null.
    public static Material MaterialOf(GameObject model)
    {
        MeshRenderer r = model != null ? model.GetComponentInChildren<MeshRenderer>(true) : null;
        return r != null ? r.sharedMaterial : null;
    }

    const string TowerShader = "Shader Graphs/General Tower";

    static bool Light(GameObject model, Material like)
    {
        if (like == null || like.shader == null || like.shader.name != TowerShader)
            like = ArcherMaterial();
        if (like == null || like.shader == null || like.shader.name != TowerShader)
        {
            RWLog.Warn("models: no game material on " + TowerShader + " found yet; " + model.name + " waits, the stock look stays");
            return false;
        }
        MeshRenderer[] all = model.GetComponentsInChildren<MeshRenderer>(true);
        Texture palette = null;
        foreach (MeshRenderer r in all)
            if (palette == null && r.sharedMaterial != null)
                palette = r.sharedMaterial.HasProperty("_BaseMap") ? r.sharedMaterial.GetTexture("_BaseMap") : r.sharedMaterial.mainTexture;
        if (palette == null)
        {
            RWLog.Warn("models: " + model.name + " has no palette texture; the stock look stays");
            return false;
        }
        palette.filterMode = FilterMode.Point;
        Material mat = new Material(like);
        mat.name = model.name + "_Tower";
        mat.SetTexture("_MainTex", palette);
        if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", palette);
        foreach (MeshRenderer r in all)
            r.sharedMaterial = mat;
        RWLog.Info("models: " + model.name + " lit with a copy of " + like.name + " (" + like.shader.name + ")");
        return true;
    }

    static Material ArcherMaterial()
    {
        if (tower != null)
            return tower;
        Beingobject archer = ModKitUtil.Load<Beingobject>("Mob", "Tower_Smackyarcher");
        GameObject prefab = archer != null && archer.Entity != null && archer.Entity.Defaultvisual != null ? archer.Entity.Defaultvisual.Default.Visprefab : null;
        tower = MaterialOf(prefab);
        return tower;
    }
}
