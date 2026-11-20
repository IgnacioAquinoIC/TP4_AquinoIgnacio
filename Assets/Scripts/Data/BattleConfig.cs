using System;
using UnityEngine;

namespace RpgBattle
{
    [Serializable]
    public class StatusDef
    {
        public StatusType type;
        public string label;
        public int duration = 3;
        [Range(0f, 1f)] public float tickPercent;
        public Color color = Color.white;
    }

    [Serializable] public class PartySpawn { public UnitData unit; public Vector2Int cell; }
    [Serializable] public class EnemySpawn { public UnitData unit; public Vector2Int cell; }

    [CreateAssetMenu(menuName = "RpgBattle/Battle Config")]
    public class BattleConfig : ScriptableObject
    {
        [Header("Grid")]
        public int gridWidth = 24;
        public int gridHeight = 16;
        public float cellSize = 1f;
        public Vector2Int[] blockedCells;

        [Header("ATB")]
        public float atbMax = 100f;
        public float atbSpeedScale = 1f;
        public Vector2 initialAtbRange = new Vector2(20f, 60f);
        public float moveRecovery = 40f;
        public float skipRecovery = 50f;
        public float resetAtbValue = 0f;
        public float moveSecondsPerCell = 0.18f;

        [Header("Damage")]
        public float defenseFactor = 0.5f;
        public float damageVariance = 0.05f;
        public int minDamage = 1;

        [Header("SP generation")]
        public int spPerHitLanded = 10;
        public int spPerDamageTaken = 10;

        [Header("Encounter")]
        public PartySpawn[] party;
        public EnemySpawn[] enemies;
        public ItemStock[] items;
        public LinkAbilityData[] links;
        public StatusDef[] statuses;

        public StatusDef GetStatus(StatusType t)
        {
            foreach (var s in statuses) if (s.type == t) return s;
            return new StatusDef { type = t, label = t.ToString(), duration = 2, color = Color.white };
        }
    }
}
