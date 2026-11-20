using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    /// Synthesised placeholder sound effects. Anything in an assigned SfxLibrary takes priority.
    public static class ProceduralSfx
    {
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        const int Rate = 44100;

        public static AudioClip Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (cache.TryGetValue(key, out var c) && c != null) return c;
            c = Make(key);
            if (c != null) cache[key] = c;
            return c;
        }

        static AudioClip Make(string k)
        {
            switch (k)
            {
                case "ui_move": return Gen(k, 0.05f, 900, 900, 0f, 10);
                case "ui_confirm": return Gen(k, 0.14f, 600, 1100, 0f, 7);
                case "ui_back": return Gen(k, 0.12f, 520, 300, 0f, 8);
                case "battle_start": return Gen(k, 0.9f, 220, 880, 0.1f, 2.5f, 1);
                case "sword_swing": return Gen(k, 0.25f, 1400, 300, 0.8f, 5);
                case "magic_shot": return Gen(k, 0.3f, 300, 1300, 0.1f, 4);
                case "art_cast": return Gen(k, 0.5f, 400, 900, 0f, 2);
                case "special": return Gen(k, 0.6f, 300, 1600, 0.2f, 3, 2);
                case "ultimate": return Gen(k, 1.6f, 90, 900, 0.3f, 1.1f, 1);
                case "impact": return Gen(k, 0.18f, 200, 50, 0.6f, 10);
                case "miss": return Gen(k, 0.2f, 700, 500, 0.7f, 8);
                case "heal": return Gen(k, 0.6f, 500, 1100, 0f, 3);
                case "buff": return Gen(k, 0.4f, 600, 1300, 0f, 4);
                case "debuff": return Gen(k, 0.4f, 500, 180, 0.1f, 4);
                case "status": return Gen(k, 0.3f, 700, 400, 0.3f, 6);
                case "ko": return Gen(k, 0.7f, 300, 50, 0.4f, 4);
                case "victory": return Seq(k, new float[] { 523, 659, 784, 1047, 784, 1047 }, 0.16f);
                case "defeat": return Seq(k, new float[] { 392, 330, 262, 196 }, 0.3f);
                default: return Gen(k, 0.15f, 500, 500, 0f, 8);
            }
        }

        static AudioClip Gen(string n, float dur, float f0, float f1, float noise, float decay, int wave = 0)
        {
            int len = (int)(Rate * dur);
            var d = new float[len];
            var rnd = new System.Random(n.GetHashCode());
            float ph = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                ph += 2f * Mathf.PI * Mathf.Lerp(f0, f1, t) / Rate;
                float s = wave == 0 ? Mathf.Sin(ph) : wave == 1 ? Mathf.Sign(Mathf.Sin(ph)) * 0.5f : Mathf.PingPong(ph / Mathf.PI, 1f) * 2f - 1f;
                float nz = (float)(rnd.NextDouble() * 2 - 1);
                d[i] = (s * (1f - noise) + nz * noise) * Mathf.Exp(-decay * t) * Mathf.Min(1f, i / 150f) * 0.45f;
            }
            var clip = AudioClip.Create(n, len, 1, Rate, false);
            clip.SetData(d, 0);
            return clip;
        }

        static AudioClip Seq(string n, float[] freqs, float noteDur)
        {
            int per = (int)(Rate * noteDur), len = per * freqs.Length;
            var d = new float[len];
            for (int k = 0; k < freqs.Length; k++)
                for (int i = 0; i < per; i++)
                {
                    float t = i / (float)per;
                    d[k * per + i] = Mathf.Sin(2f * Mathf.PI * freqs[k] * i / Rate) * Mathf.Exp(-3f * t) * 0.4f;
                }
            var clip = AudioClip.Create(n, len, 1, Rate, false);
            clip.SetData(d, 0);
            return clip;
        }
    }
}
