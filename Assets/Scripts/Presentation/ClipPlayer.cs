using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace RpgBattle
{
    /// Plays imported AnimationClips by key with crossfades, without needing an Animator Controller asset.
    /// Supports non-legacy clips (Playables) and legacy clips (Animation component, e.g. glTFast legacy import).
    public class ClipPlayer : MonoBehaviour
    {
        readonly Dictionary<string, ClipBinding> bindings = new Dictionary<string, ClipBinding>();
        readonly Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        readonly List<AnimationClip> ordered = new List<AnimationClip>();
        readonly Dictionary<AnimationClip, int> indexOf = new Dictionary<AnimationClip, int>();
        readonly List<AnimationClipPlayable> playables = new List<AnimationClipPlayable>();
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        Animation legacy;
        bool useLegacy, ready, graphCreated;
        int cur = -1, prev = -1;
        float fadeDur = 0.12f, fadeT = 1f, elapsed;
        ClipBinding active;

        public bool Ready => ready;
        public bool Has(string key) => bindings.ContainsKey(key);
        public ClipBinding Binding(string key) => bindings.TryGetValue(key, out var b) ? b : null;

        public float Duration(string key)
        {
            if (!bindings.TryGetValue(key, out var b)) return 0f;
            var c = Resolve(b);
            float len = b.playLength > 0f ? b.playLength : (c != null ? c.length : 0f);
            return len / Mathf.Max(0.01f, b.speed);
        }

        AnimationClip Resolve(ClipBinding b)
        {
            if (string.IsNullOrEmpty(b.clipName)) return null;
            if (b.clipName == "*") return ordered.Count > 0 ? ordered[0] : null;
            clips.TryGetValue(b.clipName, out var c);
            return c;
        }

        public void Init(GameObject model, string resourcePath, IEnumerable<ClipBinding> binds)
        {
            foreach (var b in binds) bindings[b.key] = b;
            foreach (var c in Resources.LoadAll<AnimationClip>(resourcePath))
            {
                if (c.name.StartsWith("__preview__")) continue;
                clips[c.name] = c; ordered.Add(c);
            }
            if (ordered.Count == 0) { Debug.LogWarning("No animation clips found at Resources/" + resourcePath); return; }
            useLegacy = ordered[0].legacy;
            if (useLegacy)
            {
                legacy = model.GetComponent<Animation>();
                if (legacy == null) legacy = model.AddComponent<Animation>();
                foreach (var c in ordered) legacy.AddClip(c, c.name);
            }
            else
            {
                var anim = model.GetComponent<Animator>();
                if (anim == null) anim = model.AddComponent<Animator>();
                anim.applyRootMotion = false;
                graph = PlayableGraph.Create("Clips_" + name);
                graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                mixer = AnimationMixerPlayable.Create(graph, ordered.Count);
                for (int i = 0; i < ordered.Count; i++)
                {
                    var p = AnimationClipPlayable.Create(graph, ordered[i]);
                    p.SetApplyFootIK(false);
                    graph.Connect(p, 0, mixer, i);
                    mixer.SetInputWeight(i, 0f);
                    playables.Add(p); indexOf[ordered[i]] = i;
                }
                AnimationPlayableOutput.Create(graph, "out", anim).SetSourcePlayable(mixer);
                graph.Play();
                graphCreated = true;
            }
            ready = true;
        }

        void OnDestroy() { if (graphCreated && graph.IsValid()) graph.Destroy(); }

        public void Play(string key)
        {
            if (!ready || !bindings.TryGetValue(key, out var b)) return;
            var clip = Resolve(b);
            if (clip == null) return;
            active = b; elapsed = 0f;
            if (useLegacy)
            {
                var st = legacy[clip.name];
                st.speed = b.freezeAt >= 0f ? 0f : b.speed;
                st.wrapMode = b.loop ? WrapMode.Loop : WrapMode.ClampForever;
                st.time = b.freezeAt >= 0f ? b.freezeAt : 0f;
                legacy.CrossFade(clip.name, b.fade);
                return;
            }
            int idx = indexOf[clip];
            if (idx == cur && b.loop) return;
            prev = cur; cur = idx; fadeT = 0f; fadeDur = Mathf.Max(0.01f, b.fade);
            var p = playables[idx];
            p.SetTime(b.freezeAt >= 0f ? b.freezeAt : 0f);
            p.SetSpeed(b.freezeAt >= 0f ? 0.0 : b.speed);
        }

        void Update()
        {
            if (!ready || active == null) return;
            if (!useLegacy && cur >= 0)
            {
                fadeT = Mathf.Min(1f, fadeT + Time.deltaTime / fadeDur);
                for (int i = 0; i < playables.Count; i++)
                    mixer.SetInputWeight(i, i == cur ? fadeT : (i == prev ? 1f - fadeT : 0f));
                if (active.loop && active.freezeAt < 0f)
                {
                    var p = playables[cur];
                    float len = ordered[cur].length;
                    if (p.GetTime() >= len) p.SetTime(p.GetTime() - len);
                }
            }
            if (active.loop || active.freezeAt >= 0f || active.holdLast || active.key == "Idle") return;
            elapsed += Time.deltaTime * Mathf.Max(0.01f, active.speed);
            var c = Resolve(active);
            float limit = active.playLength > 0f ? active.playLength : (c != null ? c.length : 0f);
            if (limit > 0f && elapsed >= limit && bindings.ContainsKey("Idle")) Play("Idle");
        }
    }
}
