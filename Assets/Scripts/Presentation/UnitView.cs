using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    /// Visual representation of a combatant. Drives an Animator when a custom model provides one
    /// (triggers: Attack, Cast, Special, Ultimate, Hurt, Death; bool: Moving), otherwise procedural animation.
    public class UnitView : MonoBehaviour
    {
        Transform pivot, model, armR, armL;
        Animator animator;
        ClipPlayer clipPlayer;
        PoseDriver poseDriver;
        UnitData data;
        bool rigMode;
        float hover, hoverBob, lift, liftTarget, tilt;
        GameObject blob;
        Renderer[] rends;
        Color[] baseCols;
        Coroutine act;
        bool moving, dead;
        float seed, height = 2f;
        Vector3 pivotRest;

        public Vector3 Head => transform.position + Vector3.up * height;

        public void Build(UnitData d)
        {
            data = d;
            seed = Random.value * 10f;
            pivot = new GameObject("Pivot").transform;
            pivot.SetParent(transform, false);
            GameObject m = null;
            if (!string.IsNullOrEmpty(d.modelResourcePath)) m = ModelLoader.Load(d);
            if (m != null)
            {
                rigMode = true;
                height = d.targetHeight + d.hoverHeight;
                hover = d.hoverHeight; hoverBob = d.hoverBob;
                var inner = m.transform.Find("Model");
                clipPlayer = m.AddComponent<ClipPlayer>();
                clipPlayer.Init(inner.gameObject, d.modelResourcePath, d.clips);
                if (clipPlayer.Has("Idle")) clipPlayer.Play("Idle");
                poseDriver = m.AddComponent<PoseDriver>();
                poseDriver.Init(inner, transform, d.rigBones);
                if (hover > 0f) MakeBlob();
            }
            else if (d.modelPrefab != null)
            {
                m = Instantiate(d.modelPrefab);
                animator = m.GetComponentInChildren<Animator>();
                if (animator != null && d.animatorController != null) animator.runtimeAnimatorController = d.animatorController;
            }
            else if (m == null)
            {
                var parts = ProceduralModels.Build(d);
                m = parts.root; armR = parts.armR; armL = parts.armL; height = parts.height * d.scale;
            }
            model = m.transform;
            model.SetParent(pivot, false);
            if (!rigMode) model.localScale = Vector3.one * d.scale;
            rends = model.GetComponentsInChildren<Renderer>();
            baseCols = new Color[rends.Length];
            for (int i = 0; i < rends.Length; i++) baseCols[i] = GetTint(rends[i]);
        }

        static string TintProp(Renderer r)
        {
            var m = r.sharedMaterial;
            if (m == null) return null;
            if (m.HasProperty("_BaseColor")) return "_BaseColor";
            if (m.HasProperty("_Color")) return "_Color";
            return null;
        }

        static Color GetTint(Renderer r)
        {
            var p = TintProp(r);
            return p == null || r is ParticleSystemRenderer ? Color.white : r.material.GetColor(p);
        }

        void MakeBlob()
        {
            blob = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(blob.GetComponent<Collider>());
            blob.transform.SetParent(transform, false);
            blob.transform.localPosition = new Vector3(0, 0.04f, 0);
            blob.transform.localRotation = Quaternion.Euler(90, 0, 0);
            blob.transform.localScale = Vector3.one * 1.3f;
            blob.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Overlay(new Color(0, 0, 0, 0.35f), TextureFactory.Disc());
        }

        public void SetVisible(bool v)
        {
            foreach (var r in rends) if (r != null) r.enabled = v;
            if (blob != null) blob.SetActive(v);
        }

        public void Idle() { if (!dead && rigMode && clipPlayer.Has("Idle")) clipPlayer.Play("Idle"); }

        public float ClipDuration(string key) => clipPlayer != null && clipPlayer.Ready ? clipPlayer.Duration(key) : 0f;

        public IEnumerator Spin(float duration, float turns)
        {
            Quaternion start = transform.rotation;
            yield return Util.Tween(duration, t => transform.rotation = start * Quaternion.Euler(0, 360f * turns * t, 0));
            transform.rotation = start;
        }

        void Update()
        {
            if (dead || model == null) return;
            float y;
            if (rigMode)
            {
                lift = Mathf.MoveTowards(lift, liftTarget, Time.deltaTime * 2f);
                y = hover > 0f ? hover + lift + Mathf.Sin(Time.time * 2f + seed) * hoverBob : 0f;
                float tt = moving && hover > 0f ? 12f : 0f;
                tilt = Mathf.Lerp(tilt, tt, Time.deltaTime * 6f);
                model.localRotation = Quaternion.Euler(tilt, 0, 0);
            }
            else y = moving ? Mathf.Abs(Mathf.Sin(Time.time * 12f)) * 0.12f : Mathf.Sin(Time.time * 2f + seed) * 0.025f;
            model.localPosition = new Vector3(0, y, 0);
            if (blob != null) blob.transform.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.8f, Mathf.Clamp01((y - hover) * 1.5f));
            if (animator != null) animator.SetBool("Moving", moving);
        }

        public void Face(Vector3 worldPoint)
        {
            var dir = worldPoint - transform.position; dir.y = 0;
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
        }

        public IEnumerator StepTo(Vector3 target, float duration)
        {
            Face(target);
            moving = true;
            Vector3 start = transform.position;
            yield return Util.Tween(duration, t => transform.position = Vector3.Lerp(start, target, t));
        }

        public void SetMoving(bool v) => moving = v;

        void ResetPose()
        {
            if (pivot != null) { pivot.localPosition = Vector3.zero; pivot.localRotation = Quaternion.identity; }
            if (armR != null) armR.localRotation = Quaternion.identity;
            if (armL != null) armL.localRotation = Quaternion.identity;
        }

        public void Play(string key)
        {
            if (dead) return;
            if (rigMode)
            {
                var b = clipPlayer.Binding(key);
                if (b != null)
                {
                    clipPlayer.Play(key);
                    if (b.pose != PoseKind.None) poseDriver.Play(b.pose, b.playLength > 0f ? b.playLength : 1f);
                    liftTarget = b.lift;
                    if (b.lift > 0f) StartCoroutine(ReleaseLift(b.playLength > 0f ? b.playLength : 1f));
                    if (key == "Hurt") { if (act != null) StopCoroutine(act); act = StartCoroutine(HurtFlash()); }
                    return;
                }
                if (key != "Hurt") return;
            }
            if (animator != null) animator.SetTrigger(key);
            if (act != null) StopCoroutine(act);
            ResetPose();
            act = StartCoroutine(Procedural(key));
        }

        IEnumerator ReleaseLift(float t) { yield return new WaitForSeconds(t); liftTarget = 0f; }

        IEnumerator HurtFlash()
        {
            yield return Util.Tween(0.28f, t => Flash(new Color(1f, 0.25f, 0.2f), 1f - t));
        }

        IEnumerator Procedural(string key)
        {
            if (animator != null) yield break;
            switch (key)
            {
                case "Attack":
                    yield return Util.Tween(0.15f, t => { pivot.localPosition = new Vector3(0, 0, 0.5f * t); if (armR) armR.localRotation = Quaternion.Euler(-110f * t, 0, 0); });
                    yield return Util.Tween(0.25f, t => { pivot.localPosition = new Vector3(0, 0, 0.5f * (1 - t)); if (armR) armR.localRotation = Quaternion.Euler(-110f * (1 - t), 0, 0); });
                    break;
                case "Cast":
                    yield return Util.Tween(0.3f, t => { if (armR) armR.localRotation = Quaternion.Euler(-150f * t, 0, 0); if (armL) armL.localRotation = Quaternion.Euler(-150f * t, 0, 0); pivot.localPosition = new Vector3(0, 0.25f * t, 0); });
                    yield return new WaitForSeconds(0.5f);
                    yield return Util.Tween(0.25f, t => { if (armR) armR.localRotation = Quaternion.Euler(-150f * (1 - t), 0, 0); if (armL) armL.localRotation = Quaternion.Euler(-150f * (1 - t), 0, 0); pivot.localPosition = new Vector3(0, 0.25f * (1 - t), 0); });
                    break;
                case "Special":
                    yield return Util.Tween(0.6f, t => { pivot.localRotation = Quaternion.Euler(0, 360f * t, 0); pivot.localPosition = new Vector3(0, Mathf.Sin(t * Mathf.PI) * 0.4f, 0); if (armR) armR.localRotation = Quaternion.Euler(-100f, 0, 0); });
                    break;
                case "Ultimate":
                    yield return Util.Tween(0.4f, t => { pivot.localPosition = new Vector3(0, 0, -0.2f * t); if (armR) armR.localRotation = Quaternion.Euler(-170f * t, 0, 0); if (armL) armL.localRotation = Quaternion.Euler(-170f * t, 0, 0); });
                    yield return Util.Tween(2.2f, t => { pivot.localPosition = new Vector3(0, 0.5f + Mathf.Sin(t * 20f) * 0.05f, -0.2f); pivot.localRotation = Quaternion.Euler(0, t * 720f, 0); });
                    yield return Util.Tween(0.3f, t => { pivot.localPosition = new Vector3(0, 0.5f * (1 - t), -0.2f * (1 - t)); pivot.localRotation = Quaternion.identity; });
                    break;
                case "Hurt":
                    yield return Util.Tween(0.28f, t =>
                    {
                        pivot.localPosition = new Vector3(Random.Range(-0.05f, 0.05f), 0, -0.2f * Mathf.Sin(t * Mathf.PI));
                        Flash(new Color(1f, 0.25f, 0.2f), 1f - t);
                    });
                    break;
            }
            ResetPose();
        }

        void Flash(Color c, float k)
        {
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null) continue;
                var p = TintProp(rends[i]);
                if (p != null) rends[i].material.SetColor(p, Color.Lerp(baseCols[i], c, k));
            }
        }

        public void Hurt() { Play("Hurt"); }

        public void Death(bool vanish)
        {
            if (dead) return;
            if (animator != null) animator.SetTrigger("Death");
            if (act != null) StopCoroutine(act);
            dead = true; moving = false;
            bool clipDeath = rigMode && clipPlayer.Binding("Death") != null && !string.IsNullOrEmpty(clipPlayer.Binding("Death").clipName);
            if (clipDeath) clipPlayer.Play("Death");
            StartCoroutine(DeathRoutine(vanish, clipDeath));
        }

        IEnumerator DeathRoutine(bool vanish, bool clipDeath)
        {
            Flash(Color.white, 0f);
            if (clipDeath) yield return new WaitForSeconds(Mathf.Max(0.5f, clipPlayer.Duration("Death")));
            else if (rigMode && hover > 0f) yield return Util.Tween(0.6f, t => { hover = Mathf.Lerp(data.hoverHeight, 0.05f, t); pivot.localRotation = Quaternion.Euler(-80f * t, 0, 0); model.localPosition = new Vector3(0, hover, 0); });
            else yield return Util.Tween(0.5f, t => { pivot.localRotation = Quaternion.Euler(-90f * t, 0, 0); pivot.localPosition = new Vector3(0, 0.15f * t, 0); });
            if (vanish)
            {
                yield return new WaitForSeconds(0.3f);
                yield return Util.Tween(0.6f, t => transform.localScale = Vector3.one * (1f - t));
                gameObject.SetActive(false);
            }
            else Flash(Color.gray, 0.5f);
        }
    }
}
