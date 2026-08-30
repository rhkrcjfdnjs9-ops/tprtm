using System.Collections;
using System.Collections.Generic;
using SpiritStone.Characters;
using UnityEngine;

namespace SpiritStone.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PrototypeCombatVfxSystem : MonoBehaviour
    {
        private sealed class EffectView
        {
            public GameObject GameObject;
            public Transform Transform;
            public SpriteRenderer Renderer;
            public PixelEffectView PixelView;
        }

        private const int InitialPoolSize = 24;
        private readonly Queue<EffectView> available = new();
        private readonly HashSet<EffectView> active = new();
        private Sprite sprite;
        private Sprite[] effectFrames;
        private Texture2D effectTexture;
        private Coroutine cameraShakeRoutine;

        public int ActiveEffectCount => active.Count;
        public int TotalEffectCount => active.Count + available.Count;

        public void Initialize(Sprite effectSprite)
        {
            CreatePixelEffectFrames();
            sprite = effectFrames[1];
            while (TotalEffectCount < InitialPoolSize) available.Enqueue(CreateView());
        }

        public IEnumerator PlayProjectile(Transform origin, Transform target, SpiritElement element,
            Vector3 scale, float duration)
        {
            if (origin == null || target == null) yield break;
            EffectView view = Acquire($"{element}_Projectile");
            Color color = GetElementColor(element);
            view.Renderer.color = color;
            view.Renderer.sortingOrder = 8;
            view.PixelView.SetSprite(GetFrame(scale.x >= 0.8f ? 2 : 1));
            view.Transform.localScale = Vector3.one;
            Vector3 start = origin.position + Vector3.right * 0.4f;
            Vector3 destination = target.position + Vector3.up * 0.25f;
            float safeDuration = Mathf.Max(0.05f, duration);
            float elapsed = 0f;
            while (elapsed < safeDuration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                Vector3 position = Vector3.Lerp(start, destination, Mathf.SmoothStep(0f, 1f, t));
                position.y += Mathf.Sin(t * Mathf.PI) * GetArcHeight(element);
                view.Transform.position = SnapToPixelGrid(position);
                view.Transform.rotation = Quaternion.Euler(0f, 0f, SnapRotation(GetRotation(element, t)));
                yield return null;
            }
            Release(view);
            PlayImpact(target, element, false);
        }

        public void PlayImpact(Transform target, SpiritElement element, bool strong)
        {
            if (target == null) return;
            StartCoroutine(ImpactRoutine(target.position + Vector3.up * 0.2f, element, strong));
            if (strong) ShakeCamera(0.075f, 0.13f);
        }

        public void PlayStatus(Transform target, PrototypeCombatStatusType type)
        {
            if (target == null) return;
            StartCoroutine(StatusRoutine(target, type));
        }

        public void Flash(SpriteRenderer renderer, Color flashColor)
        {
            if (renderer != null) StartCoroutine(FlashRoutine(renderer, flashColor));
        }

        public void ShakeCamera(float intensity, float duration)
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            if (cameraShakeRoutine != null) StopCoroutine(cameraShakeRoutine);
            cameraShakeRoutine = StartCoroutine(ShakeRoutine(camera.transform, intensity, duration));
        }

        private IEnumerator ImpactRoutine(Vector3 position, SpiritElement element, bool strong)
        {
            EffectView view = Acquire($"{element}_Impact");
            view.Transform.position = position;
            view.Transform.rotation = Quaternion.identity;
            view.Renderer.sortingOrder = 9;
            Color baseColor = Color.Lerp(GetElementColor(element), Color.white, 0.25f);
            float duration = strong ? 0.2f : 0.14f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                int frameIndex = t < 0.34f ? 0 : t < 0.68f ? (strong ? 2 : 1) : 3;
                view.PixelView.SetSprite(GetFrame(frameIndex));
                view.Transform.rotation = Quaternion.Euler(0f, 0f, SnapRotation(t * (element == SpiritElement.Wind ? 225f : 90f)));
                view.Renderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t);
                yield return null;
            }
            Release(view);
        }

        private IEnumerator StatusRoutine(Transform target, PrototypeCombatStatusType type)
        {
            EffectView view = Acquire($"{type}_Status");
            view.Renderer.sortingOrder = 7;
            Color color = GetStatusColor(type);
            float duration = type == PrototypeCombatStatusType.Stun ? 0.65f : 0.4f;
            float elapsed = 0f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                view.Transform.position = SnapToPixelGrid(target.position + Vector3.up * (0.65f + Mathf.Sin(t * Mathf.PI * 4f) * 0.08f));
                view.PixelView.SetSprite(GetFrame(t < 0.5f ? 1 : 2));
                view.Transform.rotation = Quaternion.Euler(0f, 0f, SnapRotation(t * 225f));
                view.Renderer.color = new Color(color.r, color.g, color.b, 1f - t);
                yield return null;
            }
            Release(view);
        }

        private static IEnumerator FlashRoutine(SpriteRenderer renderer, Color flashColor)
        {
            Color original = renderer.color;
            renderer.color = Color.Lerp(original, flashColor, 0.72f);
            yield return new WaitForSeconds(0.07f);
            if (renderer != null) renderer.color = original;
        }

        private IEnumerator ShakeRoutine(Transform cameraTransform, float intensity, float duration)
        {
            Vector3 origin = cameraTransform.localPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damping = 1f - Mathf.Clamp01(elapsed / duration);
                cameraTransform.localPosition = origin + (Vector3)Random.insideUnitCircle * intensity * damping;
                yield return null;
            }
            cameraTransform.localPosition = origin;
            cameraShakeRoutine = null;
        }

        private EffectView Acquire(string effectName)
        {
            EffectView view = available.Count > 0 ? available.Dequeue() : CreateView();
            view.GameObject.name = effectName;
            view.GameObject.SetActive(true);
            view.Transform.localScale = Vector3.one;
            active.Add(view);
            return view;
        }

        private void Release(EffectView view)
        {
            if (view == null || !active.Remove(view)) return;
            view.GameObject.SetActive(false);
            view.Transform.SetParent(transform, false);
            available.Enqueue(view);
        }

        private EffectView CreateView()
        {
            GameObject effect = new("PooledCombatEffect");
            effect.transform.SetParent(transform, false);
            SpriteRenderer renderer = effect.AddComponent<SpriteRenderer>();
            effect.AddComponent<Animator>();
            PixelEffectView pixelView = effect.AddComponent<PixelEffectView>();
            pixelView.Configure(sprite, 8);
            effect.SetActive(false);
            return new EffectView { GameObject = effect, Transform = effect.transform, Renderer = renderer, PixelView = pixelView };
        }

        private void CreatePixelEffectFrames()
        {
            const int canvasSize = 64;
            effectTexture = new Texture2D(canvasSize * 4, canvasSize, TextureFormat.RGBA32, false)
            {
                name = "PixelEffectFrames_64",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[effectTexture.width * effectTexture.height];
            for (int index = 0; index < pixels.Length; index++) pixels[index] = Color.clear;
            int[] radii = { 5, 9, 14, 19 };
            for (int frame = 0; frame < radii.Length; frame++)
            {
                int centerX = frame * canvasSize + canvasSize / 2;
                int centerY = canvasSize / 2;
                int radius = radii[frame];
                for (int y = -radius; y <= radius; y++)
                    for (int x = -radius; x <= radius; x++)
                        if (Mathf.Abs(x) + Mathf.Abs(y) <= radius)
                            pixels[(centerY + y) * effectTexture.width + centerX + x] = Color.white;
            }
            effectTexture.SetPixels(pixels);
            effectTexture.Apply(false, true);
            effectFrames = new Sprite[4];
            for (int frame = 0; frame < effectFrames.Length; frame++)
            {
                effectFrames[frame] = Sprite.Create(effectTexture,
                    new Rect(frame * canvasSize, 0f, canvasSize, canvasSize), new Vector2(0.5f, 0.5f), canvasSize,
                    0, SpriteMeshType.FullRect);
                effectFrames[frame].name = $"PixelEffect_64_{frame}";
            }
        }

        private Sprite GetFrame(int index) => effectFrames[Mathf.Clamp(index, 0, effectFrames.Length - 1)];

        private static Vector3 SnapToPixelGrid(Vector3 position)
        {
            const float pixelsPerUnit = 64f;
            position.x = Mathf.Round(position.x * pixelsPerUnit) / pixelsPerUnit;
            position.y = Mathf.Round(position.y * pixelsPerUnit) / pixelsPerUnit;
            return position;
        }

        private static float SnapRotation(float angle) => Mathf.Round(angle / 45f) * 45f;

        private void OnDestroy()
        {
            if (effectFrames != null)
                foreach (Sprite frame in effectFrames)
                    if (frame != null) Destroy(frame);
            if (effectTexture != null) Destroy(effectTexture);
        }

        private static float GetArcHeight(SpiritElement element) => element switch
        {
            SpiritElement.Wind => 0.35f,
            SpiritElement.Water => 0.18f,
            SpiritElement.Fire => 0.12f,
            _ => 0.04f
        };

        private static float GetRotation(SpiritElement element, float t) => element switch
        {
            SpiritElement.Wind => t * 480f,
            SpiritElement.Lightning => Mathf.Sin(t * Mathf.PI * 8f) * 18f,
            _ => 0f
        };

        private static Color GetStatusColor(PrototypeCombatStatusType type) => type switch
        {
            PrototypeCombatStatusType.Burn => new Color(1f, 0.22f, 0.04f),
            PrototypeCombatStatusType.Stun => new Color(0.75f, 0.35f, 1f),
            PrototypeCombatStatusType.AttackPower => new Color(1f, 0.35f, 0.2f),
            PrototypeCombatStatusType.AttackSpeed => new Color(0.25f, 1f, 0.75f),
            PrototypeCombatStatusType.Defense => new Color(0.25f, 0.7f, 1f),
            _ => Color.white
        };

        private static Color GetElementColor(SpiritElement element) => element switch
        {
            SpiritElement.Water => new Color(0.2f, 0.65f, 1f),
            SpiritElement.Fire => new Color(1f, 0.25f, 0.08f),
            SpiritElement.Wind => new Color(0.3f, 1f, 0.58f),
            SpiritElement.Lightning => new Color(0.72f, 0.3f, 1f),
            SpiritElement.Light => new Color(1f, 0.92f, 0.55f),
            SpiritElement.Dark => new Color(0.42f, 0.18f, 0.62f),
            _ => Color.white
        };
    }
}
