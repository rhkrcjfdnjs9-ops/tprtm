using System.Collections;
using UnityEngine;

namespace SpiritStone.Characters.Arca
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PixelCharacterView))]
    [RequireComponent(typeof(Animator))]
    public sealed class ArcaDeathPreviewLoop : MonoBehaviour
    {
        private const float RestartDelay = 1.4f;
        private const float DeathDuration = 0.9f;

        [SerializeField] private PixelCharacterView characterView;
        [SerializeField] private Animator animator;

        private void Awake()
        {
            if (characterView == null) characterView = GetComponent<PixelCharacterView>();
            if (animator == null) animator = GetComponent<Animator>();
        }

        private void Start()
        {
            StartCoroutine(RepeatDeath());
        }

        private IEnumerator RepeatDeath()
        {
            while (true)
            {
                animator.Play("Idle", 0, 0f);
                yield return null;
                characterView.PlayDeath();
                yield return new WaitForSeconds(DeathDuration + RestartDelay);
            }
        }
    }
}
