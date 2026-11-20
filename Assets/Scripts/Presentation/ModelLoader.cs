using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    /// Instantiates a model from Resources, applies texture rules, normalises scale/feet and facing.
    public static class ModelLoader
    {
        static Shader lit;

        public static GameObject Load(UnitData d)
        {
            var prefab = Resources.Load<GameObject>(d.modelResourcePath);
            if (prefab == null) { Debug.LogWarning("Model not found at Resources/" + d.modelResourcePath); return null; }
            var holder = new GameObject("ModelHolder");
            var model = Object.Instantiate(prefab, holder.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            ApplyTextureRules(model, d.textureRules);

            var b = Measure(model);
            float h = Mathf.Max(0.0001f, b.size.y);
            float s = d.targetHeight / h;
            model.transform.localScale = Vector3.one * s;
            model.transform.localPosition = new Vector3(0f, -b.min.y * s, 0f);
            model.transform.localRotation = Quaternion.Euler(0f, d.modelYaw, 0f);
            return holder;
        }

        public static Bounds Measure(GameObject go)
        {
            bool first = true; var total = new Bounds();
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                Bounds rb;
                if (r is SkinnedMeshRenderer s && s.sharedMesh != null) rb = Transform(s.sharedMesh.bounds, s.transform.localToWorldMatrix);
                else rb = r.bounds;
                if (first) { total = rb; first = false; } else total.Encapsulate(rb);
            }
            return total;
        }

        static Bounds Transform(Bounds b, Matrix4x4 m)
        {
            var c = b.center; var e = b.extents; bool first = true; var o = new Bounds();
            for (int i = 0; i < 8; i++)
            {
                var p = m.MultiplyPoint3x4(c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z));
                if (first) { o = new Bounds(p, Vector3.zero); first = false; } else o.Encapsulate(p);
            }
            return o;
        }

        static void ApplyTextureRules(GameObject model, List<TextureRule> rules)
        {
            if (rules == null || rules.Count == 0) return;
            if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");
            var cache = new Dictionary<string, Material>();
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string mn = mats[i] != null ? mats[i].name.ToLowerInvariant() : "";
                    TextureRule pick = null;
                    foreach (var rule in rules)
                        if (!string.IsNullOrEmpty(rule.materialNameContains) && mn.Contains(rule.materialNameContains.ToLowerInvariant())) { pick = rule; break; }
                    if (pick == null) foreach (var rule in rules) if (string.IsNullOrEmpty(rule.materialNameContains)) { pick = rule; break; }
                    if (pick == null) continue;
                    if (!cache.TryGetValue(pick.texturePath, out var m))
                    {
                        var tex = Resources.Load<Texture2D>(pick.texturePath);
                        if (tex == null) { Debug.LogWarning("Texture not found: " + pick.texturePath); continue; }
                        m = new Material(lit) { mainTexture = tex, color = Color.white };
                        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
                        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                        cache[pick.texturePath] = m;
                    }
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
            }
        }
    }
}
