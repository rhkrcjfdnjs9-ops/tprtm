using UnityEngine;

namespace SpiritStone.Characters.Arca
{
    [DisallowMultipleComponent]
    public sealed class ThunderCoreSpriteAnimator : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float framesPerSecond = 8f;

        private SpriteRenderer spriteRenderer;
        private Sprite[] frames;
        private int startFrame;
        private float speedMultiplier = 1f;

        public void Configure(SpriteRenderer targetRenderer, Sprite[] animationFrames, int initialFrame)
        {
            spriteRenderer = targetRenderer;
            frames = animationFrames;
            startFrame = frames == null || frames.Length == 0
                ? 0
                : Mathf.Abs(initialFrame) % frames.Length;

            if (spriteRenderer != null && frames != null && frames.Length > 0)
                spriteRenderer.sprite = frames[startFrame];
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Max(0f, multiplier);
        }

        private void Update()
        {
            if (spriteRenderer == null || frames == null || frames.Length == 0) return;

            int elapsedFrames = Mathf.FloorToInt(Time.time * framesPerSecond * speedMultiplier);
            spriteRenderer.sprite = frames[(startFrame + elapsedFrames) % frames.Length];
        }
    }
}
