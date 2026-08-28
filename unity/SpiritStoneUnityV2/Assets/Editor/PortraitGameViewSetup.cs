using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SpiritStone.Editor
{
    public static class PortraitGameViewSetup
    {
        private const string PresetName = "Android Portrait 1080x1920";

        [MenuItem("Tools/Spirit Stone/Set Android Portrait 1080x1920 at 1x")]
        public static void Apply()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;

            var editorAssembly = typeof(global::UnityEditor.Editor).Assembly;
            var sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            var sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            var sizeEnumType = editorAssembly.GetType("UnityEditor.GameViewSizeType");
            var gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            if (sizesType == null || sizeType == null || sizeEnumType == null || gameViewType == null)
                throw new InvalidOperationException("Unity Game View internal types were not found.");

            var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singletonType.GetProperty("instance")?.GetValue(null);
            var getGroup = sizesType.GetMethod("GetGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var group = getGroup?.Invoke(sizes, new object[] { GameViewSizeGroupType.Standalone });
            if (group == null) throw new InvalidOperationException("Standalone Game View size group was not found.");

            int targetIndex = FindPresetIndex(group, PresetName);
            if (targetIndex < 0)
            {
                var fixedResolution = Enum.Parse(sizeEnumType, "FixedResolution");
                var constructor = sizeType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { sizeEnumType, typeof(int), typeof(int), typeof(string) },
                    null);
                var newSize = constructor?.Invoke(new[] { fixedResolution, (object)1080, 1920, PresetName });
                group.GetType().GetMethod("AddCustomSize")?.Invoke(group, new[] { newSize });
                targetIndex = FindPresetIndex(group, PresetName);
            }

            var gameView = EditorWindow.GetWindow(gameViewType);
            gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(gameView, targetIndex);
            SetOneToOneZoom(gameView, gameViewType);
            gameView.Repaint();

            AssetDatabase.SaveAssets();
            Debug.Log($"Portrait Game View applied: {PresetName}, preset index {targetIndex}, zoom 1x requested.");
        }

        private static int FindPresetIndex(object group, string displayText)
        {
            var type = group.GetType();
            int builtinCount = (int)(type.GetMethod("GetBuiltinCount")?.Invoke(group, null) ?? 0);
            int customCount = (int)(type.GetMethod("GetCustomCount")?.Invoke(group, null) ?? 0);
            var getGameViewSize = type.GetMethod("GetGameViewSize");
            for (int index = 0; index < builtinCount + customCount; index++)
            {
                var size = getGameViewSize?.Invoke(group, new object[] { index });
                var text = size?.GetType().GetProperty("displayText")?.GetValue(size) as string;
                if (text == displayText) return index;
            }
            return -1;
        }

        private static void SetOneToOneZoom(EditorWindow gameView, Type gameViewType)
        {
            var zoomField = gameViewType.GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic);
            var zoomArea = zoomField?.GetValue(gameView);
            if (zoomArea == null) return;

            var zoomType = zoomArea.GetType();
            var scaleProperty = zoomType.GetProperty("scale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (scaleProperty?.CanWrite == true)
            {
                scaleProperty.SetValue(zoomArea, Vector2.one);
                return;
            }

            zoomType.GetField("m_Scale", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(zoomArea, Vector2.one);
        }
    }
}
