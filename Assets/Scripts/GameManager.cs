// 当前项目的chatgpt聊天对话“CrazyGames 游戏类型分析”
// 具体的游戏设计在聊天对话的这个位置，直接搜索关键句即可：“好，那我们一起讨论细你说的建议”

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms;

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
    public PlayerData pdata { get; set; }

    // 当前刷怪预算
    float enemyBudget = 0;

    // 游戏时间
    float gameTime = 0;

    // 当前难度
    float difficulty = 1;

    // 尸潮相关
    bool isWave = false;
    float waveTimer = 0;
    int maxSpawnPerFrame = 5;

    public Camera mainCamera { get; set; }
    public CameraEffect cameraEffect { get; set; }

    // 敌人生成到屏幕外的偏移距离
    private float offset = 100f;

    // 震屏幕相关
    private float shakeTime = 0;
    private float shakeDuration = 0;
    private float shakeStrength = 0;

    private Vector3 cameraOriginPos;

    // 命中顿帧效果相关
    public float HitStopDuration = 0.1f;
    public float HitStopIntensity = 0.5f;

    GameObject warningObject;

    void Start()
    {
        DataManager.Init();

        // 基础难度固定
        difficulty = 3;

        mainCamera = Camera.main;
        cameraEffect = mainCamera.GetComponent<CameraEffect>();
        cameraOriginPos = mainCamera.transform.localPosition;

        GenPlayer();

        GameObject expobj = Instantiate(Resources.Load<GameObject>("exp"));
        GameObject hpobj = Instantiate(Resources.Load<GameObject>("hp"));

        playerExpSlider = expobj.transform;
        playerHpSlider = hpobj.transform;

        warningObject = SpwanWorldTxt("尸潮来袭！");
        warningObject.transform.position = Vector3.zero;
        warningObject.SetActive(false);
    }

    private void Update()
    {
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
        }

        for (int i = DataManager.allEnemyDict.Count - 1; i >= 0; i--)
        {
            GameObject enemy = DataManager.allEnemyDict[i];
            if (enemy)
            {
                enemy.GetComponent<Enemy>().EnemyUpdate();
            }
        }

        for (int i = DataManager.allDamageText.Count - 1; i >=0 ; i--)
        {
            DamageText damageText = DataManager.allDamageText[i].GetComponent<DamageText>();
            if (damageText.Dead) {
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
        difficulty = Mathf.Clamp(2 + Mathf.FloorToInt(gameTime / 30f), 2, 8);

        // 累积刷怪预算
        enemyBudget += Time.deltaTime * difficulty;
        
        // 尸潮逻辑
        UpdateWave();
        if (isWave)
        {
            enemyBudget += Time.deltaTime * 25;
        }
        // 刷怪
        TrySpawnEnemy();

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
    }

    // 尸潮逻辑
    void UpdateWave()
    {
        waveTimer += Time.deltaTime;

        // 每30秒触发一次尸潮
        if (!isWave && waveTimer >= 15)
        {
            isWave = true;
            waveTimer = 0;
            player.GetComponent<Player>().ChangeWeaponAttackType(AttackType.Sector, 10);
            Debug.Log("尸潮开始");
            foreach (var enemy in DataManager.allEnemyDict)
            {
                enemy.GetComponent<Enemy>().ChangeWeaponAttackType(AttackType.Sector);
            }
            mainCamera.backgroundColor = new Color(0.2627f, 0f, 0f);
            StartCoroutine(ShowFlashWarningTxt());
        }

        // 尸潮持续8秒
        if (isWave && waveTimer >= 8)
        {
            isWave = false;
            waveTimer = 0;
            player.GetComponent<Player>().CurrentBulletCount = 3;
            player.GetComponent<Player>().ChangeWeaponAttackType(AttackType.Sector);
            Debug.Log("尸潮结束");
            foreach (var enemy in DataManager.allEnemyDict)
            {
                enemy.GetComponent<Enemy>().ChangeWeaponAttackType(AttackType.Liner, 1);
            }
            mainCamera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            difficulty = Mathf.Max(1, difficulty * 0.5f); // 尸潮结束后暂时降低难度，给玩家喘息的机会
        }
    }

    IEnumerator ShowFlashWarningTxt()
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
        warningObject.SetActive(false);
    }

    // 尝试刷怪
    void TrySpawnEnemy()
    {
        if (isWave)
        {
            maxSpawnPerFrame = 10;
        }
        else
        {
            maxSpawnPerFrame = 4;
        }
        int currentSpawnCount = 0;
        while (enemyBudget >= 5 && currentSpawnCount < maxSpawnPerFrame)
        {
            enemyBudget -= 5;
            GenEnemy(0);
            currentSpawnCount++;
        }
    }

    void GenPlayer()
    {
        player = Instantiate(Resources.Load<GameObject>("player"));

        player.transform.position = Vector3.zero;

        pdata = new PlayerData
        {
            Level = 1,// 玩家等级
            Hp = 10000,// 玩家生命值
            power = 1.0f,// 当前游戏倍率
            MoveSpeed = 3.5f,// 玩家移动速度
            CurrentWeaponIndex = 0// 玩家当前使用的武器id
        };

        player.GetComponent<Player>().Init(pdata);
    }

    void GenEnemy(int eid)
    {
        GameObject newEnemy = Instantiate(Resources.Load<GameObject>("enemy"));
        newEnemy.GetComponent<Enemy>().target = player.transform;
        newEnemy.GetComponent<Enemy>().SetEnemy(DataManager.enemyDataDict[eid]);// 使用序号为0的敌人数据
        float x = 0;
        float y = 0;

        // 0 左 1 右 2 下 3 上
        int side = Random.Range(0, 4);

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

    public GameObject SpwanBulletSingle(BulletData bulletData, Vector3 dir, Vector3 pos, int CurrentUsedBulletIndex, Entity belongWho)
    {
        GameObject newBullet_Liner = Instantiate(Resources.Load<GameObject>("bullets/" + CurrentUsedBulletIndex));
        newBullet_Liner.transform.position = pos;
        newBullet_Liner.GetComponent<Bullet>().SetBullet(bulletData, dir, belongWho);
        newBullet_Liner.GetComponent<Bullet>().CanMove = true;
        return newBullet_Liner;
    }

    public GameObject SpwanExpBall(Vector3 pos, int expValue)
    {
        GameObject newExpBall = SpwanSingleCircle(pos);
        newExpBall.transform.localScale = Vector3.one * 0.2f;
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

    public GameObject SpwanWorldTxt(string txt)
    {
        GameObject newWarningTxt = Instantiate(Resources.Load<GameObject>("warning_txt"));
        newWarningTxt.GetComponent<TextMesh>().color = Color.red;
        newWarningTxt.GetComponent<TextMesh>().text = txt;
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

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();

        style.fontSize = 24;
        style.normal.textColor = Color.white;

        GUILayout.BeginArea(new Rect(20, 20, 500, 1000));

        GUILayout.Label("===== DEBUG =====", style);

        GUILayout.Label("Game Time : " + gameTime.ToString("F1"), style);

        GUILayout.Label("Difficulty : " + difficulty, style);

        GUILayout.Label("Enemy Budget : " + enemyBudget.ToString("F1"), style);

        GUILayout.Label("Enemy Count : " + DataManager.allEnemyDict.Count, style);

        GUILayout.Label("Player Level : " + pdata.Level, style);

        GUILayout.Label("Is Wave : " + isWave, style);

        GUILayout.EndArea();
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