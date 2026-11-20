using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    public static class EnemyAI
    {
        public static Combatant ChooseTarget(Combatant enemy, List<Combatant> units)
        {
            Combatant best = null; float bestScore = float.MaxValue;
            foreach (var u in units)
            {
                if (u.isDead || u.team == enemy.team) continue;
                float score = BattleGrid.Dist(enemy.cell, u.cell) + (u.hp / (float)u.MaxHp) * 4f + Random.Range(0f, 1.5f);
                if (score < bestScore) { bestScore = score; best = u; }
            }
            return best;
        }
    }
}
