using UnityEngine;

namespace RpgBattle
{
    [CreateAssetMenu(menuName = "RpgBattle/Link Ability")]
    public class LinkAbilityData : ScriptableObject
    {
        public LinkType type;
        public string displayName;
        [Range(0f, 1f)] public float chance = 0.5f;
        [Tooltip("Unit that performs / benefits from the link ability.")]
        public UnitData owner;
        [Tooltip("Unit whose actions or damage trigger the link ability.")]
        public UnitData partner;
        [Tooltip("Spirit Attunement: the only ability affected.")]
        public AbilityData requiredAbility;
        [Tooltip("Spirit Attunement: damage multiplier. Auto-Charge / Auto-Heal: fraction of max resource.")]
        public float value = 0.1f;
    }
}
