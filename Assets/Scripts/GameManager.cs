using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public GameObject player { get; private set; }

    public PlayerData pdata { get; set; }
    // =========================
    // 之前固定刷怪间隔保留（已弃用）
    // float spwanInterval = 0.5f;
    // =========================

    // 当前刷怪预算
    float enemyBudget = 0;

    // 游戏时间
    float gameTime = 0;

    // 当前难度
    float difficulty = 1;

    // 尸潮相关
    bool isWave = false;
    float waveTimer = 0;

    private Camera mainCamera;

    // 敌人生成到屏幕外的偏移距离
    private float offset = 100f;

    void Start()
    {
        DataManager.Init();

        mainCamera = Camera.main;

        GenPlayer();
    }

    private void Update()
    {
        if (player)
        {
            player.GetComponent<Player>().PlayerUpdate();
        }

        for (int i = DataManager.allEnemyDict.Count - 1; i >= 0; i--)
        {
            GameObject enemy = DataManager.allEnemyDict[i];
            if (enemy)
            {
                enemy.GetComponent<Enemy>().EnemyUpdate();
            }
        }

        WeaponSystem.UpdateWeapons();

        // 游戏时间累计
        gameTime += Time.deltaTime;

        // 难度持续提升
        difficulty = 1 + gameTime * 0.12f;

        // 累积刷怪预算
        enemyBudget += Time.deltaTime * difficulty;

        // 尸潮逻辑
        UpdateWave();

        // 刷怪
        TrySpawnEnemy();
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

            // 尸潮直接增加预算
            enemyBudget += 40;

            Debug.Log("尸潮开始");
        }

        // 尸潮持续8秒
        if (isWave && waveTimer >= 5)
        {
            isWave = false;
            waveTimer = 0;

            Debug.Log("尸潮结束");
            difficulty = 0; // 尸潮结束后暂时降低难度，给玩家喘息的机会
        }
    }

    // 尝试刷怪
    void TrySpawnEnemy()
    {
        if (isWave) return;// 尸潮期间不使用正常的刷怪逻辑，直接通过增加预算来实现大量刷怪
        // 防止一帧生成过多
        int maxSpawnPerFrame = 5;

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
            Hp = 1000,// 玩家生命值
            power = 1.0f,// 当前游戏倍率
            MoveSpeed = 2.5f,// 玩家移动速度
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

    public void SpwanBulletSingle(BulletData bulletData, Vector3 dir, Vector3 pos, int CurrentUsedBulletIndex, Entity belongWho)
    {
        GameObject newBullet_Liner = Instantiate(Resources.Load<GameObject>("bullets/" + CurrentUsedBulletIndex));
        newBullet_Liner.transform.position = pos;
        newBullet_Liner.GetComponent<Bullet>().SetBullet(bulletData, dir, belongWho);
        newBullet_Liner.GetComponent<Bullet>().CanMove = true;
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

    private void OnDisable()
    {
        DataManager.Clear();
        WeaponSystem.Clear();
    }

    private void OnDestroy()
    {
        DataManager.Clear();
        WeaponSystem.Clear();
    }

    private void OnApplicationQuit()
    {
        DataManager.Clear();
        WeaponSystem.Clear();
    }
}