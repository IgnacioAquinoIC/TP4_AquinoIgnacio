using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    public static class Util
    {
        public static IEnumerator Tween(float duration, Action<float> apply)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                apply(Mathf.Clamp01(t / duration));
                yield return null;
            }
            apply(1f);
        }

        public static Color WithAlpha(Color c, float a) { c.a = a; return c; }
        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
    }

    public static class MaterialFactory
    {
        static Shader lit, sprite;

        public static Material Lit(Color c, float smoothness = 0.3f, Color? emission = null)
        {
            if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");
            var m = new Material(lit);
            m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            return m;
        }

        public static Material Overlay(Color c, Texture tex = null)
        {
            if (sprite == null) sprite = Shader.Find("Sprites/Default");
            var m = new Material(sprite);
            m.color = c;
            if (tex != null) m.mainTexture = tex;
            return m;
        }
    }

    public static class TextureFactory
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        static Texture2D Get(string key, int w, int h, Func<float, float, Color> f)
        {
            if (cache.TryGetValue(key, out var t) && t != null) return t;
            t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    t.SetPixel(x, y, f((x + 0.5f) / w * 2f - 1f, (y + 0.5f) / h * 2f - 1f));
            t.Apply();
            cache[key] = t;
            return t;
        }

        public static Texture2D Disc() => Get("disc", 64, 64, (u, v) =>
        {
            float r = Mathf.Sqrt(u * u + v * v);
            return new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - r), 1.4f));
        });

        public static Texture2D Ring() => Get("ring", 128, 128, (u, v) =>
        {
            float r = Mathf.Sqrt(u * u + v * v);
            float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.85f) / 0.12f);
            return new Color(1, 1, 1, r <= 1f ? a : 0f);
        });

        public static Texture2D Streak() => Get("streak", 128, 32, (u, v) =>
        {
            float a = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(v)), 2f) * Mathf.Clamp01(1f - Mathf.Pow(Mathf.Abs(u), 4f));
            return new Color(1, 1, 1, a);
        });

        public static Texture2D Card() => Get("card", 128, 192, (u, v) =>
        {
            float au = Mathf.Abs(u), av = Mathf.Abs(v);
            Color gold = new Color(0.95f, 0.78f, 0.3f, 1f);
            if (au > 0.88f || av > 0.92f) return gold;
            float d = au + av * 0.6f;
            if (d > 0.42f && d < 0.5f) return gold;
            float star = Mathf.Clamp01(1f - (au * au + av * av) * 18f);
            Color inner = Color.Lerp(new Color(0.18f, 0.06f, 0.3f, 1f), new Color(0.6f, 0.3f, 0.9f, 1f), 1f - av);
            return Color.Lerp(inner, Color.white, star);
        });
    }
}
