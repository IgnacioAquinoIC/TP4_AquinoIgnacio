using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RpgBattle
{
    /// Executes actions: movement, range validation, costs, presentation, effects, link follow-ups.
    public class ActionRunner : MonoBehaviour
    {
        BattleManager m;
        BattleConfig cfg => m.config;
        BattleGrid grid => m.Grid;
        public int Busy;

        public void Init(BattleManager mgr) { m = mgr; }

        public IEnumerator Perform(ActionRequest r, ActionResult res)
        {
            Busy++;
            try { yield return PerformInner(r, res); }
            finally { Busy--; }
        }

        IEnumerator MoveAlong(Combatant a, List<Vector2Int> path)
        {
            if (path == null || path.Count == 0) yield break;
            grid.Clear(a);
            a.view.SetMoving(true);
            foreach (var c in path) yield return a.view.StepTo(grid.ToWorld(c), cfg.moveSecondsPerCell);
            a.view.SetMoving(false);
            grid.Place(a, path[path.Count - 1]);
        }

        IEnumerator PerformInner(ActionRequest r, ActionResult res)
        {
            var a = r.actor; var ab = r.ability;
            res.executed = false; res.recovery = cfg.moveRecovery;

            if (r.moveOnly)
            {
                var plan = grid.PlanMove(a, a.Mov, r.cell, 0, true, false);
                yield return MoveAlong(a, plan.path);
                res.executed = plan.inRange;
                yield break;
            }
            if (r.item != null) { yield return UseItem(r, res); yield break; }

            Vector2Int anchor = a.cell;
            if (ab.shape == TargetShape.Single || ab.shape == TargetShape.Line)
            {
                if (r.targetUnit == null || r.targetUnit.isDead) yield break;
                anchor = r.targetUnit.cell;
            }
            else if (ab.NeedsAnchor) anchor = r.cell;

            Vector2Int origin = a.cell;
            if (ab.NeedsAnchor)
            {
                var plan = grid.PlanMove(a, a.Mov, anchor, ab.castRange, ab.allowMovement, r.preferFar);
                yield return MoveAlong(a, plan.path);
                origin = a.cell;
                if (!plan.inRange)
                {
                    m.ui.Popup(a.view.Head, "Out of range", Color.white);
                    yield break;
                }
            }

            var cells = TargetResolver.Cells(m, a, ab, anchor, origin);
            var targets = TargetResolver.Units(m, a, ab, cells, origin);
            if (targets.Count == 0)
            {
                m.ui.Popup(a.view.Head, "No target", Color.white);
                yield break;
            }

            a.ep -= ab.epCost;
            a.sp -= ab.spCost;
            var ctx = new ActionContext { actor = a, ability = ab, targets = targets, isLink = r.isLink };
            BattleEvents.RaiseStarting(ctx);
            if (ab.category != ActionCategory.Attack) m.ui.Banner(ab.displayName, false);

            bool hide = ab.hidesUi;
            try
            {
                if (hide) m.ui.SetHidden(true);
                yield return Present(ctx, origin, anchor);
                yield return ApplyEffects(ctx);
                yield return new WaitForSeconds(Mathf.Max(0.1f, ab.totalDuration - ab.hitDelay));
            }
            finally
            {
                if (hide) m.ui.SetHidden(false);
                m.ui.SetDim(0f);
                m.cam.ResetView();
            }

            a.view.Idle();
            ctx.executed = true; res.executed = true; res.recovery = ab.recoveryUnits;
            BattleEvents.RaiseFinished(ctx);

            for (int i = 0; i < ctx.followUps.Count; i++)
            {
                var f = ctx.followUps[i];
                if (f.actor.isDead || f.targetUnit == null || f.targetUnit.isDead || m.state == BattleManager.State.Ended) continue;
                yield return new WaitForSeconds(0.25f);
                yield return PerformInner(f, new ActionResult());
            }
        }

        IEnumerator Present(ActionContext ctx, Vector2Int origin, Vector2Int anchor)
        {
            var a = ctx.actor; var ab = ctx.ability; var v = a.view;
            Vector3 aw = grid.ToWorld(origin), tw = grid.ToWorld(anchor);
            if (ab.NeedsAnchor && anchor != origin) v.Face(tw);
            v.Play(ab.animKey);
            m.audio.Play(ab.sfxKey);
            Color col = ab.vfxColor;
            Vector3 chest = aw + Vector3.up * 1.1f;
            Vector3 dir = (tw - aw).sqrMagnitude > 0.01f ? (tw - aw).normalized : a.view.transform.forward;
            switch (ab.presentation)
            {
                case Presentation.MeleeSlash:
                    yield return new WaitForSeconds(ab.hitDelay);
                    foreach (var t in ctx.targets) m.vfx.Slash(t.view.Head - Vector3.up * 0.6f, col, Random.Range(-40f, 40f));
                    break;
                case Presentation.Projectile:
                    yield return new WaitForSeconds(ab.hitDelay * 0.6f);
                    yield return m.vfx.Projectile(chest + dir * 0.6f, ctx.targets[0].view.Head - Vector3.up * 0.5f, col);
                    break;
                case Presentation.PiercingBeam:
                    yield return new WaitForSeconds(ab.hitDelay * 0.6f);
                    yield return m.vfx.Beam(chest, aw + dir * ab.lineLength * grid.CellSize + Vector3.up * 0.2f, col, Mathf.Max(0.5f, ab.lineWidth * 0.5f), 0.45f);
                    break;
                case Presentation.GroundSlam:
                    {
                        yield return new WaitForSeconds(ab.hitDelay);
                        Vector3 start = aw + dir * 0.9f;
                        m.cam.Shake(0.3f, 0.22f);
                        m.audio.Play("impact", 0.7f);
                        m.vfx.Ring(start, 1.6f, col, 0.5f);
                        m.vfx.Burst(start + Vector3.up * 0.2f, new Color(0.85f, 0.8f, 0.7f), 30, 4f, 0.4f, 0.7f, 0.6f);
                        yield return m.vfx.Shockwave(start, aw + dir * ab.lineLength * grid.CellSize, col, Mathf.Max(0.6f, ab.lineWidth * 0.55f));
                        break;
                    }
                case Presentation.BlinkStrike:
                    yield return m.vfx.BlinkStrike(a, ctx.targets, col);
                    break;
                case Presentation.CircleBurst:
                    {
                        Vector3 c = grid.ToWorld(ab.areaCenter == AreaCenter.Caster ? origin : anchor);
                        yield return new WaitForSeconds(ab.hitDelay * 0.5f);
                        m.vfx.Ring(c, ab.ResolveRadius(a), col, 0.7f);
                        for (int i = 0; i < 2; i++)
                            foreach (var t in ctx.targets)
                            {
                                m.vfx.Slash(t.view.Head - Vector3.up * 0.5f, col, Random.Range(-80f, 80f));
                                m.audio.Play("sword_swing", 1f + i * 0.2f);
                                yield return new WaitForSeconds(0.06f);
                            }
                        yield return new WaitForSeconds(ab.hitDelay * 0.3f);
                        break;
                    }
                case Presentation.Cast:
                case Presentation.Heal:
                case Presentation.Buff:
                    yield return new WaitForSeconds(ab.hitDelay);
                    if (ab.shape == TargetShape.Circle) m.vfx.Ring(grid.ToWorld(ab.areaCenter == AreaCenter.Caster ? origin : anchor), ab.ResolveRadius(a), col, 0.7f);
                    foreach (var t in ctx.targets) { m.vfx.Rise(t.view.transform.position + Vector3.up * 0.3f, col); m.vfx.Burst(t.view.Head, col, 14, 2f, 0.2f, 0.7f); }
                    break;
                case Presentation.ValiantDawn:
                    yield return m.vfx.ValiantDawn(a, ctx.targets);
                    break;
                case Presentation.FatesCard:
                    yield return m.vfx.FatesCard(a, grid.ToWorld(ab.areaCenter == AreaCenter.Caster ? origin : anchor), ab.ResolveRadius(a), ctx.targets);
                    break;
                default:
                    yield return new WaitForSeconds(ab.hitDelay);
                    break;
            }
            if (ab.vfxPrefab != null) Destroy(Instantiate(ab.vfxPrefab, chest, Quaternion.LookRotation(dir)), 4f);
        }

        IEnumerator ApplyEffects(ActionContext ctx)
        {
            var ab = ctx.ability;
            foreach (var t in ctx.targets.ToList())
            {
                foreach (var e in ab.effects)
                {
                    if (e.onCaster || t.isDead) continue;
                    ApplyEffect(ctx, t, e);
                }
                yield return new WaitForSeconds(ctx.targets.Count > 3 ? 0.04f : 0.12f);
            }
            foreach (var e in ab.effects) if (e.onCaster) ApplyCasterEffect(ctx.actor, e);
        }

        void ApplyCasterEffect(Combatant a, EffectEntry e)
        {
            if (e.type == EffectType.GrantSp) { m.GainSp(a, Mathf.RoundToInt(e.value)); m.ui.Popup(a.view.Head, "+" + Mathf.RoundToInt(e.value) + " SP", new Color(0.6f, 1f, 0.5f), 28); }
        }

        void ApplyEffect(ActionContext ctx, Combatant t, EffectEntry e)
        {
            var ab = ctx.ability;
            bool hasDamage = ab != null && ab.effects.Any(x => x.type == EffectType.Damage);
            bool landed = ctx.hits.Any(h => h.target == t && h.landed);
            if (e.type != EffectType.Damage && hasDamage && !landed) return;
            if (e.type != EffectType.Damage && e.chance < 1f && Random.value >= e.chance) return;

            switch (e.type)
            {
                case EffectType.Damage: DamageHit(ctx, t, e.value); break;
                case EffectType.ApplyStatus: m.ApplyStatus(t, e.status); break;
                case EffectType.InstantKill: m.InstantKill(t); break;
                case EffectType.RandomStatus:
                    {
                        var pool = new[] { StatusType.Poison, StatusType.Freeze, StatusType.Blind, StatusType.InstantKill };
                        var s = pool[Random.Range(0, pool.Length)];
                        if (s == StatusType.InstantKill) m.InstantKill(t); else m.ApplyStatus(t, s);
                        break;
                    }
                case EffectType.ApplyBuff: m.ApplyBuff(t, e.stat, e.value, e.duration); break;
                case EffectType.GrantShield:
                    t.shield += Mathf.RoundToInt(e.value);
                    m.ui.Popup(t.view.Head, "Earth Guard", new Color(0.9f, 0.7f, 0.4f));
                    m.audio.Play("buff");
                    break;
                case EffectType.HealMaxHpPercent: m.Heal(t, Mathf.RoundToInt(t.MaxHp * e.value)); break;
                case EffectType.HealFlat: m.Heal(t, Mathf.RoundToInt(e.value)); break;
                case EffectType.RestoreEpFlat: m.RestoreEp(t, Mathf.RoundToInt(e.value)); break;
                case EffectType.RestoreEpPercent: m.RestoreEp(t, Mathf.RoundToInt(t.MaxEp * e.value)); break;
                case EffectType.CureStatuses: m.Cure(t); break;
                case EffectType.ResetAtb: m.ResetAtb(t); break;
                case EffectType.GrantSp: m.GainSp(t, Mathf.RoundToInt(e.value)); break;
            }
        }

        void DamageHit(ActionContext ctx, Combatant t, float power)
        {
            var a = ctx.actor; var ab = ctx.ability;
            var h = new HitContext { action = ctx, attacker = a, target = t, ability = ab };
            if (ab.damageType == DamageType.Physical && a.Has(StatusType.Blind))
            {
                h.missed = true;
                m.ui.Popup(t.view.Head, "Miss", Color.gray);
                m.audio.Play("miss");
            }
            else
            {
                BattleEvents.RaiseBefore(h);
                if (!h.nullified && t.shield > 0)
                {
                    t.shield--;
                    h.nullified = true; h.shieldBlocked = true;
                    m.ui.Popup(t.view.Head, "Guarded", new Color(0.9f, 0.7f, 0.4f));
                    m.audio.Play("buff");
                    m.vfx.Burst(t.view.Head, new Color(0.9f, 0.7f, 0.4f), 20, 3f, 0.3f);
                }
                if (!h.nullified)
                {
                    h.damage = DamageCalculator.Compute(a, t, ab, power, cfg, ctx.damageMultiplier);
                    h.landed = true;
                    m.audio.Play(ab.hitSfxKey);
                    m.vfx.Burst(t.view.Head - Vector3.up * 0.4f, ab.vfxColor, 12, 3f, 0.22f, 0.5f);
                    m.GainSp(a, cfg.spPerHitLanded);
                    m.GainSp(t, cfg.spPerDamageTaken);
                    m.DealDamage(t, h.damage);
                }
            }
            ctx.hits.Add(h);
            BattleEvents.RaiseAfter(h);
        }

        IEnumerator UseItem(ActionRequest r, ActionResult res)
        {
            var a = r.actor; var item = r.item; var t = r.targetUnit;
            if (t == null || !m.inventory.Consume(item)) yield break;
            var ctx = new ActionContext { actor = a, item = item, isLink = false };
            ctx.targets.Add(t);
            a.view.Face(t.view.transform.position);
            m.ui.Banner(item.displayName, false);
            m.audio.Play(item.sfxKey);
            yield return new WaitForSeconds(0.3f);
            foreach (var e in item.effects) ApplyEffect(ctx, t, e);
            yield return new WaitForSeconds(0.6f);
            res.executed = true; res.recovery = item.recoveryUnits;
        }
    }
}
