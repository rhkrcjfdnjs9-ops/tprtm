using UnityEngine;

namespace SpiritStone.Preview
{
    [DisallowMultipleComponent]
    public sealed class PreviewProtagonistPath : MonoBehaviour
    {
        [SerializeField] private Vector2 travelDistance = new(0.8f, 0.12f);
        [SerializeField, Min(0.05f)] private float cycleDuration = 4.5f;

        private Vector3 origin;

        private void OnEnable()
        {
            origin = transform.position;
        }

        private void Update()
        {
            float phase = Time.time / cycleDuration * Mathf.PI * 2f;
            transform.position = origin + new Vector3(
                Mathf.Sin(phase) * travelDistance.x,
                Mathf.Sin(phase * 2f) * travelDistance.y,
                0f);
        }

        private void OnDisable()
        {
            transform.position = origin;
        }
    }
}
