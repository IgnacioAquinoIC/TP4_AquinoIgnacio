using System;
using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    public enum PoseKind { None, HandToChest, ArmsRaised, PointForward, ArmsSpread }

    [Serializable]
    public class ClipBinding
    {
        [Tooltip("Idle, Move, Attack, Cast, Special, Hold, Ultimate, Hurt, Death (any key an AbilityData.animKey uses)")]
        public string key;
        [Tooltip("Animation clip name inside the model asset. '*' = first clip. Empty = keep current clip (pose only).")]
        public string clipName;
        public bool loop;
        public float speed = 1f;
        [Tooltip("Seconds of the clip to play (0 = whole clip); also how long a Pose is held.")]
        public float playLength;
        public float fade = 0.12f;
        [Tooltip(">= 0 freezes the clip at this time (static pose).")]
        public float freezeAt = -1f;
        public bool holdLast;
        public PoseKind pose;
        [Tooltip("Metres the character rises while this plays (hover characters).")]
        public float lift;
    }

    [Serializable]
    public class TextureRule
    {
        [Tooltip("Material name must contain this text (empty = fallback for everything else).")]
        public string materialNameContains;
        public string texturePath;
    }

    [Serializable]
    public class RigBones
    {
        public string upperArmR, lowerArmR, handR, upperArmL, lowerArmL, handL;
    }

    [CreateAssetMenu(menuName = "RpgBattle/Unit")]
    public class UnitData : ScriptableObject
    {
        public string displayName;
        public Team team;
        public ModelStyle modelStyle;
        public Color tint = Color.white;
        public float scale = 1f;
        public bool ranged;
        public int maxHp = 500, maxEp = 50, startSp = 100, maxSp = 150;
        public int str = 50, def = 50, ats = 50, adf = 50, spd = 10, mov = 4;
        [Tooltip("0 = unlimited. Enemy attacks are capped by this value.")]
        public int maxDamagePerHit;
        public AbilityData basicAttack;
        public List<AbilityData> arts = new List<AbilityData>();
        public List<AbilityData> specials = new List<AbilityData>();

        [Header("Imported model (Resources path without extension). Empty = procedural placeholder.")]
        public string modelResourcePath;
        public float targetHeight = 1.9f;
        public float modelYaw;
        public float hoverHeight;
        public float hoverBob = 0.05f;
        public List<TextureRule> textureRules = new List<TextureRule>();
        public List<ClipBinding> clips = new List<ClipBinding>();
        public RigBones rigBones = new RigBones();
        [Header("HUD portrait (render of the model)")]
        public float portraitHeightFraction = 0.9f;
        public float portraitSize = 0.3f;

        [Header("Optional prefab replacement (Animator based)")]
        public GameObject modelPrefab;
        public RuntimeAnimatorController animatorController;
    }
}
