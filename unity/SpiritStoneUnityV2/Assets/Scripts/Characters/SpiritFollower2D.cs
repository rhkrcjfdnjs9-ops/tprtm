using UnityEngine;

namespace SpiritStone.Characters
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class SpiritFollower2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private string fallbackTargetName = "ProtagonistAnchor";
        [SerializeField] private Vector2 followOffset = new(-1.5f, 0.2f);
        [SerializeField, Min(0.01f)] private float smoothTime = 0.28f;
        [SerializeField, Min(0.01f)] private float catchUpSmoothTime = 0.12f;
        [SerializeField, Min(0f)] private float maxSpeed = 5f;
        [SerializeField, Min(0f)] private float catchUpDistance = 1.8f;
        [SerializeField, Min(0f)] private float emergencyReturnDistance = 5f;
        [SerializeField, Min(0f)] private float settleDistance = 0.005f;
        [SerializeField, Range(0f, 8f)] private float maximumLeanAngle = 2.5f;
        [SerializeField, Min(0.1f)] private float leanResponse = 8f;
        [SerializeField] private bool keepInsideCamera = true;
        [SerializeField, Range(0f, 0.25f)] private float viewportPadding = 0.04f;

        private Vector3 velocity;
        private Transform visualBody;
        private Quaternion visualBaseRotation;
        private SpriteRenderer[] renderers;
        private Camera cachedCamera;

        private void OnEnable()
        {
            ResolveTarget();
            velocity = Vector3.zero;
            visualBody = transform.Find("ArcaBody");
            if (visualBody != null) visualBaseRotation = visualBody.localRotation;
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            cachedCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                ResolveTarget();
                if (target == null) return;
            }

            Vector3 desired = target.position + (Vector3)followOffset;
            desired.z = transform.position.z;
            float distance = Vector2.Distance(transform.position, desired);

            if (emergencyReturnDistance > 0f && distance >= emergencyReturnDistance)
            {
                transform.position = desired;
                velocity = Vector3.zero;
                ClampInsideCamera();
                UpdateMovementLean();
                return;
            }

            float activeSmoothTime = distance >= catchUpDistance ? catchUpSmoothTime : smoothTime;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desired,
                ref velocity,
                activeSmoothTime,
                maxSpeed,
                Time.deltaTime);

            if ((transform.position - desired).sqrMagnitude <= settleDistance * settleDistance)
            {
                transform.position = desired;
                velocity = Vector3.zero;
            }

            ClampInsideCamera();
            UpdateMovementLean();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            velocity = Vector3.zero;
        }

        private void ResolveTarget()
        {
            if (target != null || string.IsNullOrWhiteSpace(fallbackTargetName)) return;
            GameObject candidate = GameObject.Find(fallbackTargetName);
            if (candidate != null) target = candidate.transform;
        }

        private void UpdateMovementLean()
        {
            if (visualBody == null) return;
            float targetAngle = Mathf.Clamp(-velocity.x * maximumLeanAngle, -maximumLeanAngle, maximumLeanAngle);
            Quaternion targetRotation = visualBaseRotation * Quaternion.Euler(0f, 0f, targetAngle);
            float blend = 1f - Mathf.Exp(-leanResponse * Time.deltaTime);
            visualBody.localRotation = Quaternion.Slerp(visualBody.localRotation, targetRotation, blend);
        }

        private void ClampInsideCamera()
        {
            if (!keepInsideCamera) return;
            if (cachedCamera == null) cachedCamera = Camera.main;
            if (cachedCamera == null || !cachedCamera.orthographic || renderers == null || renderers.Length == 0) return;

            bool hasBounds = false;
            Bounds combinedBounds = default;
            foreach (SpriteRenderer spriteRenderer in renderers)
            {
                if (spriteRenderer == null || !spriteRenderer.enabled) continue;
                if (!hasBounds)
                {
                    combinedBounds = spriteRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(spriteRenderer.bounds);
                }
            }
            if (!hasBounds) return;

            float halfHeight = cachedCamera.orthographicSize;
            float halfWidth = halfHeight * cachedCamera.aspect;
            Vector3 cameraPosition = cachedCamera.transform.position;
            float paddingX = halfWidth * viewportPadding;
            float paddingY = halfHeight * viewportPadding;
            float left = cameraPosition.x - halfWidth + paddingX;
            float right = cameraPosition.x + halfWidth - paddingX;
            float bottom = cameraPosition.y - halfHeight + paddingY;
            float top = cameraPosition.y + halfHeight - paddingY;

            Vector3 correction = Vector3.zero;
            float availableWidth = right - left;
            float availableHeight = top - bottom;
            correction.x = combinedBounds.size.x > availableWidth
                ? cameraPosition.x - combinedBounds.center.x
                : Mathf.Max(left - combinedBounds.min.x, Mathf.Min(0f, right - combinedBounds.max.x));
            correction.y = combinedBounds.size.y > availableHeight
                ? cameraPosition.y - combinedBounds.center.y
                : Mathf.Max(bottom - combinedBounds.min.y, Mathf.Min(0f, top - combinedBounds.max.y));

            if (Mathf.Abs(correction.x) > 0.0001f) velocity.x = 0f;
            if (Mathf.Abs(correction.y) > 0.0001f) velocity.y = 0f;
            transform.position += correction;
        }

        private void OnDisable()
        {
            velocity = Vector3.zero;
            if (visualBody != null) visualBody.localRotation = visualBaseRotation;
        }
    }
}
