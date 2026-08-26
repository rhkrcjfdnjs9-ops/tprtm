using System.Collections;
using UnityEngine;

namespace RealStone
{
    public sealed class ActorVisualMotion : MonoBehaviour
    {
        public enum LoopMotion { None, Idle, Run }

        private Vector3 basePosition;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private LoopMotion loopMotion;
        private float clock;
        private bool actionPlaying;
        private bool heavy;

        public void Configure(bool isHeavy)
        {
            heavy = isHeavy;
            basePosition = transform.localPosition;
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
            loopMotion = LoopMotion.Idle;
        }

        public void SetLoop(LoopMotion motion)
        {
            loopMotion = motion;
            clock = 0f;
            if (!actionPlaying) ResetPose();
        }

        private void LateUpdate()
        {
            if (actionPlaying) return;
            clock += Time.deltaTime;
            if (loopMotion == LoopMotion.Idle) ApplyIdle();
            else if (loopMotion == LoopMotion.Run) ApplyRun();
            else ResetPose();
        }

        private void ApplyIdle()
        {
            var speed = heavy ? 2.2f : 2.8f;
            var wave = Mathf.Sin(clock * speed);
            var breath = (wave + 1f) * 0.5f;
            transform.localPosition = basePosition + Vector3.up * (heavy ? 0.018f : 0.028f) * wave;
            transform.localScale = Vector3.Scale(baseScale,
                new Vector3(1f - breath * 0.006f, 1f + breath * 0.008f, 1f));
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, wave * (heavy ? 0.25f : 0.45f));
        }

        private void ApplyRun()
        {
            var phase = clock * (heavy ? 10f : 14f);
            var step = Mathf.Abs(Mathf.Sin(phase));
            var lean = heavy ? -1.5f : -3f;
            transform.localPosition = basePosition + Vector3.up * step * (heavy ? 0.055f : 0.075f);
            transform.localScale = Vector3.Scale(baseScale,
                new Vector3(1f + step * 0.012f, 1f - step * 0.01f, 1f));
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, lean + Mathf.Sin(phase) * 0.7f);
        }

        public IEnumerator PlayAttack(float duration)
        {
            yield return PlayCurve(duration, (t) =>
            {
                var anticipation = SmoothRange(t, 0f, 0.24f);
                var strike = SmoothRange(t, 0.24f, 0.48f);
                var recovery = SmoothRange(t, 0.48f, 1f);
                var drive = strike * (1f - recovery);
                var pull = anticipation * (1f - strike);
                transform.localPosition = basePosition + Vector3.right * (drive * (heavy ? 0.08f : 0.15f) - pull * 0.05f);
                transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f,
                    pull * (heavy ? 2f : 4f) - drive * (heavy ? 2.5f : 5f));
                transform.localScale = Vector3.Scale(baseScale,
                    new Vector3(1f + drive * 0.018f, 1f - drive * 0.012f, 1f));
            });
        }

        public IEnumerator PlayHit(float duration)
        {
            yield return PlayCurve(duration, (t) =>
            {
                var kick = Mathf.Sin(Mathf.Clamp01(t * 1.8f) * Mathf.PI) * (1f - t);
                transform.localPosition = basePosition + Vector3.left * kick * (heavy ? 0.07f : 0.12f);
                transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, kick * (heavy ? 2f : 4f));
                transform.localScale = Vector3.Scale(baseScale, new Vector3(1f + kick * 0.02f, 1f - kick * 0.025f, 1f));
            });
        }

        public IEnumerator PlayDeath(float duration)
        {
            yield return PlayCurve(duration, (t) =>
            {
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                transform.localPosition = basePosition + Vector3.down * eased * 0.08f;
                transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, eased * (heavy ? -3f : 5f));
                transform.localScale = Vector3.Scale(baseScale, new Vector3(1f + eased * 0.025f, 1f - eased * 0.035f, 1f));
            }, false);
        }

        private IEnumerator PlayCurve(float duration, System.Action<float> pose, bool reset = true)
        {
            actionPlaying = true;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                pose(Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
                yield return null;
            }
            actionPlaying = false;
            if (reset) ResetPose();
        }

        private void ResetPose()
        {
            transform.localPosition = basePosition;
            transform.localScale = baseScale;
            transform.localRotation = baseRotation;
        }

        private static float SmoothRange(float value, float start, float end) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));
    }
}
