using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameBalanceConfigCreator
{
    const string AssetPath = "Assets/Resources/configs/GameBalanceConfig.asset";

    [MenuItem("ShuabaoGame/Create Default Game Balance Config")]
    public static void CreateDefaultConfig()
    {
        GameBalanceConfig existing = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(AssetPath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
            return;
        }

        string folder = Path.GetDirectoryName(AssetPath);
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
        AssetDatabase.CreateAsset(config, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
    }
}
