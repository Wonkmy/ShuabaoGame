// 当前项目的chatgpt聊天对话“CrazyGames 游戏类型分析”
// 具体的游戏设计在聊天对话的这个位置，直接搜索关键句即可：“好，那我们一起讨论细你说的建议”

using System.Collections;
using System.Collections.Generic;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public GameObject player { get; private set; }
    public Transform playerExpSlider { get; private set; }
    public Transform playerHpSlider { get; private set; }

    public Transform gameLoadingSlider { get; set; }
    int currentLoadStep = 0;
    int totalLoadStep = 5;
    public PlayerData pdata { get; set; }

    public GameObject levelPanel;
    public GameObject gameOverPanel;
    public GameObject cultivatePanel;

    public bool GameOver { get; set; }
    public bool IsGameStarted { get; set; }

    // 技能释放相关
    public Button btn_ReleaseSkill;
    public Text coolDownLabel;
    float skillCooldownTimer = 0;// 技能冷却计时器
    float totalSkillCooldownTime = 0;// 技能总冷却时间，根据玩家类型从DataManager.playerSkillTypeCDDict获取
    bool canUseSkill = true;
    // =========================
    // 数值配置区
    // =========================
    [Header("波次配置")]
    public float spawnWaveIntervalBase = 4f;
    public int normalGroupMin = 1;
    public int normalGroupMax = 3;
    public int normalEnemyMin = 3;
    public int normalEnemyMax = 7;
    public int waveEnemyMultiplier = 2;

    [Header("尸潮配置")]
    public float waveAppearInterval = 15f;
    public float waveDuration = 5f;

    // 当前这一轮特殊事件需要等待的时间
    public float nextSpecialEventInterval { get; set; }
    public int MaxEnemyCount = 30;// 场上最大敌人数量，超过后不再刷怪，直到数量降低
    [Header("玩家配置")]
    public float dashCooldownTime = 1.2f;
    public float dashSpeed = 18f;
    public float dashDuration = 0.12f;

    // 当前刷怪预算
    //float enemyBudget = 0;
    // =========================
    // 波次刷怪系统
    // =========================

    // 波次计时器
    float spawnWaveTimer = 0;

    // 波次间隔
    float spawnWaveInterval = 4f;// 当前动态波次间隔

    // 每组怪物数量
    int enemyCountPerGroup = 6;

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
    bool isWave = false;
    float waveTimer = 0;
    int maxSpawnPerFrame = 5;
    int safeSide = 0;// 尸潮来袭时的安全区，0左1右2下3上
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
    // private float specoalApperInterval = 45f;// 已废弃，改为配置区
    public bool IsSpecialEvent { get; set; }// 是否正在进行特殊事件
    private EnemyType[] enemyTypes;// 特殊事件专用的敌人类型：精英、Boss

    // 时间停止相关
    public bool IsTimeStop = false;
    float timeStopTimer = 0;// 剩余时间

    // 黑洞技能相关
    public bool IsBlackHole = false;
    public Vector3 BlackHolePos;
    float blackHoleTimer = 0;

    Coroutine gameStepCoroutine;
    private void Start()
    {
        gameStepCoroutine = StartCoroutine(LoadGameStep());
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

        LoadDefaultUpgradeConfig();// 加载默认构筑配置
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

        playerExpSlider = expobj.transform;
        playerHpSlider = hpobj.transform;
    }
    IEnumerator Init()
    {
        ShowCultivatePanel(true);
        yield return new WaitUntil(()=> CultivatePanelActive() == false);
        GameOver = false;
        IsGameStarted = true;
        // 基础难度固定
        difficulty = 3;
        // 当前特殊事件等待时间
        nextSpecialEventInterval = 45.0f;
        mainCamera = Camera.main;
        cameraEffect = mainCamera.GetComponent<CameraEffect>();
        cameraOriginPos = mainCamera.transform.localPosition;
        GenPlayer();
        totalSkillCooldownTime = DataManager.playerSkillTypeCDDict[player.GetComponent<Player>().playerType];
        skillCooldownTimer = totalSkillCooldownTime;
        btn_ReleaseSkill.onClick.AddListener(() => {
            UseSkill();
        });
        AudioManager.instance.PlayBGM("main");
    }

    void LoadDefaultUpgradeConfig()
    {
        // =========================
        // 基础成长（只保留少量）
        // =========================

        DataManager.upgradeList.Add(new UpgradeData()
        {
            name = "+1子弹",
            tag = "bullet",
            action = () =>
            {
                player.GetComponent<Player>().CurrentBulletCount += 1;
            }
        });

        DataManager.upgradeList.Add(new UpgradeData()
        {
            name = "+5攻击力",
            tag = "attack",
            action = () =>
            {
                player.GetComponent<Player>().GetCurrentWeapon().ChangeAttack(5);
            }
        });
        DataManager.upgradeList.Add(new UpgradeData()
        {
            name = "+1穿透",
            tag = "bullet",
            action = () =>
            {
                player.GetComponent<Player>().GetCurrentWeapon().ChangeBulletPierce(1);
            }
        });



        DataManager.upgradeList.Add(new UpgradeData()
        {
            name = "伤害倍率提升",
            tag = "power",
            action = () =>
            {
                player.GetComponent<Player>().playerData.Atk += 1;
            }
        });

        // =========================
        // 真正构筑开始
        // =========================

        // 超级散射
        DataManager.upgradeList.Add(new UpgradeData()
        {
            name = "超级散射",
            tag = "bullet",
            action = () =>
            {
                // 加一个最大的子弹数限制，这里限制为不能超过10发
                int maxBullet = 10;
                int v = player.GetComponent<Player>().CurrentBulletCount + 3;
                if (v >= maxBullet)
                {
                    v = maxBullet;
                }
                player.GetComponent<Player>().CurrentBulletCount = v;
            }
        });

        // 精准射击
        DataManager.upgradeList.Add(new UpgradeData()
        {
            name = "精准射击",
            tag = "power",
            action = () =>
            {
                // 高倍率
                player.GetComponent<Player>().playerData.Atk += 1f;
            }
        });

        // 游击模式
        DataManager.upgradeList.Add(new UpgradeData()
        {
            name = "游击模式",
            tag = "move_speed",
            action = () =>
            {
                player.GetComponent<Player>().moveSpeed += 3f;

                // 移速高但伤害降低
                player.GetComponent<Player>().playerData.Atk -= 0.2f;
            }
        });

        // 重装炮台
        DataManager.upgradeList.Add(new UpgradeData()
        {
            name = "重装炮台",
            tag = "power",
            action = () =>
            {
                player.GetComponent<Player>().playerData.Atk += 1.5f;

                // 降低移速
                player.GetComponent<Player>().moveSpeed -= 1f;

                // 降低攻速
                player.GetComponent<Player>().GetCurrentWeapon().ChangeFireInterval(-0.05f);
            }
        });

        // 暴击爆炸
        DataManager.upgradeList.Add(new UpgradeData()
        {
            name = "暴击爆炸",
            tag = "crit",

            action = () =>
            {
                player.GetComponent<Player>().HasCritExplosion = true;
            }
        });

        DataManager.upgradeList.Add(new UpgradeData()
        {
            name = "穿透爆炸",
            tag = "pierce",

            action = () =>
            {
                player.GetComponent<Player>().HasPierceExplosion = true;
            }
        });

        DataManager.upgradeList.Add(new UpgradeData()
        {
            name = "精准重炮",
            tag = "power",

            action = () =>
            {
                player.GetComponent<Player>().HasLowBulletHighDamage = true;
            }
        });
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
        Time.timeScale = show == true ? 0 : 1;
    }

    public void ShowGameOverPanel(bool show)
    {
        gameOverPanel.SetActive(show);
        Player playerC = player.GetComponent<Player>();
        // 将存活时长、击杀数、最高难度、玩家等级等数据传递给结算界面
        gameOverPanel.GetComponent<GameOverPanel>().Init(gameTime, playerC.KilledCount, difficulty, playerC.GetCurrentLevel());
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
    }

    void ResetAllGameDatas()
    {
        try
        {
            // 清理所有敌人和子弹、数据、字典、玩家已有的构筑列表buildDict、HasCritExplosion、HasPierceExplosion、HasLowBulletHighDamage
            DataManager.Clear();
            WeaponSystem.Clear();
            Player playerConponent = player.GetComponent<Player>();
            playerConponent.HasCritExplosion = false;
            playerConponent.HasPierceExplosion = false;
            playerConponent.HasLowBulletHighDamage = false;
            playerConponent.buildDict.Clear();
            // 重置尸潮、难度、预算、游戏时间等所有数据
            isWave = false;
            difficulty = 3;
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
            enemyCountPerGroup = 6;
            // 当前波次组数量
            currentWaveGroupCount = 1;
            // 特殊事件相关
            IsSpecialEvent = false;
            specialEventTimer = 0;

            GameManager.Instance.cameraEffect.intensity = 0;
            mainCamera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            Destroy(player);
            player = null;
            AudioManager.instance.StopAllBGM();
        }
        catch (System.Exception e)
        {
            Debug.LogError("ShowGameOverPanel error: " + e.Message);
        }
    }
    public void RestartGame()
    {
        ResetAllGameDatas();
        StartCoroutine(Init());
    }
    public void Revival()
    {

    }
    private void Update()
    {
        if (GameOver)
        {
            if (playerHpSlider != null)
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
            if (skillCooldownTimer > 0)
            {
                skillCooldownTimer -= Time.deltaTime;
                coolDownLabel.text = skillCooldownTimer.ToString("F1") + "s";
                if (skillCooldownTimer < 0)
                {
                    skillCooldownTimer = 0;
                    canUseSkill = true;
                }
            }
        }

        for (int i = DataManager.allEnemyDict.Count - 1; i >= 0; i--)
        {
            GameObject enemy = DataManager.allEnemyDict[i];
            if (enemy)
            {
                enemy.GetComponent<Enemy>().EnemyUpdate();
            }
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

        difficulty = Mathf.Clamp(2 + Mathf.FloorToInt(gameTime / 30f), 2, 8);

        // 特殊事件逻辑
        specialEventTimer += Time.deltaTime;

        // 时间到了并且当前没有特殊事件
        if (!IsSpecialEvent && specialEventTimer >= nextSpecialEventInterval)
        {
            specialEventTimer = 0;

            IsSpecialEvent = true;

            EnemyType enemyType = enemyTypes[Random.Range(0, enemyTypes.Length)];
            StartCoroutine(SpawnSpecialEnemy(enemyType));

            cameraEffect.darkIntensity = 0.45f;

            var specialEventObj = SpwanWorldTxt($"{enemyType.ToString()}来袭！",1.0f);
            StartCoroutine(ShowFlashWarningTxt(specialEventObj));

            player.GetComponent<Player>().SetWeaponAttackRange(3);
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
        //if (Input.GetKeyDown(KeyCode.C))
        //{
        //    ExecuteTimeStop();
        //}
        //if (Input.GetKeyDown(KeyCode.V))
        //{
        //    ExecuteBlackHole(player.transform.position);
        //}
        //if (Input.GetKeyDown(KeyCode.R))
        //{
        //    ExecuteNuke();
        //}
        if (Input.GetKeyDown(KeyCode.P))
        {
            // 直接进入特殊事件
            specialEventTimer = nextSpecialEventInterval;
        }
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
        skillCooldownTimer = totalSkillCooldownTime;
        switch (player.GetComponent<Player>().playerType)
        {
            case PlayerType.Normal:
                ExecuteUnstoppable();
                break;
            case PlayerType.BlackHole:
                ExecuteBlackHole(transform.position);
                break;
            case PlayerType.TimeStop:
                ExecuteTimeStop();
                break;
            case PlayerType.Rage:
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
        StartCoroutine(ResetUnstoppable());
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

        StartCoroutine(NukeEffect());

        for (int i = DataManager.allEnemyDict.Count - 1; i >= 0; i--)
        {
            if (DataManager.allEnemyDict[i] == null)
                continue;

            Enemy enemy =
                DataManager.allEnemyDict[i]
                .GetComponent<Enemy>();

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
        float enemyPressure = (float)DataManager.allEnemyDict.Count / MaxEnemyCount;

        pressureScore += Mathf.FloorToInt(enemyPressure * 50f);

        // 血量越低，压力越高
        pressureScore += Mathf.FloorToInt((1f - playerC.GetHpProgress()) * 40f);

        if (isWave)
        {
            pressureScore += 30;
        }

        // Boss/精英事件期间不用算，但保险
        if (IsSpecialEvent)
        {
            pressureScore += 30;
        }

        // 压力高，延后特殊事件
        if (pressureScore >= 70)
        {
            return 75f;
        }
        else if (pressureScore >= 45)
        {
            return 60f;
        }
        else if (pressureScore >= 25)
        {
            return 45f;
        }
        else
        {
            return 35f;
        }
    }
    void UpdateDynamicDifficulty(out int playerScore)
    {
        Player player = GameManager.Instance.player.GetComponent<Player>();

        // =========================
        // 计算玩家战力
        // =========================

        int levelScore = player.playerData.Level * 5;

        int killScore = player.KilledCount / 10;

        int buildScore = player.buildDict.Count * 8;

        int powerScore = Mathf.FloorToInt(player.playerData.Atk * 10);

        playerPowerScore = levelScore + killScore + buildScore + powerScore;

        // =========================
        // 根据战力修改刷怪
        // =========================

        // 波次间隔
        spawnWaveInterval =Mathf.Clamp(5f - playerPowerScore * 0.02f,1.5f,5f);

        // 每组敌人数量
        enemyCountPerGroup =
            Mathf.Clamp(
                5 + playerPowerScore / 15,
                5,
                25);

        // 最大同时敌群数量
        currentWaveGroupCount =
            Mathf.Clamp(
                1 + playerPowerScore / 40,
                1,
                5);

        Debug.Log("玩家评分:" + playerPowerScore + " 敌群:" + currentWaveGroupCount + " 每组:" + enemyCountPerGroup);
        playerScore = playerPowerScore;
    }

    // 生成特殊敌人
    IEnumerator SpawnSpecialEnemy(EnemyType enemyType)
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
            player.GetComponent<Player>().CurrentBulletCount += 10;
            Debug.Log("尸潮开始");
            safeSide = Random.Range(0, 4);
            Debug.Log("本轮尸潮安全区是：" + (safeSide == 0 ? "左" : safeSide == 1 ? "右" : safeSide == 2 ? "下" : "上"));
            foreach (var enemy in DataManager.allEnemyDict)
            {
                enemy.GetComponent<Enemy>().AddShield();
            }
            mainCamera.backgroundColor = new Color(0.2627f, 0f, 0f);


            var shichao = SpwanWorldTxt("尸潮来袭！");
            StartCoroutine(ShowFlashWarningTxt(shichao));
        }

        // 尸潮持续8秒
        if (isWave && waveTimer >= 8)
        {
            isWave = false;
            waveTimer = 0;
            player.GetComponent<Player>().CurrentBulletCount -= 10;
            Debug.Log("尸潮结束");
            mainCamera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            difficulty = Mathf.Max(1, difficulty * 0.5f); // 尸潮结束后暂时降低难度，给玩家喘息的机会
        }
    }

    IEnumerator ShowFlashWarningTxt(GameObject warningObject)
    {
        warningObject.SetActive(true);
        float timer = 0;
        while (timer < 2)
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
        Destroy(warningObject);
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

        // 难度成长
        //currentWaveGroupCount = Mathf.Clamp(1 + Mathf.FloorToInt(gameTime / 60f), 1, 5);

        //enemyCountPerGroup = Mathf.Clamp(6 + Mathf.FloorToInt(gameTime / 30f), 6, 20);
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
            StartCoroutine(SpawnEnemyGroup(0));

            return;
        }

        // =========================
        // 普通波次

        // 随机生成1~3个敌群
        int groupCount = Random.Range(normalGroupMin, normalGroupMax + 1);

        for (int i = 0; i < groupCount; i++)
        {
            StartCoroutine(SpawnEnemyGroup(i * 0.25f));
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
            count = Random.Range(2, 6);
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

        pdata = new PlayerData
        {
            Level = 1,// 玩家等级
            Hp = 500 + DataManager.myGameData.PermanentHp,// 玩家生命值 = 500 + 永久增加的生命值
            Atk = DataManager.myGameData.PermanentAtk,// 当前玩家攻击力
            MoveSpeed = 5.6f + DataManager.myGameData.PermanentMoveSpeed,// 玩家移动速度
            CurrentWeaponIndex = 0// 玩家当前使用的武器id
        };

        player.GetComponent<Player>().Init(pdata);
    }

    void GenEnemy()
    {
        GameObject newEnemy = Instantiate(Resources.Load<GameObject>("enemy"));
        newEnemy.GetComponent<Enemy>().target = player.transform;
        int enemyId = 0;
        if (isWave)
        {
            enemyId = Random.Range(0, 3);
        }
        newEnemy.GetComponent<Enemy>().SetEnemy(DataManager.enemyDataDict[enemyId]);// 使用序号为enemyId的敌人数据
        float x = 0;
        float y = 0;

        // 0 左 1 右 2 下 3 上
        int side = Random.Range(0, 4);
        if (isWave)
        {
            // 安全方向不生成敌人
            while (side == safeSide)
            {
                side = Random.Range(0, 4);
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
        }

        Vector3 wpos = GetWorldPosByScreenPos(new Vector3(x, y, 0));

        // 保持敌人在2D世界层级
        wpos.z = 0;

        newEnemy.transform.position = wpos;

        DataManager.allEnemyDict.Add(newEnemy);
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

    public GameObject SpwanBulletSingle(BulletData bulletData, Vector3 dir, Vector3 pos, string CurrentUsedBulletPrefab, Entity belongWho)
    {
        //GameObject newBullet_Liner = Instantiate(Resources.Load<GameObject>("bullets/" + CurrentUsedBulletPrefab));
        GameObject newBullet_Liner = BulletPool.Instance.Get(CurrentUsedBulletPrefab);
        newBullet_Liner.transform.position = pos;
        Bullet bullet = newBullet_Liner.GetComponent<Bullet>();
        bullet.SetBulletPrefabId(CurrentUsedBulletPrefab);
        bullet.SetBullet(bulletData, pos, dir, belongWho);
        bullet.CanMove = true;
        return newBullet_Liner;
    }
    public GameObject SpwanChest(Vector3 pos)
    {
        GameObject newChest = SpwanSingleCircle(pos);
        newChest.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("sprites/chest");
        newChest.AddComponent<ChestBall>();
        return newChest;
    }

    public GameObject SpwanCoin(Vector3 pos, int coinValue)
    {
        GameObject newCoin = SpwanSingleCircle(pos);
        newCoin.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("sprites/coin");
        newCoin.AddComponent<CoinBall>().SetCoinValue(coinValue, player);
        return newCoin;
    }
    public GameObject SpwanExpBall(Vector3 pos,EnemyType enemyType, int expValue)
    {
        GameObject newExpBall = SpwanSingleCircle(pos);
        float baseScale = 0.2f;
        switch(enemyType)
        {
            case EnemyType.Normal:
                baseScale = 0.2f;
                break;
            case EnemyType.Thick:
                baseScale = 0.4f;
                break;
            case EnemyType.Elite:
                baseScale = 0.3f;
                break;
            case EnemyType.Boss:
                baseScale = 0.5f;
                break;
        }
        newExpBall.transform.localScale = Vector3.one * baseScale;
        newExpBall.GetComponent<SpriteRenderer>().color = Color.cyan;
        newExpBall.AddComponent<ExpBall>().SetExpValue(expValue, player);
        DataManager.allExpBall.Add(newExpBall);
        return newExpBall;
    }
    public GameObject SpwanSingleCircle(Vector3 pos)// cicle  0.4  0.2
    {
        GameObject newExpBall = Instantiate(Resources.Load<GameObject>("cicle"));
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
    private void OnDisable()
    {
        DataManager.Clear();
        WeaponSystem.Clear();
        lineObjs.Clear();
        foreach (var l in lineObjs)
        {
            Destroy(l);
        }
    }

    private void OnDestroy()
    {
        DataManager.Clear();
        WeaponSystem.Clear();
        lineObjs.Clear();
        foreach (var l in lineObjs)
        {
            Destroy(l);
        }
    }

    private void OnApplicationQuit()
    {
        DataManager.Clear();
        WeaponSystem.Clear();

        lineObjs.Clear();
        foreach (var l in lineObjs)
        {
            Destroy(l);
        }
    }
}