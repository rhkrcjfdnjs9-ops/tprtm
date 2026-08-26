using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealStone
{
    public sealed class RealStoneBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (SceneManager.GetActiveScene().name == "GraniaExactRigPreview") return;
            if (FindFirstObjectByType<RealStoneBootstrap>() != null) return;
            new GameObject("Real Stone Bootstrap").AddComponent<RealStoneBootstrap>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            BuildCameraAndBackground(out var camera);
            var hero = CreateActor(
                "Grania",
                new Vector3(-3.7f, -2.2f, 0),
                "Art/Grania",
                3.5f,
                120);
            var enemy = CreateActor(
                "Granite Golem",
                new Vector3(3.4f, -2.15f, 0),
                "Art/Golem",
                3.0f,
                120);
            var hud = BattleHud.Create();
            var settings = Resources.Load<BattleSettings>("Data/BattleSettings");
            if (settings == null) settings = ScriptableObject.CreateInstance<BattleSettings>();
            hero.SetMaxHealth(settings.heroMaxHp);
            var controller = new GameObject("Battle Controller").AddComponent<BattleController>();
            controller.Configure(hero, enemy, hud, camera, settings);
        }

        private static void BuildCameraAndBackground(out Camera camera)
        {
            var cameraObject = new GameObject("Battle Camera", typeof(Camera), typeof(AudioListener));
            camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.6f;
            camera.backgroundColor = new Color(0.025f, 0.04f, 0.075f);
            camera.transform.position = new Vector3(0, 0, -10);

            var backgroundSprite = Resources.Load<Sprite>("Art/Backgrounds/crystal_mine_battlefield");
            if (backgroundSprite == null) return;
            var background = new GameObject("Crystal Mine", typeof(SpriteRenderer));
            var renderer = background.GetComponent<SpriteRenderer>();
            renderer.sprite = backgroundSprite;
            renderer.sortingOrder = -20;
            var heightScale = camera.orthographicSize * 2f / backgroundSprite.bounds.size.y;
            var widthScale = camera.orthographicSize * 2f * camera.aspect / backgroundSprite.bounds.size.x;
            background.transform.localScale = Vector3.one * Mathf.Max(heightScale, widthScale);
        }

        private static BattleActor CreateActor(string name, Vector3 position, string resourceRoot, float height, int hp)
        {
            var actorObject = new GameObject(name, typeof(BattleActor), typeof(ActorSecondaryLayers), typeof(Animator));
            actorObject.transform.position = position;
            var visualObject = new GameObject("Visual", typeof(SpriteRenderer), typeof(SpriteSequencePlayer), typeof(ActorVisualMotion));
            visualObject.transform.SetParent(actorObject.transform, false);
            var renderer = visualObject.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = 5;
            var idle = LoadSequence($"{resourceRoot}/Idle");
            var run = LoadSequence($"{resourceRoot}/Run");
            var normalizedAttackRoot = resourceRoot.Contains("Golem") ? "ArtNormalized/Golem/Attack" : $"{resourceRoot}/Attack";
            var attack = LoadSequence(normalizedAttackRoot);
            var hit = LoadSequence($"{resourceRoot}/Hit");
            var death = LoadSequence($"{resourceRoot}/Death");
            if (idle.Length > 0)
            {
                renderer.sprite = idle[0];
                var spriteHeight = Mathf.Max(0.01f, idle[0].bounds.size.y);
                visualObject.transform.localScale = Vector3.one * (height / spriteHeight);
            }
            visualObject.GetComponent<ActorVisualMotion>().Configure(resourceRoot.Contains("Golem"));
            actorObject.GetComponent<ActorSecondaryLayers>().Configure(resourceRoot.Contains("Grania"));
            var controllerName = resourceRoot.Contains("Grania") ? "GraniaMotion" : "GolemMotion";
            var animator = actorObject.GetComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>($"Data/Animation/{controllerName}");
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var actor = actorObject.GetComponent<BattleActor>();
            actor.Configure(idle, run, attack, hit, death, hp);
            return actor;
        }

        private static Sprite[] LoadSequence(string path) => Resources.LoadAll<Sprite>(path)
            .OrderBy(sprite => ExtractTrailingNumber(sprite.name))
            .ToArray();

        private static int ExtractTrailingNumber(string value)
        {
            var digits = new string(value.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
            return int.TryParse(digits, out var number) ? number : 0;
        }
    }
}
