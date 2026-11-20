using System.Collections.Generic;
using UnityEngine;

namespace RpgBattle
{
    /// Rig-agnostic arm posing. Bones are aimed along character-space directions (FromToRotation on the
    /// bone->child vector), so it needs no knowledge of bone axes. Runs in LateUpdate over the clip animation.
    public class PoseDriver : MonoBehaviour
    {
        class Arm { public Transform upper, lower, hand; public float outward = 1f; }
        Arm armR, armL;
        Transform unit;
        PoseKind pose;
        float weight, holdUntil;

        public bool Ready => armR != null || armL != null;

        static Transform Find(Transform root, string n)
        {
            if (string.IsNullOrEmpty(n)) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (string.Equals(t.name, n, System.StringComparison.OrdinalIgnoreCase)) return t;
            return null;
        }

        public void Init(Transform model, Transform unitRoot, RigBones rb)
        {
            unit = unitRoot;
            armR = Make(model, rb.upperArmR, rb.lowerArmR, rb.handR);
            armL = Make(model, rb.upperArmL, rb.lowerArmL, rb.handL);
        }

        Arm Make(Transform root, string u, string l, string h)
        {
            var a = new Arm { upper = Find(root, u), lower = Find(root, l), hand = Find(root, h) };
            if (a.upper == null || a.lower == null || a.hand == null) return null;
            float side = Vector3.Dot(a.upper.position - unit.position, unit.right);
            a.outward = side >= 0f ? 1f : -1f;
            return a;
        }

        public void Play(PoseKind kind, float hold)
        {
            if (!Ready || kind == PoseKind.None) return;
            pose = kind; holdUntil = Time.time + hold;
        }

        public void Release() { holdUntil = 0f; }

        Vector3 Dir(Arm a, float o, float u, float f) => (unit.right * (o * a.outward) + unit.up * u + unit.forward * f).normalized;

        static void Aim(Transform bone, Transform child, Vector3 wanted, float w)
        {
            var cur = (child.position - bone.position);
            if (cur.sqrMagnitude < 1e-8f) return;
            var q = Quaternion.FromToRotation(cur.normalized, wanted) * bone.rotation;
            bone.rotation = Quaternion.Slerp(bone.rotation, q, w);
        }

        void Apply(Arm a, PoseKind kind, bool primary, float w)
        {
            if (a == null || w <= 0.001f) return;
            Vector3 up, lo;
            switch (kind)
            {
                case PoseKind.HandToChest:
                    up = Dir(a, 0.15f, -0.7f, 0.35f); lo = Dir(a, -0.5f, 0.3f, 0.8f); break;
                case PoseKind.ArmsRaised:
                    up = Dir(a, 0.55f, 0.65f, 0.5f); lo = Dir(a, 0.25f, 0.85f, 0.45f); break;
                case PoseKind.ArmsSpread:
                    up = Dir(a, 0.95f, 0.35f, 0f); lo = Dir(a, 0.9f, 0.4f, 0.1f); break;
                case PoseKind.PointForward:
                    if (primary) { up = Dir(a, 0.1f, 0.2f, 0.95f); lo = Dir(a, 0.05f, 0.15f, 1f); }
                    else { up = Dir(a, 0.15f, -0.7f, 0.35f); lo = Dir(a, -0.5f, 0.3f, 0.8f); }
                    break;
                default: return;
            }
            Aim(a.upper, a.lower, up, w);
            Aim(a.lower, a.hand, lo, w);
        }

        void LateUpdate()
        {
            float target = (pose != PoseKind.None && Time.time < holdUntil) ? 1f : 0f;
            weight = Mathf.MoveTowards(weight, target, Time.deltaTime * 6f);
            if (weight <= 0.001f) return;
            switch (pose)
            {
                case PoseKind.HandToChest:
                    Apply(armL ?? armR, pose, true, weight); break;
                case PoseKind.PointForward:
                    Apply(armR, pose, true, weight); Apply(armL, pose, false, weight); break;
                default:
                    Apply(armR, pose, true, weight); Apply(armL, pose, true, weight); break;
            }
        }
    }
}
