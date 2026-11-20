using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RpgBattle
{
    public class ActiveEffect
    {
        public string label;
        public StatusType status;
        public bool isBuff;
        public StatKey stat;
        public float multiplier = 1f;
        public int turns;
        public float tickPercent;
        public bool fresh;
        public Color color = Color.white;
    }

    public class Combatant : MonoBehaviour
    {
        public UnitData data;
        public string displayName;
        public Team team;
        public Vector2Int cell;
        public int hp, ep, sp, shield;
        public float atb;
        public bool isDead;
        public readonly List<ActiveEffect> effects = new List<ActiveEffect>();
        public UnitView view;

        public int MaxHp => data.maxHp;
        public int MaxEp => data.maxEp;
        public int MaxSp => data.maxSp;
        public bool IsPlayer => team == Team.Player;
        public int Mov => Mathf.Max(1, Mathf.RoundToInt(Stat(StatKey.MOV)));

        public void Init(UnitData d, string name)
        {
            data = d; displayName = name; team = d.team;
            hp = d.maxHp; ep = d.maxEp; sp = Mathf.Min(d.startSp, d.maxSp);
        }

        public float Stat(StatKey k)
        {
            float v;
            switch (k)
            {
                case StatKey.STR: v = data.str; break;
                case StatKey.DEF: v = data.def; break;
                case StatKey.ATS: v = data.ats; break;
                case StatKey.ADF: v = data.adf; break;
                case StatKey.SPD: v = data.spd; break;
                default: v = data.mov; break;
            }
            foreach (var e in effects)
                if (e.isBuff && e.stat == k) v *= e.multiplier;
            return v;
        }

        public bool Has(StatusType s) => effects.Any(e => !e.isBuff && e.status == s);
    }
}
