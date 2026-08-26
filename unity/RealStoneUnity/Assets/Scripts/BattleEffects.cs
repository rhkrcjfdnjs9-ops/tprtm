using System.Collections;
using UnityEngine;

namespace RealStone
{
    public sealed class BattleEffects : MonoBehaviour
    {
        private Camera battleCamera;
        private bool shaking;
        public void Configure(Camera camera) => battleCamera = camera;

        public IEnumerator HitStop(float seconds)
        {
            if (seconds <= 0f) yield break;
            var previousScale = Time.timeScale;
            Time.timeScale = 0.04f;
            yield return new WaitForSecondsRealtime(seconds);
            Time.timeScale = previousScale;
        }

        public void Shake(float duration, float strength)
        {
            if (!shaking) StartCoroutine(ShakeRoutine(duration, strength));
        }

        private IEnumerator ShakeRoutine(float duration, float strength)
        {
            if (battleCamera == null) yield break;
            shaking = true;
            var origin = battleCamera.transform.position;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var fade = 1f - elapsed / duration;
                battleCamera.transform.position = origin + (Vector3)(Random.insideUnitCircle * strength * fade);
                yield return null;
            }
            battleCamera.transform.position = origin;
            shaking = false;
        }
    }
}
