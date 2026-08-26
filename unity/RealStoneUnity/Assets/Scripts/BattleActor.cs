using System.Collections;
using UnityEngine;

namespace RealStone
{
    public sealed class BattleActor : MonoBehaviour
    {
        private SpriteSequencePlayer sequence;
        private Sprite[] idle;
        private Sprite[] run;
        private Sprite[] attack;
        private Sprite[] hit;
        private Sprite[] death;
        private SpriteRenderer spriteRenderer;
        private ActorVisualMotion visualMotion;
        private ActorSecondaryLayers secondaryLayers;
        private int attackImpactFrame;
        private Animator animator;
        private bool usesAnimator;

        public int MaxHp { get; private set; }
        public int Hp { get; private set; }
        public bool IsDead => Hp <= 0;

        public void Configure(Sprite[] idleFrames, Sprite[] runFrames, Sprite[] attackFrames,
            Sprite[] hitFrames, Sprite[] deathFrames, int maxHp)
        {
            sequence = GetComponentInChildren<SpriteSequencePlayer>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            visualMotion = GetComponentInChildren<ActorVisualMotion>();
            secondaryLayers = GetComponent<ActorSecondaryLayers>();
            animator = GetComponent<Animator>();
            usesAnimator = animator != null && animator.runtimeAnimatorController != null;
            if (usesAnimator) visualMotion.enabled = false;
            idle = idleFrames;
            run = runFrames;
            attack = attackFrames;
            hit = hitFrames;
            death = deathFrames;
            attackImpactFrame = Mathf.Clamp(Mathf.RoundToInt((attack.Length - 1) * 0.58f), 0, Mathf.Max(0, attack.Length - 1));
            SetMaxHealth(maxHp);
            PlayIdle();
        }

        public void ResetHealth()
        {
            Hp = MaxHp;
            gameObject.SetActive(true);
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            secondaryLayers.SetMoving(false);
            PlayIdle();
        }

        public void SetMaxHealth(int value)
        {
            MaxHp = Mathf.Max(1, value);
            Hp = MaxHp;
        }

        public void TakeDamage(int amount) => Hp = Mathf.Max(0, Hp - amount);
        public void PlayIdle()
        {
            sequence.Play(idle, 8f, true);
            PlayMotionState("Idle", 0.08f);
            secondaryLayers.SetMoving(false);
        }

        public void PlayRun()
        {
            sequence.Play(run, 12f, true);
            PlayMotionState("Run", 0.08f);
            secondaryLayers.SetMoving(true);
        }

        public IEnumerator PlayAttackToImpact()
        {
            secondaryLayers.SetMoving(false);
            PlayMotionState("Attack", 0.035f);
            if (!usesAnimator) StartCoroutine(visualMotion.PlayAttack(attack.Length / 14f));
            yield return sequence.PlayRangeAndWait(attack, 0, attackImpactFrame, 14f);
        }

        public IEnumerator PlayAttackRecovery()
        {
            if (attackImpactFrame + 1 < attack.Length)
                yield return sequence.PlayRangeAndWait(attack, attackImpactFrame + 1, attack.Length - 1, 14f);
        }

        public IEnumerator PlayHit()
        {
            secondaryLayers.SetMoving(false);
            PlayMotionState("Hit", 0.025f);
            if (!usesAnimator) StartCoroutine(visualMotion.PlayHit(hit.Length / 12f));
            yield return sequence.PlayAndWait(hit, 12f);
        }

        public IEnumerator PlayDeath()
        {
            secondaryLayers.SetMoving(false);
            PlayMotionState("Death", 0.025f);
            if (!usesAnimator)
            {
                visualMotion.SetLoop(ActorVisualMotion.LoopMotion.None);
                StartCoroutine(visualMotion.PlayDeath(death.Length / 9f));
            }
            yield return sequence.PlayAndWait(death, 9f);
        }

        public void PulseAttackAura(bool heavy) => secondaryLayers.PulseAttack(heavy);

        private void PlayMotionState(string stateName, float transition)
        {
            if (usesAnimator) animator.CrossFadeInFixedTime(stateName, transition, 0, 0f);
            else if (stateName == "Idle") visualMotion.SetLoop(ActorVisualMotion.LoopMotion.Idle);
            else if (stateName == "Run") visualMotion.SetLoop(ActorVisualMotion.LoopMotion.Run);
        }

        public IEnumerator AttackStep(Vector3 direction, float distance, float seconds)
        {
            var origin = transform.position;
            var peak = origin + direction.normalized * distance;
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / seconds);
                var curve = Mathf.Sin(t * Mathf.PI);
                transform.position = Vector3.Lerp(origin, peak, curve);
                yield return null;
            }
            transform.position = origin;
        }

        public IEnumerator ReactToHit(Vector2 direction, float distance = 0.18f)
        {
            var origin = transform.position;
            var target = origin + (Vector3)(direction.normalized * distance);
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.55f, 0.55f);
            var elapsed = 0f;
            const float duration = 0.12f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(target, origin, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            transform.position = origin;
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
        }

        public IEnumerator MoveTo(Vector3 target, float seconds)
        {
            var start = transform.position;
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / seconds));
                yield return null;
            }
            transform.position = target;
        }
    }
}
