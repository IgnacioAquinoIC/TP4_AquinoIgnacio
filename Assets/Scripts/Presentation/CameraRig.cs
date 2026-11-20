using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    public class CameraRig : MonoBehaviour
    {
        public Camera cam;
        public float pitch = 40f, yaw = 45f, distance = 40f;
        public float defaultSize = 11.5f;
        public List<Combatant> mustSee = new List<Combatant>();
        Vector3 baseFocus, focus, targetFocus;
        float size, targetSize, speed = 4f, shakeT, shakeMag;

        public void Init(Vector3 gridCenter)
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.7f, 0.85f);
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 200f;
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            baseFocus = gridCenter - fwd * 3f;
            focus = targetFocus = baseFocus;
            size = targetSize = defaultSize;
            Apply();
        }

        public void FocusOn(Vector3 pos, float orthoSize, float lerpSpeed = 4f) { targetFocus = pos; targetSize = orthoSize; speed = lerpSpeed; }
        public void ResetView() { targetFocus = baseFocus; targetSize = defaultSize; speed = 3f; }
        public void Shake(float duration, float magnitude) { shakeT = duration; shakeMag = magnitude; }

        void LateUpdate()
        {
            if (cam == null) return;
            focus = Vector3.Lerp(focus, targetFocus, 1f - Mathf.Exp(-speed * Time.deltaTime));
            size = Mathf.Lerp(size, targetSize, 1f - Mathf.Exp(-speed * Time.deltaTime));
            Apply();
            KeepVisible();
        }

        // Guarantees living player characters stay inside the safe screen area (clear of the HUD panels).
        void KeepVisible()
        {
            foreach (var c in mustSee)
            {
                if (c == null || c.isDead || c.view == null) continue;
                var vp = cam.WorldToViewportPoint(c.view.transform.position + Vector3.up);
                if (vp.x < 0.05f || vp.x > 0.95f || vp.y < 0.22f || vp.y > 0.92f)
                {
                    targetSize = Mathf.Min(defaultSize, targetSize * 1.04f + 0.1f);
                    targetFocus = Vector3.Lerp(targetFocus, Vector3.Lerp(baseFocus, c.view.transform.position, 0.5f), 0.1f);
                    size = Mathf.Max(size, Mathf.Min(defaultSize, size * 1.04f));
                }
            }
        }

        void Apply()
        {
            var rot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 offset = Vector3.zero;
            if (shakeT > 0) { shakeT -= Time.deltaTime; offset = Random.insideUnitSphere * shakeMag; }
            cam.transform.rotation = rot;
            cam.transform.position = focus - rot * Vector3.forward * distance + offset;
            cam.orthographicSize = size;
        }
    }
}
