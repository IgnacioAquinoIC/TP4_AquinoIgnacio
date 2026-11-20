using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RpgBattle
{
    public static class TargetResolver
    {
        public static Team SideTeam(Combatant actor, AbilityData ab)
            => ab.side == TargetSide.Ally ? actor.team : (actor.team == Team.Player ? Team.Enemy : Team.Player);

        public static List<Vector2Int> Cells(BattleManager m, Combatant actor, AbilityData ab, Vector2Int anchor, Vector2Int origin)
        {
            var g = m.Grid;
            switch (ab.shape)
            {
                case TargetShape.Single: return new List<Vector2Int> { anchor };
                case TargetShape.Line: return g.LineCells(origin, anchor, ab.lineLength, ab.lineWidth);
                case TargetShape.Circle: return g.CircleCells(ab.areaCenter == AreaCenter.Caster ? origin : anchor, ab.ResolveRadius(actor));
                default:
                    var team = SideTeam(actor, ab);
                    return m.Units.Where(u => !u.isDead && u.team == team).Select(u => u.cell).ToList();
            }
        }

        public static List<Combatant> Units(BattleManager m, Combatant actor, AbilityData ab, List<Vector2Int> cells, Vector2Int origin)
        {
            var team = SideTeam(actor, ab);
            var set = new HashSet<Vector2Int>(cells);
            return m.Units.Where(u => !u.isDead && u.team == team && set.Contains(u.cell))
                          .OrderBy(u => BattleGrid.Dist(u.cell, origin)).ToList();
        }
    }
}
