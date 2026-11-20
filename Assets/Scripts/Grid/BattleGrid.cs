using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    public class MovePlan
    {
        public List<Vector2Int> path = new List<Vector2Int>();
        public Vector2Int final;
        public bool inRange;
    }

    public class BattleGrid
    {
        public readonly int Width, Height;
        public readonly float CellSize;
        readonly bool[,] blocked;
        readonly Combatant[,] occ;

        static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        public BattleGrid(BattleConfig cfg)
        {
            Width = cfg.gridWidth; Height = cfg.gridHeight; CellSize = cfg.cellSize;
            blocked = new bool[Width, Height];
            occ = new Combatant[Width, Height];
            if (cfg.blockedCells != null)
                foreach (var c in cfg.blockedCells) if (InBounds(c)) blocked[c.x, c.y] = true;
        }

        public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height;
        public bool IsBlocked(Vector2Int c) => !InBounds(c) || blocked[c.x, c.y];
        public Combatant OccupantAt(Vector2Int c) => InBounds(c) ? occ[c.x, c.y] : null;
        public static int Dist(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        public Vector3 ToWorld(Vector2Int c) => new Vector3(c.x * CellSize, 0f, c.y * CellSize);
        public Vector2Int ToCell(Vector3 w) => new Vector2Int(Mathf.RoundToInt(w.x / CellSize), Mathf.RoundToInt(w.z / CellSize));
        public Vector3 Center => new Vector3((Width - 1) * 0.5f * CellSize, 0f, (Height - 1) * 0.5f * CellSize);

        public void Place(Combatant c, Vector2Int cell)
        {
            Clear(c);
            c.cell = cell;
            occ[cell.x, cell.y] = c;
        }

        public void Clear(Combatant c)
        {
            if (InBounds(c.cell) && occ[c.cell.x, c.cell.y] == c) occ[c.cell.x, c.cell.y] = null;
        }

        bool Walkable(Vector2Int c, Combatant mover)
        {
            if (IsBlocked(c)) return false;
            var o = occ[c.x, c.y];
            return o == null || o == mover || o.isDead;
        }

        public Dictionary<Vector2Int, int> Reach(Combatant mover, int maxSteps, out Dictionary<Vector2Int, Vector2Int> parent)
        {
            var dist = new Dictionary<Vector2Int, int> { { mover.cell, 0 } };
            parent = new Dictionary<Vector2Int, Vector2Int>();
            var q = new Queue<Vector2Int>();
            q.Enqueue(mover.cell);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                int d = dist[c];
                if (d >= maxSteps) continue;
                foreach (var dir in Dirs)
                {
                    var n = c + dir;
                    if (dist.ContainsKey(n) || !Walkable(n, mover)) continue;
                    if (dir.x != 0 && dir.y != 0 && (IsBlocked(new Vector2Int(c.x + dir.x, c.y)) || IsBlocked(new Vector2Int(c.x, c.y + dir.y)))) continue;
                    dist[n] = d + 1;
                    parent[n] = c;
                    q.Enqueue(n);
                }
            }
            return dist;
        }

        static List<Vector2Int> BuildPath(Dictionary<Vector2Int, Vector2Int> parent, Vector2Int start, Vector2Int goal)
        {
            var list = new List<Vector2Int>();
            var c = goal;
            while (c != start) { list.Add(c); c = parent[c]; }
            list.Reverse();
            return list;
        }

        public MovePlan PlanMove(Combatant mover, int mov, Vector2Int anchor, int range, bool canMove, bool preferFar)
        {
            var dist = Reach(mover, canMove ? mov : 0, out var parent);
            var plan = new MovePlan();
            bool found = false; Vector2Int best = mover.cell; int bestScore = int.MaxValue;
            foreach (var kv in dist)
            {
                int ad = Dist(kv.Key, anchor);
                if (ad > range) continue;
                int score = preferFar ? -ad * 100 + kv.Value : kv.Value * 100 - ad;
                if (score < bestScore) { bestScore = score; best = kv.Key; found = true; }
            }
            if (!found)
            {
                int bd = int.MaxValue, bs = int.MaxValue;
                foreach (var kv in dist)
                {
                    int ad = Dist(kv.Key, anchor);
                    if (ad < bd || (ad == bd && kv.Value < bs)) { bd = ad; bs = kv.Value; best = kv.Key; }
                }
            }
            plan.inRange = found;
            plan.final = best;
            if (best != mover.cell) plan.path = BuildPath(parent, mover.cell, best);
            return plan;
        }

        public List<Vector2Int> CircleCells(Vector2Int center, float radius)
        {
            var list = new List<Vector2Int>();
            int r = Mathf.CeilToInt(radius);
            for (int x = center.x - r; x <= center.x + r; x++)
                for (int y = center.y - r; y <= center.y + r; y++)
                {
                    var c = new Vector2Int(x, y);
                    if (InBounds(c) && ((Vector2)(c - center)).magnitude <= radius + 0.01f) list.Add(c);
                }
            return list;
        }

        public List<Vector2Int> LineCells(Vector2Int from, Vector2Int to, int length, float width)
        {
            var list = new List<Vector2Int>();
            Vector2 o = from;
            Vector2 dir = ((Vector2)(to - from));
            dir = dir.sqrMagnitude < 0.001f ? Vector2.right : dir.normalized;
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    Vector2 v = new Vector2(x, y) - o;
                    float along = Vector2.Dot(v, dir);
                    if (along <= 0.01f || along > length + 0.01f) continue;
                    float perp = Mathf.Abs(v.x * dir.y - v.y * dir.x);
                    if (perp <= width * 0.5f + 0.01f) list.Add(new Vector2Int(x, y));
                }
            return list;
        }
    }
}
