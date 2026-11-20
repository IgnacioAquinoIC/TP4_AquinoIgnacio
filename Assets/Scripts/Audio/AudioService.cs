using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace RpgBattle
{
    /// Separate music/SFX volume paths. Optionally routes through an AudioMixer exposing
    /// "MusicVolume" and "SfxVolume" parameters (assign mixer + groups); otherwise scales source volumes directly.
    public class AudioService : MonoBehaviour
    {
        public AudioMixer mixer;
        public AudioMixerGroup musicGroup, sfxGroup;
        SfxLibrary library;
        AudioClip musicClip;
        AudioSource musicSrc;
        readonly List<AudioSource> pool = new List<AudioSource>();
        int next;

        public float MusicVolume { get; private set; }
        public float SfxVolume { get; private set; }

        public void Init(SfxLibrary lib, AudioClip music, AudioMixer mix = null, AudioMixerGroup mg = null, AudioMixerGroup sg = null)
        {
            library = lib; musicClip = music; mixer = mix; musicGroup = mg; sfxGroup = sg;
            if (musicClip == null) musicClip = Resources.Load<AudioClip>("Audio/battle-music");
            musicSrc = gameObject.AddComponent<AudioSource>();
            musicSrc.loop = true; musicSrc.playOnAwake = false; musicSrc.outputAudioMixerGroup = musicGroup;
            for (int i = 0; i < 10; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false; s.outputAudioMixerGroup = sfxGroup;
                pool.Add(s);
            }
            SetMusicVolume(PlayerPrefs.GetFloat("rpg_music_vol", 0.6f));
            SetSfxVolume(PlayerPrefs.GetFloat("rpg_sfx_vol", 0.8f));
        }

        static float Db(float v) => Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f;

        public void SetMusicVolume(float v)
        {
            MusicVolume = Mathf.Clamp01(v);
            musicSrc.volume = MusicVolume;
            if (mixer != null) mixer.SetFloat("MusicVolume", Db(MusicVolume));
            PlayerPrefs.SetFloat("rpg_music_vol", MusicVolume);
        }

        public void SetSfxVolume(float v)
        {
            SfxVolume = Mathf.Clamp01(v);
            if (mixer != null) mixer.SetFloat("SfxVolume", Db(SfxVolume));
            PlayerPrefs.SetFloat("rpg_sfx_vol", SfxVolume);
        }

        public void Play(string key, float pitch = 1f)
        {
            if (string.IsNullOrEmpty(key)) return;
            var clip = library != null ? library.Get(key) : null;
            if (clip == null) clip = ProceduralSfx.Get(key);
            if (clip == null) return;
            var s = pool[next]; next = (next + 1) % pool.Count;
            s.pitch = pitch; s.volume = mixer != null ? 1f : SfxVolume; s.clip = clip; s.Play();
        }

        public void PlayMusic()
        {
            if (musicClip == null) { Debug.LogWarning("No music clip assigned. Expected Assets/Resources/Audio/battle-music.mp3 (or assign it on BattleBootstrap)."); return; }
            musicSrc.clip = musicClip; musicSrc.Play();
        }

        public void FadeOutMusic(float duration) => StartCoroutine(Util.Tween(duration, t => musicSrc.volume = MusicVolume * (1f - t)));
    }
}
