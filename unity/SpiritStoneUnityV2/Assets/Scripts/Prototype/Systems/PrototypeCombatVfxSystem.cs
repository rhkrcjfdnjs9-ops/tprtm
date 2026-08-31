using System.Collections;
using System.Collections.Generic;
using SpiritStone.Characters;
using UnityEngine;
using Action = System.Action;

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
        private Sprite lineSprite;
        private Sprite[] effectFrames;
        private Sprite[] arcaLaunchFlashFrames;
        private Sprite[] arcaProjectileFrames;
        private Sprite[] arcaImpactFrames;
        private Sprite[] arcaLightningOrbGatherFrames;
        private Sprite[] arcaLightningOrbProjectileFrames;
        private Sprite[] arcaLightningOrbImpactFrames;
        private Sprite[] arcaLightningLinkFrames;
        private Sprite arcaOverchargeSmallRing;
        private Sprite arcaOverchargeLargeRing;
        private Texture2D effectTexture;
        private Coroutine cameraShakeRoutine;

        public int ActiveEffectCount => active.Count;
        public int TotalEffectCount => active.Count + available.Count;

        public void Initialize(Sprite effectSprite)
        {
            CreatePixelEffectFrames();
            lineSprite = effectSprite;
            arcaLaunchFlashFrames = LoadSortedFrames("Characters/Arca/Effects/BasicAttackV3/LaunchFlashV3");
            arcaProjectileFrames = LoadSortedFrames("Characters/Arca/Effects/BasicAttackV3/ProjectileV3");
            arcaImpactFrames = LoadSortedFrames("Characters/Arca/Effects/BasicAttackV3/ImpactV3");
            arcaLightningOrbGatherFrames = LoadSortedFrames("Characters/Arca/Effects/LightningOrbV1/Gather");
            arcaLightningOrbProjectileFrames = LoadSortedFrames("Characters/Arca/Effects/LightningOrbV1/Projectile");
            arcaLightningOrbImpactFrames = LoadSortedFrames("Characters/Arca/Effects/LightningOrbV1/Impact");
            arcaLightningLinkFrames = LoadSortedFrames("Characters/Arca/Effects/LightningLinkV3");
            arcaOverchargeSmallRing = Resources.Load<Sprite>("Characters/Arca/Effects/OverchargeV1/Arca_Overcharge_Ring_Small_v2");
            arcaOverchargeLargeRing = Resources.Load<Sprite>("Characters/Arca/Effects/OverchargeV1/Arca_Overcharge_Ring_Large_v2");
            sprite = effectFrames[1];
            while (TotalEffectCount < InitialPoolSize) available.Enqueue(CreateView());
        }

        public IEnumerator PlayArcaBasicAttack(Transform origin, Transform target, float duration)
        {
            if (origin == null || target == null) yield break;
            if (!HasFrames(arcaLaunchFlashFrames) || !HasFrames(arcaProjectileFrames) || !HasFrames(arcaImpactFrames))
            {
                yield return PlayProjectile(origin, target, SpiritElement.Lightning, new Vector3(0.5f, 0.09f, 1f), duration);
                yield break;
            }

            StartCoroutine(PlayFrameSequence(origin.position, arcaLaunchFlashFrames, 0.04f, "Arca_LaunchFlash", 9));
            yield return new WaitForSeconds(0.08f);

            EffectView projectile = Acquire("Arca_LightningProjectile");
            projectile.Renderer.color = Color.white;
            projectile.Renderer.sortingOrder = 9;
            projectile.Transform.rotation = Quaternion.identity;
            Vector3 start = origin.position + Vector3.right * 0.35f;
            float safeDuration = Mathf.Max(0.05f, duration);
            float elapsed = 0f;
            while (elapsed < safeDuration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                int frameIndex = Mathf.Min(arcaProjectileFrames.Length - 1,
                    Mathf.FloorToInt(t * arcaProjectileFrames.Length));
                projectile.PixelView.SetSprite(arcaProjectileFrames[frameIndex]);
                Vector3 destination = target.position + Vector3.up * 0.25f;
                projectile.Transform.position = SnapToPixelGrid(Vector3.Lerp(start, destination, Mathf.SmoothStep(0f, 1f, t)));
                yield return null;
            }
            Vector3 impactPosition = target != null ? target.position + Vector3.up * 0.25f : projectile.Transform.position;
            Release(projectile);
            StartCoroutine(PlayFrameSequence(impactPosition, arcaImpactFrames, 0.045f, "Arca_LightningImpact", 10));
            yield return new WaitForSeconds(0.09f);
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

        public IEnumerator PlayArcaLightningOrb(IReadOnlyList<Transform> cores, Transform origin,
            IReadOnlyList<Transform> targets)
        {
            if (origin == null || targets == null || targets.Count == 0) yield break;
            Vector3 chargeCenter = origin.position + new Vector3(0.08f, 0.66f, 0f);
            if (cores != null && cores.Count > 0)
            {
                chargeCenter = Vector3.zero;
                int validCoreCount = 0;
                foreach (Transform core in cores)
                {
                    if (core == null) continue;
                    chargeCenter += core.position;
                    validCoreCount++;
                }
                if (validCoreCount > 0) chargeCenter /= validCoreCount;
            }

            if (!HasFrames(arcaLightningOrbGatherFrames) || !HasFrames(arcaLightningOrbProjectileFrames)
                || !HasFrames(arcaLightningOrbImpactFrames))
            {
                yield return PlayProjectile(origin, targets[0], SpiritElement.Lightning,
                    new Vector3(0.85f, 0.16f, 1f), 0.25f);
                yield break;
            }

            EffectView gather = Acquire("Arca_LightningOrb_Gather");
            gather.Renderer.sortingOrder = 11;
            gather.Renderer.color = Color.white;
            gather.Transform.rotation = Quaternion.identity;
            gather.Transform.position = SnapToPixelGrid(chargeCenter);
            for (int frame = 0; frame < arcaLightningOrbGatherFrames.Length; frame++)
            {
                gather.PixelView.SetSprite(arcaLightningOrbGatherFrames[frame]);
                gather.Transform.localScale = Vector3.one * 0.38f;
                yield return new WaitForSeconds(0.055f);
            }
            Release(gather);

            Vector3 targetCenter = Vector3.zero;
            int targetCount = 0;
            foreach (Transform target in targets)
            {
                if (target == null) continue;
                targetCenter += target.position + Vector3.up * 0.25f;
                targetCount++;
            }
            if (targetCount == 0) yield break;
            targetCenter /= targetCount;

            EffectView projectile = Acquire("Arca_LightningOrb_Projectile");
            projectile.Renderer.sortingOrder = 12;
            projectile.Renderer.color = Color.white;
            projectile.Transform.rotation = Quaternion.identity;
            const float projectileDuration = 0.28f;
            float elapsed = 0f;
            while (elapsed < projectileDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / projectileDuration);
                int frame = Mathf.Min(arcaLightningOrbProjectileFrames.Length - 1,
                    Mathf.FloorToInt(t * arcaLightningOrbProjectileFrames.Length));
                projectile.PixelView.SetSprite(arcaLightningOrbProjectileFrames[frame]);
                projectile.Transform.localScale = Vector3.one * 0.58f;
                projectile.Transform.position = SnapToPixelGrid(Vector3.Lerp(chargeCenter, targetCenter,
                    Mathf.SmoothStep(0f, 1f, t)));
                yield return null;
            }
            Release(projectile);

            EffectView impact = Acquire("Arca_LightningOrb_Impact");
            impact.Renderer.sortingOrder = 13;
            impact.Renderer.color = Color.white;
            impact.Transform.rotation = Quaternion.identity;
            impact.Transform.position = SnapToPixelGrid(targetCenter);
            for (int frame = 0; frame < arcaLightningOrbImpactFrames.Length; frame++)
            {
                impact.PixelView.SetSprite(arcaLightningOrbImpactFrames[frame]);
                impact.Transform.localScale = Vector3.one * 0.72f;
                yield return new WaitForSeconds(0.065f);
            }
            Release(impact);

            ShakeCamera(0.075f, 0.14f);
        }

        public void PlayArcaOvercharge(Transform origin, IReadOnlyList<Transform> cores)
        {
            if (origin == null) return;
            StartCoroutine(ArcaOverchargeRoutine(origin, cores));
        }

        private IEnumerator ArcaOverchargeRoutine(Transform origin, IReadOnlyList<Transform> cores)
        {
            if (arcaOverchargeSmallRing == null || arcaOverchargeLargeRing == null) yield break;

            const float ringCenterOffsetY = -0.12f;
            Vector3 ringScale = Vector3.one * 0.5f;

            EffectView ring = Acquire("Arca_OverchargeRing");
            ring.Renderer.sortingOrder = 7;
            ring.Renderer.color = Color.white;
            ring.Transform.rotation = Quaternion.identity;
            ring.Transform.position = SnapToPixelGrid(origin.position + Vector3.up * ringCenterOffsetY);
            ring.PixelView.SetSprite(arcaOverchargeSmallRing);
            ring.Transform.localScale = ringScale;
            yield return new WaitForSeconds(0.18f);

            if (origin == null)
            {
                Release(ring);
                yield break;
            }

            ring.Transform.position = SnapToPixelGrid(origin.position + Vector3.up * ringCenterOffsetY);
            ring.PixelView.SetSprite(arcaOverchargeLargeRing);
            ring.Transform.localScale = ringScale;
            var triangleEffects = new List<EffectView>();
            if (cores != null && cores.Count >= 3)
            {
                for (int index = 0; index < 3; index++)
                {
                    Transform start = cores[index];
                    Transform end = cores[(index + 1) % 3];
                    if (start != null && end != null)
                        CreateLightningPath(start.position, end.position, 410 + index, triangleEffects, 8);
                }
            }
            yield return new WaitForSeconds(0.32f);

            for (int fadeStep = 0; fadeStep < 3; fadeStep++)
            {
                if (origin != null)
                    ring.Transform.position = SnapToPixelGrid(origin.position +
                        Vector3.up * (ringCenterOffsetY + fadeStep * 0.04f));
                float alpha = 1f - (fadeStep + 1) / 3f;
                ring.Renderer.color = new Color(1f, 1f, 1f, alpha);
                yield return new WaitForSeconds(0.08f);
            }

            foreach (EffectView triangleEffect in triangleEffects) Release(triangleEffect);
            Release(ring);
        }

        private IEnumerator PlayLightningLinkFrames(Vector3 start, Vector3 end, int linkIndex)
        {
            if (!HasFrames(arcaLightningLinkFrames))
            {
                var fallbackEffects = new List<EffectView>();
                CreateLightningPath(start, end, 220 + linkIndex, fallbackEffects, 11);
                yield return new WaitForSeconds(0.07f);
                foreach (EffectView fallbackEffect in fallbackEffects) Release(fallbackEffect);
                yield break;
            }

            Vector3 direction = end - start;
            EffectView link = Acquire($"Arca_LightningLinkV3_{linkIndex + 1}");
            link.Renderer.sortingOrder = 12;
            link.Renderer.color = Color.white;
            link.Transform.position = SnapToPixelGrid(Vector3.Lerp(start, end, 0.5f));
            link.Transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            link.Transform.localScale = new Vector3(Mathf.Clamp(direction.magnitude / 1.75f, 0.55f, 1.55f), 1f, 1f);

            for (int frame = 0; frame < arcaLightningLinkFrames.Length; frame++)
            {
                link.PixelView.SetSprite(arcaLightningLinkFrames[frame]);
                link.Renderer.color = frame < 4
                    ? Color.white
                    : new Color(0.8f, 0.45f, 1f, frame == 4 ? 0.8f : 0.5f);
                yield return new WaitForSeconds(frame < 4 ? 0.03f : 0.045f);
            }
            Release(link);
        }

        private void CreateLightningPath(Vector3 start, Vector3 end, int seed,
            ICollection<EffectView> effects, int sortingOrder)
        {
            const int segmentCount = 6;
            Vector3 direction = end - start;
            Vector3 perpendicular = direction.sqrMagnitude > 0.0001f
                ? new Vector3(-direction.y, direction.x, 0f).normalized
                : Vector3.up;
            Vector3 previous = start;
            for (int segment = 1; segment <= segmentCount; segment++)
            {
                float t = segment / (float)segmentCount;
                Vector3 next = Vector3.Lerp(start, end, t);
                if (segment < segmentCount)
                {
                    float noise = Mathf.Sin((seed + segment * 7) * 1.93f) * 0.095f;
                    next += perpendicular * noise;
                }
                CreateLightningSegment(previous, next, new Color(0.22f, 0.03f, 0.34f), 0.105f,
                    sortingOrder, effects);
                CreateLightningSegment(previous, next, Color.white, 0.035f,
                    sortingOrder + 1, effects);
                previous = next;
            }
        }

        private void CreateLightningBurst(Vector3 center, int seed, ICollection<EffectView> effects, int rayCount)
        {
            for (int ray = 0; ray < rayCount; ray++)
            {
                float angle = ray * 360f / rayCount + Mathf.Sin(seed * 0.7f + ray) * 12f;
                float length = rayCount > 4 ? 0.42f : 0.27f;
                Vector3 end = center + Quaternion.Euler(0f, 0f, angle) * Vector3.right * length;
                CreateLightningPath(center, end, seed + ray * 13, effects, 12);
            }
        }

        private void CreateLightningSegment(Vector3 start, Vector3 end, Color color, float width,
            int sortingOrder, ICollection<EffectView> effects)
        {
            Vector3 direction = end - start;
            EffectView segment = Acquire("Arca_ChainLightningSegment");
            segment.PixelView.SetSprite(lineSprite != null ? lineSprite : GetFrame(0));
            segment.Renderer.color = color;
            segment.Renderer.sortingOrder = sortingOrder;
            segment.Transform.position = SnapToPixelGrid(Vector3.Lerp(start, end, 0.5f));
            segment.Transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            segment.Transform.localScale = new Vector3(direction.magnitude, width, 1f);
            effects.Add(segment);
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

        public void PlayRevive(Transform target, SpriteRenderer characterRenderer, SpiritElement element,
            Action onReveal = null)
        {
            if (target == null || characterRenderer == null) return;
            StartCoroutine(ReviveRoutine(target, characterRenderer, element, onReveal));
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

        private IEnumerator ReviveRoutine(Transform target, SpriteRenderer characterRenderer,
            SpiritElement element, Action onReveal)
        {
            characterRenderer.enabled = false;
            Color elementColor = GetElementColor(element);
            EffectView outerGlow = Acquire($"{element}_ReviveGlow");
            EffectView innerSpark = Acquire($"{element}_ReviveSpark");
            outerGlow.Renderer.sortingOrder = characterRenderer.sortingOrder + 1;
            innerSpark.Renderer.sortingOrder = characterRenderer.sortingOrder + 2;
            outerGlow.Transform.rotation = Quaternion.identity;
            innerSpark.Transform.rotation = Quaternion.identity;

            const float gatherDuration = 0.32f;
            float elapsed = 0f;
            while (elapsed < gatherDuration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / gatherDuration);
                Vector3 center = target.position + Vector3.up * 0.32f;
                outerGlow.Transform.position = SnapToPixelGrid(center);
                innerSpark.Transform.position = SnapToPixelGrid(center + Vector3.up * Mathf.Lerp(0.55f, 0f, t));
                outerGlow.PixelView.SetSprite(GetFrame(Mathf.Clamp(3 - Mathf.FloorToInt(t * 4f), 0, 3)));
                innerSpark.PixelView.SetSprite(GetFrame(t < 0.55f ? 1 : 0));
                outerGlow.Renderer.color = new Color(elementColor.r, elementColor.g, elementColor.b,
                    Mathf.Lerp(0.2f, 0.95f, t));
                innerSpark.Renderer.color = Color.Lerp(elementColor, Color.white, 0.75f);
                yield return null;
            }

            if (characterRenderer != null)
            {
                characterRenderer.enabled = true;
                onReveal?.Invoke();
                StartCoroutine(FlashRoutine(characterRenderer, Color.white));
            }

            const float burstDuration = 0.22f;
            elapsed = 0f;
            while (elapsed < burstDuration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / burstDuration);
                Vector3 center = target.position + Vector3.up * 0.32f;
                outerGlow.Transform.position = SnapToPixelGrid(center);
                innerSpark.Transform.position = SnapToPixelGrid(center);
                outerGlow.PixelView.SetSprite(GetFrame(Mathf.Min(3, Mathf.FloorToInt(t * 4f))));
                innerSpark.PixelView.SetSprite(GetFrame(t < 0.5f ? 2 : 1));
                outerGlow.Renderer.color = new Color(elementColor.r, elementColor.g, elementColor.b, 1f - t);
                innerSpark.Renderer.color = new Color(1f, 1f, 1f, 1f - t);
                yield return null;
            }

            Release(innerSpark);
            Release(outerGlow);
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

        private IEnumerator PlayFrameSequence(Vector3 position, Sprite[] frames, float frameDuration,
            string effectName, int sortingOrder)
        {
            EffectView view = Acquire(effectName);
            view.Transform.position = SnapToPixelGrid(position);
            view.Transform.rotation = Quaternion.identity;
            view.Renderer.color = Color.white;
            view.Renderer.sortingOrder = sortingOrder;
            for (int index = 0; index < frames.Length; index++)
            {
                view.PixelView.SetSprite(frames[index]);
                yield return new WaitForSeconds(frameDuration);
            }
            Release(view);
        }

        private static Sprite[] LoadSortedFrames(string resourcePath)
        {
            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);
            System.Array.Sort(frames, (left, right) => string.CompareOrdinal(left.name, right.name));
            return frames;
        }

        private static bool HasFrames(Sprite[] frames) => frames != null && frames.Length > 0;

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
            const float pixelsPerUnit = 32f;
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
