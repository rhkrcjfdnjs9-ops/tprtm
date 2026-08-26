using System.Collections;
using UnityEngine;

namespace RealStone
{
    public sealed class CombatVfx : MonoBehaviour
    {
        private static readonly Color LightSlash = new Color(0.35f, 0.9f, 1f, 1f);
        private static readonly Color HeavySlash = new Color(1f, 0.82f, 0.25f, 1f);

        public void PlayHit(Vector3 position, int damage, bool heavy)
        {
            StartCoroutine(SlashRoutine(position, heavy));
            StartCoroutine(DamageNumberRoutine(position + Vector3.up * 1.15f, damage, heavy));
        }

        private IEnumerator SlashRoutine(Vector3 position, bool heavy)
        {
            var root = new GameObject(heavy ? "Heavy Impact" : "Slash Impact");
            root.transform.position = position + Vector3.up * 0.65f;
            var lineCount = heavy ? 5 : 3;
            var lines = new LineRenderer[lineCount];
            var color = heavy ? HeavySlash : LightSlash;
            for (var i = 0; i < lineCount; i++)
            {
                var lineObject = new GameObject($"Ray {i}");
                lineObject.transform.SetParent(root.transform, false);
                var line = lineObject.AddComponent<LineRenderer>();
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.positionCount = 2;
                line.useWorldSpace = false;
                line.sortingOrder = 20;
                line.startColor = color;
                line.endColor = new Color(color.r, color.g, color.b, 0f);
                line.startWidth = heavy ? 0.11f : 0.07f;
                line.endWidth = 0.01f;
                var angle = Mathf.Lerp(-65f, 65f, lineCount == 1 ? 0.5f : (float)i / (lineCount - 1)) * Mathf.Deg2Rad;
                var length = heavy ? 1.25f : 0.8f;
                line.SetPosition(0, Vector3.zero);
                line.SetPosition(1, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * length);
                lines[i] = line;
            }

            var elapsed = 0f;
            const float duration = 0.16f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                root.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.25f, t);
                foreach (var line in lines)
                {
                    var c = line.startColor;
                    c.a = 1f - t;
                    line.startColor = c;
                }
                yield return null;
            }
            Destroy(root);
        }

        private IEnumerator DamageNumberRoutine(Vector3 position, int damage, bool heavy)
        {
            var go = new GameObject("Damage Number");
            go.transform.position = position;
            var text = go.AddComponent<TextMesh>();
            text.text = heavy ? $"{damage}!" : damage.ToString();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = heavy ? 82 : 64;
            text.characterSize = 0.035f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = heavy ? HeavySlash : Color.white;
            text.GetComponent<MeshRenderer>().sortingOrder = 30;

            var elapsed = 0f;
            const float duration = 0.65f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                go.transform.position = position + Vector3.up * (0.65f * t);
                var color = text.color;
                color.a = 1f - Mathf.SmoothStep(0.55f, 1f, t);
                text.color = color;
                go.transform.localScale = Vector3.one * (1f + (heavy ? 0.25f : 0.1f) * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            Destroy(go);
        }
    }
}
