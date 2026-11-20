using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    /// Builds a stone plaza around the grid: lamp posts, trees, planters, pillars on blocked cells.
    public static class EnvironmentBuilder
    {
        static GameObject P(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale, Color c, bool glow = false)
        {
            var g = GameObject.CreatePrimitive(t);
            var col = g.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            g.transform.SetParent(parent, false);
            g.transform.position = pos; g.transform.localScale = scale;
            g.GetComponent<Renderer>().material = MaterialFactory.Lit(c, 0.2f, glow ? (Color?)(c * 2f) : null);
            return g;
        }


        class PieceInfo { public Quaternion rot; public Vector3 offset; }
        static readonly Dictionary<string, PieceInfo> infos = new Dictionary<string, PieceInfo>();
        static float kitScale = 1f;

        static Bounds Measure(GameObject go)
        {
            bool first = true; var b = new Bounds();
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
            return b;
        }

        static int ThinAxis(Vector3 s) => s.x <= s.y && s.x <= s.z ? 0 : (s.y <= s.z ? 1 : 2);
        static int LongAxis(Vector3 s) => s.x >= s.y && s.x >= s.z ? 0 : (s.y >= s.z ? 1 : 2);

        static PieceInfo Info(string name, GameObject prefab, bool floor)
        {
            if (infos.TryGetValue(name, out var pi)) return pi;
            var tmp = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            var b0 = Measure(tmp);
            int axis = floor ? ThinAxis(b0.size) : LongAxis(b0.size);
            Quaternion rot = axis == 2 ? Quaternion.Euler(-90, 0, 0) : (axis == 0 ? Quaternion.Euler(0, 0, 90) : Quaternion.identity);
            tmp.transform.rotation = rot;
            tmp.transform.localScale = Vector3.one * kitScale;
            var b = Measure(tmp);
            pi = new PieceInfo { rot = rot, offset = new Vector3(-b.center.x, floor ? -b.max.y + 0.01f : -b.min.y, -b.center.z) };
            Object.Destroy(tmp);
            infos[name] = pi;
            return pi;
        }

        static GameObject Piece(string name, bool floor, Vector3 pos, float yaw, Material mat, Transform parent)
        {
            var prefab = Resources.Load<GameObject>("Environment/" + name);
            if (prefab == null) return null;
            var pi = Info(name, prefab, floor);
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            var inst = Object.Instantiate(prefab, holder.transform);
            inst.transform.localRotation = pi.rot;
            inst.transform.localScale = Vector3.one * kitScale;
            inst.transform.localPosition = pi.offset;
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
            holder.transform.position = pos;
            holder.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return holder;
        }

        static void Lighting(Transform parent)
        {
            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lg = new GameObject("Sun");
                lg.transform.SetParent(parent, false);
                var l = lg.AddComponent<Light>();
                l.type = LightType.Directional; l.intensity = 1.15f; l.color = new Color(1f, 0.96f, 0.88f);
                l.transform.rotation = Quaternion.Euler(52f, 70f, 0f);
                l.shadows = LightShadows.Soft;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.54f, 0.6f);
        }

        /// Returns true when the imported floor kit was used (GridView then skips its own tile cubes).
        public static bool Build(BattleGrid g, BattleConfig cfg, Transform parent)
        {
            var f1 = Resources.Load<GameObject>("Environment/Floor_1");
            var palette = Resources.Load<Texture2D>("Environment/Color2");
            if (f1 == null) { BuildProcedural(g, cfg, parent); return false; }

            infos.Clear();
            var probe = Object.Instantiate(f1, Vector3.zero, Quaternion.identity);
            var pb = Measure(probe).size;
            Object.Destroy(probe);
            kitScale = 2f / Mathf.Max(pb.x, Mathf.Max(pb.y, pb.z));

            var env = new GameObject("Environment").transform;
            env.SetParent(parent, false);
            var arenaMat = MaterialFactory.Lit(Color.white, 0.1f);
            var outerMat = MaterialFactory.Lit(new Color(0.6f, 0.6f, 0.66f), 0.1f);
            if (palette != null)
            {
                arenaMat.mainTexture = palette; outerMat.mainTexture = palette;
                if (arenaMat.HasProperty("_BaseMap")) { arenaMat.SetTexture("_BaseMap", palette); outerMat.SetTexture("_BaseMap", palette); }
            }
            var rnd = new System.Random(11);
            string[] variants = { "Floor_4", "Floor_5", "Floor_6", "Floor_7", "Floor_8" };
            int tilesX = Mathf.CeilToInt(g.Width / 2f), tilesY = Mathf.CeilToInt(g.Height / 2f);
            float half = 0.5f * g.CellSize;

            // Playable area: 2x2 kit tiles aligned exactly to 2x2 grid cells.
            for (int i = 0; i < tilesX; i++)
                for (int j = 0; j < tilesY; j++)
                {
                    string n = rnd.NextDouble() < 0.5 ? "Floor_1" : variants[rnd.Next(variants.Length)];
                    Piece(n, true, new Vector3((2 * i + 0.5f) * g.CellSize, 0f, (2 * j + 0.5f) * g.CellSize), 90f * rnd.Next(4), arenaMat, env);
                }
            // Surrounding floor, darker, mostly plain tiles.
            for (int i = -6; i < tilesX + 6; i++)
                for (int j = -6; j < tilesY + 8; j++)
                {
                    if (i >= 0 && i < tilesX && j >= 0 && j < tilesY) continue;
                    string n = rnd.NextDouble() < 0.9 ? "Floor_1" : variants[rnd.Next(variants.Length)];
                    Piece(n, true, new Vector3((2 * i + 0.5f) * g.CellSize, 0f, (2 * j + 0.5f) * g.CellSize), 90f * rnd.Next(4), outerMat, env);
                }
            var under = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(under.GetComponent<Collider>());
            under.transform.SetParent(env, false);
            under.transform.position = new Vector3(g.Center.x, -0.4f, g.Center.z);
            under.transform.localScale = new Vector3(160f, 0.3f, 160f);
            under.GetComponent<Renderer>().material = MaterialFactory.Lit(new Color(0.12f, 0.12f, 0.15f), 0.05f);

            // Playable-area outline (flat strips, no walls).
            var edge = new Color(0.92f, 0.92f, 0.95f);
            float W = g.Width * g.CellSize, H = g.Height * g.CellSize;
            P(env, PrimitiveType.Cube, new Vector3(g.Center.x, 0.02f, -half - 0.05f), new Vector3(W + 0.3f, 0.03f, 0.1f), edge);
            P(env, PrimitiveType.Cube, new Vector3(g.Center.x, 0.02f, H - half + 0.05f), new Vector3(W + 0.3f, 0.03f, 0.1f), edge);
            P(env, PrimitiveType.Cube, new Vector3(-half - 0.05f, 0.02f, g.Center.z), new Vector3(0.1f, 0.03f, H + 0.3f), edge);
            P(env, PrimitiveType.Cube, new Vector3(W - half + 0.05f, 0.02f, g.Center.z), new Vector3(0.1f, 0.03f, H + 0.3f), edge);

            // Props: only on the far sides (high x / high z), outside the playable area.
            float farZ = H - half + 1.4f, farX = W - half + 1.4f;
            for (float x = 0f; x <= W - 1f; x += 4f) Piece("Pillar_Standalone", false, new Vector3(x, 0f, farZ), 0f, outerMat, env);
            for (float z = 0f; z <= H - 1f; z += 4f) Piece("Pillar_Standalone", false, new Vector3(farX, 0f, z), 0f, outerMat, env);
            Piece("Pillar_Corner", false, new Vector3(farX, 0f, farZ), 0f, outerMat, env);

            // Blocked cells: low stumps so nothing can hide a character from the camera.
            if (cfg.blockedCells != null)
                foreach (var b in cfg.blockedCells)
                {
                    var stump = Piece("Pillar_Standalone", false, g.ToWorld(b), 0f, outerMat, env);
                    if (stump != null) stump.transform.localScale = new Vector3(1f, 0.22f, 1f);
                }
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = new Color(0.1f, 0.11f, 0.15f);
            Lighting(parent);
            return true;
        }

        static void BuildProcedural(BattleGrid g, BattleConfig cfg, Transform parent)
        {
            var env = new GameObject("Environment").transform;
            env.SetParent(parent, false);
            float W = g.Width * g.CellSize, H = g.Height * g.CellSize;
            Vector3 c = g.Center;
            P(env, PrimitiveType.Cube, new Vector3(c.x, -0.15f, c.z), new Vector3(W + 0.1f, 0.1f, H + 0.1f), new Color(0.25f, 0.18f, 0.15f));
            P(env, PrimitiveType.Cube, new Vector3(c.x, -0.4f, c.z), new Vector3(W + 22f, 0.3f, H + 22f), new Color(0.6f, 0.58f, 0.52f));
            var edge = new Color(0.95f, 0.95f, 0.95f);
            float ex = -0.5f * g.CellSize, ez = -0.5f * g.CellSize;
            P(env, PrimitiveType.Cube, new Vector3(c.x, 0.03f, ez - 0.05f), new Vector3(W + 0.3f, 0.06f, 0.12f), edge);
            P(env, PrimitiveType.Cube, new Vector3(ex - 0.05f, 0.03f, c.z), new Vector3(0.12f, 0.06f, H + 0.3f), edge);
            P(env, PrimitiveType.Cube, new Vector3(c.x, 0.03f, H + ez + 0.05f), new Vector3(W + 0.3f, 0.06f, 0.12f), edge);
            P(env, PrimitiveType.Cube, new Vector3(W + ex + 0.05f, 0.03f, c.z), new Vector3(0.12f, 0.06f, H + 0.3f), edge);

            var lampCols = new Color(0.15f, 0.3f, 0.2f);
            Vector3[] lamps =
            {
                new Vector3(-2f, 0, H + 0.5f), new Vector3(W + 0.5f, 0, H + 0.5f), new Vector3(W + 0.5f, 0, -2f)
            };
            foreach (var p in lamps)
            {
                P(env, PrimitiveType.Cylinder, p + Vector3.up * 1.2f, new Vector3(0.15f, 1.2f, 0.15f), lampCols);
                P(env, PrimitiveType.Sphere, p + Vector3.up * 2.55f, new Vector3(0.4f, 0.4f, 0.4f), new Color(1f, 0.85f, 0.5f), true);
            }
            var rnd = new System.Random(7);
            for (int i = 0; i < 12; i++)
            {
                bool alongX = i % 2 == 0;
                float u = (float)rnd.NextDouble();
                Vector3 p = alongX ? new Vector3(u * W, 0, H + 3f + (float)rnd.NextDouble() * 3f) : new Vector3(W + 3f + (float)rnd.NextDouble() * 3f, 0, u * H);
                float s = 0.8f + (float)rnd.NextDouble() * 0.6f;
                P(env, PrimitiveType.Cylinder, p + Vector3.up * 1f * s, new Vector3(0.35f, 1f * s, 0.35f), new Color(0.35f, 0.25f, 0.15f));
                P(env, PrimitiveType.Sphere, p + Vector3.up * 2.6f * s, new Vector3(2.4f * s, 2.0f * s, 2.4f * s), new Color(0.18f, 0.45f, 0.2f));
            }
            if (cfg.blockedCells != null)
                foreach (var b in cfg.blockedCells)
                {
                    var p = g.ToWorld(b);
                    P(env, PrimitiveType.Cylinder, p + Vector3.up * 0.25f, new Vector3(0.8f, 0.25f, 0.8f), new Color(0.62f, 0.6f, 0.58f));
                    P(env, PrimitiveType.Cube, p + Vector3.up * 0.52f, new Vector3(0.95f, 0.08f, 0.95f), new Color(0.5f, 0.48f, 0.46f));
                }

            var existing = Object.FindFirstObjectByType<Light>();
            if (existing == null)
            {
                var lg = new GameObject("Sun");
                lg.transform.SetParent(parent, false);
                var l = lg.AddComponent<Light>();
                l.type = LightType.Directional; l.intensity = 1.2f; l.color = new Color(1f, 0.95f, 0.85f);
                l.transform.rotation = Quaternion.Euler(52f, 70f, 0f);
                l.shadows = LightShadows.Soft;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.65f);
        }
    }
}
