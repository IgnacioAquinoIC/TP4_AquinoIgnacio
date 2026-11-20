using UnityEngine;
using UnityEngine.Audio;

namespace RpgBattle
{
    /// Add to an empty GameObject in an empty scene. Builds the whole encounter at runtime.
    /// Leave 'config' empty to use the built-in defaults from DefaultData.
    public class BattleBootstrap : MonoBehaviour
    {
        public BattleConfig config;
        public SfxLibrary sfxLibrary;
        public AudioClip music;
        public AudioMixer mixer;
        public AudioMixerGroup musicGroup, sfxGroup;
        GameObject root;

        void Start() { Build(); }

        public void Restart()
        {
            if (root != null) Destroy(root);
            BattleEvents.Clear();
            StartCoroutine(RebuildNextFrame());
        }

        System.Collections.IEnumerator RebuildNextFrame() { yield return null; Build(); }

        void Build()
        {
            BattleEvents.Clear();
            root = new GameObject("BattleRoot");
            var m = root.AddComponent<BattleManager>();
            m.bootstrap = this;
            m.config = config != null ? config : DefaultData.CreateConfig();
            m.input = root.AddComponent<InputService>();
            m.audio = root.AddComponent<AudioService>();
            m.audio.Init(sfxLibrary, music, mixer, musicGroup, sfxGroup);
            m.cam = root.AddComponent<CameraRig>();
            m.vfx = root.AddComponent<VfxService>();
            m.runner = root.AddComponent<ActionRunner>();
            var gv = new GameObject("GridView");
            gv.transform.SetParent(root.transform, false);
            m.gridView = gv.AddComponent<GridView>();
            var uiGo = new GameObject("BattleUI");
            uiGo.transform.SetParent(root.transform, false);
            m.ui = uiGo.AddComponent<BattleUI>();
            m.Init();
        }
    }
}
