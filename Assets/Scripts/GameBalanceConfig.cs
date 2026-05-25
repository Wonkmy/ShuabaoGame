using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "ShuabaoGame/Game Balance Config")]
public class GameBalanceConfig : ScriptableObject
{
    [Header("玩家初始数值与升级节奏")]
    public PlayerTuning player = new PlayerTuning();

    [Header("普通刷怪与尸潮节奏")]
    public WaveTuning wave = new WaveTuning();

    [Header("动态难度计算参数")]
    public DynamicDifficultyTuning dynamicDifficulty = new DynamicDifficultyTuning();

    [Header("特殊事件触发节奏")]
    public SpecialEventTuning specialEvent = new SpecialEventTuning();

    [Header("经验、金币、宝箱奖励")]
    public RewardTuning reward = new RewardTuning();

    [Header("升级词条出现规则")]
    public UpgradeRuleTuning upgradeRules = new UpgradeRuleTuning();

    [Header("调试工具开关与快捷键")]
    public DebugTuning debug = new DebugTuning();

    [Header("心流时间轴阶段配置")]
    public TimelineSegment[] timeline =
    {
        new TimelineSegment { startTime = 0f, endTime = 30f, label = "Opening", goal = "First kills and first upgrade", expectedEnemies = "Normal/Fast", pressure = "Low" },
        new TimelineSegment { startTime = 30f, endTime = 90f, label = "First Pressure", goal = "Build direction appears", expectedEnemies = "Normal/Fast/Thick", pressure = "Medium" },
        new TimelineSegment { startTime = 90f, endTime = 180f, label = "Build Check", goal = "Elite pressure and combo payoff", expectedEnemies = "Thick/SelfExplosion/Elite", pressure = "High" },
        new TimelineSegment { startTime = 180f, endTime = 9999f, label = "Endless Scaling", goal = "Boss and wave endurance", expectedEnemies = "All", pressure = "Rising" },
    };

    public TimelineSegment GetTimelineSegment(float gameTime)
    {
        if (timeline == null)
            return null;

        for (int i = 0; i < timeline.Length; i++)
        {
            TimelineSegment segment = timeline[i];
            if (segment != null && gameTime >= segment.startTime && gameTime < segment.endTime)
                return segment;
        }

        return timeline.Length > 0 ? timeline[timeline.Length - 1] : null;
    }
}

[Serializable]
public class PlayerTuning
{
    [Header("玩家初始生命值")]
    public float startHp = 520f;

    [Header("玩家初始攻击力")]
    public float startAttack = 5f;

    [Header("玩家初始移动速度")]
    public float startMoveSpeed = 5.4f;

    [Header("玩家初始防御力")]
    public float startDefence = 15f;

    [Header("首次升级所需经验")]
    public int firstLevelExp = 34;

    [Header("每次升级后的经验需求成长倍率")]
    public float expGrowth = 1.34f;
}

[Serializable]
public class WaveTuning
{
    [Header("基础刷怪间隔，单位秒")]
    public float spawnWaveIntervalBase = 5.5f;

    [Header("普通刷怪每轮最少组数")]
    public int normalGroupMin = 1;

    [Header("普通刷怪每轮最多组数")]
    public int normalGroupMax = 2;

    [Header("普通刷怪每组最少敌人数")]
    public int normalEnemyMin = 2;

    [Header("普通刷怪每组最多敌人数")]
    public int normalEnemyMax = 5;

    [Header("尸潮期间每组敌人数倍率")]
    public int waveEnemyMultiplier = 2;

    [Header("尸潮出现间隔，单位秒")]
    public float waveAppearInterval = 35f;

    [Header("尸潮持续时间，单位秒")]
    public float waveDuration = 7f;

    [Header("场上最大敌人数量")]
    public int maxEnemyCount = 36;

    [Header("开局每组敌人数量")]
    public int initialEnemyCountPerGroup = 4;

    [Header("同一轮多组刷怪之间的延迟，单位秒")]
    public float groupSpawnDelay = 0.25f;
}

[Serializable]
public class DynamicDifficultyTuning
{
    [Header("开局基础难度")]
    public float initialDifficulty = 2f;

    [Header("时间难度基础值")]
    public float difficultyBase = 1.5f;

    [Header("时间难度每隔多少秒提升一级")]
    public float difficultyStepSeconds = 35f;

    [Header("时间难度最小值")]
    public float minDifficulty = 1.5f;

    [Header("时间难度最大值")]
    public float maxDifficulty = 8f;

    [Header("动态难度刷新间隔，单位秒")]
    public float updateInterval = 10f;

    [Header("战力评分为0时的刷怪间隔")]
    public float spawnIntervalBase = 6f;

    [Header("玩家战力每点对刷怪间隔的缩短量")]
    public float spawnIntervalPowerScale = 0.018f;

    [Header("动态刷怪间隔最小值")]
    public float minSpawnInterval = 2.2f;

    [Header("动态刷怪间隔最大值")]
    public float maxSpawnInterval = 6f;

    [Header("每组敌人数量基础值")]
    public int enemyCountBase = 3;

    [Header("玩家战力每多少点增加每组敌人数量")]
    public int enemyCountPowerDivisor = 25;

    [Header("每组敌人数量最小值")]
    public int minEnemyCountPerGroup = 3;

    [Header("每组敌人数量最大值")]
    public int maxEnemyCountPerGroup = 16;

    [Header("每轮刷怪组数基础值")]
    public int groupCountBase = 1;

    [Header("玩家战力每多少点增加每轮刷怪组数")]
    public int groupCountPowerDivisor = 40;

    [Header("每轮刷怪组数最小值")]
    public int minGroupCount = 1;

    [Header("每轮刷怪组数最大值")]
    public int maxGroupCount = 5;

    [Header("敌人强度成长计算的玩家战力基准")]
    public float powerFactorBase = 50f;

    [Header("敌人强度成长随游戏时间放缓的系数")]
    public float powerFactorTimeScale = 0.8f;

    [Header("敌人血量倍率最大值")]
    public float maxEnemyHpFactor = 4f;

    [Header("敌人攻击倍率最大值")]
    public float maxEnemyAtkFactor = 2f;

    [Header("玩家每升一级增加的战力评分")]
    public int levelScorePerLevel = 6;

    [Header("每多少击杀增加1点战力评分")]
    public int killScoreDivisor = 15;

    [Header("每个构筑标签增加的战力评分")]
    public int buildScorePerTag = 8;

    [Header("每点攻击力增加的战力评分")]
    public float attackScorePerPoint = 6f;

    [Header("攻击评分是否只计算超过初始攻击力的部分")]
    public bool scoreOnlyAttackAboveStart = true;
}

[Serializable]
public class SpecialEventTuning
{
    [Header("首次特殊事件触发时间，单位秒")]
    public float firstSpecialEventInterval = 85f;

    [Header("敌人数量压力评分")]
    public int enemyPressureScore = 50;

    [Header("低血量压力评分")]
    public int lowHpPressureScore = 40;

    [Header("尸潮期间额外压力评分")]
    public int wavePressureBonus = 30;

    [Header("特殊事件期间额外压力评分")]
    public int specialEventPressureBonus = 30;

    [Header("高压力阈值")]
    public int highPressureThreshold = 70;

    [Header("中压力阈值")]
    public int midPressureThreshold = 45;

    [Header("低压力阈值")]
    public int lowPressureThreshold = 25;

    [Header("高压力下下一次特殊事件间隔")]
    public float highPressureInterval = 85f;

    [Header("中压力下下一次特殊事件间隔")]
    public float midPressureInterval = 70f;

    [Header("低压力下下一次特殊事件间隔")]
    public float lowPressureInterval = 55f;

    [Header("平稳状态下下一次特殊事件间隔")]
    public float calmInterval = 40f;
}

[Serializable]
public class RewardTuning
{
    [Header("尸潮敌人死亡生成金币数量")]
    public int waveCoinCount = 2;

    [Header("尸潮敌人金币单个价值")]
    public int waveCoinValue = 1;

    [Header("精英和Boss掉落宝箱概率")]
    [Range(0f, 1f)] public float eliteBossChestChance = 0.5f;

    [Header("精英怪金币掉落数量")]
    public int eliteCoinCount = 5;

    [Header("精英怪金币单个价值")]
    public int eliteCoinValue = 2;

    [Header("Boss金币掉落数量")]
    public int bossCoinCount = 8;

    [Header("Boss金币单个价值")]
    public int bossCoinValue = 4;

    [Header("暴击击杀经验倍率")]
    public float critExpMultiplier = 1.25f;

    [Header("不同敌人类型的经验奖励配置")]
    public EnemyReward[] enemyRewards =
    {
        new EnemyReward { enemyType = EnemyType.Normal, baseExp = 3.2f, expBallCount = 1, spreadRadius = 0f },
        new EnemyReward { enemyType = EnemyType.Fast, baseExp = 4.2f, expBallCount = 1, spreadRadius = 0f },
        new EnemyReward { enemyType = EnemyType.Thick, baseExp = 2.5f, expBallCount = 15, spreadRadius = 1.85f },
        new EnemyReward { enemyType = EnemyType.SelfExplosion, baseExp = 0f, expBallCount = 1, spreadRadius = 0f },
        new EnemyReward { enemyType = EnemyType.Elite, baseExp = 3.8f, expBallCount = 12, spreadRadius = 1.5f },
        new EnemyReward { enemyType = EnemyType.Boss, baseExp = 6.5f, expBallCount = 18, spreadRadius = 2.35f },
    };

    public EnemyReward GetEnemyReward(EnemyType enemyType)
    {
        if (enemyRewards != null)
        {
            for (int i = 0; i < enemyRewards.Length; i++)
            {
                if (enemyRewards[i] != null && enemyRewards[i].enemyType == enemyType)
                    return enemyRewards[i];
            }
        }

        return new EnemyReward { enemyType = enemyType, baseExp = 0f, expBallCount = 1, spreadRadius = 0f };
    }
}

[Serializable]
public class EnemyReward
{
    [Header("敌人类型")]
    public EnemyType enemyType;

    [Header("基础经验值")]
    public float baseExp;

    [Header("经验球生成数量")]
    public int expBallCount = 1;

    [Header("多个经验球的扩散半径")]
    public float spreadRadius;
}

[Serializable]
public class UpgradeRuleTuning
{
    [Header("暴击爆炸出现所需暴击层数")]
    public int critExplosionMinCritStacks = 2;

    [Header("穿透爆炸出现所需穿透层数")]
    public int pierceExplosionMinPierceStacks = 2;

    [Header("无限火力出现所需火力层数")]
    public int legendFireMinFireStacks = 2;

    [Header("无限火力出现所需子弹层数")]
    public int legendFireMinBulletStacks = 5;
}

[Serializable]
public class DebugTuning
{
    [Header("是否显示调参HUD")]
    public bool showDebugHud = true;

    [Header("是否生成跑局报告")]
    public bool writeRunReport = true;

    [Header("调参HUD显示隐藏快捷键")]
    public KeyCode toggleHudKey = KeyCode.F12;

    [Header("是否启用心流阶段跳转")]
    public bool enableFlowStageJump = true;

    [Header("心流阶段跳转快捷键列表")]
    public KeyCode[] flowStageKeys = { KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4 };

    [Header("心流阶段快照配置")]
    public FlowStageSnapshot[] flowStageSnapshots =
    {
        new FlowStageSnapshot { stageIndex = 0, targetTime = 5f, playerLevel = 1, expRatio = 0.2f, hpRatio = 1f, upgradeIds = new int[0], secondsBeforeSpecialEvent = 80f },
        new FlowStageSnapshot { stageIndex = 1, targetTime = 35f, playerLevel = 2, expRatio = 0.15f, hpRatio = 1f, upgradeIds = new[] { 1 }, secondsBeforeSpecialEvent = 45f },
        new FlowStageSnapshot { stageIndex = 2, targetTime = 95f, playerLevel = 4, expRatio = 0.25f, hpRatio = 0.85f, upgradeIds = new[] { 1, 6, 7 }, secondsBeforeSpecialEvent = 25f },
        new FlowStageSnapshot { stageIndex = 3, targetTime = 185f, playerLevel = 7, expRatio = 0.35f, hpRatio = 0.75f, upgradeIds = new[] { 1, 4, 6, 7, 3, 5 }, secondsBeforeSpecialEvent = 18f },
    };
}

[Serializable]
public class TimelineSegment
{
    [Header("阶段开始时间，单位秒")]
    public float startTime;

    [Header("阶段结束时间，单位秒")]
    public float endTime;

    [Header("阶段名称")]
    public string label;

    [Header("阶段体验目标")]
    public string goal;

    [Header("该阶段预期出现的敌人类型")]
    public string expectedEnemies;

    [Header("该阶段预期压力等级")]
    public string pressure;
}

public enum FlowJumpMode
{
    TimeOnly,
    FlowSnapshot
}

[Serializable]
public class FlowStageSnapshot
{
    [Header("对应的心流阶段索引")]
    public int stageIndex;

    [Header("跳转后的游戏时间，单位秒")]
    public float targetTime;

    [Header("快照模式下设置的玩家等级")]
    public int playerLevel = 1;

    [Header("快照模式下当前等级经验进度")]
    [Range(0f, 0.95f)] public float expRatio;

    [Header("快照模式下玩家血量比例")]
    [Range(0.01f, 1f)] public float hpRatio = 1f;

    [Header("快照模式下自动套用的升级词条ID")]
    public int[] upgradeIds;

    [Header("快照模式下距离下一次特殊事件的剩余时间")]
    public float secondsBeforeSpecialEvent = 30f;
}
