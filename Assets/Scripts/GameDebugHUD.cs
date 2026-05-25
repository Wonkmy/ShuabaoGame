using UnityEngine;

public class GameDebugHUD : MonoBehaviour
{
    bool visible = true;
    GUIStyle boxStyle;
    GUIStyle labelStyle;

    void Update()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
            return;

        if (Input.GetKeyDown(manager.BalanceConfig.debug.toggleHudKey))
        {
            visible = !visible;
        }
    }

    void OnGUI()
    {
        if (!visible || GameManager.Instance == null)
            return;

        GameManager manager = GameManager.Instance;
        Player player = manager.GetPlayer();
        InitStyles();

        GUILayout.BeginArea(new Rect(12f, 72f, 390f, 380f), boxStyle);
        GUILayout.Label("Balance Debug (F12)", labelStyle);

        TimelineSegment segment = manager.CurrentTimelineSegment;
        if (segment != null)
        {
            GUILayout.Label("Timeline: " + segment.label + "  " + segment.startTime + "-" + segment.endTime + "s", labelStyle);
            GUILayout.Label("Goal: " + segment.goal, labelStyle);
            GUILayout.Label("Pressure: " + segment.pressure + " / Enemies: " + segment.expectedEnemies, labelStyle);
        }

        GUILayout.Space(6f);
        GUILayout.Label("Time: " + manager.GameTime.ToString("F1") + "s", labelStyle);
        GUILayout.Label("Difficulty: " + manager.Difficulty.ToString("F2") + "  Power: " + manager.playerPowerScore, labelStyle);
        GUILayout.Label("Enemy: " + DataManager.allEnemyDict.Count + "/" + manager.MaxEnemyCount + "  HPx " + manager.currentEnemyHpFactor.ToString("F2") + "  ATKx " + manager.currentEnemyAtkFactor.ToString("F2"), labelStyle);
        GUILayout.Label("Spawn: " + manager.SpawnWaveInterval.ToString("F2") + "s  Group " + manager.CurrentWaveGroupCount + " x " + manager.EnemyCountPerGroup, labelStyle);
        GUILayout.Label("Wave: " + manager.isWave + "  Special: " + manager.IsSpecialEvent + "  NextSpecial: " + manager.SpecialEventRemainingTime.ToString("F1") + "s", labelStyle);
        GUILayout.Label("Jump: F1-F4 TimeOnly / Shift+F1-F4 Snapshot", labelStyle);

        if (player != null)
        {
            GUILayout.Space(6f);
            GUILayout.Label("Player Lv: " + player.GetCurrentLevel() + "  Exp: " + player.GetCurrentExp() + "/" + player.GetNeedExp(), labelStyle);
            GUILayout.Label("Kills: " + player.KilledCount + "  HP: " + Mathf.RoundToInt(player.GetHpProgress() * 100f) + "%", labelStyle);
            GUILayout.Label("Build: " + manager.GetBuildSummary(), labelStyle);
        }

        GUILayout.EndArea();
    }

    void InitStyles()
    {
        if (boxStyle != null)
            return;

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.alignment = TextAnchor.UpperLeft;
        boxStyle.padding = new RectOffset(10, 10, 8, 8);

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 13;
        labelStyle.normal.textColor = Color.white;
        labelStyle.wordWrap = false;
    }
}
