using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class RunReportLogger
{
    public static void Write(GameManager manager, Player player)
    {
        if (manager == null || player == null)
            return;

        TimelineSegment segment = manager.CurrentTimelineSegment;
        RunTelemetry telemetry = manager.RunTelemetry;
        RunReportData report = new RunReportData
        {
            fieldNotes = CreateFieldNotes(),
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            gameTime = manager.GameTime,
            timelineLabel = segment != null ? segment.label : "",
            timelineGoal = segment != null ? segment.goal : "",
            difficulty = manager.Difficulty,
            playerPowerScore = manager.playerPowerScore,
            enemyHpFactor = manager.currentEnemyHpFactor,
            enemyAtkFactor = manager.currentEnemyAtkFactor,
            spawnWaveInterval = manager.SpawnWaveInterval,
            enemyCountPerGroup = manager.EnemyCountPerGroup,
            currentWaveGroupCount = manager.CurrentWaveGroupCount,
            maxEnemyCount = manager.MaxEnemyCount,
            finalEnemyCount = DataManager.allEnemyDict.Count,
            isWave = manager.isWave,
            isSpecialEvent = manager.IsSpecialEvent,
            specialEventRemainingTime = manager.SpecialEventRemainingTime,
            playerLevel = player.GetCurrentLevel(),
            playerCurrentExp = player.GetCurrentExp(),
            playerNeedExp = player.GetNeedExp(),
            playerHpProgress = player.GetHpProgress(),
            killCount = player.KilledCount,
            buildSummary = manager.GetBuildSummary(),
            levelUpCount = telemetry.levelUpCount,
            levelUpTimes = telemetry.levelUpTimes,
            levelUpLevels = telemetry.levelUpLevels,
            upgradeSelectCount = telemetry.upgradeSelectCount,
            upgradeSelectTimes = telemetry.upgradeSelectTimes,
            upgradeSelectNames = telemetry.upgradeSelectNames,
            expBallSpawned = telemetry.expBallSpawned,
            expBallCollected = telemetry.expBallCollected,
            expValueSpawned = telemetry.expValueSpawned,
            expValueCollected = telemetry.expValueCollected,
            expCollectRate = telemetry.expBallSpawned > 0 ? (float)telemetry.expBallCollected / telemetry.expBallSpawned : 0f,
            coinSpawned = telemetry.coinSpawned,
            coinCollected = telemetry.coinCollected,
            coinValueSpawned = telemetry.coinValueSpawned,
            coinValueCollected = telemetry.coinValueCollected,
            chestSpawned = telemetry.chestSpawned,
            chestCollected = telemetry.chestCollected,
            totalDamageTaken = telemetry.totalDamageTaken,
            lastDamageTaken = telemetry.lastDamageTaken,
            normalKillCount = telemetry.killByEnemyType[(int)EnemyType.Normal],
            fastKillCount = telemetry.killByEnemyType[(int)EnemyType.Fast],
            thickKillCount = telemetry.killByEnemyType[(int)EnemyType.Thick],
            selfExplosionKillCount = telemetry.killByEnemyType[(int)EnemyType.SelfExplosion],
            eliteKillCount = telemetry.killByEnemyType[(int)EnemyType.Elite],
            bossKillCount = telemetry.killByEnemyType[(int)EnemyType.Boss]
            ,
            debugJumpUsed = telemetry.debugJumpUsed,
            debugJumpMode = telemetry.debugJumpMode,
            debugJumpStage = telemetry.debugJumpStage,
            debugJumpTime = telemetry.debugJumpTime
        };

        string folder = Path.Combine(Application.persistentDataPath, "ShuabaoGameRunReports");
        Directory.CreateDirectory(folder);

        string fileName = "run_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
        string path = Path.Combine(folder, fileName);
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        Debug.Log("Run report saved: " + path);
    }

    static FieldNote[] CreateFieldNotes()
    {
        return new[]
        {
            new FieldNote("timestamp", "报告生成时间"),
            new FieldNote("gameTime", "本局存活时间，单位秒"),
            new FieldNote("timelineLabel", "死亡或结算时所处的时间轴阶段"),
            new FieldNote("timelineGoal", "该时间轴阶段原本希望玩家达到的体验目标"),
            new FieldNote("difficulty", "当前基础时间难度"),
            new FieldNote("playerPowerScore", "动态难度系统计算出的玩家战力评分"),
            new FieldNote("enemyHpFactor", "当前敌人血量倍率"),
            new FieldNote("enemyAtkFactor", "当前敌人攻击倍率"),
            new FieldNote("spawnWaveInterval", "当前普通刷怪间隔，单位秒"),
            new FieldNote("enemyCountPerGroup", "当前每组刷怪数量"),
            new FieldNote("currentWaveGroupCount", "当前每轮同时刷几组敌人"),
            new FieldNote("maxEnemyCount", "场上敌人数量上限"),
            new FieldNote("finalEnemyCount", "结算时场上敌人数量"),
            new FieldNote("isWave", "结算时是否处于尸潮"),
            new FieldNote("isSpecialEvent", "结算时是否处于特殊事件"),
            new FieldNote("specialEventRemainingTime", "距离下一次特殊事件还剩多少秒"),
            new FieldNote("playerLevel", "结算时玩家等级"),
            new FieldNote("playerCurrentExp", "结算时当前等级已获得经验"),
            new FieldNote("playerNeedExp", "当前等级升到下一级所需经验"),
            new FieldNote("playerHpProgress", "结算时玩家血量百分比，0到1"),
            new FieldNote("killCount", "本局总击杀数"),
            new FieldNote("buildSummary", "本局构筑标签和层数摘要"),
            new FieldNote("levelUpCount", "本局升级次数"),
            new FieldNote("levelUpTimes", "每次升级发生的时间，单位秒"),
            new FieldNote("levelUpLevels", "每次升级后达到的等级"),
            new FieldNote("upgradeSelectCount", "本局实际选择升级词条次数"),
            new FieldNote("upgradeSelectTimes", "每次选择升级词条的时间，单位秒"),
            new FieldNote("upgradeSelectNames", "每次选择的升级词条名称"),
            new FieldNote("expBallSpawned", "本局生成的经验球数量"),
            new FieldNote("expBallCollected", "本局拾取的经验球数量"),
            new FieldNote("expValueSpawned", "本局生成的经验总值"),
            new FieldNote("expValueCollected", "本局拾取的经验总值"),
            new FieldNote("expCollectRate", "经验球拾取率，0到1"),
            new FieldNote("coinSpawned", "本局生成的金币数量"),
            new FieldNote("coinCollected", "本局拾取的金币数量"),
            new FieldNote("coinValueSpawned", "本局生成的金币总值"),
            new FieldNote("coinValueCollected", "本局拾取的金币总值"),
            new FieldNote("chestSpawned", "本局生成的宝箱数量"),
            new FieldNote("chestCollected", "本局拾取的宝箱数量"),
            new FieldNote("totalDamageTaken", "本局玩家累计受到的实际伤害"),
            new FieldNote("lastDamageTaken", "死亡前最后一次受到的实际伤害"),
            new FieldNote("normalKillCount", "普通怪击杀数"),
            new FieldNote("fastKillCount", "快速怪击杀数"),
            new FieldNote("thickKillCount", "血厚怪击杀数"),
            new FieldNote("selfExplosionKillCount", "自爆怪击杀数"),
            new FieldNote("eliteKillCount", "精英怪击杀数"),
            new FieldNote("bossKillCount", "Boss击杀数")
            ,
            new FieldNote("debugJumpUsed", "本局是否使用过心流阶段跳转"),
            new FieldNote("debugJumpMode", "心流阶段跳转模式，TimeOnly只跳时间，FlowSnapshot套用阶段快照"),
            new FieldNote("debugJumpStage", "最后一次跳转到的心流阶段"),
            new FieldNote("debugJumpTime", "最后一次跳转后的游戏时间，单位秒")
        };
    }
}

[Serializable]
public class RunReportData
{
    public FieldNote[] fieldNotes;
    public string timestamp;
    public float gameTime;
    public string timelineLabel;
    public string timelineGoal;
    public float difficulty;
    public int playerPowerScore;
    public float enemyHpFactor;
    public float enemyAtkFactor;
    public float spawnWaveInterval;
    public int enemyCountPerGroup;
    public int currentWaveGroupCount;
    public int maxEnemyCount;
    public int finalEnemyCount;
    public bool isWave;
    public bool isSpecialEvent;
    public float specialEventRemainingTime;
    public int playerLevel;
    public int playerCurrentExp;
    public int playerNeedExp;
    public float playerHpProgress;
    public int killCount;
    public string buildSummary;
    public int levelUpCount;
    public List<float> levelUpTimes;
    public List<int> levelUpLevels;
    public int upgradeSelectCount;
    public List<float> upgradeSelectTimes;
    public List<string> upgradeSelectNames;
    public int expBallSpawned;
    public int expBallCollected;
    public int expValueSpawned;
    public int expValueCollected;
    public float expCollectRate;
    public int coinSpawned;
    public int coinCollected;
    public int coinValueSpawned;
    public int coinValueCollected;
    public int chestSpawned;
    public int chestCollected;
    public int totalDamageTaken;
    public int lastDamageTaken;
    public int normalKillCount;
    public int fastKillCount;
    public int thickKillCount;
    public int selfExplosionKillCount;
    public int eliteKillCount;
    public int bossKillCount;
    public bool debugJumpUsed;
    public string debugJumpMode;
    public string debugJumpStage;
    public float debugJumpTime;
}

[Serializable]
public class FieldNote
{
    public string field;
    public string note;

    public FieldNote(string field, string note)
    {
        this.field = field;
        this.note = note;
    }
}

[Serializable]
public class RunTelemetry
{
    public int levelUpCount;
    public List<float> levelUpTimes = new List<float>();
    public List<int> levelUpLevels = new List<int>();
    public int upgradeSelectCount;
    public List<float> upgradeSelectTimes = new List<float>();
    public List<string> upgradeSelectNames = new List<string>();
    public int expBallSpawned;
    public int expBallCollected;
    public int expValueSpawned;
    public int expValueCollected;
    public int coinSpawned;
    public int coinCollected;
    public int coinValueSpawned;
    public int coinValueCollected;
    public int chestSpawned;
    public int chestCollected;
    public int totalDamageTaken;
    public int lastDamageTaken;
    public int[] killByEnemyType = new int[6];
    public bool debugJumpUsed;
    public string debugJumpMode;
    public string debugJumpStage;
    public float debugJumpTime;
}
