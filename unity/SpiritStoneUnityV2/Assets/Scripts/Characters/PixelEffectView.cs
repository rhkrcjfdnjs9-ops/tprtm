using UnityEngine;

namespace SpiritStone.Characters
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Animator))]
    public sealed class PixelEffectView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;

        public SpriteRenderer Renderer => spriteRenderer;

        private void Awake()
        {
            CacheComponents();
            transform.localScale = Vector3.one;
        }

        public void Configure(Sprite sprite, int sortingOrder)
        {
            CacheComponents();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;
            transform.localScale = Vector3.one;
        }

        public void SetSprite(Sprite sprite)
        {
            spriteRenderer.sprite = sprite;
            transform.localScale = Vector3.one;
        }

        private void CacheComponents()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (animator == null) animator = GetComponent<Animator>();
        }
    }
}
