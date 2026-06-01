using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameBalanceConfig))]
public class GameBalanceConfigEditor : Editor
{
    bool showLevel1 = true;
    bool showLevel2 = true;
    bool showLevel3 = false;

    readonly HashSet<string> bossCommon = new HashSet<string> { "phase2HpPercent" };
    readonly HashSet<string> deathCommon = new HashSet<string> { "durationJuice", "shakeJuice", "pulseJuice" };
    readonly HashSet<string> rewardCommon = new HashSet<string> { "coinJuice", "expJuice", "rewardSpreadJuice" };
    readonly HashSet<string> upgradeCommon = new HashSet<string> { "bulletUpgradeImpact", "powerUpgradeImpact", "fireUpgradeImpact", "specialUpgradeImpact" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawLevel1();
        DrawLevel2();
        DrawLevel3();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawLevel1()
    {
        showLevel1 = DrawBigFoldout(showLevel1, "第一层：常用总控参数");
        if (!showLevel1)
            return;

        EditorGUILayout.HelpBox("日常调心流优先只改这里。这里的值已经实装到奖励、死亡、Boss和升级效果的读取逻辑中。", MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("designer"), new GUIContent("常用总控参数"), true);
    }

    void DrawLevel2()
    {
        showLevel2 = DrawBigFoldout(showLevel2, "第二层：模块化常用参数");
        if (!showLevel2)
            return;

        EditorGUI.indentLevel++;
        DrawFullModule("player", "玩家初始数值与升级节奏");
        DrawFullModule("wave", "普通刷怪与尸潮节奏");
        DrawFullModule("dynamicDifficulty", "动态难度计算参数");
        DrawFullModule("specialEvent", "特殊事件触发节奏");
        DrawFullModule("chapter", "战斗章节事件配置");
        DrawCommonModule("bossCombat", "Boss战斗表现", bossCommon);
        DrawCommonModule("deathEffect", "死亡效果", deathCommon);
        DrawFullModule("enemyMovement", "敌人移动思考");
        DrawFullModule("playerTargeting", "玩家索敌与提前量");
        DrawCommonModule("reward", "奖励掉落", rewardCommon);
        DrawFullModule("upgradeRules", "升级词条出现规则");
        DrawCommonModule("upgradeEffects", "升级词条生效强度", upgradeCommon);
        DrawFullModule("debug", "调试工具");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("timeline"), new GUIContent("心流时间轴阶段配置"), true);
        EditorGUI.indentLevel--;
    }

    void DrawLevel3()
    {
        showLevel3 = DrawBigFoldout(showLevel3, "第三层：高级细节参数");
        if (!showLevel3)
            return;

        EditorGUILayout.HelpBox("这里放不常改的细节值。只有第一层、第二层调不出效果时，再展开这里微调。", MessageType.None);

        EditorGUI.indentLevel++;
        DrawAdvancedModule("bossCombat", "Boss高级细节参数", bossCommon);
        DrawAdvancedModule("deathEffect", "死亡效果高级细节参数", deathCommon);
        DrawAdvancedModule("reward", "奖励高级细节参数", rewardCommon);
        DrawAdvancedModule("upgradeEffects", "升级效果高级细节参数", upgradeCommon);
        EditorGUI.indentLevel--;
    }

    bool DrawBigFoldout(bool expanded, string title)
    {
        EditorGUILayout.Space(8f);
        Rect rect = EditorGUILayout.GetControlRect(false, 24f);
        rect.x += 2f;
        rect.width -= 4f;
        EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f, 1f));
        rect.x += 6f;
        expanded = EditorGUI.Foldout(rect, expanded, title, true, EditorStyles.boldLabel);
        return expanded;
    }

    void DrawFullModule(string propertyName, string title)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            return;

        EditorGUILayout.PropertyField(property, new GUIContent(title), true);
    }

    void DrawCommonModule(string propertyName, string title, HashSet<string> commonFields)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            return;

        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        foreach (string fieldName in commonFields)
        {
            SerializedProperty child = property.FindPropertyRelative(fieldName);
            if (child != null)
                EditorGUILayout.PropertyField(child, true);
        }
        EditorGUI.indentLevel--;
    }

    void DrawAdvancedModule(string propertyName, string title, HashSet<string> commonFields)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            return;

        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        DrawRemainingChildren(property, commonFields);
        EditorGUI.indentLevel--;
    }

    void DrawRemainingChildren(SerializedProperty parent, HashSet<string> skippedNames)
    {
        SerializedProperty iterator = parent.Copy();
        SerializedProperty end = parent.GetEndProperty();
        bool enterChildren = true;

        while (iterator.Next(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
        {
            enterChildren = false;

            if (skippedNames.Contains(iterator.name))
                continue;

            EditorGUILayout.PropertyField(iterator, true);
        }
    }
}
