using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    /// Event-driven Link abilities. Reacts to BattleEvents; knows nothing about individual attacks.
    public class LinkSystem
    {
        readonly BattleManager m;
        readonly Dictionary<LinkType, LinkAbilityData> links = new Dictionary<LinkType, LinkAbilityData>();

        public LinkSystem(BattleManager manager, IEnumerable<LinkAbilityData> data)
        {
            m = manager;
            foreach (var l in data) links[l.type] = l;
            BattleEvents.ActionStarting += OnStarting;
            BattleEvents.BeforeHit += OnBefore;
            BattleEvents.AfterHit += OnAfter;
            BattleEvents.ActionFinished += OnFinished;
        }

        public void Dispose()
        {
            BattleEvents.ActionStarting -= OnStarting;
            BattleEvents.BeforeHit -= OnBefore;
            BattleEvents.AfterHit -= OnAfter;
            BattleEvents.ActionFinished -= OnFinished;
        }

        Combatant Alive(UnitData d) => d == null ? null : m.Units.Find(u => u.data == d && !u.isDead);

        bool Roll(LinkType t, out LinkAbilityData l)
        {
            if (!links.TryGetValue(t, out l)) return false;
            return Random.value < l.chance;
        }

        void Notice(LinkAbilityData l, Combatant who)
        {
            m.ui.Banner(l.displayName + "!", true);
            m.ui.Popup(who.view.Head + Vector3.up * 0.4f, l.displayName, new Color(1f, 0.9f, 0.4f), 30);
            m.audio.Play("buff");
        }

        void OnStarting(ActionContext ctx)
        {
            if (ctx.isLink || ctx.ability == null) return;
            if (!links.TryGetValue(LinkType.SpiritAttunement, out var l)) return;
            if (ctx.ability != l.requiredAbility || ctx.actor.data != l.partner) return;
            var owner = Alive(l.owner);
            if (owner == null || Random.value >= l.chance) return;
            ctx.damageMultiplier *= l.value;
            Notice(l, owner);
        }

        void OnBefore(HitContext h)
        {
            if (!links.TryGetValue(LinkType.MagicCover, out var l)) return;
            if (h.target.data != l.partner || h.attacker.team == h.target.team) return;
            var owner = Alive(l.owner);
            if (owner == null || Random.value >= l.chance) return;
            h.nullified = true; h.covered = true;
            Notice(l, owner);
            m.vfx.Burst(h.target.view.Head, new Color(0.6f, 0.8f, 1f), 24, 3f, 0.3f);
            m.vfx.Ring(h.target.view.transform.position, 1.2f, new Color(0.6f, 0.8f, 1f), 0.5f);
            m.ui.Popup(h.target.view.Head, "Nullified", new Color(0.7f, 0.9f, 1f));
        }

        void OnAfter(HitContext h)
        {
            if (h.action.isLink) return;
            if (links.TryGetValue(LinkType.AutoHeal, out var heal) && h.landed && !h.covered && h.target.data == heal.partner && !h.target.isDead)
            {
                var owner = Alive(heal.owner);
                if (owner != null && Random.value < heal.chance)
                {
                    Notice(heal, owner);
                    m.Heal(h.target, Mathf.RoundToInt(h.target.MaxHp * heal.value));
                    m.Cure(h.target);
                }
            }
            if (links.TryGetValue(LinkType.Counterattack, out var counter) && h.attacker.team == Team.Enemy && h.target.data == counter.partner)
            {
                var owner = Alive(counter.owner);
                if (owner != null && !h.attacker.isDead && Random.value < counter.chance)
                {
                    Notice(counter, owner);
                    h.action.followUps.Add(new ActionRequest { actor = owner, ability = owner.data.basicAttack, targetUnit = h.attacker, isLink = true });
                }
            }
            if (links.TryGetValue(LinkType.LinkAttack, out var la) && h.landed && h.attacker.data == la.partner && h.ability != null
                && h.ability.category == ActionCategory.Attack && !h.ability.IsAoe && !h.target.isDead)
            {
                var owner = Alive(la.owner);
                if (owner != null && Random.value < la.chance)
                {
                    Notice(la, owner);
                    h.action.followUps.Add(new ActionRequest { actor = owner, ability = owner.data.basicAttack, targetUnit = h.target, isLink = true });
                }
            }
        }

        void OnFinished(ActionContext ctx)
        {
            if (ctx.isLink || !ctx.executed || ctx.ability == null || ctx.ability.category != ActionCategory.Art) return;
            if (!links.TryGetValue(LinkType.AutoCharge, out var l) || ctx.actor.data != l.partner) return;
            var owner = Alive(l.owner);
            if (owner == null || Random.value >= l.chance) return;
            Notice(l, owner);
            m.RestoreEp(owner, Mathf.RoundToInt(owner.MaxEp * l.value));
        }
    }
}
