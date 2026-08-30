using UnityEngine;

namespace SpiritStone.Characters
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Animator))]
    public sealed class PixelEnemyView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        private Sprite normalSprite;
        private Sprite bossSprite;

        public SpriteRenderer Renderer => spriteRenderer;

        private void Awake()
        {
            CacheComponents();
            transform.localScale = Vector3.one;
        }

        public void Configure(Sprite normal, Sprite boss, int sortingOrder)
        {
            CacheComponents();
            normalSprite = normal;
            bossSprite = boss;
            spriteRenderer.sprite = normalSprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = sortingOrder;
            transform.localScale = Vector3.one;
        }

        public void SetBossAppearance(bool isBoss)
        {
            spriteRenderer.sprite = isBoss ? bossSprite : normalSprite;
            spriteRenderer.color = Color.white;
            transform.localScale = Vector3.one;
        }

        private void CacheComponents()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (animator == null) animator = GetComponent<Animator>();
        }
    }
}
