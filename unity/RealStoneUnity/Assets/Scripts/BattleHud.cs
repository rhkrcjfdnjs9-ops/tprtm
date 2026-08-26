using UnityEngine;
using UnityEngine.UI;

namespace RealStone
{
    public sealed class BattleHud : MonoBehaviour
    {
        private Slider heroHp;
        private Slider enemyHp;
        private Text status;
        private Text stage;

        public static BattleHud Create()
        {
            var canvasObject = new GameObject("Battle HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var hud = canvasObject.AddComponent<BattleHud>();
            hud.heroHp = CreateSlider(canvasObject.transform, new Vector2(34, -80), new Vector2(430, 24), new Color(0.25f, 0.85f, 0.95f));
            hud.enemyHp = CreateSlider(canvasObject.transform, new Vector2(-34, -80), new Vector2(430, 24), new Color(1f, 0.34f, 0.26f), true);
            hud.stage = CreateText(canvasObject.transform, "\uB9AC\uC5BC \uB3CC \uD0A4\uC6B0\uAE30  \u00B7  \uB358\uC804 1-1", 34, TextAnchor.UpperCenter);
            SetRect(hud.stage.rectTransform, new Vector2(0, -28), new Vector2(600, 58), new Vector2(0.5f, 1f));
            hud.status = CreateText(canvasObject.transform, "\uC804\uD22C \uC900\uBE44 \uC911", 30, TextAnchor.MiddleCenter);
            SetRect(hud.status.rectTransform, new Vector2(0, 90), new Vector2(920, 78), new Vector2(0.5f, 0f));
            return hud;
        }

        public void Refresh(BattleActor hero, BattleActor enemy, int dungeonStage, string message)
        {
            heroHp.value = hero.MaxHp == 0 ? 0 : (float)hero.Hp / hero.MaxHp;
            enemyHp.value = enemy.MaxHp == 0 ? 0 : (float)enemy.Hp / enemy.MaxHp;
            stage.text = $"\uB9AC\uC5BC \uB3CC \uD0A4\uC6B0\uAE30  \u00B7  \uB358\uC804 {dungeonStage}-1";
            status.text = message;
        }

        private static Slider CreateSlider(Transform parent, Vector2 position, Vector2 size, Color color, bool right = false)
        {
            var root = new GameObject(right ? "Enemy HP" : "Hero HP", typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            SetRect(root.GetComponent<RectTransform>(), position, size, new Vector2(right ? 1f : 0f, 1f));
            var background = CreateImage(root.transform, "Background", new Color(0.05f, 0.08f, 0.12f, 0.86f));
            Stretch(background.rectTransform);
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>(), 4);
            var fill = CreateImage(fillArea.transform, "Fill", color);
            Stretch(fill.rectTransform);
            var slider = root.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = fill;
            slider.direction = right ? Slider.Direction.RightToLeft : Slider.Direction.LeftToRight;
            slider.value = 1;
            return slider;
        }

        private static Text CreateText(Transform parent, string value, int size, TextAnchor alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float inset = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
