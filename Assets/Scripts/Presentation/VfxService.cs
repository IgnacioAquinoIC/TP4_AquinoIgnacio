using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    public class VfxService : MonoBehaviour
    {
        BattleManager m;
        public void Init(BattleManager mgr) { m = mgr; }

        GameObject Quad(Texture tex, Color c, Vector3 pos, Vector3 scale, Quaternion rot, out Material mat)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.transform.position = pos; q.transform.rotation = rot; q.transform.localScale = scale;
            mat = MaterialFactory.Overlay(c, tex);
            q.GetComponent<Renderer>().sharedMaterial = mat;
            return q;
        }

        Quaternion Billboard(float roll = 0f) => m.cam.cam.transform.rotation * Quaternion.Euler(0, 0, roll);

        public void Burst(Vector3 pos, Color c, int count = 18, float speed = 3.5f, float size = 0.25f, float life = 0.6f, float gravity = 0f)
        {
            var go = new GameObject("fx");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = life; main.loop = false; main.startLifetime = life; main.startSpeed = speed;
            main.startSize = size; main.startColor = c; main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.15f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                         new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            col.color = grad;
            go.GetComponent<ParticleSystemRenderer>().material = MaterialFactory.Overlay(Color.white, TextureFactory.Disc());
            ps.Play();
            Destroy(go, life + 1f);
        }

        public void Rise(Vector3 pos, Color c) => Burst(pos, c, 14, 1.2f, 0.2f, 0.9f, -0.2f);

        public void Slash(Vector3 pos, Color c, float angle, float length = 2.2f)
        {
            StartCoroutine(SlashRoutine(pos, c, angle, length));
        }

        IEnumerator SlashRoutine(Vector3 pos, Color c, float angle, float length)
        {
            var q = Quad(TextureFactory.Streak(), c, pos, new Vector3(length, 0.35f, 1), Billboard(angle), out var mat);
            yield return Util.Tween(0.22f, t =>
            {
                q.transform.localScale = new Vector3(length * (0.6f + t * 0.6f), 0.35f * (1f - t * 0.7f), 1);
                mat.color = Util.WithAlpha(c, 1f - t);
            });
            Destroy(q);
        }

        public IEnumerator Projectile(Vector3 from, Vector3 to, Color c, float speed = 18f)
        {
            var q = Quad(TextureFactory.Disc(), c, from, Vector3.one * 0.6f, Billboard(), out _);
            float dur = Mathf.Max(0.12f, Vector3.Distance(from, to) / speed);
            float t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                q.transform.position = Vector3.Lerp(from, to, t / dur);
                q.transform.rotation = Billboard();
                if (Random.value < 0.5f) Burst(q.transform.position, c, 2, 0.5f, 0.15f, 0.3f);
                yield return null;
            }
            Destroy(q);
        }

        public IEnumerator Beam(Vector3 from, Vector3 to, Color c, float width, float dur)
        {
            Vector3 dir = to - from; float len = dir.magnitude;
            var q = Quad(TextureFactory.Streak(), c, (from + to) * 0.5f, new Vector3(len, width, 1), Quaternion.identity, out var mat);
            var qg = Quad(TextureFactory.Streak(), Color.white, (from + to) * 0.5f, new Vector3(len, width * 0.35f, 1), Quaternion.identity, out _);
            Vector3 flat = new Vector3(dir.x, 0, dir.z);
            // lay the quad flat on the ground plane, long axis along the beam direction
            float ang = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            q.transform.rotation = Quaternion.Euler(90, ang - 90f, 0);
            qg.transform.rotation = q.transform.rotation;
            q.transform.position = (from + to) * 0.5f; qg.transform.position = q.transform.position;
            Burst(from, c, 20, 5f, 0.25f, 0.5f);
            yield return Util.Tween(dur, t =>
            {
                float w = width * (1f - t * 0.6f);
                q.transform.localScale = new Vector3(len, w, 1);
                qg.transform.localScale = new Vector3(len, w * 0.35f, 1);
                mat.color = Util.WithAlpha(c, 1f - t);
            });
            Destroy(q); Destroy(qg);
        }

        public void Ring(Vector3 center, float radius, Color c, float dur = 0.6f)
        {
            StartCoroutine(RingRoutine(center, radius, c, dur));
        }

        IEnumerator RingRoutine(Vector3 center, float radius, Color c, float dur)
        {
            var q = Quad(TextureFactory.Ring(), c, center + Vector3.up * 0.08f, Vector3.one * 0.2f, Quaternion.Euler(90, 0, 0), out var mat);
            yield return Util.Tween(dur, t =>
            {
                q.transform.localScale = Vector3.one * (radius * 2f * Mathf.Sqrt(t) + 0.2f);
                mat.color = Util.WithAlpha(c, 1f - t);
            });
            Destroy(q);
        }

        public void Column(Vector3 pos, Color c, float height = 7f, float width = 1.2f, float dur = 0.8f)
        {
            StartCoroutine(ColumnRoutine(pos, c, height, width, dur));
        }

        IEnumerator ColumnRoutine(Vector3 pos, Color c, float height, float width, float dur)
        {
            var q = Quad(TextureFactory.Streak(), c, pos + Vector3.up * height * 0.5f, new Vector3(height, width, 1), Quaternion.identity, out var mat);
            yield return Util.Tween(dur, t =>
            {
                q.transform.rotation = Billboard(90f);
                q.transform.localScale = new Vector3(height, width * (1f - t * 0.8f), 1);
                mat.color = Util.WithAlpha(c, Mathf.Sin(t * Mathf.PI));
            });
            Destroy(q);
        }

        // ---- Ultimate presentations ----
        public IEnumerator Shockwave(Vector3 from, Vector3 to, Color c, float width)
        {
            StartCoroutine(Beam(from, to, c, width, 0.5f));
            float dur = 0.45f;
            float t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                Vector3 p = Vector3.Lerp(from, to, t / dur);
                Burst(p + Vector3.up * 0.2f, new Color(0.85f, 0.8f, 0.7f), 6, 2.5f, 0.35f, 0.6f, 0.6f);
                yield return null;
            }
        }

        public IEnumerator Blink(Combatant c, Vector3 to, Color col)
        {
            Vector3 from = c.view.transform.position;
            StartCoroutine(Beam(from + Vector3.up * 0.1f, to + Vector3.up * 0.1f, col, 0.7f, 0.2f));
            Burst(from + Vector3.up, col, 14, 3f, 0.25f, 0.4f);
            c.view.SetVisible(false);
            c.view.transform.position = to;
            yield return null;
            c.view.SetVisible(true);
            Burst(to + Vector3.up, col, 14, 3f, 0.25f, 0.4f);
        }

        public IEnumerator BlinkStrike(Combatant caster, List<Combatant> targets, Color col)
        {
            Vector3 home = caster.view.transform.position;
            Quaternion rot0 = caster.view.transform.rotation;
            yield return new WaitForSeconds(0.25f);
            foreach (var t in targets)
            {
                if (t == null) continue;
                Vector3 tp = t.view.transform.position;
                Vector3 p = tp + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * 1.1f;
                yield return Blink(caster, p, col);
                caster.view.Face(tp);
                Slash(t.view.Head - Vector3.up * 0.5f, col, Random.Range(-60f, 60f), 2.6f);
                Burst(t.view.Head - Vector3.up * 0.4f, Color.white, 10, 3f, 0.25f, 0.4f);
                m.audio.Play("sword_swing", Random.Range(0.95f, 1.3f));
                m.cam.Shake(0.08f, 0.08f);
                yield return new WaitForSeconds(0.2f);
            }
            yield return Blink(caster, home, col);
            caster.view.transform.rotation = rot0;
            caster.view.Idle();
        }

        IEnumerator Tornado(Vector3 center, Color c, float dur)
        {
            const int n = 30; const float h = 7f;
            var qs = new GameObject[n]; var mats = new Material[n];
            for (int i = 0; i < n; i++) qs[i] = Quad(TextureFactory.Streak(), c, center, new Vector3(1.8f, 0.3f, 1f), Billboard(), out mats[i]);
            yield return Util.Tween(dur, t =>
            {
                float grow = Mathf.Min(1f, t * 3f);
                float fade = t > 0.85f ? (1f - t) / 0.15f : 1f;
                for (int i = 0; i < n; i++)
                {
                    float k = i / (float)n;
                    float ang = k * Mathf.PI * 6f + t * dur * 9f;
                    float r = Mathf.Lerp(0.6f, 2.8f, k) * grow;
                    qs[i].transform.position = center + new Vector3(Mathf.Cos(ang) * r, k * h * grow, Mathf.Sin(ang) * r);
                    qs[i].transform.rotation = Billboard(Mathf.Sin(ang) * 25f);
                    mats[i].color = Util.WithAlpha(Color.Lerp(c, Color.white, k * 0.5f), 0.9f * fade);
                }
                if (Random.value < 0.6f) Burst(center + new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(0f, h), Random.Range(-1.5f, 1.5f)), c, 3, 2f, 0.3f, 0.5f);
            });
            foreach (var q in qs) Destroy(q);
        }

        // ---- Ultimate presentations ----
        public IEnumerator ValiantDawn(Combatant caster, List<Combatant> targets)
        {
            var gold = new Color(1f, 0.85f, 0.4f);
            var view = caster.view;
            Vector3 home = view.transform.position;
            Quaternion rot0 = view.transform.rotation;
            Vector3 land = Vector3.zero;
            foreach (var t in targets) land += t.view.transform.position;
            land /= Mathf.Max(1, targets.Count);

            m.ui.SetDim(0.5f);
            m.cam.FocusOn(home + Vector3.up, 7f, 5f);
            m.audio.Play("ultimate");
            Ring(home, 3f, gold, 0.8f);
            Column(home, gold, 8f, 2.2f, 0.9f);
            yield return new WaitForSeconds(0.8f);

            view.Face(land);
            m.cam.FocusOn((home + land) * 0.5f + Vector3.up * 2f, 11f, 3f);
            float jump = 0.95f, apex = 5.5f;
            yield return Util.Tween(jump, t => view.transform.position = Vector3.Lerp(home, land, t) + Vector3.up * (apex * 4f * t * (1f - t)));
            view.transform.position = land;

            m.cam.FocusOn(land + Vector3.up, 8f, 5f);
            m.cam.Shake(0.4f, 0.3f);
            m.audio.Play("impact", 0.6f);
            Ring(land, 4f, gold, 0.7f);
            Burst(land + Vector3.up * 0.2f, new Color(0.9f, 0.85f, 0.7f), 40, 6f, 0.5f, 0.8f, 0.4f);
            view.Play("Ultimate");
            float spinDur = 2.4f;
            StartCoroutine(Tornado(land, gold, spinDur));
            m.audio.Play("sword_swing", 0.8f);
            StartCoroutine(view.Spin(spinDur, 3f));
            for (float t = 0; t < spinDur; t += 0.3f)
            {
                m.audio.Play("sword_swing", 0.9f + t * 0.1f);
                Ring(land, 3f, gold, 0.5f);
                yield return new WaitForSeconds(0.3f);
            }

            m.ui.Flash(new Color(1f, 0.95f, 0.8f), 0.7f);
            m.cam.Shake(0.5f, 0.35f);
            foreach (var t in targets)
            {
                Ring(t.view.transform.position, 2.2f, gold, 0.7f);
                Column(t.view.transform.position, gold, 7f, 1.6f, 0.8f);
                Burst(t.view.Head, gold, 30, 6f, 0.35f, 0.9f);
            }
            m.audio.Play("impact", 0.6f);
            yield return new WaitForSeconds(0.6f);
            yield return Blink(caster, home, gold);
            view.transform.rotation = rot0;
            view.Idle();
            m.ui.SetDim(0f);
            m.cam.ResetView();
        }

        public IEnumerator FatesCard(Combatant caster, Vector3 center, float radius, List<Combatant> targets)
        {
            var violet = new Color(0.75f, 0.45f, 1f);
            m.ui.SetDim(0.5f);
            m.cam.FocusOn(center, 7.5f, 4f);
            m.audio.Play("ultimate");
            Ring(caster.view.transform.position, 2.5f, violet, 0.8f);
            Rise(caster.view.Head, violet);
            yield return new WaitForSeconds(0.8f);
            var card = Quad(TextureFactory.Card(), Color.white, center + Vector3.up * 0.5f, new Vector3(0.1f, 0.15f, 1), Billboard(), out var mat);
            Ring(center, radius + 0.5f, violet, 1.2f);
            yield return Util.Tween(1.6f, t =>
            {
                float rise = Mathf.SmoothStep(0, 1, t);
                card.transform.position = center + Vector3.up * (0.5f + rise * 3.2f);
                card.transform.rotation = Billboard();
                float flip = Mathf.Cos(t * 14f);
                card.transform.localScale = new Vector3(1.6f * Mathf.Abs(flip) + 0.05f, 2.4f * Mathf.Min(1f, t * 3f), 1);
                if (Random.value < 0.6f) Burst(card.transform.position, violet, 3, 1.5f, 0.2f, 0.6f);
            });
            m.ui.Flash(new Color(0.8f, 0.6f, 1f), 0.6f);
            m.cam.Shake(0.4f, 0.25f);
            Destroy(card);
            Ring(center, radius + 1f, violet, 0.8f);
            Burst(center + Vector3.up * 3.5f, violet, 60, 7f, 0.4f, 1f);
            foreach (var t in targets) Column(t.view.transform.position, violet, 6f, 1.4f, 0.7f);
            m.audio.Play("impact", 0.7f);
            yield return new WaitForSeconds(0.8f);
            m.ui.SetDim(0f);
            m.cam.ResetView();
        }
    }
}
