using UnityEngine;

namespace SpiritStone.Characters.Arca
{
    [DisallowMultipleComponent]
    public sealed class ArcaIdleFloat : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float verticalAmplitude = 0.045f;
        [SerializeField, Min(0f)] private float verticalFrequency = 1.15f;
        [SerializeField, Range(0f, 2f)] private float horizontalRatio = 0.18f;

        private Vector3 baseLocalPosition;

        private void OnEnable()
        {
            baseLocalPosition = transform.localPosition;
        }

        private void LateUpdate()
        {
            float phase = Time.time * verticalFrequency * Mathf.PI * 2f;
            float vertical = Mathf.Sin(phase) * verticalAmplitude;
            float horizontal = Mathf.Sin(phase * 0.5f) * verticalAmplitude * horizontalRatio;
            transform.localPosition = baseLocalPosition + new Vector3(horizontal, vertical, 0f);
        }

        private void OnDisable()
        {
            transform.localPosition = baseLocalPosition;
        }
    }
}
