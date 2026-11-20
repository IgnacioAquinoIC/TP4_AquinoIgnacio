using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    public class GridView : MonoBehaviour
    {
        BattleGrid g;
        Material[,] markMats;
        Renderer[,] marks;

        bool kit;

        public void Build(BattleGrid grid, bool kitFloor)
        {
            g = grid; kit = kitFloor;
            var a = MaterialFactory.Lit(new Color(0.74f, 0.66f, 0.55f), 0.15f);
            var b = MaterialFactory.Lit(new Color(0.66f, 0.58f, 0.48f), 0.15f);
            marks = new Renderer[g.Width, g.Height];
            markMats = new Material[g.Width, g.Height];
            for (int x = 0; x < g.Width; x++)
                for (int y = 0; y < g.Height; y++)
                {
                    var c = new Vector2Int(x, y);
                    if (!kit)
                    {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(tile.GetComponent<Collider>());
                    tile.transform.SetParent(transform, false);
                    tile.transform.position = g.ToWorld(c) + Vector3.down * 0.05f;
                    tile.transform.localScale = new Vector3(g.CellSize * 0.96f, 0.1f, g.CellSize * 0.96f);
                    tile.GetComponent<Renderer>().sharedMaterial = ((x + y) & 1) == 0 ? a : b;
                    }

                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Destroy(q.GetComponent<Collider>());
                    q.transform.SetParent(transform, false);
                    q.transform.position = g.ToWorld(c) + Vector3.up * 0.03f;
                    q.transform.rotation = Quaternion.Euler(90, 0, 0);
                    q.transform.localScale = Vector3.one * g.CellSize * 0.94f;
                    var r = q.GetComponent<Renderer>();
                    markMats[x, y] = MaterialFactory.Overlay(Color.clear);
                    r.sharedMaterial = markMats[x, y];
                    r.enabled = false;
                    marks[x, y] = r;
                }
            Clear();
        }

        Color Base(int x, int y) => ((x + y) & 1) == 0 ? new Color(1, 1, 1, 0.08f) : new Color(0, 0, 0, 0.10f);

        public void Clear()
        {
            if (marks == null) return;
            for (int x = 0; x < g.Width; x++)
                for (int y = 0; y < g.Height; y++)
                {
                    markMats[x, y].color = Base(x, y);
                    marks[x, y].enabled = kit;
                }
        }

        public void Mark(IEnumerable<Vector2Int> cells, Color color)
        {
            foreach (var c in cells)
            {
                if (!g.InBounds(c)) continue;
                markMats[c.x, c.y].color = color;
                marks[c.x, c.y].enabled = true;
            }
        }

        public void MarkCell(Vector2Int c, Color color) => Mark(new[] { c }, color);
    }
}
