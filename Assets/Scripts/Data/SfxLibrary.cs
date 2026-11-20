using System;
using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    [Serializable]
    public class SfxEntry
    {
        public string key;
        public AudioClip[] clips;
    }

    [CreateAssetMenu(menuName = "RpgBattle/Sfx Library")]
    public class SfxLibrary : ScriptableObject
    {
        public List<SfxEntry> entries = new List<SfxEntry>();

        public AudioClip Get(string key)
        {
            foreach (var e in entries)
                if (e.key == key && e.clips != null && e.clips.Length > 0)
                    return e.clips[UnityEngine.Random.Range(0, e.clips.Length)];
            return null;
        }
    }
}
