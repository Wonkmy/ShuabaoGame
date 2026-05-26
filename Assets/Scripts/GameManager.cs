// 当前项目的chatgpt聊天对话“CrazyGames 游戏类型分析”
// 具体的游戏设计在聊天对话的这个位置，直接搜索关键句即可：“好，那我们一起讨论细你说的建议”

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameBalanceConfig balanceConfig;
    GameBalanceConfig runtimeDefaultBalanceConfig;

    void Awake()
    {
        Instance = this;
        EnsureBalanceConfig();
        ApplyBalanceConfig();
        ConfigureDebugTools();
    }

    public GameBalanceConfig BalanceConfig
    {
        get
        {
            EnsureBalanceConfig();
            return balanceConfig != null ? balanceConfig : runtimeDefaultBalanceConfig;
        }
    }

    public float GameTime => gameTime;
    public float Difficulty => difficulty;
    public float SpawnWaveInterval => spawnWaveInterval;
    public int EnemyCountPerGroup => enemyCountPerGroup;
    public int CurrentWaveGroupCount => currentWaveGroupCount;
    public float SpecialEventRemainingTime => Mathf.Max(0f, nextSpecialEventInterval - specialEventTimer);
    public TimelineSegment CurrentTimelineSegment => BalanceConfig.GetTimelineSegment(gameTime);

    public string GetBuildSummary()
    {
        if (player == null)
            return "";

        Player playerC = player.GetComponent<Player>();
        if (playerC == null || playerC.buildDict == null || playerC.buildDict.Count == 0)
            return "";

        List<string> entries = new List<string>();
        foreach (KeyValuePair<string, int> pair in playerC.buildDict)
        {
            entries.Add(pair.Key + ":" + pair.Value);
        }

        entries.Sort();
        return string.Join(", ", entries);
    }

    public void RecordLevelUp(int newLevel)
    {
        RunTelemetry.levelUpCount++;
        RunTelemetry.levelUpTimes.Add(gameTime);
        RunTelemetry.levelUpLevels.Add(newLevel);
    }

    public void RecordUpgradeSelected(UpgradeData data)
    {
        RunTelemetry.upgradeSelectCount++;
        RunTelemetry.upgradeSelectTimes.Add(gameTime);
        RunTelemetry.upgradeSelectNames.Add(data.name);
    }

    public void RecordEnemyKilled(EnemyType enemyType)
    {
        RunTelemetry.killByEnemyType[(int)enemyType]++;
    }

    public void RecordExpSpawned(int expValue)
    {
        RunTelemetry.expBallSpawned++;
        RunTelemetry.expValueSpawned += expValue;
    }

    public void RecordExpCollected(int expValue)
    {
        RunTelemetry.expBallCollected++;
        RunTelemetry.expValueCollected += expValue;
    }

    public void RecordCoinSpawned(int coinValue)
    {
        RunTelemetry.coinSpawned++;
        RunTelemetry.coinValueSpawned += coinValue;
    }

    public void RecordCoinCollected(int coinValue)
    {
        RunTelemetry.coinCollected++;
        RunTelemetry.coinValueCollected += coinValue;
    }

    public void RecordChestSpawned()
    {
        RunTelemetry.chestSpawned++;
    }

    public void RecordChestCollected()
    {
        RunTelemetry.chestCollected++;
    }

    public void RecordPlayerDamageTaken(int damage)
    {
        RunTelemetry.totalDamageTaken += damage;
        RunTelemetry.lastDamageTaken = damage;
    }

    public void RecordDebugFlowJump(string stageLabel, FlowJumpMode mode)
    {
        RunTelemetry.debugJumpUsed = true;
        RunTelemetry.debugJumpStage = stageLabel;
        RunTelemetry.debugJumpMode = mode.ToString();
        RunTelemetry.debugJumpTime = gameTime;
    }

    public void RecordChapterEvent(string chapterName, EnemyType enemyType)
    {
        RunTelemetry.chapterEventCount++;
        RunTelemetry.chapterEventTimes.Add(gameTime);
        RunTelemetry.chapterEventNames.Add(chapterName);
        RunTelemetry.chapterEnemyTypes.Add(enemyType.ToString());
    }

    public Coroutine StartRuntimeCoroutine(IEnumerator routine)
    {
        if (routine == null)
            return null;

        Coroutine coroutine = null;
        coroutine = StartCoroutine(TrackRuntimeCoroutine(routine, () => runtimeCoroutines.Remove(coroutine)));
        runtimeCoroutines.Add(coroutine);
        return coroutine;
    }

    IEnumerator TrackRuntimeCoroutine(IEnumerator routine, System.Action onComplete)
    {
        yield return routine;
        onComplete?.Invoke();
    }

    void StopRuntimeCoroutines()
    {
        for (int i = runtimeCoroutines.Count - 1; i >= 0; i--)
        {
            if (runtimeCoroutines[i] != null)
            {
                StopCoroutine(runtimeCoroutines[i]);
            }
        }

        runtimeCoroutines.Clear();
    }

    public void DebugJumpToFlowStage(int stageIndex, FlowJumpMode mode)
    {
        if (!IsGameStarted || player == null)
            return;

        FlowStageSnapshot snapshot = GetFlowStageSnapshot(stageIndex);
        TimelineSegment segment = GetTimelineSegmentByIndex(stageIndex);
        if (snapshot == null && segment == null)
            return;

        float targetTime = snapshot != null ? snapshot.targetTime : segment.startTime;
        gameTime = Mathf.Max(0f, targetTime);
        SyncChapterEventProgress();
        difficultyUpdateTimer = difficultyUpdateInterval;
        ApplyDifficultyFromCurrentTime();

        if (mode == FlowJumpMode.FlowSnapshot && snapshot != null)
        {
            ApplyFlowSnapshot(snapshot);
        }

        UpdateDynamicDifficulty(out _);
        RecordDebugFlowJump(segment != null ? segment.label : "Stage " + stageIndex, mode);
        Debug.Log("Flow stage jump: " + mode + " -> " + (segment != null ? segment.label : stageIndex.ToString()));
    }

    FlowStageSnapshot GetFlowStageSnapshot(int stageIndex)
    {
        FlowStageSnapshot[] snapshots = BalanceConfig.debug.flowStageSnapshots;
        if (snapshots == null)
            return null;

        for (int i = 0; i < snapshots.Length; i++)
        {
            if (snapshots[i] != null && snapshots[i].stageIndex == stageIndex)
                return snapshots[i];
        }

        return stageIndex >= 0 && stageIndex < snapshots.Length ? snapshots[stageIndex] : null;
    }

    TimelineSegment GetTimelineSegmentByIndex(int stageIndex)
    {
        TimelineSegment[] timeline = BalanceConfig.timeline;
        if (timeline == null || stageIndex < 0 || stageIndex >= timeline.Length)
            return null;

        return timeline[stageIndex];
    }

    void ApplyFlowSnapshot(FlowStageSnapshot snapshot)
    {
        Player playerC = player.GetComponent<Player>();
        playerC.DebugSetProgression(snapshot.playerLevel, snapshot.expRatio, snapshot.hpRatio);

        playerC.HasCritExplosion = false;
        playerC.HasPierceExplosion = false;
        playerC.HasLegendSplit = false;
        playerC.HasLowBulletHighDamage = false;
        playerC.HasNuclearBuild = false;
        playerC.HasSplitBuild = false;
        playerC.HasFireBuild = false;
        playerC.buildDict.Clear();

        if (snapshot.upgradeIds != null)
        {
            for (int i = 0; i < snapshot.upgradeIds.Length; i++)
            {
                DebugApplyUpgradeById(snapshot.upgradeIds[i]);
            }
        }

        playerC.CheckBuildCombo();
        isWave = false;
        IsSpecialEvent = false;
        waveTimer = Mathf.Repeat(gameTime, Mathf.Max(1f, waveAppearInterval));
        specialEventTimer = Mathf.Max(0f, nextSpecialEventInterval - snapshot.secondsBeforeSpecialEvent);
    }

    void DebugApplyUpgradeById(int upgradeId)
    {
        UpgradeData data = default;
        bool found = false;
        for (int i = 0; i < DataManager.upgradeList.Count; i++)
        {
            if (DataManager.upgradeList[i].id == upgradeId)
            {
                data = DataManager.upgradeList[i];
                found = true;
                break;
            }
        }

        if (!found)
            return;

        ExecuteUpgrade(data);
        Player playerC = player.GetComponent<Player>();
        if (!playerC.buildDict.ContainsKey(data.tag))
        {
            playerC.buildDict.Add(data.tag, 0);
        }

        playerC.buildDict[data.tag]++;
    }

    void ApplyDifficultyFromCurrentTime()
    {
        DynamicDifficultyTuning dynamicTuning = BalanceConfig.dynamicDifficulty;
        difficulty = Mathf.Clamp(
            dynamicTuning.difficultyBase + Mathf.FloorToInt(gameTime / dynamicTuning.difficultyStepSeconds),
            dynamicTuning.minDifficulty,
            dynamicTuning.maxDifficulty);
    }

    void SyncChapterEventProgress()
    {
        CombatChapterTuning chapterTuning = BalanceConfig.chapter;
        nextMiniBossChapterIndex = 0;
        finalBossChapterTriggered = false;

        if (chapterTuning == null)
            return;

        if (chapterTuning.miniBossTimes != null)
        {
            for (int i = 0; i < chapterTuning.miniBossTimes.Length; i++)
            {
                if (gameTime >= chapterTuning.miniBossTimes[i])
                {
                    nextMiniBossChapterIndex = i + 1;
                }
            }
        }

        finalBossChapterTriggered = gameTime >= chapterTuning.finalBossTime;
    }

    void EnsureBalanceConfig()
    {
        if (balanceConfig == null && runtimeDefaultBalanceConfig == null)
        {
            runtimeDefaultBalanceConfig = ScriptableObject.CreateInstance<GameBalanceConfig>();
        }

        if (balanceConfig != null)
        {
            balanceConfig.EnsureNestedConfigs();
        }

        if (runtimeDefaultBalanceConfig != null)
        {
            runtimeDefaultBalanceConfig.EnsureNestedConfigs();
        }
    }

    void ApplyBalanceConfig()
    {
        GameBalanceConfig config = BalanceConfig;
        spawnWaveIntervalBase = config.wave.spawnWaveIntervalBase;
        normalGroupMin = config.wave.normalGroupMin;
        normalGroupMax = config.wave.normalGroupMax;
        normalEnemyMin = config.wave.normalEnemyMin;
        normalEnemyMax = config.wave.normalEnemyMax;
        waveEnemyMultiplier = config.wave.waveEnemyMultiplier;
        waveAppearInterval = config.wave.waveAppearInterval;
        waveDuration = config.wave.waveDuration;
        MaxEnemyCount = config.wave.maxEnemyCount;
        spawnWaveInterval = config.wave.spawnWaveIntervalBase;
        enemyCountPerGroup = config.wave.initialEnemyCountPerGroup;
        difficultyUpdateInterval = config.dynamicDifficulty.updateInterval;
    }

    void ConfigureDebugTools()
    {
        if (BalanceConfig.debug.showDebugHud && GetComponent<GameDebugHUD>() == null)
        {
            gameObject.AddComponent<GameDebugHUD>();
        }

        if (BalanceConfig.debug.enableFlowStageJump && GetComponent<FlowStageDebugController>() == null)
        {
            gameObject.AddComponent<FlowStageDebugController>();
        }
    }

    void ConfigureSpaceBackground()
    {
        if (mainCamera != null && mainCamera.GetComponent<SpaceBackgroundController>() == null)
        {
            mainCamera.gameObject.AddComponent<SpaceBackgroundController>();
        }
    }

    public GameObject player { get; private set; }
    public Transform playerExpSlider { get; private set; }
    public Transform playerHpSlider { get; private set; }

    public Transform gameLoadingSlider { get; set; }
    int currentLoadStep = 0;
    int totalLoadStep = 5;
    public PlayerData pdata { get; set; }

    public bool RunningGame;

    public GameObject levelPanel;
    public GameObject gameOverPanel;
    public GameObject cultivatePanel;

    public bool GameOver { get; set; }
    public bool IsGameStarted { get; set; }
    bool runReportWritten = false;
    public RunTelemetry RunTelemetry { get; private set; } = new RunTelemetry();
    readonly List<Coroutine> runtimeCoroutines = new List<Coroutine>();

    // 技能释放相关
    public Button btn_ReleaseSkill;
    public Text coolDownLabel;
    public Image coolDownMask;// 技能冷却遮罩，随着冷却时间减少逐渐显示
    float skillCooldownTimer = 0;// 技能冷却计时器
    float totalSkillCooldownTime = 0;// 技能总冷却时间，根据玩家类型从DataManager.playerSkillTypeCDDict获取
    bool canUseSkill = true;
    // =========================
    // 数值配置区
    // =========================
    [Header("波次配置")]
    public float spawnWaveIntervalBase = 5.5f;
    public int normalGroupMin = 1;
    public int normalGroupMax = 2;
    public int normalEnemyMin = 3;
    public int normalEnemyMax = 7;
    public int waveEnemyMultiplier = 2;

    [Header("尸潮配置")]
    public float waveAppearInterval = 35f;
    public float waveDuration = 7f;

    // 当前这一轮特殊事件需要等待的时间
    public float nextSpecialEventInterval { get; set; }
    public int MaxEnemyCount = 36;// 场上最大敌人数量，超过后不再刷怪，直到数量降低
    [Header("玩家配置")]
    public float dashCooldownTime = 1.2f;
    public float dashSpeed = 18f;
    public float dashDuration = 0.12f;

    // =========================
    // 波次刷怪系统
    // =========================

    // 波次计时器
    float spawnWaveTimer = 0;

    // 波次间隔
    float spawnWaveInterval = 5.5f;// 当前动态波次间隔

    // 每组怪物数量
    int enemyCountPerGroup = 4;

    // 当前波次组数量
    int currentWaveGroupCount = 1;
    // 游戏时间
    float gameTime = 0;
    // =========================
    // 动态难度系统
    // =========================

    // 玩家战力评分
    public int playerPowerScore = 0;

    // 难度刷新计时器
    float difficultyUpdateTimer = 0;

    // 多久重新计算一次难度
    float difficultyUpdateInterval = 10f;
    // 当前难度
    float difficulty = 1;
    // 尸潮相关
    public bool isWave { get; set; }
    float waveTimer = 0;
    int maxSpawnPerFrame = 5;
    int safeSide = 0;// 尸潮来袭时的安全区，0左1右2下3上

    // 敌人血量和攻击力的动态调整系数，初始为1，随着难度增加而增加
    public float currentEnemyHpFactor = 1f;
    public float currentEnemyAtkFactor = 1f;
    // 相机相关
    public Camera mainCamera { get; set; }
    public CameraEffect cameraEffect { get; set; }
    private Vector3 cameraOriginPos;
    // 震屏幕相关
    private float shakeTime = 0;
    private float shakeDuration = 0;
    private float shakeStrength = 0;
    // 敌人生成到屏幕外的偏移距离
    private float offset = 100f;
    // 命中顿帧效果相关
    public float HitStopDuration { get; set; }
    public float HitStopIntensity { get; set; }

    // 特殊事件相关
    private float specialEventTimer = 0;
    public bool IsSpecialEvent { get; set; }// 是否正在进行特殊事件
    private EnemyType[] enemyTypes;// 特殊事件专用的敌人类型：精英、Boss
    int nextMiniBossChapterIndex = 0;
    bool finalBossChapterTriggered = false;

    // 时间停止相关
    public bool IsTimeStop = false;
    float timeStopTimer = 0;// 剩余时间

    // 黑洞技能相关
    public bool IsBlackHole = false;
    public Vector3 BlackHolePos;
    float blackHoleTimer = 0;

    Coroutine gameStepCoroutine;

    public Transform startPos;
    public Transform middlePos;
    public Transform endPos;

    public GameObject dash_slider;
    private void Start()
    {
        if (RunningGame)
        {
            gameStepCoroutine = StartCoroutine(LoadGameStep());
        }
    }

    IEnumerator LoadGameStep()
    {
        gameLoadingSlider = Instantiate(Resources.Load<GameObject>("gameLoadingSlider")).transform;
        gameLoadingSlider.transform.position = new Vector3(-4, -9.25f, 0);
        // 打开局外金币永久升级（暂时只实现攻击力、血量、移速和暴击）
        NextLoadStep();
        yield return new WaitForSeconds(0.025f);
        DataManager.Init();
        NextLoadStep();
        yield return new WaitForSeconds(0.2f);// 由于DataManager.Init()数据量大，所以停留了较长时间，之后每一步停留0.025秒

        //LoadDefaultUpgradeConfig();// 加载默认构筑配置
        NextLoadStep();
        yield return new WaitForSeconds(0.025f);

        SpwanExpAndHpBar();// 生成经验和血条
        NextLoadStep();
        yield return new WaitForSeconds(0.025f);

        // 特殊事件的敌人类型，目前是精英和Boss，可以根据需要增加
        enemyTypes = new EnemyType[2];
        enemyTypes[0] = EnemyType.Elite;
        enemyTypes[1] = EnemyType.Boss;
        NextLoadStep();
        yield return new WaitForSeconds(0.025f);
        gameLoadingSlider.gameObject.SetActive(false);
        StartCoroutine(Init()); // 初始化游戏
    }
    void UpdateLoading(float progress)
    {
        gameLoadingSlider.Find("slider").localScale = new Vector3(progress, 1, 1);
    }
    void NextLoadStep()
    {
        currentLoadStep++;

        float progress = (float)currentLoadStep / totalLoadStep;

        UpdateLoading(progress);
    }
    void SpwanExpAndHpBar()
    {
        GameObject expobj = Instantiate(Resources.Load<GameObject>("exp"));
        GameObject hpobj = Instantiate(Resources.Load<GameObject>("hp"));

        expobj.SetActive(false);
        hpobj.SetActive(false);

        playerExpSlider = expobj.transform;
        playerHpSlider = hpobj.transform;
    }
    IEnumerator Init()
    {
        yield return new WaitForSeconds(0.6f);
        ShowCultivatePanel(true);
        yield return new WaitUntil(()=> CultivatePanelActive() == false);
        btn_ReleaseSkill.gameObject.SetActive(true);
        coolDownMask.fillAmount = 0;
        GameOver = false;
        IsGameStarted = true;
        runReportWritten = false;
        RunTelemetry = new RunTelemetry();
        nextMiniBossChapterIndex = 0;
        finalBossChapterTriggered = false;
        // 基础难度固定
        difficulty = BalanceConfig.dynamicDifficulty.initialDifficulty;
        // 当前特殊事件等待时间
        nextSpecialEventInterval = BalanceConfig.specialEvent.firstSpecialEventInterval;
        mainCamera = Camera.main;
        cameraEffect = mainCamera.GetComponent<CameraEffect>();
        cameraOriginPos = mainCamera.transform.localPosition;
        ConfigureSpaceBackground();
        yield return new WaitForSeconds(0.1f);
        // 生成玩家
        GenPlayer();
        yield return new WaitForSeconds(0.1f);
        // 显示经验和血条
        playerExpSlider.gameObject.SetActive(true);
        playerHpSlider.gameObject.SetActive(true);

        // 根据玩家类型配置技能冷却时间
        try
        {
            totalSkillCooldownTime = DataManager.playerSkillTypeCDDict[GetPlayer().playerType].skillCD;
            btn_ReleaseSkill.GetComponent<Image>().sprite = Resources.Load<Sprite>("sprites/" + DataManager.playerSkillTypeCDDict[GetPlayer().playerType].iconString);
            skillCooldownTimer = totalSkillCooldownTime;
        }
        catch (System.Exception)
        {
        }

        // 技能按钮监听
        btn_ReleaseSkill.onClick.AddListener(() => {
            UseSkill();
        });
        yield return new WaitForSeconds(0.1f);
        // 播放BGM
        AudioManager.instance.PlayBGM("main");
    }

    public bool LevelUpPanelActive()
    {
        return levelPanel.activeSelf;
    }
    public void ShowLevelUpPanel(bool show)
    {
        levelPanel.SetActive(show);
        if(show == true)
        {
            levelPanel.GetComponent<ChooseOnePanel>().Init();
        }
        else
        {
            levelPanel.GetComponent<ChooseOnePanel>().Dispose();
        }
            
        Time.timeScale = show == true ? 0 : 1;
    }

    public void ShowGameOverPanel(bool show)
    {
        gameOverPanel.SetActive(show);
        
        if (show == true)
        {
            Player playerC = player.GetComponent<Player>();
            // 将存活时长、击杀数、最高难度、玩家等级等数据传递给结算界面
            gameOverPanel.GetComponent<GameOverPanel>().Init(gameTime, playerC.KilledCount, difficulty, playerC.GetCurrentLevel());

            TryWriteRunReport(playerC);

            DataManager.Clear();
        }
        else
        {
            gameOverPanel.GetComponent<GameOverPanel>().Dispose();
        }
    }

    void TryWriteRunReport(Player playerC)
    {
        if (runReportWritten || !BalanceConfig.debug.writeRunReport)
            return;

        runReportWritten = true;
        RunReportLogger.Write(this, playerC);
    }
    public bool CultivatePanelActive()
    {
        return cultivatePanel.activeSelf || cultivatePanel == null;
    }
    public void ShowCultivatePanel(bool show)
    {
        cultivatePanel.SetActive(show);
        if (show == true)
        {
            cultivatePanel.GetComponent<CultivatePanel>().Init();
        }
        else
        {
            cultivatePanel.GetComponent<CultivatePanel>().Dispose();
        }
    }

    public Player GetPlayer()
    {
        if(player!= null)
        {
            return player.GetComponent<Player>();
        }
        return null;
    }
    void RevivalGame()
    {
        StopRuntimeCoroutines();
        // 玩家满血复活，重置尸潮、难度、预算、游戏时间等所有数据，但不清理敌人和子弹，玩家已有的构筑列表buildDict、HasCritExplosion、HasPierceExplosion、HasLowBulletHighDamage等保持不变
        Player playerConponent = player.GetComponent<Player>();
        playerConponent.FilledTotalHp();
        playerConponent.HasCritExplosion = false;
        playerConponent.HasPierceExplosion = false;
        playerConponent.HasLowBulletHighDamage = false;
        playerConponent.buildDict.Clear();
        // 重置尸潮、难度、预算、游戏时间等所有数据
        isWave = false;
        difficulty = BalanceConfig.dynamicDifficulty.initialDifficulty;
        gameTime = 0;
        HitStopDuration = 0;
        HitStopIntensity = 0;
        safeSide = 0;
        waveTimer = 0;
        // 波次计时器
        spawnWaveTimer = 0;
        // 波次间隔
        spawnWaveInterval = spawnWaveIntervalBase;
        // 每组怪物数量
        enemyCountPerGroup = BalanceConfig.wave.initialEnemyCountPerGroup;
        // 当前波次组数量
        currentWaveGroupCount = 1;
        // 特殊事件相关
        IsSpecialEvent = false;
        specialEventTimer = 0;
        nextMiniBossChapterIndex = 0;
        finalBossChapterTriggered = false;

        cameraEffect.intensity = 0;
        mainCamera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
        GameOver = false;
        IsGameStarted = true;
    }
    void ResetAllGameDatas()
    {
        StopRuntimeCoroutines();
        // 清理所有敌人和子弹、数据、字典、玩家已有的构筑列表buildDict、HasCritExplosion、HasPierceExplosion、HasLowBulletHighDamage
        
        WeaponSystem.Clear();
            // 重置尸潮、难度、预算、游戏时间等所有数据
            isWave = false;
            difficulty = BalanceConfig.dynamicDifficulty.initialDifficulty;
        gameTime = 0;
        HitStopDuration = 0;
        HitStopIntensity = 0;
        safeSide = 0;
        waveTimer = 0;
        // 波次计时器
        spawnWaveTimer = 0;
        // 波次间隔
        spawnWaveInterval = spawnWaveIntervalBase;
            // 每组怪物数量
            enemyCountPerGroup = BalanceConfig.wave.initialEnemyCountPerGroup;
        // 当前波次组数量
        currentWaveGroupCount = 1;
        // 特殊事件相关
        IsSpecialEvent = false;
        specialEventTimer = 0;
        nextMiniBossChapterIndex = 0;
        finalBossChapterTriggered = false;

        // 隐藏经验和血条
        playerExpSlider.gameObject.SetActive(false);
        playerHpSlider.gameObject.SetActive(false);
        dash_slider.GetComponent<Image>().fillAmount = 0;
        dash_slider.SetActive(false);

        cameraEffect.intensity = 0;
        mainCamera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);

        Destroy(player);
        player = null;
        AudioManager.instance.StopAllBGM();
    }
    public void RestartGame()
    {
        ResetAllGameDatas();
        ShowGameOverPanel(false);
        StartCoroutine(Init()); 
    }
    public void Revival()
    {
        RevivalGame();
        ShowGameOverPanel(false);
    }
    private void Update()
    {
        if (GameOver)
        {
            if (player && playerHpSlider != null)
            {
                playerHpSlider.Find("slider").localScale = new Vector3(player.GetComponent<Player>().GetHpProgress(), 1, 1);
            } 
            return;
        }
        if(IsGameStarted == false)
        {
            return;
        }
        if (HitStopIntensity > 0)
        {
            HitStopIntensity -= Time.deltaTime;
        }
        if (HitStopDuration > 0)
        {
            HitStopDuration -= Time.deltaTime;
            return;
        }
        if (player)
        {
            player.GetComponent<Player>().PlayerUpdate();
            if (playerExpSlider != null)
            {
                Vector3 spos = new Vector3(50, Screen.height - 50, 0);
                Vector3 wpos = mainCamera.ScreenToWorldPoint(spos);
                wpos.z = 0;
                playerExpSlider.transform.position = wpos;

                playerExpSlider.Find("slider").localScale = new Vector3(player.GetComponent<Player>().GetExpProgress(), 1, 1);
            }
            if (playerHpSlider != null)
            {
                Vector3 spos = new Vector3(50, Screen.height - 150, 0);
                Vector3 wpos = mainCamera.ScreenToWorldPoint(spos);
                wpos.z = 0;
                playerHpSlider.transform.position = wpos;

                playerHpSlider.Find("slider").localScale = new Vector3(player.GetComponent<Player>().GetHpProgress(), 1, 1);
            }

            // 在这里进行玩家技能的冷却时间计算与更新
            if (skillCooldownTimer > 0 && canUseSkill == false)
            {
                skillCooldownTimer -= Time.deltaTime;
                coolDownMask.fillAmount = skillCooldownTimer / totalSkillCooldownTime;
                coolDownLabel.text = skillCooldownTimer.ToString("F1") + "s";
                if (skillCooldownTimer < 0)
                {
                    skillCooldownTimer = 0;
                    canUseSkill = true;
                    btn_ReleaseSkill.interactable = true;
                    coolDownLabel.text = "";
                }
            }
        }
        // 敌人的更新
        if(DataManager.allEnemyDict.Count > 0)
        {
            float nearest = float.MaxValue;
            for (int i = DataManager.allEnemyDict.Count - 1; i >= 0; i--)
            {
                GameObject enemy = DataManager.allEnemyDict[i];
                if (enemy == null) continue;
                float d = Vector3.Distance(player.transform.position, enemy.transform.position);
                nearest = Mathf.Min(nearest, d);

                enemy.GetComponent<Enemy>().EnemyUpdate();
            }
            float pressure = Mathf.InverseLerp(6f, 1.5f, nearest);
            cameraEffect.intensity = Mathf.Max(cameraEffect.intensity, pressure * 0.35f);
        }
        

        for (int i = DataManager.allDamageText.Count - 1; i >= 0; i--)
        {
            DamageText damageText = DataManager.allDamageText[i].GetComponent<DamageText>();
            if (damageText.Dead)
            {
                Destroy(DataManager.allDamageText[i]);// 销毁对象
                DataManager.allDamageText.RemoveAt(i);// 从列表中移除
            }
            else
            {
                if (DataManager.allDamageText[i] != null)
                {
                    DataManager.allDamageText[i].GetComponent<DamageText>().DamageTextUpdate();
                }
            }
        }

        for (int i = DataManager.allExpBall.Count - 1; i >= 0; i--)
        {
            DataManager.allExpBall[i].GetComponent<ExpBall>().ExpBallUpdate();
        }

        WeaponSystem.UpdateWeapons();

        // 游戏时间累计
        gameTime += Time.deltaTime;

        // 动态难度刷新
        difficultyUpdateTimer += Time.deltaTime;
        int playerScore = 0;
        if (difficultyUpdateTimer >= difficultyUpdateInterval)
        {
            difficultyUpdateTimer = 0;
            UpdateDynamicDifficulty(out playerScore);
        }

        DynamicDifficultyTuning dynamicTuning = BalanceConfig.dynamicDifficulty;
        difficulty = Mathf.Clamp(
            dynamicTuning.difficultyBase + Mathf.FloorToInt(gameTime / dynamicTuning.difficultyStepSeconds),
            dynamicTuning.minDifficulty,
            dynamicTuning.maxDifficulty);

        // 特殊事件逻辑
        specialEventTimer += Time.deltaTime;
        UpdateChapterEvents();

        // 时间到了并且当前没有特殊事件
        if (!IsSpecialEvent && specialEventTimer >= nextSpecialEventInterval)
        {
            specialEventTimer = 0;

            IsSpecialEvent = true;

            EnemyType enemyType = enemyTypes[Random.Range(0, enemyTypes.Length)];
            StartRuntimeCoroutine(SpawnSpecialEnemy(enemyType));

            cameraEffect.darkIntensity = 0.45f;

            //var specialEventObj = SpwanWorldTxt($"{enemyType.ToString()}来袭！",1.0f);
            //StartCoroutine(ShowFlashWarningTxt(specialEventObj));
            Debug.Log("特殊事件开始");
        }

        // 只有非特殊事件时
        // 才正常刷怪
        if (!IsSpecialEvent)
        {
            // 尸潮逻辑
            UpdateWave();

            // 波次刷怪
            UpdateSpawnWave();
        }

        // 时间停止计时
        if (IsTimeStop)
        {
            timeStopTimer -= Time.deltaTime;

            if (timeStopTimer <= 0)
            {
                timeStopTimer = 0;

                IsTimeStop = false;
            }
        }


        // 黑洞停止计时
        if (IsBlackHole)
        {
            blackHoleTimer -= Time.deltaTime;

            if (blackHoleTimer <= 0)
            {
                blackHoleTimer = 0;

                IsBlackHole = false;
            }
        }

        // 绘制网格
        DrawGrid();

        // 震屏逻辑
        if (shakeTime > 0)
        {
            shakeTime -= Time.deltaTime;

            // 越接近结束震动越弱
            float power = shakeTime / shakeDuration;

            Vector3 offset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0
            ) * shakeStrength * power;

            mainCamera.transform.localPosition = cameraOriginPos + offset;

            // 结束后恢复
            if (shakeTime <= 0)
            {
                mainCamera.transform.localPosition = cameraOriginPos;
            }
        }

        // 测试代码
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Time.timeScale = 1;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Time.timeScale = 2;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Time.timeScale = 5;
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            player.GetComponent<Player>().AddExp(100);
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            isWave = false;
            waveTimer = 15;
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            player.GetComponent<Player>().IsInvincible = true;
        }
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.F1))
        {
            Player p = player.GetComponent<Player>();
            p.SetCurrentLevel(10);

            p.playerData.Atk += 5;

            p.AddKilledCount(500);
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            player.GetComponent<Player>().TakeDamage(9999, false);   
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            ShowLevelUpPanel(true);
        }
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.B))
        {
            finalBossChapterTriggered = true;
            TriggerChapterEvent(BalanceConfig.chapter.finalBossTitle, EnemyType.Boss);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            // 直接进入特殊事件
            specialEventTimer = nextSpecialEventInterval;
        }
    }


    void UpdateChapterEvents()
    {
        CombatChapterTuning chapterTuning = BalanceConfig.chapter;
        if (chapterTuning == null || !chapterTuning.enableChapterEvents || IsSpecialEvent)
            return;

        if (!finalBossChapterTriggered && gameTime >= chapterTuning.finalBossTime)
        {
            finalBossChapterTriggered = true;
            TriggerChapterEvent(chapterTuning.finalBossTitle, EnemyType.Boss);
            return;
        }

        float[] miniBossTimes = chapterTuning.miniBossTimes;
        if (miniBossTimes == null)
            return;

        if (nextMiniBossChapterIndex < miniBossTimes.Length && gameTime >= miniBossTimes[nextMiniBossChapterIndex])
        {
            nextMiniBossChapterIndex++;
            TriggerChapterEvent(chapterTuning.miniBossTitle, EnemyType.Elite);
        }
    }

    void TriggerChapterEvent(string chapterName, EnemyType enemyType)
    {
        CombatChapterTuning chapterTuning = BalanceConfig.chapter;
        specialEventTimer = 0f;
        IsSpecialEvent = true;
        cameraEffect.darkIntensity = chapterTuning.darkIntensity;
        ShakeMainCamera(chapterTuning.cameraShakeDuration, chapterTuning.cameraShakeStrength);
        RecordChapterEvent(chapterName, enemyType);
        StartRuntimeCoroutine(SpawnChapterSpecialEnemy(chapterName, enemyType));
    }

    IEnumerator SpawnChapterSpecialEnemy(string chapterName, EnemyType enemyType)
    {
        CombatChapterTuning chapterTuning = BalanceConfig.chapter;
        GameObject title = SpwanWorldTxt(chapterName, chapterTuning.chapterTitleSize);
        StartRuntimeCoroutine(ShowFlashWarningTxt(title));

        Vector3 centerPos = GetEnemyGroupCenter();
        GameObject centerObj = new GameObject("ChapterEnemyGroupCenter");
        centerObj.transform.position = centerPos;
        GameObject warning = ShowWarning(centerObj.transform, enemyType == EnemyType.Boss ? "warning_boss" : "warning");

        yield return new WaitForSeconds(chapterTuning.warningDelay);

        Destroy(warning);
        Destroy(centerObj);
        StartRuntimeCoroutine(SpawnSpecialEnemy(enemyType, enemyType == EnemyType.Boss));
    }



    void UseSkill()
    {
        if(player == null)
        {
            return;
        }
        if(canUseSkill == false)
        {
            Debug.Log("技能冷却中...");
            return;
        }
        canUseSkill = false;
        btn_ReleaseSkill.interactable = false;
        skillCooldownTimer = totalSkillCooldownTime;
        coolDownMask.fillAmount = 1;
        coolDownLabel.text = skillCooldownTimer.ToString("F1") + "s";
        switch (player.GetComponent<Player>().playerType)
        {
            case AirplaneType.Normal:
                ExecuteUnstoppable();
                break;
            case AirplaneType.BlackHole:
                ExecuteBlackHole(transform.position);
                break;
            case AirplaneType.TimeStop:
                ExecuteTimeStop();
                break;
            case AirplaneType.Rage:
                ExecuteNuke();
                break;
        }
    }
    /// <summary>
    /// 无敌技能，持续5秒，期间玩家不会受到任何伤害
    /// </summary>
    public void ExecuteUnstoppable()
    {
        player.GetComponent<Player>().IsInvincible = true;
        StartRuntimeCoroutine(ResetUnstoppable());
    }
    IEnumerator ResetUnstoppable()
    {
        yield return new WaitForSeconds(5f);
        player.GetComponent<Player>().IsInvincible = false;
    }
    /// <summary>
    /// 黑洞启动
    /// </summary>
    /// <param name="pos"></param>
    public void ExecuteBlackHole(Vector3 pos)
    {
        IsBlackHole = true;

        BlackHolePos = pos;

        blackHoleTimer = 4f;

        Debug.Log("黑洞启动");
    }

    /// <summary>
    /// 时间停止
    /// </summary>
    /// <param name="duration"></param>
    public void ExecuteTimeStop(float duration = 3f)
    {
        IsTimeStop = true;

        timeStopTimer = duration;

        Debug.Log("时间停止");
    }

    /// <summary>
    /// 核爆，清屏技能，直接秒杀所有非Boss/精英敌人，Boss/精英敌人伤害9999
    /// </summary>
    public void ExecuteNuke()
    {
        ShakeMainCamera(0.6f, 0.5f);

        StartRuntimeCoroutine(NukeEffect());

        for (int i = DataManager.allEnemyDict.Count - 1; i >= 0; i--)
        {
            if (DataManager.allEnemyDict[i] == null)
                continue;

            Enemy enemy = DataManager.allEnemyDict[i].GetComponent<Enemy>();

            // Boss/精英
            if (enemy.IsSpecialEnemy)
            {
                enemy.TakeDamage(600, false);
            }
            else
            {
                enemy.TakeDamage(999999, false);
            }
        }
    }

    IEnumerator NukeEffect()
    {
        mainCamera.backgroundColor = Color.white;
        yield return new WaitForSeconds(0.08f);
        mainCamera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
        mainCamera.backgroundColor = Color.white;
        yield return new WaitForSeconds(0.08f);
        mainCamera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
    }
    public float CalculateDynamicSpecialEventInterval()
    {
        Player playerC = player.GetComponent<Player>();

        int pressureScore = 0;

        // 当前敌人数量占上限的比例
        SpecialEventTuning specialTuning = BalanceConfig.specialEvent;

        float enemyPressure = (float)DataManager.allEnemyDict.Count / MaxEnemyCount;

        pressureScore += Mathf.FloorToInt(enemyPressure * specialTuning.enemyPressureScore);

        // 血量越低，压力越高
        pressureScore += Mathf.FloorToInt((1f - playerC.GetHpProgress()) * specialTuning.lowHpPressureScore);

        if (isWave)
        {
            pressureScore += specialTuning.wavePressureBonus;
        }

        // Boss/精英事件期间不用算，但保险
        if (IsSpecialEvent)
        {
            pressureScore += specialTuning.specialEventPressureBonus;
        }

        // 压力高，延后特殊事件
        if (pressureScore >= specialTuning.highPressureThreshold)
        {
            return specialTuning.highPressureInterval;
        }
        else if (pressureScore >= specialTuning.midPressureThreshold)
        {
            return specialTuning.midPressureInterval;
        }
        else if (pressureScore >= specialTuning.lowPressureThreshold)
        {
            return specialTuning.lowPressureInterval;
        }
        else
        {
            return specialTuning.calmInterval;
        }
    }
    void UpdateDynamicDifficulty(out int playerScore)
    {
        Player player = GameManager.Instance.player.GetComponent<Player>();

        // =========================
        // 计算玩家战力
        // =========================

        DynamicDifficultyTuning dynamicTuning = BalanceConfig.dynamicDifficulty;

        int levelScore = Mathf.Max(0, player.GetCurrentLevel() - 1) * dynamicTuning.levelScorePerLevel;

        int killScore = player.KilledCount / Mathf.Max(1, dynamicTuning.killScoreDivisor);

        int buildScore = player.buildDict.Count * dynamicTuning.buildScorePerTag;

        float attackForScore = dynamicTuning.scoreOnlyAttackAboveStart
            ? Mathf.Max(0f, player.playerData.Atk - BalanceConfig.player.startAttack)
            : player.playerData.Atk;
        int powerScore = Mathf.FloorToInt(attackForScore * dynamicTuning.attackScorePerPoint);

        playerPowerScore = levelScore + killScore + buildScore + powerScore;

        // =========================
        // 根据战力修改刷怪
        // =========================

        // 波次间隔
        spawnWaveInterval = Mathf.Clamp(
            dynamicTuning.spawnIntervalBase - playerPowerScore * dynamicTuning.spawnIntervalPowerScale,
            dynamicTuning.minSpawnInterval,
            dynamicTuning.maxSpawnInterval);

        // 每组敌人数量
        enemyCountPerGroup = Mathf.Clamp(
            dynamicTuning.enemyCountBase + playerPowerScore / dynamicTuning.enemyCountPowerDivisor,
            dynamicTuning.minEnemyCountPerGroup,
            dynamicTuning.maxEnemyCountPerGroup);

        // 最大同时敌群数量
        currentWaveGroupCount = Mathf.Clamp(
            dynamicTuning.groupCountBase + playerPowerScore / dynamicTuning.groupCountPowerDivisor,
            dynamicTuning.minGroupCount,
            dynamicTuning.maxGroupCount);

        Debug.Log("玩家评分:" + playerPowerScore + " 敌群:" + currentWaveGroupCount + " 每组:" + enemyCountPerGroup);

        //float powerFactor = Mathf.Clamp01(playerPowerScore / 120f);
        float powerFactor = Mathf.Clamp01(
            playerPowerScore /
            (dynamicTuning.powerFactorBase + gameTime * dynamicTuning.powerFactorTimeScale));

        // 血量成长明显
        currentEnemyHpFactor = Mathf.Lerp(1f, dynamicTuning.maxEnemyHpFactor, powerFactor);

        // 攻击成长轻微
        currentEnemyAtkFactor = Mathf.Lerp(1f, dynamicTuning.maxEnemyAtkFactor, powerFactor);

        playerScore = playerPowerScore;
    }

    // 生成特殊敌人
    IEnumerator SpawnSpecialEnemy(EnemyType enemyType, bool isFinalBoss = false)
    {
        // 如果enemyType是Elite，则生成两只。如果是Boss，则生成一只。
        int count = enemyType == EnemyType.Elite ? 2 : 1;
        // 生成警告物体提示boss或者精英怪即将出现
        if(enemyType == EnemyType.Boss)
        {
            Vector3 centerPos = GetEnemyGroupCenter();
            GameObject centerObj = new GameObject("SpeciaEnemyGroupCenter");
            centerObj.transform.position = centerPos;
            GameObject warning = ShowWarning(centerObj.transform, "warning_boss");
            yield return new WaitForSeconds(0.8f);
            Destroy(warning);
        }

        for (int i = 0; i < count; i++)
        {
            GameObject newEnemy = Instantiate(Resources.Load<GameObject>("enemy"));
            Enemy enemy = newEnemy.GetComponent<Enemy>();
            enemy.target = player.transform;
            enemy.SetEnemy(DataManager.enemyDataDict[(int)enemyType]);
            enemy.ConfigureFinalBossCombat(isFinalBoss);
            enemy.IsSpecialEnemy = true;
            // 屏幕外随机位置
            Vector3 centerPos2 = GetEnemyGroupCenter();

            newEnemy.transform.position = centerPos2;

            DataManager.allEnemyDict.Add(newEnemy);
        }
    }

    // 尸潮逻辑
    void UpdateWave()
    {
        waveTimer += Time.deltaTime;

        // 每30秒触发一次尸潮
        if (!isWave && waveTimer >= waveAppearInterval)
        {
            isWave = true;
            waveTimer = 0;
            player.GetComponent<Player>().ChangeWhenInWave(true);
            Debug.Log("尸潮开始");
            safeSide = Random.Range(0, 4);
            Debug.Log("本轮尸潮安全区是：" + (safeSide == 0 ? "左" : safeSide == 1 ? "右" : safeSide == 2 ? "下" : "上"));
            foreach (var enemy in DataManager.allEnemyDict)
            {
                enemy.GetComponent<Enemy>().AddShield();
            }
            mainCamera.backgroundColor = new Color(0.2627f, 0f, 0f);


            var shichao = SpwanWorldTxt("尸潮来袭！");
            StartRuntimeCoroutine(ShowFlashWarningTxt(shichao));
        }

        // 尸潮持续一小段时间
        if (isWave && waveTimer >= waveDuration)
        {
            isWave = false;
            waveTimer = 0;
            player.GetComponent<Player>().ChangeWhenInWave(false);
            Debug.Log("尸潮结束");
            mainCamera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            difficulty = Mathf.Max(1, difficulty * 0.5f); // 尸潮结束后暂时降低难度，给玩家喘息的机会
        }
    }

    public IEnumerator ShowFlashWarningTxt(GameObject warningObject)
    {
        if (warningObject == null)
            yield break;

        warningObject.SetActive(true);
        float timer = 0;
        while (timer < 2 && warningObject != null)
        {
            timer += Time.deltaTime;
            // 每0.5秒闪烁一次
            if (Mathf.FloorToInt(timer * 2) % 2 == 0)
            {
                warningObject.SetActive(true);
            }
            else
            {
                warningObject.SetActive(false);
            }
            yield return null;
        }

        if (warningObject != null)
        {
            Destroy(warningObject);
        }
    }

    // 波次刷怪更新
    void UpdateSpawnWave()
    {
        if (DataManager.allEnemyDict.Count >= MaxEnemyCount)
            return;

        spawnWaveTimer += Time.deltaTime;

        // 时间到，生成波次
        if (spawnWaveTimer >= spawnWaveInterval)
        {
            spawnWaveTimer = 0;

            SpawnWave();
        }
    }

    // 生成一整个波次
    void SpawnWave()
    {
        // =========================
        // 尸潮
        // =========================
        if (isWave)
        {
            // 尸潮保持单方向大群
            StartRuntimeCoroutine(SpawnEnemyGroup(0));

            return;
        }

        // =========================
        // 普通波次

        // 随机生成1~3个敌群
        int groupCount = Random.Range(normalGroupMin, normalGroupMax + 1);

        for (int i = 0; i < groupCount; i++)
        {
            StartRuntimeCoroutine(SpawnEnemyGroup(i * BalanceConfig.wave.groupSpawnDelay));
        }
    }

    // 生成一组敌人
    IEnumerator SpawnEnemyGroup(float delay)
    {
        yield return new WaitForSeconds(delay);

        int count = 0;

        if (isWave)
        {
            count = enemyCountPerGroup * waveEnemyMultiplier;
        }
        else
        {
            count = Random.Range(normalEnemyMin, normalEnemyMax + 1);
        }

        Vector3 centerPos = GetEnemyGroupCenter();

        // =========================
        // 创建敌群中心
        // =========================
        GameObject centerObj = new GameObject("EnemyGroupCenter");

        centerObj.transform.position = centerPos;

        // =========================
        // 创建动态预警
        // =========================
        GameObject warning = ShowWarning(centerObj.transform);

        // 预警时间
        yield return new WaitForSeconds(0.8f);

        Destroy(warning);

        // =========================
        // 正式生成
        // =========================
        for (int i = 0; i < count; i++)
        {
            float spreadRadius = 0;

            if (isWave)
            {
                spreadRadius = 6f;
            }
            else
            {
                spreadRadius = 4f;
            }

            Vector2 randomOffset = Random.insideUnitCircle * spreadRadius;

            Vector3 spawnPos = centerPos + new Vector3(randomOffset.x, randomOffset.y, 0);

            GenEnemy(spawnPos);
        }

        Destroy(centerObj);
    }

    GameObject ShowWarning(Transform target,string wpath = "warning")
    {
        GameObject obj = Instantiate(Resources.Load<GameObject>(wpath));

        EnemyWarning warning = obj.GetComponent<EnemyWarning>();
        warning.Init();
        warning.mainCamera = mainCamera;
        warning.target = target;

        // 立刻刷新一次
        warning.RefreshPosition();

        return obj;
    }

    void GenPlayer()
    {
        player = Instantiate(Resources.Load<GameObject>("player"));

        player.transform.position = Vector3.zero;
        dash_slider.transform.parent.gameObject.SetActive(true);
        pdata = new PlayerData
        {
            Level = 1,// 玩家等级
            Hp = BalanceConfig.player.startHp + DataManager.myGameData.PermanentHp,// 玩家生命值 = 基础值 + 永久增加的生命值
            Atk = BalanceConfig.player.startAttack + DataManager.myGameData.PermanentAtk,// 当前玩家攻击力
            MoveSpeed = BalanceConfig.player.startMoveSpeed + DataManager.myGameData.PermanentMoveSpeed,// 玩家移动速度
            Def = BalanceConfig.player.startDefence,// 玩家防御力
        };

        player.GetComponent<Player>().Init(pdata);
    }

    public void ExecuteUpgrade(UpgradeData data)
    {
        Player p = player.GetComponent<Player>();
        RecordUpgradeSelected(data);

        switch (data.type)
        {
            // 子弹数量
            case UpgradeType.BulletCount:

                p.CurrentBulletCount += (int)data.value;

                // 最大限制
                p.CurrentBulletCount = Mathf.Clamp(p.CurrentBulletCount, 1, 10);

                break;

            // 重型弹头
            case UpgradeType.HeavyBullet:

                p.GetCurrentWeapon().ChangeAttack((int)data.value);
                p.EnhancedShotDamageMultiplier += 0.08f;
                p.GetCurrentWeapon().ChangeBulletScale(0.1f);
                break;

            // 穿透
            case UpgradeType.Pierce:

                p.GetCurrentWeapon().ChangeBulletPierce((int)data.value);
                break;

            // 攻击倍率
            case UpgradeType.AtkRatio:

                p.playerData.Atk += data.value;
                p.EnhancedShotDamageMultiplier += 0.12f;

                break;

            // 游击模式
            case UpgradeType.MoveFast:

                p.moveSpeed += data.value;

                // 高移速低伤害
                p.playerData.Atk -= 0.2f;

                break;

            // 重装炮台
            case UpgradeType.HeavyMode:

                p.playerData.Atk += 1.5f;

                p.moveSpeed -= 1f;

                // 提升攻速
                p.GetCurrentWeapon().ChangeFireInterval(-0.05f);
                // 提升防御。
                p.AddDefence(2);
                break;

            // 暴击爆炸
            case UpgradeType.CritExplosion:

                p.HasCritExplosion = true;

                break;

            // 穿透爆炸
            case UpgradeType.PierceExplosion:

                p.HasPierceExplosion = true;

                break;

            // 精准重炮
            case UpgradeType.LowBulletHighDamage:

                p.HasLowBulletHighDamage = true;
                p.GetCurrentWeapon().ChangeAttack(2);
                p.GetCurrentWeapon().ChangeBulletScale(0.25f);
                p.EnhancedShotDamageMultiplier += 0.35f;
                ShakeMainCamera(0.12f, 0.12f);
                break;

            // 传奇裂变
            case UpgradeType.LegendSplit:
                p.HasLegendSplit = true;
                break;

            // 无限火力
            case UpgradeType.LegendFire:

                p.GetCurrentWeapon().ChangeFireInterval(-0.15f);

                break;

            case UpgradeType.CritChance:

                p.GetCurrentWeapon().ChangeCritical(data.value);

                break;

            case UpgradeType.FireRate:

                p.GetCurrentWeapon().ChangeFireInterval(-Mathf.Abs(data.value));

                break;

            case UpgradeType.EnhancedShot:

                p.EnhancedShotInterval = Mathf.Max(3, p.EnhancedShotInterval - 1);
                p.EnhancedShotDamageMultiplier += data.value;
                p.EnhancedShotBonusPierce = Mathf.Min(5, p.EnhancedShotBonusPierce + 1);
                p.EnhancedShotScaleMultiplier += 0.08f;

                break;

            default:
                break;
        }
    }

    void GenEnemy(Vector3 spawnPos)
    {
        GameObject newEnemy = Instantiate(Resources.Load<GameObject>("enemy"));

        newEnemy.GetComponent<Enemy>().target = player.transform;

        int enemyId = 0;
        if (isWave)
        {
            enemyId = Random.Range(0, 3);
        }
        else
        {
            // 根据游戏时间和难度来决定敌人类型，随着时间推移更强的敌人出现概率增加。但是不会生成最后一个和倒数第二个，因为最后一个通常是boss，倒数第二个通常是特殊敌人
            enemyId = Random.Range(0, Mathf.Min(2 + Mathf.FloorToInt(gameTime / 60f), DataManager.enemyDataDict.Count - 2));
        }
        newEnemy.GetComponent<Enemy>().SetEnemy(DataManager.enemyDataDict[enemyId]);

        newEnemy.transform.position = spawnPos;

        DataManager.allEnemyDict.Add(newEnemy);
    }
    Vector3 GetEnemyGroupCenter()
    {
        float x = 0;
        float y = 0;

        // 0 左 1 右 2 下 3 上 4 左上 5 右上 6 左下 7 右下
        int side = Random.Range(0, 8);

        if (isWave)
        {
            while (side == safeSide)
            {
                side = Random.Range(0, 8);
            }
        }

        switch (side)
        {
            case 0:
                x = -offset;
                y = Random.Range(0, Screen.height);
                break;
            case 1:
                x = Screen.width + offset;
                y = Random.Range(0, Screen.height);
                break;
            case 2:
                x = Random.Range(0, Screen.width);
                y = -offset;
                break;
            case 3:
                x = Random.Range(0, Screen.width);
                y = Screen.height + offset;
                break;
            case 4:
                x = -offset;
                y = Screen.height + offset;
                break;
            case 5:
                x = Screen.width + offset;
                y = Screen.height + offset;
                break;
            case 6:
                x = -offset;
                y = -offset;
                break;
            case 7:
                x = Screen.width + offset;
                y = -offset;
                break;
        }

        Vector3 wpos = GetWorldPosByScreenPos(new Vector3(x, y, 0));
        wpos.z = 0;

        return wpos;
    }

    public GameObject SpwanBulletSingle(BulletData bulletData, Vector3 dir, Vector3 pos,float bulletScale, string EntityTag, Entity belongWho)
    {
        GameObject newBullet_Liner = BulletPool.Instance.Get(bulletData.prefabString);
        newBullet_Liner.transform.position = pos;
        Bullet bullet = newBullet_Liner.GetComponent<Bullet>();
        bullet.SetBulletPrefabId(bulletData.prefabString);
        bullet.SetBullet(bulletData, pos, dir, belongWho);
        if(belongWho.EntityTag == "player")
        {
            newBullet_Liner.transform.Find("fx").localScale += new Vector3(bulletScale, bulletScale, 0);
        }
        else
        {
            newBullet_Liner.transform.localScale += new Vector3(bulletScale, bulletScale, 0);
        }
        bullet.CanMove = true;
        return newBullet_Liner;
    }
    public GameObject SpwanChest(Vector3 pos)
    {
        GameObject newChest = SpwanSingleCircle(pos);
        newChest.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("sprites/chest");
        newChest.AddComponent<ChestBall>().SetChestValue(player);
        RecordChestSpawned();
        return newChest;
    }

    public GameObject SpwanCoin(Vector3 pos, int coinValue)
    {
        GameObject newCoin = SpwanSingleCircle(pos);
        newCoin.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("sprites/coin");
        newCoin.AddComponent<CoinBall>().SetCoinValue(coinValue, player);
        RecordCoinSpawned(coinValue);
        return newCoin;
    }
    public GameObject SpwanExpBall(Vector3 pos,EnemyType enemyType, int expValue)
    {
        GameObject newExpBall = SpwanSingleExpBall(pos);
        float baseScale = 0.46f;
        switch(enemyType)
        {
            case EnemyType.Normal:
                baseScale = 0.46f;
                break;
            case EnemyType.Thick:
                baseScale = 0.5f;
                break;
            case EnemyType.Elite:
                baseScale = 0.52f;
                break;
            case EnemyType.Boss:
                baseScale = 0.68f;
                break;
        }
        newExpBall.transform.localScale = Vector3.one * baseScale;
        newExpBall.AddComponent<ExpBall>().SetExpValue(expValue, player);
        DataManager.allExpBall.Add(newExpBall);
        RecordExpSpawned(expValue);
        return newExpBall;
    }
    public GameObject SpwanSingleCircle(Vector3 pos)// cicle  0.4  0.2
    {
        GameObject newExpBall = Instantiate(Resources.Load<GameObject>("cicle"));
        newExpBall.transform.position = pos;
        return newExpBall;
    }

    public GameObject SpwanMuzzleflash(Vector3 pos)
    {
        GameObject newMuzzleflash = Instantiate(Resources.Load<GameObject>("muzzleflash"));
        newMuzzleflash.transform.position = pos;
        return newMuzzleflash;
    }

    public GameObject SpwanSingleExpBall(Vector3 pos)
    {
        GameObject newExpBall = Instantiate(Resources.Load<GameObject>("expBall"));
        newExpBall.transform.position = pos;
        return newExpBall;
    }

    public GameObject SpwanWorldTxt(string txt,float charactorSize = 0.6f)
    {
        GameObject newWarningTxt = Instantiate(Resources.Load<GameObject>("warning_txt"));
        Vector2 screenPos = new Vector2(Screen.width / 2.0f - 0.5f, Screen.height - 100 - 0.5f);
        Vector3 wPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
        wPos.z = 0;
        newWarningTxt.transform.position = wPos;

        TextMesh textMesh = newWarningTxt.GetComponent<TextMesh>();
        textMesh.characterSize = charactorSize;
        textMesh.color = Color.red;
        textMesh.text = txt;
        return newWarningTxt;
    }

    public GameObject SpwanHitFx(Vector3 pos)
    {
        GameObject fx = Instantiate(Resources.Load<GameObject>("muzzleflash"));
        fx.transform.position = pos;
        fx.transform.localScale = Vector3.one * 0.45f;
        Destroy(fx, 0.12f);
        return fx;
    }

    public GameObject SpwanEnemyAttackPulse(Vector3 pos, Color color, float targetScale, float duration)
    {
        GameObject pulse = SpwanSingleCircle(pos);
        pulse.name = "EnemyAttackPulse";

        SpriteRenderer sr = pulse.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = color;
            sr.sortingOrder = 30;
        }

        pulse.transform.localScale = Vector3.one * 0.35f;
        PulseFx fx = pulse.AddComponent<PulseFx>();
        fx.Init(targetScale, duration);
        return pulse;
    }

    public void SpwanBossAttackWarning(Vector3 pos, Vector3 fireDir, bool circleAttack, int phase, float duration)
    {
        if (circleAttack)
        {
            float scale = BalanceConfig.bossCombat.GetCircleWarningScale(phase);
            SpwanEnemyAttackPulse(pos, new Color(1f, 0.1f, 0.05f, 0.42f), scale, duration);
            StartRuntimeCoroutine(BossCircleWarningFlow(pos, phase, duration));
            return;
        }

        float radius = BalanceConfig.bossCombat.GetSectorWarningRadius(phase);
        float angle = BalanceConfig.bossCombat.GetSectorWarningAngle(phase);
        GameObject warning = CreateBossSectorWarning(pos, fireDir, angle, radius);
        StartRuntimeCoroutine(FadeAndDestroyWarning(warning, duration));
        StartRuntimeCoroutine(BossSectorWarningFlow(pos, fireDir, angle, radius, phase, duration));
    }

    IEnumerator BossCircleWarningFlow(Vector3 pos, int phase, float duration)
    {
        float targetScale = BalanceConfig.bossCombat.GetCircleWarningScale(phase);
        float interval = phase == 1 ? 0.16f : phase == 2 ? 0.13f : 0.1f;
        float pulseDuration = Mathf.Max(0.18f, duration * 0.42f);
        float elapsed = 0f;
        float nextPulseTime = interval;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= nextPulseTime)
            {
                Color color = new Color(1f, 0.22f, 0.06f, phase == 1 ? 0.28f : 0.34f);
                SpwanEnemyAttackPulse(pos, color, targetScale, pulseDuration);
                nextPulseTime += interval;
            }
            yield return null;
        }
    }

    IEnumerator BossSectorWarningFlow(Vector3 pos, Vector3 fireDir, float angle, float radius, int phase, float duration)
    {
        if (fireDir.sqrMagnitude <= 0.001f)
            fireDir = Vector3.right;

        int lineCount = phase == 1 ? 7 : phase == 2 ? 8 : 9;
        LineRenderer[] lines = new LineRenderer[lineCount];
        GameObject[] heads = new GameObject[lineCount];
        float[] angleOffsets = new float[lineCount];
        float[] phaseOffsets = new float[lineCount];
        float baseAngle = Mathf.Atan2(fireDir.y, fireDir.x) * Mathf.Rad2Deg;
        float innerRadius = 0.45f;
        float segmentLength = phase == 1 ? 1.55f : phase == 2 ? 1.8f : 2.05f;

        for (int i = 0; i < lineCount; i++)
        {
            float laneT = lineCount == 1 ? 0.5f : i / (float)(lineCount - 1);
            angleOffsets[i] = Mathf.Lerp(-angle * 0.43f, angle * 0.43f, laneT);
            phaseOffsets[i] = i * (1f / lineCount);
            lines[i] = CreateBossWarningFlowLine("BossSectorFlowLine", phase);
            heads[i] = SpwanSingleCircle(pos);
            heads[i].name = "BossSectorFlowHead";
            heads[i].transform.localScale = Vector3.one * (phase == 1 ? 0.16f : phase == 2 ? 0.2f : 0.24f);
            SpriteRenderer headRenderer = heads[i].GetComponent<SpriteRenderer>();
            if (headRenderer != null)
            {
                headRenderer.sortingOrder = 32;
                headRenderer.color = new Color(1f, 0.78f, 0.18f, 0.85f);
            }
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < lineCount; i++)
            {
                if (lines[i] == null)
                    continue;

                float flowT = Mathf.Repeat(normalizedTime * 1.95f + phaseOffsets[i], 1f);
                float easedT = 1f - Mathf.Pow(1f - flowT, 2.2f);
                float headDistance = Mathf.Lerp(innerRadius + segmentLength, radius, easedT);
                float tailDistance = Mathf.Max(innerRadius, headDistance - segmentLength);
                Vector3 dir = Quaternion.Euler(0f, 0f, baseAngle + angleOffsets[i]) * Vector3.right;
                Vector3 headPos = pos + dir * headDistance;

                lines[i].SetPosition(0, pos + dir * tailDistance);
                lines[i].SetPosition(1, headPos);

                float alpha = Mathf.Sin(flowT * Mathf.PI);
                Color start = new Color(1f, 0.1f, 0.02f, alpha * 0.24f);
                Color end = new Color(1f, 0.86f, 0.12f, alpha * 0.88f);
                lines[i].startColor = start;
                lines[i].endColor = end;

                if (heads[i] != null)
                {
                    heads[i].transform.position = headPos;
                    SpriteRenderer headRenderer = heads[i].GetComponent<SpriteRenderer>();
                    if (headRenderer != null)
                    {
                        headRenderer.color = new Color(1f, 0.82f, 0.18f, alpha * 0.92f);
                    }
                }
            }

            yield return null;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] != null)
                Destroy(lines[i].gameObject);

            if (heads[i] != null)
                Destroy(heads[i]);
        }
    }

    LineRenderer CreateBossWarningFlowLine(string objName, int phase)
    {
        GameObject lineObj = new GameObject(objName);
        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.material.color = Color.white;
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 5;
        line.sortingOrder = 31;
        line.startWidth = phase == 1 ? 0.14f : phase == 2 ? 0.17f : 0.2f;
        line.endWidth = phase == 1 ? 0.28f : phase == 2 ? 0.34f : 0.4f;
        return line;
    }

    GameObject CreateBossSectorWarning(Vector3 pos, Vector3 fireDir, float angle, float radius)
    {
        GameObject obj = new GameObject("BossSectorWarning");
        obj.transform.position = pos;

        int segments = 24;
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];
        vertices[0] = Vector3.zero;

        float baseAngle = Mathf.Atan2(fireDir.y, fireDir.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle - angle * 0.5f;
        for (int i = 0; i <= segments; i++)
        {
            float a = startAngle + angle * i / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0f) * radius;
        }

        for (int i = 0; i < segments; i++)
        {
            int triIndex = i * 3;
            triangles[triIndex] = 0;
            triangles[triIndex + 1] = i + 1;
            triangles[triIndex + 2] = i + 2;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        MeshFilter filter = obj.AddComponent<MeshFilter>();
        filter.mesh = mesh;
        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.material.color = new Color(1f, 0.18f, 0.05f, 0.22f);
        renderer.sortingOrder = 25;
        return obj;
    }

    IEnumerator FadeAndDestroyWarning(GameObject warning, float duration)
    {
        if (warning == null)
            yield break;

        MeshRenderer renderer = warning.GetComponent<MeshRenderer>();
        Material material = renderer != null ? renderer.material : null;
        float timer = 0f;
        while (timer < duration && warning != null)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            if (material != null)
            {
                Color c = material.color;
                c.a = Mathf.Lerp(0.22f, 0.04f, t);
                material.color = c;
            }
            yield return null;
        }

        Destroy(warning);
    }

    public void BeginFinalBossAtmosphere()
    {
        if (cameraEffect != null)
        {
            cameraEffect.darkIntensity = BalanceConfig.bossCombat.phase1DarkIntensity;
        }

        if (mainCamera != null)
        {
            mainCamera.backgroundColor = new Color(0.07f, 0.02f, 0.04f);
        }
    }

    public void SetFinalBossPhaseAtmosphere(int phase)
    {
        if (cameraEffect != null)
        {
            cameraEffect.darkIntensity = BalanceConfig.bossCombat.GetDarkIntensity(phase);
        }

        Color phaseColor = phase == 1
            ? new Color(0.07f, 0.02f, 0.04f)
            : phase == 2
                ? new Color(0.12f, 0.025f, 0.035f)
                : new Color(0.18f, 0.025f, 0.025f);

        if (mainCamera != null)
        {
            mainCamera.backgroundColor = phaseColor;
        }
    }

    public void EndFinalBossAtmosphere()
    {
        if (cameraEffect != null)
        {
            cameraEffect.darkIntensity = 0f;
        }

        if (mainCamera != null)
        {
            mainCamera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
        }
    }

    public void PlayFinalBossDeathReward(Vector3 pos)
    {
        EnemyDeathEffectTuning tuning = BalanceConfig.deathEffect;
        ShakeMainCamera(tuning.finalBossRewardShakeDuration, tuning.finalBossRewardShakeStrength);
        var title = SpwanWorldTxt("最终Boss击破！", 1.25f);
        StartRuntimeCoroutine(ShowFlashWarningTxt(title));

        for (int i = 0; i < 20; i++)
        {
            float angle = i * (360f / 20f);
            Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f) * Random.Range(1.2f, 3.8f);
            SpwanCoin(pos + offset, 5);
        }

        for (int i = 0; i < 24; i++)
        {
            float angle = i * (360f / 24f);
            Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f) * Random.Range(1.0f, 4.2f);
            SpwanExpBall(pos + offset, EnemyType.Boss, 6);
        }
    }

    public List<GameObject> FindCicleAllEnemysByDistance(Vector3 pos, float distance)
    {
        List<GameObject> enemys = new List<GameObject>();
        for (int i = DataManager.allEnemyDict.Count - 1; i >= 0; i--)
        {
            GameObject enemy = DataManager.allEnemyDict[i];
            if (enemy && enemy.GetComponent<Enemy>().Dead == false)
            {
                float dis = Vector3.Distance(pos, enemy.transform.position);
                if (dis <= distance)
                {
                    enemys.Add(enemy);
                }
            }
        }
        return enemys;
    }
    public GameObject FindClosedEnemy(Vector3 pos)
    {
        GameObject closedEnemy = null;
        float minDistance = float.MaxValue;
        for (int i = DataManager.allEnemyDict.Count - 1; i >= 0; i--)
        {
            GameObject enemy = DataManager.allEnemyDict[i];
            if (enemy)
            {
                float distance = Vector3.Distance(pos, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closedEnemy = enemy;
                }
            }
        }
        return closedEnemy;
    }

    Vector3 GetWorldPosByScreenPos(Vector3 screenPos)
    {
        screenPos.z = 0;
        return mainCamera.ScreenToWorldPoint(screenPos);
    }
    List<GameObject> lineObjs = new List<GameObject>();

    private void DrawGrid()
    {
        int size = 3;

        // 使用屏幕四个角转换世界坐标，而不是直接用Screen.width/height
        Vector3 lb = mainCamera.ScreenToWorldPoint(new Vector3(0, 0, 0));
        Vector3 rt = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));

        float padding = 100;

        float startX = Mathf.Floor((lb.x - padding) / size) * size;
        float endX = Mathf.Ceil((rt.x + padding) / size) * size;

        float startY = Mathf.Floor((lb.y - padding) / size) * size;
        float endY = Mathf.Ceil((rt.y + padding) / size) * size;

        int verticalCount = Mathf.FloorToInt((endX - startX) / size) + 1;
        int horizontalCount = Mathf.FloorToInt((endY - startY) / size) + 1;

        int needCount = verticalCount + horizontalCount;

        // 不再Destroy，每次只创建不足的部分
        while (lineObjs.Count < needCount)
        {
            GameObject line = new GameObject("GridLine");

            LineRenderer liner = line.AddComponent<LineRenderer>();
            liner.positionCount = 2;
            liner.startWidth = 0.08f;
            liner.endWidth = 0.08f;

            // 只创建一次材质
            liner.material = new Material(Shader.Find("Sprites/Default"));

            liner.startColor = new Color(0.16f, 0.17f, 0.2f);
            liner.endColor = new Color(0.16f, 0.17f, 0.2f);

            lineObjs.Add(line);
        }

        // 多余的线直接隐藏
        for (int i = needCount; i < lineObjs.Count; i++)
        {
            lineObjs[i].SetActive(false);
        }

        int index = 0;

        // 绘制竖线
        for (float x = startX; x <= endX; x += size)
        {
            GameObject line = lineObjs[index];
            line.SetActive(true);

            LineRenderer liner = line.GetComponent<LineRenderer>();

            liner.SetPosition(0, new Vector3(x, startY, 0));
            liner.SetPosition(1, new Vector3(x, endY, 0));

            index++;
        }

        // 绘制横线
        for (float y = startY; y <= endY; y += size)
        {
            GameObject line = lineObjs[index];
            line.SetActive(true);

            LineRenderer liner = line.GetComponent<LineRenderer>();

            liner.SetPosition(0, new Vector3(startX, y, 0));
            liner.SetPosition(1, new Vector3(endX, y, 0));

            index++;
        }
    }

    public List<UpgradeData> GetUpgradeOptions(int count)
    {
        Player player = this.player.GetComponent<Player>();

        // 临时升级池
        List<UpgradeData> tempList = new List<UpgradeData>();

        tempList.AddRange(DataManager.upgradeList);

        // =========================
        // 传奇词条条件过滤
        // =========================

        for (int i = tempList.Count - 1; i >= 0; i--)
        {
            UpgradeData data = tempList[i];
            UpgradeRuleTuning upgradeRules = BalanceConfig.upgradeRules;

            // 暴击爆炸
            if (data.type == UpgradeType.CritExplosion)
            {
                if (player.HasCritExplosion ||
                    !player.buildDict.ContainsKey("crit") ||
                    player.buildDict["crit"] < upgradeRules.critExplosionMinCritStacks)
                {
                    tempList.RemoveAt(i);

                    continue;
                }
            }

            // 穿透爆炸
            if (data.type == UpgradeType.PierceExplosion)
            {
                if (player.HasPierceExplosion ||
                    !player.buildDict.ContainsKey("pierce") ||
                    player.buildDict["pierce"] < upgradeRules.pierceExplosionMinPierceStacks)
                {
                    tempList.RemoveAt(i);

                    continue;
                }
            }

            // 传奇裂变
            if (data.type == UpgradeType.LegendSplit)
            {
                if (player.HasLegendSplit ||
                    (!player.HasCritExplosion && !player.HasPierceExplosion))
                {
                    tempList.RemoveAt(i);

                    continue;
                }
            }

            // 无限火力
            if (data.type == UpgradeType.LegendFire)
            {
                if (!player.buildDict.ContainsKey("fire") ||
                    !player.buildDict.ContainsKey("bullet") ||
                    player.buildDict["fire"] < upgradeRules.legendFireMinFireStacks ||
                    player.buildDict["bullet"] < upgradeRules.legendFireMinBulletStacks)
                {
                    tempList.RemoveAt(i);

                    continue;
                }
            }

            if (data.type == UpgradeType.LowBulletHighDamage && player.HasLowBulletHighDamage)
            {
                tempList.RemoveAt(i);

                continue;
            }
        }

        // =========================
        // 根据流派增加权重
        // =========================

        for (int i = 0; i < DataManager.upgradeList.Count; i++)
        {
            UpgradeData data = DataManager.upgradeList[i];

            if (player.buildDict.ContainsKey(data.tag))
            {
                int weight = player.buildDict[data.tag];

                for (int j = 0; j < weight; j++)
                {
                    tempList.Add(data);
                }
            }
        }

        // =========================
        // 最终结果
        // =========================

        List<UpgradeData> result = new List<UpgradeData>();

        for (int i = 0; i < count; i++)
        {
            if (tempList.Count <= 0)
                break;

            int id = Random.Range(0, tempList.Count);

            UpgradeData data = tempList[id];

            result.Add(data);

            // 防止重复
            tempList.Remove(data);
        }

        return result;
    }

    /// <summary>
    /// 震屏
    /// </summary>
    /// <param name="power"></param>
    public void ShakeMainCamera(float duration, float strength)
    {
        shakeDuration = duration;
        shakeStrength = strength;
        shakeTime = duration;
    }
    public void SaveGame()
    {
        string gameDataJson = JsonUtility.ToJson(DataManager.myGameData);
        PlayerPrefs.SetString("gamedata", gameDataJson);
        PlayerPrefs.Save();
    }

    private void Dispose()
    {
        DataManager.Clear();
        WeaponSystem.Clear();
        lineObjs.Clear();
        gameStepCoroutine = null;
        foreach (var l in lineObjs)
        {
            Destroy(l);
        }
    }
    private void OnDisable()
    {
        Dispose();
    }

    private void OnDestroy()
    {
        Dispose();
    }

    private void OnApplicationQuit()
    {
        Dispose();
    }
}
