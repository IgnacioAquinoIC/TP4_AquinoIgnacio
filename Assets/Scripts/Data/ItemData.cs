using System;
using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    [CreateAssetMenu(menuName = "RpgBattle/Item")]
    public class ItemData : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public float recoveryUnits = 40f;
        public string sfxKey = "heal";
        public Color color = Color.green;
        public List<EffectEntry> effects = new List<EffectEntry>();
    }

    [Serializable]
    public class ItemStock
    {
        public ItemData item;
        public int quantity = 3;
    }
}
