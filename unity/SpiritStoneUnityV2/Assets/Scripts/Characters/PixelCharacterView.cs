using UnityEngine;

namespace SpiritStone.Characters
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Animator))]
    public sealed class PixelCharacterView : MonoBehaviour
    {
        private static readonly int AttackTrigger = Animator.StringToHash("Attack");
        private static readonly int SkillTrigger = Animator.StringToHash("Skill");
        private static readonly int SkillTwoTrigger = Animator.StringToHash("SkillTwo");
        private static readonly int UltimateTrigger = Animator.StringToHash("Ultimate");
        private static readonly int HitTrigger = Animator.StringToHash("Hit");
        private static readonly int DeathTrigger = Animator.StringToHash("Death");
        private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
        private static readonly int IdleState = Animator.StringToHash("Idle");

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        private bool hasMovingParameter;
        private Vector3 previousPosition;

        public SpriteRenderer Renderer => spriteRenderer;

        private void Awake()
        {
            CacheComponents();
            transform.localScale = Vector3.one;
            previousPosition = transform.position;
        }

        private void OnEnable()
        {
            previousPosition = transform.position;
        }

        private void LateUpdate()
        {
            Vector3 currentPosition = transform.position;
            if (hasMovingParameter && animator != null)
                animator.SetBool(IsMovingParameter, (currentPosition - previousPosition).sqrMagnitude > 0.000001f);
            previousPosition = currentPosition;
        }

        public void Configure(Sprite sprite, int sortingOrder)
        {
            CacheComponents();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.enabled = true;
            transform.localScale = Vector3.one;
        }

        public void PlayAttack()
        {
            SetTriggerIfPresent(AttackTrigger);
        }

        public void SetSprite(Sprite sprite)
        {
            CacheComponents();
            if (sprite != null) spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
            transform.localScale = Vector3.one;
        }

        public void SetAnimatorController(RuntimeAnimatorController controller)
        {
            CacheComponents();
            animator.runtimeAnimatorController = controller;
            hasMovingParameter = false;
            if (controller != null)
            {
                foreach (AnimatorControllerParameter parameter in animator.parameters)
                {
                    if (parameter.nameHash != IsMovingParameter || parameter.type != AnimatorControllerParameterType.Bool) continue;
                    hasMovingParameter = true;
                    break;
                }
            }
            previousPosition = transform.position;
        }

        public void PlaySkill()
        {
            SetTriggerIfPresent(SkillTrigger);
        }

        public void PlaySkillTwo()
        {
            SetTriggerIfPresent(SkillTwoTrigger);
        }

        public void PlayUltimate()
        {
            SetTriggerIfPresent(UltimateTrigger);
        }

        public void PlayHit()
        {
            SetTriggerIfPresent(HitTrigger);
        }

        public void PlayDeath()
        {
            SetTriggerIfPresent(DeathTrigger);
        }

        public void ResetToIdle()
        {
            CacheComponents();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.ResetTrigger(AttackTrigger);
                animator.ResetTrigger(SkillTrigger);
                animator.ResetTrigger(SkillTwoTrigger);
                animator.ResetTrigger(UltimateTrigger);
                animator.ResetTrigger(HitTrigger);
                animator.ResetTrigger(DeathTrigger);
                if (hasMovingParameter) animator.SetBool(IsMovingParameter, false);
                animator.Play(IdleState, 0, 0f);
                animator.Update(0f);
            }
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            previousPosition = transform.position;
        }

        private void CacheComponents()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (animator == null) animator = GetComponent<Animator>();
        }

        private void SetTriggerIfPresent(int triggerHash)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash != triggerHash || parameter.type != AnimatorControllerParameterType.Trigger) continue;
                animator.SetTrigger(triggerHash);
                return;
            }
        }
    }
}
