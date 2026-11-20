using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RpgBattle
{
    public class Inventory
    {
        readonly List<ItemData> order = new List<ItemData>();
        readonly Dictionary<ItemData, int> qty = new Dictionary<ItemData, int>();
        public Inventory(IEnumerable<ItemStock> stock)
        {
            foreach (var s in stock) { order.Add(s.item); qty[s.item] = s.quantity; }
        }
        public IReadOnlyList<ItemData> Items => order;
        public int Count(ItemData i) => qty.TryGetValue(i, out var q) ? q : 0;
        public bool Consume(ItemData i)
        {
            if (Count(i) <= 0) return false;
            qty[i]--; return true;
        }
    }

    public class Holder<T> { public T value; }

    /// Owns battle state: spawning, ATB ticking, turn flow, status ticking, win/lose.
    public class BattleManager : MonoBehaviour
    {
        public enum State { Intro, Running, PlayerCommand, Executing, Ended }

        public BattleConfig config;
        public BattleGrid Grid;
        public GridView gridView;
        public ActionRunner runner;
        public BattleUI ui;
        public AudioService audio;
        public VfxService vfx;
        public CameraRig cam;
        public InputService input;
        public BattleBootstrap bootstrap;
        public readonly List<Combatant> Units = new List<Combatant>();
        public Inventory inventory;
        public State state = State.Intro;
        public Combatant CurrentActor { get; private set; }

        readonly Queue<Combatant> ready = new Queue<Combatant>();
        LinkSystem links;
        TargetingController targeting;

        public void Init()
        {
            Grid = new BattleGrid(config);
            targeting = new TargetingController(this);
            cam.Init(Grid.Center);
            bool kit = EnvironmentBuilder.Build(Grid, config, transform);
            gridView.Build(Grid, kit);
            runner.Init(this);
            vfx.Init(this);

            foreach (var p in config.party) Spawn(p.unit, p.cell, p.unit.displayName);
            var counts = new Dictionary<string, int>();
            foreach (var e in config.enemies)
            {
                counts.TryGetValue(e.unit.displayName, out int n);
                counts[e.unit.displayName] = n + 1;
                Spawn(e.unit, e.cell, e.unit.displayName);
            }
            cam.mustSee = Units.Where(u => u.IsPlayer).ToList();
            inventory = new Inventory(config.items);
            links = new LinkSystem(this, config.links);
            ui.Build(this);
            ui.ShowStart();
        }

        void OnDestroy() { links?.Dispose(); BattleEvents.Clear(); }

        void Spawn(UnitData d, Vector2Int cell, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var c = go.AddComponent<Combatant>();
            c.Init(d, name);
            var v = go.AddComponent<UnitView>();
            v.Build(d);
            c.view = v;
            Grid.Place(c, cell);
            go.transform.position = Grid.ToWorld(cell);
            go.transform.rotation = Quaternion.LookRotation(d.team == Team.Player ? Vector3.right : Vector3.left);
            c.atb = Random.Range(config.initialAtbRange.x, config.initialAtbRange.y);
            Units.Add(c);
        }

        public void Begin() => StartCoroutine(BeginRoutine());

        IEnumerator BeginRoutine()
        {
            audio.Play("battle_start");
            audio.PlayMusic();
            ui.HideStart();
            yield return new WaitForSeconds(0.6f);
            state = State.Running;
        }

        public void RequestRestart() => bootstrap.Restart();

        void Update()
        {
            if (state != State.Running) return;
            while (ready.Count > 0 && ready.Peek().isDead) ready.Dequeue();
            if (ready.Count > 0) { StartCoroutine(TakeTurn(ready.Dequeue())); return; }
            float dt = Time.deltaTime;
            var newlyReady = new List<Combatant>();
            foreach (var u in Units)
            {
                if (u.isDead) continue;
                u.atb += u.Stat(StatKey.SPD) * config.atbSpeedScale * dt;
                if (u.atb >= config.atbMax) newlyReady.Add(u);
            }
            foreach (var u in newlyReady.OrderByDescending(x => x.atb)) ready.Enqueue(u);
        }

        // ---------- turn flow ----------
        IEnumerator TakeTurn(Combatant u)
        {
            CurrentActor = u;
            ui.SetActive(u);
            var res = new ActionResult { recovery = config.skipRecovery };
            if (u.Has(StatusType.Freeze))
            {
                state = State.Executing;
                ui.Popup(u.view.Head, "Frozen", config.GetStatus(StatusType.Freeze).color);
                yield return new WaitForSeconds(0.7f);
            }
            else if (u.IsPlayer) { state = State.PlayerCommand; yield return PlayerTurn(u, res); }
            else { state = State.Executing; yield return EnemyTurn(u, res); }
            yield return EndTurn(u, res.recovery);
        }

        IEnumerator EndTurn(Combatant u, float recovery)
        {
            if (!u.isDead)
            {
                u.atb = config.atbMax - recovery;
                yield return TickEffects(u);
            }
            ui.SetActive(null);
            CurrentActor = null;
            if (state != State.Ended) state = State.Running;
        }

        IEnumerator EnemyTurn(Combatant e, ActionResult res)
        {
            var target = EnemyAI.ChooseTarget(e, Units);
            if (target == null) yield break;
            var req = new ActionRequest { actor = e, ability = e.data.basicAttack, targetUnit = target, preferFar = e.data.ranged };
            yield return runner.Perform(req, res);
        }

        IEnumerator PlayerTurn(Combatant c, ActionResult res)
        {
            var sel = new TargetingController.Result();
            while (state != State.Ended)
            {
                var main = new List<MenuEntry>
                {
                    new MenuEntry("Attack", "", true, c.data.basicAttack.description),
                    new MenuEntry("Use Art", "", c.data.arts.Count > 0, "Cast an Art using EP."),
                    new MenuEntry("Use Special", "", c.data.specials.Count > 0, "Perform a Craft using SP."),
                    new MenuEntry("Use Item", "", inventory.Items.Any(i => inventory.Count(i) > 0), "Use a consumable."),
                    new MenuEntry("Move", "", true, "Move up to " + c.Mov + " squares."),
                };
                var pick = new MenuResult();
                yield return ui.RunMenu(c.displayName, main, pick);
                if (pick.index < 0) continue;

                AbilityData ab = null; ItemData item = null; bool move = false;
                if (pick.index == 0) ab = c.data.basicAttack;
                else if (pick.index == 1 || pick.index == 2)
                {
                    var h = new Holder<AbilityData>();
                    yield return PickAbility(c, pick.index == 1 ? c.data.arts : c.data.specials, pick.index == 1, h);
                    ab = h.value;
                    if (ab == null) continue;
                }
                else if (pick.index == 3)
                {
                    var h = new Holder<ItemData>();
                    yield return PickItem(h);
                    item = h.value;
                    if (item == null) continue;
                }
                else move = true;

                yield return targeting.Select(c, ab, item, move, sel);
                if (sel.cancelled) continue;
                ui.SetTargetInfo("");
                var req = new ActionRequest { actor = c, ability = ab, item = item, targetUnit = sel.unit, cell = sel.cell, moveOnly = move };
                yield return runner.Perform(req, res);
                break;
            }
        }

        IEnumerator PickAbility(Combatant c, List<AbilityData> list, bool arts, Holder<AbilityData> h)
        {
            var entries = list.Select(a => new MenuEntry(a.displayName, arts ? a.epCost + " EP" : a.spCost + " SP",
                arts ? c.ep >= a.epCost : c.sp >= a.spCost, a.description)).ToList();
            var r = new MenuResult();
            yield return ui.RunMenu(arts ? "Arts" : "Specials", entries, r);
            h.value = r.index >= 0 ? list[r.index] : null;
        }

        IEnumerator PickItem(Holder<ItemData> h)
        {
            var list = inventory.Items.ToList();
            var entries = list.Select(i => new MenuEntry(i.displayName, "x" + inventory.Count(i), inventory.Count(i) > 0, i.description)).ToList();
            var r = new MenuResult();
            yield return ui.RunMenu("Items", entries, r);
            h.value = r.index >= 0 ? list[r.index] : null;
        }

        IEnumerator TickEffects(Combatant u)
        {
            foreach (var e in u.effects.ToList())
            {
                if (u.isDead) yield break;
                if (e.fresh) { e.fresh = false; continue; }
                if (!e.isBuff && e.tickPercent > 0f)
                {
                    DealDamage(u, Mathf.Max(1, Mathf.RoundToInt(u.MaxHp * e.tickPercent)), e.color);
                    yield return new WaitForSeconds(0.45f);
                    if (u.isDead) yield break;
                }
                e.turns--;
                if (e.turns <= 0)
                {
                    u.effects.Remove(e);
                    ui.Popup(u.view.Head, e.label + " ended", Color.white, 24);
                }
            }
        }

        // ---------- state mutation services ----------
        public void DealDamage(Combatant t, int dmg, Color? color = null)
        {
            if (t.isDead) return;
            t.hp = Mathf.Max(0, t.hp - dmg);
            ui.Popup(t.view.Head, dmg.ToString(), color ?? (t.IsPlayer ? new Color(1f, 0.55f, 0.45f) : Color.white), 38);
            t.view.Hurt();
            if (t.hp <= 0) Kill(t);
        }

        public void Kill(Combatant t)
        {
            if (t.isDead) return;
            t.hp = 0; t.isDead = true; t.effects.Clear(); t.shield = 0;
            Grid.Clear(t);
            t.view.Death(!t.IsPlayer);
            audio.Play("ko");
            vfx.Burst(t.view.Head, new Color(1f, 0.9f, 0.7f), 20, 3f, 0.3f, 0.8f);
            CheckEnd();
        }

        void CheckEnd()
        {
            if (state == State.Ended) return;
            bool enemies = Units.Any(u => !u.isDead && u.team == Team.Enemy);
            bool players = Units.Any(u => !u.isDead && u.team == Team.Player);
            if (!enemies) StartCoroutine(EndSequence(true));
            else if (!players) StartCoroutine(EndSequence(false));
        }

        IEnumerator EndSequence(bool win)
        {
            state = State.Ended;
            while (runner.Busy > 0) yield return null;
            yield return new WaitForSeconds(0.8f);
            audio.FadeOutMusic(1f);
            audio.Play(win ? "victory" : "defeat");
            ui.ShowEnd(win);
        }

        public void InstantKill(Combatant t)
        {
            if (t.isDead) return;
            ui.Popup(t.view.Head, "Instant Kill", new Color(0.8f, 0.4f, 1f), 36);
            audio.Play("status");
            Kill(t);
        }

        public void ApplyStatus(Combatant t, StatusType s)
        {
            if (t.isDead || s == StatusType.None) return;
            if (s == StatusType.InstantKill) { InstantKill(t); return; }
            var def = config.GetStatus(s);
            var e = t.effects.Find(x => !x.isBuff && x.status == s);
            if (e == null) { e = new ActiveEffect { label = def.label, status = s, color = def.color, tickPercent = def.tickPercent }; t.effects.Add(e); }
            e.turns = def.duration; e.fresh = t == CurrentActor;
            ui.Popup(t.view.Head, def.label, def.color, 32);
            audio.Play("status");
            vfx.Burst(t.view.Head, def.color, 16, 2f, 0.25f, 0.7f);
        }

        public void ApplyBuff(Combatant t, StatKey k, float mult, int turns)
        {
            if (t.isDead) return;
            string lbl = k + (mult >= 1f ? " Up" : " Down");
            var e = t.effects.Find(x => x.isBuff && x.label == lbl);
            if (e == null) { e = new ActiveEffect { label = lbl, isBuff = true, stat = k, color = mult >= 1f ? new Color(0.5f, 1f, 0.6f) : new Color(0.8f, 0.5f, 1f) }; t.effects.Add(e); }
            e.multiplier = mult; e.turns = turns; e.fresh = t == CurrentActor;
            ui.Popup(t.view.Head, lbl, e.color, 30);
            audio.Play(mult >= 1f ? "buff" : "debuff");
        }

        public void Heal(Combatant t, int amount)
        {
            if (t.isDead) return;
            int a = Mathf.Min(amount, t.MaxHp - t.hp);
            t.hp += a;
            ui.Popup(t.view.Head, "+" + a, new Color(0.5f, 1f, 0.6f), 36);
            audio.Play("heal");
            vfx.Rise(t.view.transform.position + Vector3.up * 0.3f, new Color(0.5f, 1f, 0.6f));
        }

        public void RestoreEp(Combatant t, int amount)
        {
            if (t.isDead) return;
            int a = Mathf.Min(amount, t.MaxEp - t.ep);
            t.ep += a;
            ui.Popup(t.view.Head, "+" + a + " EP", new Color(0.5f, 0.8f, 1f), 32);
            audio.Play("buff");
            vfx.Rise(t.view.transform.position + Vector3.up * 0.3f, new Color(0.5f, 0.8f, 1f));
        }

        public void Cure(Combatant t)
        {
            if (t.effects.RemoveAll(e => !e.isBuff) > 0) ui.Popup(t.view.Head, "Cured", Color.white, 30);
        }

        public void GainSp(Combatant t, int n)
        {
            if (t.MaxSp <= 0 || t.isDead) return;
            t.sp = Mathf.Min(t.MaxSp, t.sp + n);
        }

        public void ResetAtb(Combatant t)
        {
            t.atb = config.resetAtbValue;
            ui.Popup(t.view.Head, "ATB Reset", new Color(1f, 0.9f, 0.5f), 26);
        }
    }
}
