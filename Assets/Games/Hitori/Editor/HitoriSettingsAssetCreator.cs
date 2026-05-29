using UnityEditor;
using UnityEngine;

namespace Gamio.Games.Hitori.Editor
{
    public static class HitoriSettingsAssetCreator
    {
        [MenuItem("Gamio/Hitori/Create Settings Asset")]
        public static void CreateAsset()
        {
            var settings = ScriptableObject.CreateInstance<HitoriGameSettingsSO>();
            settings.gridSize = 7;
            AssetDatabase.CreateAsset(settings, "Assets/Games/Hitori/Resources/HitoriSettings.asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = settings;
        }
    }
}
