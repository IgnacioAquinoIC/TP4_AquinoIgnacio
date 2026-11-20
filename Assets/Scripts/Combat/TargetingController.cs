using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RpgBattle
{
    /// Cursor-based selection for moves, abilities and items. Works with mouse, keyboard and pads.
    public class TargetingController
    {
        readonly BattleManager m;
        public class Result { public bool cancelled; public Vector2Int cell; public Combatant unit; }

        public TargetingController(BattleManager mgr) { m = mgr; }

        static readonly Color ReachCol = new Color(0.3f, 0.6f, 1f, 0.28f);
        static readonly Color EnemyCol = new Color(1f, 0.35f, 0.15f, 0.5f);
        static readonly Color AllyCol = new Color(0.3f, 1f, 0.55f, 0.5f);
        static readonly Color CursorOk = new Color(1f, 0.92f, 0.2f, 0.75f);
        static readonly Color CursorBad = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        static readonly Color StandCol = new Color(0.3f, 0.95f, 1f, 0.55f);

        public IEnumerator Select(Combatant a, AbilityData ab, ItemData item, bool moveMode, Result r)
        {
            yield return null;
            var g = m.Grid; var input = m.input;
            r.cancelled = false; r.unit = null;
            bool cursorless = ab != null && (ab.shape == TargetShape.All || (ab.shape == TargetShape.Circle && ab.areaCenter == AreaCenter.Caster));

            Dictionary<Vector2Int, int> reach = null;
            if (moveMode) reach = g.Reach(a, a.Mov, out _);

            List<Vector2Int> Candidates()
            {
                if (moveMode) return reach.Keys.Where(k => k != a.cell).OrderBy(k => BattleGrid.Dist(k, a.cell)).ToList();
                Team team = item != null ? a.team : TargetResolver.SideTeam(a, ab);
                return m.Units.Where(u => !u.isDead && u.team == team).OrderBy(u => BattleGrid.Dist(u.cell, a.cell)).Select(u => u.cell).ToList();
            }

            var cands = Candidates();
            Vector2Int cursor = cands.Count > 0 ? cands[0] : a.cell;
            if (ab != null && ab.side == TargetSide.Ally && !moveMode) cursor = a.cell;
            int idx = 0;

            while (m.state != BattleManager.State.Ended)
            {
                if (input.RawNav != Vector2Int.zero) { var d = InputService.ScreenToGrid(input.RawNav); cursor = new Vector2Int(Mathf.Clamp(cursor.x + d.x, 0, g.Width - 1), Mathf.Clamp(cursor.y + d.y, 0, g.Height - 1)); m.audio.Play("ui_move"); }
                if (input.PointerMoved && input.PointerCell(m.cam.cam, g, out var pc)) cursor = pc;
                if ((input.Next || input.Prev) && cands.Count > 0)
                {
                    idx = cands.IndexOf(cursor);
                    idx = (idx + (input.Next ? 1 : -1) + cands.Count * 2) % cands.Count;
                    cursor = cands[idx];
                    m.audio.Play("ui_move");
                }

                bool valid = false; var shape = new List<Vector2Int>(); Color shapeCol = EnemyCol; Combatant unitAt = g.OccupantAt(cursor);
                if (unitAt != null && unitAt.isDead) unitAt = null;
                string info = "";
                MovePlan plan = null;

                m.gridView.Clear();
                m.gridView.MarkCell(a.cell, StandCol);

                if (moveMode)
                {
                    m.gridView.Mark(reach.Keys, ReachCol);
                    valid = reach.ContainsKey(cursor) && cursor != a.cell && unitAt == null;
                    info = "Move";
                }
                else if (item != null)
                {
                    valid = unitAt != null && unitAt.team == a.team;
                    shape.Add(cursor); shapeCol = AllyCol;
                    info = unitAt != null ? Describe(unitAt) : item.displayName;
                }
                else
                {
                    shapeCol = ab.side == TargetSide.Ally ? AllyCol : EnemyCol;
                    Vector2Int anchor = cursor, origin = a.cell;
                    if (cursorless) { valid = true; }
                    else
                    {
                        if (ab.shape != TargetShape.Circle)
                            valid = unitAt != null && unitAt.team == TargetResolver.SideTeam(a, ab);
                        else valid = true;
                        if (valid && ab.NeedsAnchor)
                        {
                            plan = g.PlanMove(a, a.Mov, anchor, ab.castRange, ab.allowMovement, false);
                            valid = plan.inRange;
                            origin = plan.final;
                            if (plan.path.Count > 0) m.gridView.Mark(plan.path, ReachCol);
                            if (plan.final != a.cell) m.gridView.MarkCell(plan.final, StandCol);
                        }
                    }
                    if (valid || ab.shape == TargetShape.Circle)
                    {
                        shape = TargetResolver.Cells(m, a, ab, anchor, origin);
                        var units = TargetResolver.Units(m, a, ab, shape, origin);
                        if (units.Count == 0) valid = false;
                        info = valid ? string.Join(", ", units.Select(u => u.displayName)) + (ab.spCost > 0 ? "" : "") : "No valid target";
                    }
                    if (unitAt != null && !cursorless && ab.shape != TargetShape.Circle) info = Describe(unitAt);
                    if (cursorless) info = ab.displayName + " - confirm to execute";
                }

                m.gridView.Mark(shape, shapeCol);
                if (!cursorless) m.gridView.MarkCell(cursor, valid ? CursorOk : CursorBad);
                m.ui.SetTargetInfo(info);

                if (input.Cancel) { r.cancelled = true; m.audio.Play("ui_back"); break; }
                if (input.Confirm && !(input.ConfirmByPointer && !input.PointerCell(m.cam.cam, g, out _)))
                {
                    if (valid) { r.cell = cursorless ? a.cell : cursor; r.unit = unitAt; m.audio.Play("ui_confirm"); break; }
                    m.audio.Play("ui_back");
                }
                yield return null;
            }
            m.gridView.Clear();
            m.ui.SetTargetInfo("");
        }

        string Describe(Combatant u)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(u.displayName).Append("   HP ").Append(u.hp).Append('/').Append(u.MaxHp);
            foreach (var e in u.effects) sb.Append("  [").Append(e.label).Append(' ').Append(e.turns).Append(']');
            if (u.shield > 0) sb.Append("  [Guard]");
            return sb.ToString();
        }
    }
}
