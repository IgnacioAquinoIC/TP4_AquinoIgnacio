using UnityEngine;

namespace RpgBattle
{
    public class ModelParts
    {
        public GameObject root;
        public Transform armR, armL;
        public float height = 1.9f;
    }

    /// Stylised placeholder models assembled from primitives, coloured after the supplied references.
    /// Replace per unit by assigning UnitData.modelPrefab (+ animatorController).
    public static class ProceduralModels
    {
        static readonly Color Skin = new Color(0.95f, 0.8f, 0.7f);
        static readonly Color Gold = new Color(0.82f, 0.62f, 0.25f);
        static readonly Color Silver = new Color(0.8f, 0.82f, 0.88f);

        static GameObject P(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale, Color c, Vector3 euler = default, bool glow = false)
        {
            var g = GameObject.CreatePrimitive(t);
            var col = g.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.transform.localEulerAngles = euler;
            g.GetComponent<Renderer>().material = MaterialFactory.Lit(c, 0.35f, glow ? (Color?)(c * 1.6f) : null);
            return g;
        }

        static Transform Arm(Transform parent, string name, Vector3 shoulder, Color sleeve, Color hand)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = shoulder;
            P(pivot, PrimitiveType.Capsule, new Vector3(0, -0.28f, 0), new Vector3(0.13f, 0.28f, 0.13f), sleeve);
            P(pivot, PrimitiveType.Sphere, new Vector3(0, -0.58f, 0), new Vector3(0.12f, 0.12f, 0.12f), hand);
            return pivot;
        }

        public static ModelParts Build(UnitData d)
        {
            switch (d.modelStyle)
            {
                case ModelStyle.Fang: return Fang();
                case ModelStyle.Xiangling: return Xiangling();
                default: return Enemy(d.modelStyle, d.tint);
            }
        }

        static ModelParts Fang()
        {
            var mp = new ModelParts { root = new GameObject("Model"), height = 2.0f };
            var t = mp.root.transform;
            var teal = new Color(0.1f, 0.42f, 0.4f); var cream = new Color(0.86f, 0.8f, 0.66f);
            var white = new Color(0.93f, 0.93f, 0.9f); var red = new Color(0.72f, 0.12f, 0.15f);
            var navy = new Color(0.14f, 0.16f, 0.28f); var hair = new Color(0.1f, 0.1f, 0.12f);
            P(t, PrimitiveType.Cylinder, new Vector3(-0.12f, 0.45f, 0), new Vector3(0.17f, 0.45f, 0.17f), new Color(0.1f, 0.1f, 0.13f));
            P(t, PrimitiveType.Cylinder, new Vector3(0.12f, 0.45f, 0), new Vector3(0.17f, 0.45f, 0.17f), new Color(0.1f, 0.1f, 0.13f));
            P(t, PrimitiveType.Cube, new Vector3(-0.12f, 0.07f, 0.04f), new Vector3(0.18f, 0.12f, 0.28f), navy);
            P(t, PrimitiveType.Cube, new Vector3(0.12f, 0.07f, 0.04f), new Vector3(0.18f, 0.12f, 0.28f), navy);
            P(t, PrimitiveType.Cube, new Vector3(0, 0.75f, 0), new Vector3(0.5f, 0.45f, 0.3f), teal);
            P(t, PrimitiveType.Cube, new Vector3(0, 0.55f, -0.17f), new Vector3(0.44f, 0.7f, 0.05f), cream, new Vector3(8, 0, 0));
            P(t, PrimitiveType.Cube, new Vector3(0, 1.2f, 0), new Vector3(0.46f, 0.55f, 0.27f), white);
            P(t, PrimitiveType.Cube, new Vector3(-0.2f, 1.22f, 0.02f), new Vector3(0.1f, 0.55f, 0.29f), teal);
            P(t, PrimitiveType.Cube, new Vector3(0.2f, 1.22f, 0.02f), new Vector3(0.1f, 0.55f, 0.29f), teal);
            P(t, PrimitiveType.Cylinder, new Vector3(0, 0.98f, 0), new Vector3(0.5f, 0.05f, 0.34f), red);
            P(t, PrimitiveType.Sphere, new Vector3(-0.31f, 1.46f, 0), new Vector3(0.2f, 0.17f, 0.2f), Gold);
            P(t, PrimitiveType.Sphere, new Vector3(0.31f, 1.46f, 0), new Vector3(0.2f, 0.17f, 0.2f), Silver);
            P(t, PrimitiveType.Sphere, new Vector3(0, 1.7f, 0), new Vector3(0.3f, 0.32f, 0.3f), Skin);
            P(t, PrimitiveType.Sphere, new Vector3(0, 1.76f, -0.03f), new Vector3(0.34f, 0.3f, 0.34f), hair);
            P(t, PrimitiveType.Cube, new Vector3(0.02f, 1.77f, 0.13f), new Vector3(0.28f, 0.09f, 0.08f), hair, new Vector3(0, 0, 8));
            mp.armR = Arm(t, "ArmR", new Vector3(0.32f, 1.42f, 0), teal, Skin);
            mp.armL = Arm(t, "ArmL", new Vector3(-0.32f, 1.42f, 0), teal, Skin);
            P(mp.armR, PrimitiveType.Cube, new Vector3(0, -0.58f, 0.5f), new Vector3(0.035f, 0.07f, 1.0f), Silver);
            P(mp.armR, PrimitiveType.Cube, new Vector3(0, -0.58f, -0.02f), new Vector3(0.1f, 0.04f, 0.04f), Gold);
            P(t, PrimitiveType.Cube, new Vector3(-0.27f, 0.7f, -0.05f), new Vector3(0.08f, 0.08f, 0.75f), white, new Vector3(-35, 0, 0));
            return mp;
        }

        static ModelParts Xiangling()
        {
            var mp = new ModelParts { root = new GameObject("Model"), height = 2.25f };
            var t = mp.root.transform;
            var black = new Color(0.07f, 0.08f, 0.14f); var teal = new Color(0.2f, 0.7f, 0.6f);
            var hair = new Color(0.14f, 0.1f, 0.1f);
            P(t, PrimitiveType.Cylinder, new Vector3(0, 0.5f, 0), new Vector3(0.7f, 0.5f, 0.7f), black);
            P(t, PrimitiveType.Cylinder, new Vector3(0, 0.02f, 0), new Vector3(0.74f, 0.02f, 0.74f), Gold);
            P(t, PrimitiveType.Capsule, new Vector3(0, 1.15f, 0), new Vector3(0.36f, 0.3f, 0.26f), black);
            P(t, PrimitiveType.Cylinder, new Vector3(0, 0.98f, 0), new Vector3(0.42f, 0.025f, 0.3f), Gold);
            P(t, PrimitiveType.Cube, new Vector3(0, 1.2f, 0.14f), new Vector3(0.2f, 0.35f, 0.03f), Gold);
            P(t, PrimitiveType.Sphere, new Vector3(0, 1.38f, 0.1f), new Vector3(0.1f, 0.1f, 0.05f), teal, default, true);
            P(t, PrimitiveType.Capsule, new Vector3(0, 1.36f, -0.12f), new Vector3(0.28f, 0.42f, 0.18f), hair);
            P(t, PrimitiveType.Sphere, new Vector3(0, 1.62f, 0), new Vector3(0.28f, 0.3f, 0.28f), Skin);
            P(t, PrimitiveType.Sphere, new Vector3(0, 1.68f, -0.03f), new Vector3(0.31f, 0.28f, 0.31f), hair);
            P(t, PrimitiveType.Cylinder, new Vector3(0, 1.8f, 0), new Vector3(0.95f, 0.015f, 0.95f), black, new Vector3(4, 0, 0));
            P(t, PrimitiveType.Cylinder, new Vector3(0, 1.79f, 0), new Vector3(0.97f, 0.01f, 0.97f), Gold);
            P(t, PrimitiveType.Cylinder, new Vector3(0, 1.93f, 0), new Vector3(0.42f, 0.13f, 0.42f), black);
            P(t, PrimitiveType.Cylinder, new Vector3(0, 2.1f, 0), new Vector3(0.22f, 0.1f, 0.22f), black);
            P(t, PrimitiveType.Cylinder, new Vector3(0, 1.86f, 0), new Vector3(0.44f, 0.015f, 0.44f), Gold);
            mp.armR = Arm(t, "ArmR", new Vector3(0.28f, 1.35f, 0), black, Skin);
            mp.armL = Arm(t, "ArmL", new Vector3(-0.28f, 1.35f, 0), black, Skin);
            P(mp.armR, PrimitiveType.Cube, new Vector3(0.05f, -0.3f, 0), new Vector3(0.3f, 0.35f, 0.2f), black, new Vector3(0, 0, 8));
            P(mp.armL, PrimitiveType.Cube, new Vector3(-0.05f, -0.3f, 0), new Vector3(0.3f, 0.35f, 0.2f), black, new Vector3(0, 0, -8));
            P(mp.armR, PrimitiveType.Cylinder, new Vector3(0, -0.45f, 0.1f), new Vector3(0.05f, 0.75f, 0.05f), Gold);
            P(mp.armR, PrimitiveType.Sphere, new Vector3(0, 0.3f, 0.1f), new Vector3(0.22f, 0.22f, 0.22f), new Color(1f, 0.85f, 0.4f), default, true);
            return mp;
        }

        static ModelParts Enemy(ModelStyle s, Color tint)
        {
            var mp = new ModelParts { root = new GameObject("Model"), height = 1.95f };
            var t = mp.root.transform;
            var metal = new Color(0.55f, 0.57f, 0.63f); var dark = new Color(0.18f, 0.18f, 0.22f);
            bool caster = s == ModelStyle.RangedMage || s == ModelStyle.RangedOrb;
            P(t, PrimitiveType.Cylinder, new Vector3(-0.12f, 0.4f, 0), new Vector3(0.18f, 0.4f, 0.18f), dark);
            P(t, PrimitiveType.Cylinder, new Vector3(0.12f, 0.4f, 0), new Vector3(0.18f, 0.4f, 0.18f), dark);
            if (caster) P(t, PrimitiveType.Cylinder, new Vector3(0, 0.7f, 0), new Vector3(0.6f, 0.7f, 0.6f), tint);
            P(t, PrimitiveType.Cube, new Vector3(0, 1.05f, 0), new Vector3(0.52f, 0.55f, 0.33f), tint);
            P(t, PrimitiveType.Cube, new Vector3(0, 1.05f, 0.17f), new Vector3(0.3f, 0.35f, 0.04f), metal);
            P(t, PrimitiveType.Sphere, new Vector3(-0.32f, 1.35f, 0), new Vector3(0.22f, 0.2f, 0.22f), metal);
            P(t, PrimitiveType.Sphere, new Vector3(0.32f, 1.35f, 0), new Vector3(0.22f, 0.2f, 0.22f), metal);
            P(t, PrimitiveType.Sphere, new Vector3(0, 1.62f, 0), new Vector3(0.34f, 0.36f, 0.34f), caster ? tint * 0.7f : metal);
            P(t, PrimitiveType.Sphere, new Vector3(0, 1.62f, 0.15f), new Vector3(0.18f, 0.06f, 0.05f), new Color(1f, 0.2f, 0.1f), default, true);
            if (!caster) P(t, PrimitiveType.Cylinder, new Vector3(0, 1.85f, -0.02f), new Vector3(0.06f, 0.14f, 0.3f), tint);
            mp.armR = Arm(t, "ArmR", new Vector3(0.34f, 1.3f, 0), tint, metal);
            mp.armL = Arm(t, "ArmL", new Vector3(-0.34f, 1.3f, 0), tint, metal);
            var gold = new Color(1f, 0.8f, 0.3f);
            switch (s)
            {
                case ModelStyle.MeleeSoldier:
                    P(mp.armR, PrimitiveType.Cylinder, new Vector3(0, -0.58f, 0.6f), new Vector3(0.05f, 0.7f, 0.05f), dark, new Vector3(90, 0, 0));
                    P(mp.armR, PrimitiveType.Cube, new Vector3(0, -0.58f, 1.3f), new Vector3(0.09f, 0.04f, 0.3f), metal);
                    break;
                case ModelStyle.MeleeLancer:
                    P(mp.armR, PrimitiveType.Cylinder, new Vector3(0, -0.58f, 0.9f), new Vector3(0.05f, 1.1f, 0.05f), dark, new Vector3(90, 0, 0));
                    P(mp.armR, PrimitiveType.Cube, new Vector3(0, -0.58f, 2.05f), new Vector3(0.1f, 0.04f, 0.4f), metal);
                    break;
                case ModelStyle.MeleeBrute:
                    P(mp.armR, PrimitiveType.Cylinder, new Vector3(0, -0.58f, 0.45f), new Vector3(0.14f, 0.45f, 0.14f), dark, new Vector3(90, 0, 0));
                    P(mp.armR, PrimitiveType.Sphere, new Vector3(0, -0.58f, 1.0f), new Vector3(0.35f, 0.35f, 0.35f), metal);
                    break;
                case ModelStyle.MeleeBlade:
                    P(mp.armR, PrimitiveType.Cube, new Vector3(0, -0.58f, 0.45f), new Vector3(0.03f, 0.08f, 0.8f), metal);
                    P(mp.armL, PrimitiveType.Cube, new Vector3(0, -0.58f, 0.45f), new Vector3(0.03f, 0.08f, 0.8f), metal);
                    break;
                case ModelStyle.MeleeShield:
                    P(mp.armR, PrimitiveType.Cube, new Vector3(0, -0.58f, 0.4f), new Vector3(0.04f, 0.07f, 0.65f), metal);
                    P(mp.armL, PrimitiveType.Cube, new Vector3(-0.1f, -0.4f, 0.2f), new Vector3(0.08f, 0.8f, 0.55f), metal);
                    P(mp.armL, PrimitiveType.Cube, new Vector3(-0.15f, -0.4f, 0.2f), new Vector3(0.03f, 0.3f, 0.25f), tint);
                    break;
                case ModelStyle.MeleeBerserker:
                    P(mp.armR, PrimitiveType.Cylinder, new Vector3(0, -0.58f, 0.4f), new Vector3(0.05f, 0.4f, 0.05f), dark, new Vector3(90, 0, 0));
                    P(mp.armR, PrimitiveType.Cube, new Vector3(0, -0.58f, 0.85f), new Vector3(0.05f, 0.5f, 0.4f), metal);
                    break;
                case ModelStyle.RangedArcher:
                    P(mp.armL, PrimitiveType.Cube, new Vector3(0, -0.5f, 0.35f), new Vector3(0.04f, 1.1f, 0.04f), gold);
                    break;
                case ModelStyle.RangedCrossbow:
                    P(mp.armR, PrimitiveType.Cube, new Vector3(0, -0.5f, 0.4f), new Vector3(0.08f, 0.08f, 0.6f), dark);
                    P(mp.armR, PrimitiveType.Cube, new Vector3(0, -0.5f, 0.6f), new Vector3(0.7f, 0.04f, 0.04f), metal);
                    break;
                case ModelStyle.RangedMage:
                    P(mp.armR, PrimitiveType.Cylinder, new Vector3(0, -0.4f, 0.1f), new Vector3(0.05f, 0.9f, 0.05f), dark);
                    P(mp.armR, PrimitiveType.Sphere, new Vector3(0, 0.55f, 0.1f), new Vector3(0.22f, 0.22f, 0.22f), tint * 1.4f, default, true);
                    break;
                case ModelStyle.RangedOrb:
                    P(mp.armR, PrimitiveType.Sphere, new Vector3(0, -0.4f, 0.4f), new Vector3(0.3f, 0.3f, 0.3f), tint * 1.4f, default, true);
                    break;
            }
            return mp;
        }
    }
}
