using UnityEngine;

namespace RpgBattle
{
    public static class DamageCalculator
    {
        public static int Compute(Combatant attacker, Combatant target, AbilityData ability, float power, BattleConfig cfg, float multiplier)
        {
            bool phys = ability.damageType == DamageType.Physical;
            float atk = attacker.Stat(phys ? StatKey.STR : StatKey.ATS);
            float def = target.Stat(phys ? StatKey.DEF : StatKey.ADF);
            float v = (atk * power - def * cfg.defenseFactor) * Random.Range(1f - cfg.damageVariance, 1f + cfg.damageVariance) * multiplier;
            int dmg = Mathf.Max(cfg.minDamage, Mathf.RoundToInt(v));
            if (attacker.data.maxDamagePerHit > 0) dmg = Mathf.Min(dmg, attacker.data.maxDamagePerHit);
            return dmg;
        }
    }
}
