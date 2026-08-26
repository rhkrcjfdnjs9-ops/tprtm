using System.Collections;
using UnityEngine;

namespace RealStone
{
    public sealed class ActorSecondaryLayers : MonoBehaviour
    {
        private SpriteRenderer shadow;
        private LineRenderer aura;
        private ParticleSystem dust;
        private bool hero;
        private float clock;
        private Coroutine pulseRoutine;

        public void Configure(bool isHero)
        {
            hero = isHero;
            CreateShadow();
            CreateDust();
            if (hero) CreateAura();
        }

        public void SetMoving(bool moving)
        {
            if (dust == null) return;
            var emission = dust.emission;
            emission.enabled = moving;
            if (moving && !dust.isPlaying) dust.Play();
            if (!moving) dust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        public void PulseAttack(bool heavy)
        {
            if (aura == null) return;
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine(heavy));
        }

        private void LateUpdate()
        {
            clock += Time.deltaTime;
            if (shadow != null)
            {
                var wave = 1f + Mathf.Sin(clock * 2.5f) * 0.025f;
                shadow.transform.localScale = new Vector3(wave, 1f, 1f);
            }
        }

        private void CreateShadow()
        {
            var go = new GameObject("Ground Shadow", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, -1.55f, 0f);
            go.transform.localScale = hero ? new Vector3(2.2f, 0.42f, 1f) : new Vector3(1.8f, 0.38f, 1f);
            shadow = go.GetComponent<SpriteRenderer>();
            shadow.sprite = CreateSoftDisc();
            shadow.color = new Color(0f, 0f, 0f, 0.34f);
            shadow.sortingOrder = 1;
        }

        private void CreateAura()
        {
            var go = new GameObject("Awakening Aura", typeof(LineRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0.48f, 0.58f, 0f);
            aura = go.GetComponent<LineRenderer>();
            aura.material = new Material(Shader.Find("Sprites/Default"));
            aura.loop = true;
            aura.useWorldSpace = false;
            aura.positionCount = 48;
            aura.startWidth = 0.025f;
            aura.endWidth = 0.025f;
            aura.startColor = aura.endColor = new Color(0.25f, 0.8f, 1f, 0f);
            aura.sortingOrder = 3;
            for (var i = 0; i < aura.positionCount; i++)
            {
                var angle = i * Mathf.PI * 2f / aura.positionCount;
                aura.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.68f, Mathf.Sin(angle) * 0.82f, 0f));
            }
            aura.enabled = false;
        }

        private void CreateDust()
        {
            var go = new GameObject("Foot Dust", typeof(ParticleSystem));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, -1.48f, 0f);
            dust = go.GetComponent<ParticleSystem>();
            var main = dust.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
            main.startColor = new Color(0.55f, 0.62f, 0.68f, 0.35f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 24;
            var emission = dust.emission;
            emission.rateOverTime = hero ? 16f : 9f;
            emission.enabled = false;
            var shape = dust.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(hero ? 0.7f : 0.9f, 0.05f, 0f);
            var velocity = dust.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.08f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.04f, 0.16f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var renderer = dust.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.sortingOrder = 4;
        }

        private IEnumerator PulseRoutine(bool heavy)
        {
            var baseColor = new Color(0.25f, 0.8f, 1f, 0.28f);
            var peakColor = heavy ? new Color(1f, 0.78f, 0.2f, 0.85f) : new Color(0.45f, 0.95f, 1f, 0.65f);
            var elapsed = 0f;
            var duration = heavy ? 0.34f : 0.22f;
            aura.enabled = true;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var flash = Mathf.Sin(t * Mathf.PI);
                aura.startColor = aura.endColor = Color.Lerp(baseColor, peakColor, flash);
                aura.transform.localScale = Vector3.one * (1f + flash * (heavy ? 0.18f : 0.1f));
                yield return null;
            }
            aura.startColor = aura.endColor = baseColor;
            aura.transform.localScale = Vector3.one;
            aura.enabled = false;
            pulseRoutine = null;
        }

        private static Sprite CreateSoftDisc()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Runtime Soft Shadow";
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = (x + 0.5f) / size * 2f - 1f;
                var dy = (y + 0.5f) / size * 2f - 1f;
                var alpha = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
