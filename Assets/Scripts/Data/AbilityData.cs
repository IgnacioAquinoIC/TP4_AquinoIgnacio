using System;
using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    [Serializable]
    public class EffectEntry
    {
        public EffectType type;
        public float value;
        [Range(0f, 1f)] public float chance = 1f;
        public StatusType status;
        public StatKey stat;
        public int duration = 3;
        public bool onCaster;
    }

    [CreateAssetMenu(menuName = "RpgBattle/Ability")]
    public class AbilityData : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public ActionCategory category;
        public DamageType damageType;
        public TargetShape shape;
        public TargetSide side;
        public int epCost;
        public int spCost;
        [Tooltip("Max grid distance between the (post-movement) caster and the selected cell/target.")]
        public int castRange = 1;
        public bool allowMovement = true;
        public AreaCenter areaCenter = AreaCenter.Cursor;
        public int radius;
        [Tooltip("Adds the caster's current MOV to the radius (Crescent Gale: 1 + MOV).")]
        public bool radiusAddsMov;
        public int lineLength = 8;
        public float lineWidth = 1f;
        [Tooltip("ATB units that must refill before the next turn. Higher = longer recovery.")]
        public float recoveryUnits = 50f;
        public bool hidesUi;
        public Presentation presentation;
        public Color vfxColor = Color.white;
        public GameObject vfxPrefab;
        public string animKey = "Attack";
        public string sfxKey;
        public string hitSfxKey = "impact";
        public float hitDelay = 0.4f;
        public float totalDuration = 0.9f;
        public List<EffectEntry> effects = new List<EffectEntry>();

        public bool IsAoe => shape != TargetShape.Single;
        public bool NeedsAnchor => shape == TargetShape.Single || shape == TargetShape.Line || (shape == TargetShape.Circle && areaCenter == AreaCenter.Cursor);
        public int ResolveRadius(Combatant c) => radius + (radiusAddsMov ? c.Mov : 0);
    }
}
