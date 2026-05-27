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

    [Header("战斗章节事件配置")]
    public CombatChapterTuning chapter = new CombatChapterTuning();

    [Header("最终Boss战节奏、阶段和预警表现")]
    public BossCombatTuning bossCombat = new BossCombatTuning();

    [Header("敌人死亡效果时长、震屏和重量感")]
    public EnemyDeathEffectTuning deathEffect = new EnemyDeathEffectTuning();

    [Header("敌人移动思考与不同敌人移动气质")]
    public EnemyMovementTuning enemyMovement = new EnemyMovementTuning();

    [Header("玩家索敌与移动目标提前量瞄准")]
    public PlayerTargetingTuning playerTargeting = new PlayerTargetingTuning();

    [Header("经验、金币、宝箱奖励")]
    public RewardTuning reward = new RewardTuning();

    [Header("升级词条出现规则")]
    public UpgradeRuleTuning upgradeRules = new UpgradeRuleTuning();

    [Header("升级词条实际生效数值和反馈类型")]
    public UpgradeEffectTuning upgradeEffects = new UpgradeEffectTuning();

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

    void OnEnable()
    {
        EnsureNestedConfigs();
    }

    public void EnsureNestedConfigs()
    {
        if (chapter == null)
        {
            chapter = new CombatChapterTuning();
        }

        if (bossCombat == null)
        {
            bossCombat = new BossCombatTuning();
        }

        if (deathEffect == null)
        {
            deathEffect = new EnemyDeathEffectTuning();
        }

        if (enemyMovement == null)
        {
            enemyMovement = new EnemyMovementTuning();
        }

        if (playerTargeting == null)
        {
            playerTargeting = new PlayerTargetingTuning();
        }

        if (upgradeEffects == null)
        {
            upgradeEffects = new UpgradeEffectTuning();
        }
    }

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
public class CombatChapterTuning
{
    [Header("是否启用战斗章节事件")]
    public bool enableChapterEvents = true;

    [Header("小Boss章节触发时间，单位秒")]
    public float[] miniBossTimes = { 90f, 180f };

    [Header("最终Boss章节触发时间，单位秒")]
    public float finalBossTime = 300f;

    [Header("小Boss章节标题")]
    public string miniBossTitle = "小型Boss战";

    [Header("最终Boss章节标题")]
    public string finalBossTitle = "最终Boss战";

    [Header("章节标题字号")]
    public float chapterTitleSize = 1.25f;

    [Header("章节出场前警告等待时间")]
    public float warningDelay = 1.0f;

    [Header("章节触发时镜头暗化强度")]
    [Range(0f, 1f)] public float darkIntensity = 0.5f;

    [Header("章节触发时震屏时长")]
    public float cameraShakeDuration = 0.25f;

    [Header("章节触发时震屏强度")]
    public float cameraShakeStrength = 0.18f;
}

[Serializable]
public class BossCombatTuning
{
    [Header("最终Boss第2阶段触发血量比例")]
    [Range(0.01f, 0.99f)] public float phase2HpPercent = 0.7f;

    [Header("最终Boss第3阶段触发血量比例")]
    [Range(0.01f, 0.99f)] public float phase3HpPercent = 0.4f;

    [Header("最终Boss第1阶段攻击间隔")]
    public float phase1FireInterval = 1.25f;

    [Header("最终Boss第2阶段攻击间隔")]
    public float phase2FireInterval = 1.0f;

    [Header("最终Boss第3阶段攻击间隔")]
    public float phase3FireInterval = 0.82f;

    [Header("最终Boss攻击前预警提前时间")]
    public float warningLeadTime = 0.65f;

    [Header("最终Boss每次攻击后的虚弱窗口时长")]
    public float vulnerableDuration = 0.85f;

    [Header("最终Boss攻击前蓄力闪烁时长")]
    public float chargeFlashDuration = 0.58f;

    [Header("最终Boss阶段切换震屏时长")]
    public float phaseChangeShakeDuration = 0.35f;

    [Header("最终Boss第2阶段切换震屏强度")]
    public float phase2ShakeStrength = 0.22f;

    [Header("最终Boss第3阶段切换震屏强度")]
    public float phase3ShakeStrength = 0.32f;

    [Header("最终Boss第2阶段标题字号")]
    public float phase2TitleSize = 1.05f;

    [Header("最终Boss第3阶段标题字号")]
    public float phase3TitleSize = 1.22f;

    [Header("最终Boss第1阶段暗场强度")]
    [Range(0f, 1f)] public float phase1DarkIntensity = 0.58f;

    [Header("最终Boss第2阶段暗场强度")]
    [Range(0f, 1f)] public float phase2DarkIntensity = 0.66f;

    [Header("最终Boss第3阶段暗场强度")]
    [Range(0f, 1f)] public float phase3DarkIntensity = 0.78f;

    [Header("最终Boss环形预警第1阶段范围")]
    public float circleWarningScalePhase1 = 4.8f;

    [Header("最终Boss环形预警第2阶段范围")]
    public float circleWarningScalePhase2 = 5.8f;

    [Header("最终Boss环形预警第3阶段范围")]
    public float circleWarningScalePhase3 = 6.8f;

    [Header("最终Boss扇形预警第1阶段半径")]
    public float sectorWarningRadiusPhase1 = 6.5f;

    [Header("最终Boss扇形预警第2阶段半径")]
    public float sectorWarningRadiusPhase2 = 7.5f;

    [Header("最终Boss扇形预警第3阶段半径")]
    public float sectorWarningRadiusPhase3 = 8.5f;

    [Header("最终Boss扇形预警第1阶段角度")]
    public float sectorWarningAnglePhase1 = 54f;

    [Header("最终Boss扇形预警第2阶段角度")]
    public float sectorWarningAnglePhase2 = 66f;

    [Header("最终Boss扇形预警第3阶段角度")]
    public float sectorWarningAnglePhase3 = 78f;

    public float GetFireInterval(int phase)
    {
        return phase == 1 ? phase1FireInterval : phase == 2 ? phase2FireInterval : phase3FireInterval;
    }

    public float GetDarkIntensity(int phase)
    {
        return phase == 1 ? phase1DarkIntensity : phase == 2 ? phase2DarkIntensity : phase3DarkIntensity;
    }

    public float GetCircleWarningScale(int phase)
    {
        return phase == 1 ? circleWarningScalePhase1 : phase == 2 ? circleWarningScalePhase2 : circleWarningScalePhase3;
    }

    public float GetSectorWarningRadius(int phase)
    {
        return phase == 1 ? sectorWarningRadiusPhase1 : phase == 2 ? sectorWarningRadiusPhase2 : sectorWarningRadiusPhase3;
    }

    public float GetSectorWarningAngle(int phase)
    {
        return phase == 1 ? sectorWarningAnglePhase1 : phase == 2 ? sectorWarningAnglePhase2 : sectorWarningAnglePhase3;
    }
}

[Serializable]
public class EnemyDeathEffectTuning
{
    [Header("普通怪死亡收缩时长")]
    public float normalDeathDuration = 0.4f;

    [Header("普通怪死亡膨胀倍率")]
    public float normalDeathExpandScale = 1.25f;

    [Header("精英怪死亡蓄力时长")]
    public float eliteChargeDuration = 0.18f;

    [Header("Boss死亡蓄力时长")]
    public float bossChargeDuration = 0.28f;

    [Header("精英怪死亡坍缩时长")]
    public float eliteCollapseDuration = 0.32f;

    [Header("Boss死亡坍缩时长")]
    public float bossCollapseDuration = 0.46f;

    [Header("精英怪死亡自身抖动强度")]
    public float eliteShakeStrength = 0.09f;

    [Header("Boss死亡自身抖动强度")]
    public float bossShakeStrength = 0.18f;

    [Header("精英怪死亡蓄力震屏时长")]
    public float eliteChargeShakeDuration = 0.12f;

    [Header("Boss死亡蓄力震屏时长")]
    public float bossChargeShakeDuration = 0.22f;

    [Header("精英怪死亡蓄力震屏强度")]
    public float eliteChargeShakeStrength = 0.12f;

    [Header("Boss死亡蓄力震屏强度")]
    public float bossChargeShakeStrength = 0.24f;

    [Header("精英怪死亡爆发震屏时长")]
    public float eliteCollapseShakeDuration = 0.18f;

    [Header("Boss死亡爆发震屏时长")]
    public float bossCollapseShakeDuration = 0.34f;

    [Header("精英怪死亡爆发震屏强度")]
    public float eliteCollapseShakeStrength = 0.18f;

    [Header("Boss死亡爆发震屏强度")]
    public float bossCollapseShakeStrength = 0.34f;

    [Header("精英和Boss死亡蓄力膨胀倍率")]
    public float heavyChargeExpandScale = 1.18f;

    [Header("精英和Boss死亡横向压扁倍率")]
    public float heavyWideScaleX = 1.28f;

    [Header("精英和Boss死亡纵向压扁倍率")]
    public float heavyWideScaleY = 0.74f;

    [Header("精英和Boss死亡坍缩末段纵向倍率")]
    public float heavyFinalScaleY = 0.35f;

    [Header("精英怪死亡旋转角度")]
    public float eliteCollapseRotateAngle = 22f;

    [Header("Boss死亡旋转角度")]
    public float bossCollapseRotateAngle = 38f;

    [Header("精英怪死亡冲击波范围")]
    public float eliteDeathPulseScale = 3.2f;

    [Header("Boss死亡冲击波范围")]
    public float bossDeathPulseScale = 5.2f;

    [Header("精英和Boss死亡冲击波时长")]
    public float heavyDeathPulseDuration = 0.35f;

    [Header("最终Boss死亡奖励喷发震屏时长")]
    public float finalBossRewardShakeDuration = 0.55f;

    [Header("最终Boss死亡奖励喷发震屏强度")]
    public float finalBossRewardShakeStrength = 0.45f;
}

[Serializable]
public class EnemyMovementTuning
{
    [Header("是否启用敌人移动思考")]
    public bool enableMovementBrain = true;

    [Header("普通怪最短重新思考间隔")]
    public float normalThinkIntervalMin = 0.75f;

    [Header("普通怪最长重新思考间隔")]
    public float normalThinkIntervalMax = 1.25f;

    [Header("普通怪追击时横向偏移强度")]
    [Range(0f, 1f)] public float normalDriftWeight = 0.18f;

    [Header("血厚怪最短重新思考间隔")]
    public float thickThinkIntervalMin = 0.9f;

    [Header("血厚怪最长重新思考间隔")]
    public float thickThinkIntervalMax = 1.45f;

    [Header("血厚怪期望保持的攻击距离倍率")]
    public float thickDesiredRangeRatio = 0.82f;

    [Header("血厚怪横向压迫移动强度")]
    [Range(0f, 2f)] public float thickStrafeWeight = 0.42f;

    [Header("血厚怪移动速度倍率")]
    public float thickMoveSpeedMultiplier = 0.92f;

    [Header("精英怪最短重新思考间隔")]
    public float eliteThinkIntervalMin = 0.55f;

    [Header("精英怪最长重新思考间隔")]
    public float eliteThinkIntervalMax = 1.0f;

    [Header("精英怪期望保持的攻击距离倍率")]
    public float eliteDesiredRangeRatio = 0.78f;

    [Header("精英怪横向绕行强度")]
    [Range(0f, 2f)] public float eliteStrafeWeight = 0.9f;

    [Header("精英怪重新换位概率")]
    [Range(0f, 1f)] public float eliteRepositionChance = 0.36f;

    [Header("精英怪移动速度倍率")]
    public float eliteMoveSpeedMultiplier = 1.04f;

    [Header("Boss最短重新思考间隔")]
    public float bossThinkIntervalMin = 0.7f;

    [Header("Boss最长重新思考间隔")]
    public float bossThinkIntervalMax = 1.15f;

    [Header("Boss期望保持的攻击距离倍率")]
    public float bossDesiredRangeRatio = 0.88f;

    [Header("Boss横向巡航强度")]
    [Range(0f, 2f)] public float bossStrafeWeight = 0.58f;

    [Header("Boss重新换位概率")]
    [Range(0f, 1f)] public float bossRepositionChance = 0.28f;

    [Header("Boss过近时开始后撤的距离倍率")]
    public float bossBackoffRangeRatio = 0.68f;

    [Header("Boss移动速度倍率")]
    public float bossMoveSpeedMultiplier = 0.78f;

    [Header("每升一个Boss阶段增加的思考频率")]
    public float bossPhaseThinkSpeedBonus = 0.12f;

    [Header("保持距离时允许的距离误差倍率")]
    public float rangeToleranceRatio = 0.12f;

    public float GetThinkInterval(EnemyType enemyType, int bossPhase)
    {
        float min;
        float max;
        if (enemyType == EnemyType.Thick)
        {
            min = thickThinkIntervalMin;
            max = thickThinkIntervalMax;
        }
        else if (enemyType == EnemyType.Elite)
        {
            min = eliteThinkIntervalMin;
            max = eliteThinkIntervalMax;
        }
        else if (enemyType == EnemyType.Boss)
        {
            float phaseBonus = Mathf.Max(0f, bossPhase - 1) * bossPhaseThinkSpeedBonus;
            min = Mathf.Max(0.2f, bossThinkIntervalMin - phaseBonus);
            max = Mathf.Max(min, bossThinkIntervalMax - phaseBonus);
        }
        else
        {
            min = normalThinkIntervalMin;
            max = normalThinkIntervalMax;
        }

        return UnityEngine.Random.Range(Mathf.Max(0.1f, min), Mathf.Max(min, max));
    }

    public float GetDesiredRangeRatio(EnemyType enemyType)
    {
        if (enemyType == EnemyType.Thick)
            return thickDesiredRangeRatio;
        if (enemyType == EnemyType.Elite)
            return eliteDesiredRangeRatio;
        if (enemyType == EnemyType.Boss)
            return bossDesiredRangeRatio;
        return 0.9f;
    }

    public float GetMoveSpeedMultiplier(EnemyType enemyType)
    {
        if (enemyType == EnemyType.Thick)
            return thickMoveSpeedMultiplier;
        if (enemyType == EnemyType.Elite)
            return eliteMoveSpeedMultiplier;
        if (enemyType == EnemyType.Boss)
            return bossMoveSpeedMultiplier;
        return 1f;
    }
}

[Serializable]
public class PlayerTargetingTuning
{
    [Header("是否启用玩家子弹提前量瞄准")]
    public bool enablePredictiveAim = true;

    [Header("目标移动速度参与预测的权重")]
    [Range(0f, 1.5f)] public float targetVelocityWeight = 0.82f;

    [Header("最大提前预测时间")]
    public float maxLeadTime = 0.45f;

    [Header("最小提前预测时间，低于该值不做提前量")]
    public float minLeadTime = 0.03f;

    [Header("目标速度低于该值时不做提前量")]
    public float minTargetSpeedForLead = 0.15f;

    [Header("是否让玩家飞机朝预测点转向")]
    public bool rotateToPredictedPoint = true;
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

public enum UpgradeFeedbackType
{
    None,
    BulletCount,
    HeavyBullet,
    Pierce,
    Power,
    MoveSpeed,
    FireRate,
    Crit,
    Explosion,
    Legendary,
    EnhancedShot
}

[Serializable]
public class UpgradeEffectTuning
{
    [Header("子弹数量升级后的最小子弹数")]
    public int minBulletCount = 1;

    [Header("子弹数量升级后的最大子弹数")]
    public int maxBulletCount = 10;

    [Header("重型弹头额外强化齐射伤害倍率")]
    public float heavyBulletEnhancedShotDamageAdd = 0.08f;

    [Header("重型弹头额外子弹缩放")]
    public float heavyBulletScaleAdd = 0.1f;

    [Header("聚能核心额外强化齐射伤害倍率")]
    public float attackRatioEnhancedShotDamageAdd = 0.12f;

    [Header("游击模式攻击力惩罚")]
    public float moveFastAttackPenalty = 0.2f;

    [Header("重装炮台攻击力增加")]
    public float heavyModeAttackAdd = 1.5f;

    [Header("重装炮台移速减少")]
    public float heavyModeMoveSpeedPenalty = 1f;

    [Header("重装炮台开火间隔减少")]
    public float heavyModeFireIntervalReduce = 0.05f;

    [Header("重装炮台防御增加")]
    public int heavyModeDefenceAdd = 2;

    [Header("精准重炮攻击力增加")]
    public int lowBulletHighDamageAttackAdd = 2;

    [Header("精准重炮子弹缩放增加")]
    public float lowBulletHighDamageBulletScaleAdd = 0.25f;

    [Header("精准重炮强化齐射伤害倍率增加")]
    public float lowBulletHighDamageEnhancedShotDamageAdd = 0.35f;

    [Header("精准重炮选择时震屏时长")]
    public float lowBulletHighDamageShakeDuration = 0.12f;

    [Header("精准重炮选择时震屏强度")]
    public float lowBulletHighDamageShakeStrength = 0.12f;

    [Header("无限火力开火间隔减少")]
    public float legendFireIntervalReduce = 0.15f;

    [Header("强化齐射触发间隔最小值")]
    public int enhancedShotMinInterval = 3;

    [Header("强化齐射每次升级减少的触发间隔")]
    public int enhancedShotIntervalReduce = 1;

    [Header("强化齐射额外穿透上限")]
    public int enhancedShotMaxBonusPierce = 5;

    [Header("强化齐射每次升级增加的额外穿透")]
    public int enhancedShotBonusPierceAdd = 1;

    [Header("强化齐射每次升级增加的子弹缩放")]
    public float enhancedShotScaleAdd = 0.08f;

    public UpgradeFeedbackType GetFeedbackType(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.BulletCount:
                return UpgradeFeedbackType.BulletCount;
            case UpgradeType.HeavyBullet:
            case UpgradeType.LowBulletHighDamage:
                return UpgradeFeedbackType.HeavyBullet;
            case UpgradeType.Pierce:
                return UpgradeFeedbackType.Pierce;
            case UpgradeType.AtkRatio:
            case UpgradeType.HeavyMode:
                return UpgradeFeedbackType.Power;
            case UpgradeType.MoveFast:
                return UpgradeFeedbackType.MoveSpeed;
            case UpgradeType.CritChance:
                return UpgradeFeedbackType.Crit;
            case UpgradeType.CritExplosion:
            case UpgradeType.PierceExplosion:
                return UpgradeFeedbackType.Explosion;
            case UpgradeType.FireRate:
            case UpgradeType.LegendFire:
                return UpgradeFeedbackType.FireRate;
            case UpgradeType.LegendSplit:
                return UpgradeFeedbackType.Legendary;
            case UpgradeType.EnhancedShot:
                return UpgradeFeedbackType.EnhancedShot;
            default:
                return UpgradeFeedbackType.None;
        }
    }
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
