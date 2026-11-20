using System;
using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    public class ActionRequest
    {
        public Combatant actor;
        public AbilityData ability;
        public ItemData item;
        public Combatant targetUnit;
        public Vector2Int cell;
        public bool isLink;
        public bool preferFar;
        public bool moveOnly;
    }

    public class ActionResult
    {
        public bool executed;
        public float recovery;
    }

    public class ActionContext
    {
        public Combatant actor;
        public AbilityData ability;
        public ItemData item;
        public List<Combatant> targets = new List<Combatant>();
        public float damageMultiplier = 1f;
        public bool isLink;
        public bool executed;
        public List<HitContext> hits = new List<HitContext>();
        public List<ActionRequest> followUps = new List<ActionRequest>();
    }

    public class HitContext
    {
        public ActionContext action;
        public Combatant attacker, target;
        public AbilityData ability;
        public bool missed, nullified, covered, shieldBlocked, landed;
        public int damage;
    }

    public static class BattleEvents
    {
        public static event Action<ActionContext> ActionStarting;
        public static event Action<ActionContext> ActionFinished;
        public static event Action<HitContext> BeforeHit;
        public static event Action<HitContext> AfterHit;

        public static void RaiseStarting(ActionContext c) => ActionStarting?.Invoke(c);
        public static void RaiseFinished(ActionContext c) => ActionFinished?.Invoke(c);
        public static void RaiseBefore(HitContext h) => BeforeHit?.Invoke(h);
        public static void RaiseAfter(HitContext h) => AfterHit?.Invoke(h);

        public static void Clear()
        {
            ActionStarting = null; ActionFinished = null; BeforeHit = null; AfterHit = null;
        }
    }
}
