using System.Collections;
using UnityEngine;

namespace RealStone
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteSequencePlayer : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Coroutine playback;

        public bool IsPlaying { get; private set; }

        private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        public void Show(Sprite sprite)
        {
            if (sprite != null) spriteRenderer.sprite = sprite;
        }

        public Coroutine Play(Sprite[] frames, float fps, bool loop = false)
        {
            if (playback != null) StopCoroutine(playback);
            playback = StartCoroutine(PlayRoutine(frames, fps, loop));
            return playback;
        }

        public IEnumerator PlayAndWait(Sprite[] frames, float fps, bool loop = false)
        {
            if (playback != null) StopCoroutine(playback);
            yield return PlayRoutine(frames, fps, loop);
        }

        public IEnumerator PlayRangeAndWait(Sprite[] frames, int firstFrame, int lastFrame, float fps)
        {
            if (playback != null) StopCoroutine(playback);
            if (frames == null || frames.Length == 0) yield break;
            firstFrame = Mathf.Clamp(firstFrame, 0, frames.Length - 1);
            lastFrame = Mathf.Clamp(lastFrame, firstFrame, frames.Length - 1);
            IsPlaying = true;
            var wait = new WaitForSeconds(1f / Mathf.Max(1f, fps));
            for (var i = firstFrame; i <= lastFrame; i++)
            {
                Show(frames[i]);
                yield return wait;
            }
            IsPlaying = false;
            playback = null;
        }

        private IEnumerator PlayRoutine(Sprite[] frames, float fps, bool loop)
        {
            if (frames == null || frames.Length == 0) yield break;
            IsPlaying = true;
            var wait = new WaitForSeconds(1f / Mathf.Max(1f, fps));
            do
            {
                for (var i = 0; i < frames.Length; i++)
                {
                    Show(frames[i]);
                    yield return wait;
                }
            } while (loop);
            IsPlaying = false;
            playback = null;
        }
    }
}
