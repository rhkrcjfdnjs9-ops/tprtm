using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritStone.Characters.Arca
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class ThunderCoreFormation : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float orbitAmplitudeX = 0.055f;
        [SerializeField, Min(0f)] private float orbitAmplitudeY = 0.085f;
        [SerializeField, Min(0f)] private float orbitFrequency = 0.75f;
        [SerializeField] private float rotationSpeed = 24f;
        [SerializeField, Min(0f)] private float movementLagStrength = 0.14f;
        [SerializeField, Min(0f)] private float maximumMovementLag = 0.2f;
        [SerializeField, Min(0.1f)] private float lagResponse = 7f;

        private readonly List<CoreState> cores = new();
        private Vector3 previousWorldPosition;
        private Vector3 smoothedLocalLag;

        private void OnEnable()
        {
            CacheCores();
            previousWorldPosition = transform.position;
            smoothedLocalLag = Vector3.zero;
        }

        private void LateUpdate()
        {
            float time = Time.time * orbitFrequency * Mathf.PI * 2f;
            float safeDeltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 worldVelocity = (transform.position - previousWorldPosition) / safeDeltaTime;
            previousWorldPosition = transform.position;
            Vector3 targetLocalLag = -transform.InverseTransformVector(worldVelocity) * movementLagStrength;
            targetLocalLag = Vector3.ClampMagnitude(targetLocalLag, maximumMovementLag);
            float lagBlend = 1f - Mathf.Exp(-lagResponse * Time.deltaTime);
            smoothedLocalLag = Vector3.Lerp(smoothedLocalLag, targetLocalLag, lagBlend);

            for (int index = 0; index < cores.Count; index++)
            {
                CoreState core = cores[index];
                if (core.Transform == null) continue;

                float phase = time + index * Mathf.PI * 2f / Mathf.Max(1, cores.Count);
                Vector3 offset = new(
                    Mathf.Cos(phase) * orbitAmplitudeX,
                    Mathf.Sin(phase) * orbitAmplitudeY,
                    0f);
                float lagMultiplier = 0.85f + index * 0.15f;
                core.Transform.localPosition = core.AnchorPosition + offset + smoothedLocalLag * lagMultiplier;
                core.Transform.localRotation = core.AnchorRotation * Quaternion.Euler(0f, 0f, rotationSpeed * Time.time);

                core.Transform.localScale = core.AnchorScale;
            }
        }

        private void OnDisable()
        {
            smoothedLocalLag = Vector3.zero;
            foreach (CoreState core in cores)
            {
                if (core.Transform == null) continue;
                core.Transform.localPosition = core.AnchorPosition;
                core.Transform.localRotation = core.AnchorRotation;
                core.Transform.localScale = core.AnchorScale;
            }
        }

        private void CacheCores()
        {
            cores.Clear();
            for (int index = 0; index < transform.childCount; index++)
            {
                Transform child = transform.GetChild(index);
                if (!child.name.StartsWith("ThunderCore_", StringComparison.Ordinal)) continue;
                cores.Add(new CoreState(child));
            }
        }

        private readonly struct CoreState
        {
            public CoreState(Transform transform)
            {
                Transform = transform;
                AnchorPosition = transform.localPosition;
                AnchorRotation = transform.localRotation;
                AnchorScale = transform.localScale;
            }

            public Transform Transform { get; }
            public Vector3 AnchorPosition { get; }
            public Quaternion AnchorRotation { get; }
            public Vector3 AnchorScale { get; }
        }
    }
}
