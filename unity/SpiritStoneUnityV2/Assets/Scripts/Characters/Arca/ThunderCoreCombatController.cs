using UnityEngine;

namespace SpiritStone.Characters.Arca
{
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    public sealed class ThunderCoreCombatController : MonoBehaviour
    {
        private enum CoreAction
        {
            Idle,
            BasicAttack,
            SkillOne,
            Overcharge,
            Ultimate,
            Hit,
            Death
        }

        private static readonly Vector3[] IdleOffsets =
        {
            new(-0.55f, 0.48f, 0f),
            new(0f, 1.08f, 0f),
            new(0.55f, 0.48f, 0f)
        };

        private static readonly Vector3[] SkillOneTriangleOffsets =
        {
            new(-0.42f, 0.42f, 0f),
            new(0.08f, 1.12f, 0f),
            new(0.58f, 0.42f, 0f)
        };

        [SerializeField, Min(1f)] private float positionResponse = 16f;
        [SerializeField, Min(0f)] private float movementTrail = 0.12f;

        private Transform owner;
        private Transform[] coreTransforms;
        private SpriteRenderer[] coreRenderers;
        private ThunderCoreSpriteAnimator[] coreAnimators;
        private CoreAction currentAction;
        private float actionStartedAt;
        private float actionDuration;
        private Vector3 previousOwnerPosition;
        private Vector3 movementOffset;

        public void Configure(Transform targetOwner, Transform[] transforms, SpriteRenderer[] renderers)
        {
            owner = targetOwner;
            coreTransforms = transforms;
            coreRenderers = renderers;
            coreAnimators = new ThunderCoreSpriteAnimator[coreTransforms.Length];
            for (int index = 0; index < coreTransforms.Length; index++)
                coreAnimators[index] = coreTransforms[index].GetComponent<ThunderCoreSpriteAnimator>();

            previousOwnerPosition = owner != null ? owner.position : Vector3.zero;
            currentAction = CoreAction.Idle;
            SnapToFormation();
        }

        public void PlayBasicAttack(float duration) => BeginAction(CoreAction.BasicAttack, duration, 1.7f);
        public void PlaySkillOne(float duration) => BeginAction(CoreAction.SkillOne, duration, 2.1f);
        public void PlayOvercharge(float duration) => BeginAction(CoreAction.Overcharge, duration, 2.7f);
        public void PlayUltimate(float duration) => BeginAction(CoreAction.Ultimate, duration, 3.2f);
        public void PlayHit(float duration) => BeginAction(CoreAction.Hit, duration, 0.45f);

        public void PlayDeath()
        {
            currentAction = CoreAction.Death;
            SetAnimationSpeed(0f);
            if (coreRenderers == null) return;
            foreach (SpriteRenderer coreRenderer in coreRenderers)
                if (coreRenderer != null) coreRenderer.color = new Color(0.32f, 0.26f, 0.42f, 0.45f);
        }

        public void ReturnToIdle()
        {
            currentAction = CoreAction.Idle;
            SetAnimationSpeed(1f);
            if (coreRenderers == null) return;
            foreach (SpriteRenderer coreRenderer in coreRenderers)
                if (coreRenderer != null) coreRenderer.color = Color.white;
        }

        private void LateUpdate()
        {
            if (owner == null || coreTransforms == null || coreTransforms.Length != IdleOffsets.Length) return;

            UpdateMovementTrail();
            if (currentAction != CoreAction.Idle && currentAction != CoreAction.Death
                && Time.time >= actionStartedAt + actionDuration)
                ReturnToIdle();

            float progress = actionDuration > 0f
                ? Mathf.Clamp01((Time.time - actionStartedAt) / actionDuration)
                : 0f;
            float blend = 1f - Mathf.Exp(-positionResponse * Time.deltaTime);
            for (int index = 0; index < coreTransforms.Length; index++)
            {
                Vector3 targetPosition = owner.position + GetActionOffset(index, progress) + movementOffset;
                coreTransforms[index].position = Vector3.Lerp(coreTransforms[index].position, targetPosition, blend);
            }
        }

        private void BeginAction(CoreAction action, float duration, float speedMultiplier)
        {
            if (currentAction == CoreAction.Death) return;
            currentAction = action;
            actionStartedAt = Time.time;
            actionDuration = Mathf.Max(0.05f, duration);
            SetAnimationSpeed(speedMultiplier);
        }

        private Vector3 GetActionOffset(int index, float progress)
        {
            float pulse = Mathf.Sin(progress * Mathf.PI);
            return currentAction switch
            {
                CoreAction.BasicAttack => IdleOffsets[index] + (index == 1 ? Vector3.right * 0.5f * pulse : Vector3.right * 0.12f * pulse),
                CoreAction.SkillOne => Vector3.Lerp(IdleOffsets[index], SkillOneTriangleOffsets[index], pulse),
                CoreAction.Overcharge => IdleOffsets[index] * (1f + 0.18f * pulse),
                CoreAction.Ultimate => Vector3.Lerp(IdleOffsets[index], new Vector3(0.62f, 0.35f + index * 0.4f, 0f), pulse),
                CoreAction.Hit => IdleOffsets[index] + Vector3.left * 0.14f * pulse,
                CoreAction.Death => IdleOffsets[index] + Vector3.down * 0.22f,
                _ => IdleOffsets[index]
            };
        }

        private void UpdateMovementTrail()
        {
            Vector3 velocity = (owner.position - previousOwnerPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            previousOwnerPosition = owner.position;
            Vector3 targetTrail = Vector3.ClampMagnitude(-velocity * movementTrail, 0.18f);
            movementOffset = Vector3.Lerp(movementOffset, targetTrail, 1f - Mathf.Exp(-9f * Time.deltaTime));
        }

        private void SetAnimationSpeed(float multiplier)
        {
            if (coreAnimators == null) return;
            foreach (ThunderCoreSpriteAnimator coreAnimator in coreAnimators)
                coreAnimator?.SetSpeedMultiplier(multiplier);
        }

        private void SnapToFormation()
        {
            if (owner == null || coreTransforms == null) return;
            for (int index = 0; index < coreTransforms.Length && index < IdleOffsets.Length; index++)
                coreTransforms[index].position = owner.position + IdleOffsets[index];
        }
    }
}
