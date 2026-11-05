// The mode part's model library (2026-11-05, open decisions 11-05b item 10): Build All Assets builds each model below
// into RWAssets_Library/Models/<Name>, the way the vessel parts build theirs, and the code picks them up by name
// (RWModels in Code/25_Models.cs). Temporary art (docs/TEMP_ART.md): a final model of the same name replaces it.
//
// RW_Meteor: Mind the Meteor's node (scope 14, "a node with a white crystal"): a scorched stone that fell, a cluster of
// faceted white crystals growing out of it, a ring of ash round the impact. White, never magenta (the foes' crystal,
// kit docs/18 §5): the crystal's faces step from white to a pale cold blue, lit from the upper left like the cast.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueWavesModeBuild
{
    public class ModelDef
    {
        public string Name;
        public string Group;
        public bool Projectile;
        public Func<Lp> Build;
    }

    public static class RogueWavesModeModels
    {
        static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

        public static List<ModelDef> All()
        {
            return new List<ModelDef>
            {
                new ModelDef { Name = "RW_Meteor", Group = "Objectives", Build = Meteor },
            };
        }

        // The light the faces are picked against: from the upper left and a little in front, like the sprites' light.
        static readonly Vector3 Light = new Vector3(-0.55f, 0.75f, -0.38f).normalized;

        // One faceted crystal: a six-sided shaft from 'at' along 'dir', then a point. Each face takes a palette step by how
        // much it faces the light (faceted, not smooth-shaded: the faces read as cut stone at the game's zoom).
        static void Crystal(Lp lp, Vector3 at, Vector3 dir, float r, float len, float tip, float yawDeg = 0f)
        {
            dir.Normalize();
            Vector3 u = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 w = Vector3.Cross(dir, u).normalized;
            const int sides = 6;
            var low = new Vector3[sides];
            var high = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = (i * 60f + yawDeg) * Mathf.Deg2Rad;
                Vector3 o = (u * Mathf.Cos(a) + w * Mathf.Sin(a)) * r;
                low[i] = at + o;
                high[i] = at + dir * len + o * 0.92f;
            }
            Vector3 point = at + dir * (len + tip);
            Vector3 inside = at + dir * (len * 0.5f);
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                Vector3 n = Vector3.Cross(low[j] - low[i], high[i] - low[i]).normalized;
                if (Vector3.Dot(n, (low[i] + low[j]) * 0.5f - inside) < 0f) n = -n;
                lp.Quad(low[i], low[j], high[j], high[i], Face(n, false), inside);
                Vector3 m = Vector3.Cross(high[j] - high[i], point - high[i]).normalized;
                if (Vector3.Dot(m, (high[i] + high[j] + point) / 3f - inside) < 0f) m = -m;
                lp.Tri(high[i], high[j], point, Face(m, true), inside);
            }
        }

        static int Face(Vector3 n, bool tip)
        {
            float d = Vector3.Dot(n, Light);
            int k = d > 0.55f ? Pal.Crystal1 : d > 0.2f ? Pal.Crystal2 : d > -0.15f ? Pal.Crystal4 : d > -0.5f ? Pal.Crystal5 : Pal.Crystal6;
            return tip ? Mathf.Max(Pal.Crystal0, k - 1) : k;
        }

        static Lp Meteor()
        {
            var lp = new Lp();
            // the impact: a ring of ash and a darker floor under the stone (flat, just above the ground)
            lp.Disc(V(0f, 0f, 0f), 0.46f, 9, Pal.Ash1, 0.012f, 12f);
            lp.Disc(V(0.01f, 0.012f, 0.01f), 0.34f, 8, Pal.Ash3, 0.01f, 31f);
            // the fallen stone, squat and lumpy, with two broken pieces beside it
            lp.Ball(V(0f, 0.11f, 0.02f), V(0.29f, 0.17f, 0.26f), 7, 4, Pal.Stone4, Pal.Stone2, 0.5f, Pal.Stone6, -0.4f, 14f);
            lp.Ball(V(0.2f, 0.07f, 0.17f), V(0.12f, 0.09f, 0.1f), 5, 3, Pal.Stone4, Pal.Stone2, 0.5f, -1, -0.6f, 40f);
            lp.Ball(V(-0.25f, 0.05f, -0.14f), V(0.08f, 0.06f, 0.08f), 5, 3, Pal.Stone4, Pal.Stone2, 0.5f, -1, -0.6f, 5f);
            // the white crystal: one tall shaft and three smaller ones leaning out, all from the stone's top
            Crystal(lp, V(0.02f, 0.16f, 0.01f), V(0.06f, 1f, -0.04f), 0.095f, 0.36f, 0.14f, 10f);
            Crystal(lp, V(0.11f, 0.15f, -0.07f), V(0.55f, 1f, -0.25f), 0.07f, 0.19f, 0.09f, 25f);
            Crystal(lp, V(-0.11f, 0.15f, 0.06f), V(-0.6f, 1f, 0.2f), 0.065f, 0.17f, 0.08f, 0f);
            Crystal(lp, V(-0.05f, 0.13f, -0.14f), V(-0.25f, 0.85f, -0.6f), 0.05f, 0.1f, 0.06f, 40f);
            return lp;
        }
    }
}
