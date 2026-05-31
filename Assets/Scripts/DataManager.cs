using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager
{
    public static Dictionary<int, BulletData> bulletsDataDict = new Dictionary<int, BulletData>();// 子弹数据字典
    public static Dictionary<int, EnemyData> enemyDataDict = new Dictionary<int, EnemyData>();// 怪物数据字典
    public static Dictionary<int, WeaponData> weaponDataDict = new Dictionary<int, WeaponData>();// 武器数据字典
    public static List<UpgradeData> upgradeList = new List<UpgradeData>();// 升级选项数据列表

    public static List<GameObject> allEnemyDict = new List<GameObject>();// 敌人实体字典
    public static List<GameObject> allDamageText =  new List<GameObject>();// 伤害文本字典
    public static List<GameObject> allExpBall = new List<GameObject>();// 经验球字典
    public static Dictionary<AirplaneType, AirplaneInfo> playerSkillTypeCDDict = new Dictionary<AirplaneType, AirplaneInfo>();// 玩家技能冷却时间字典

    public static GameData myGameData;
    public static void Init()
    {
        LoadBulletConfig();
        LoadEnemyConfig();
        LoadWeaponConfig();
        LoadUpgradeCsv();
        ConfigSkillCD();

        string dataStr = PlayerPrefs.GetString("gamedata");
        myGameData = new GameData();
        if (dataStr != "" && dataStr != null)
        {
            var _myGameData = JsonUtility.FromJson<GameData>(dataStr);
            myGameData.TotalCoinCount = _myGameData.TotalCoinCount;
            myGameData.PermanentAtk = _myGameData.PermanentAtk;
            myGameData.PermanentHp = _myGameData.PermanentHp;
            myGameData.PermanentMoveSpeed = _myGameData.PermanentMoveSpeed;
            myGameData.PermanentCrit = _myGameData.PermanentCrit;
            myGameData.playerType = _myGameData.playerType;
        }
        else
        {
            myGameData.TotalCoinCount = 0;
            myGameData.PermanentAtk = 0;
            myGameData.PermanentHp = 0;
            myGameData.PermanentMoveSpeed = 0;
            myGameData.PermanentCrit = 0;
            myGameData.playerType = AirplaneType.Normal;
        }
    }

    /// <summary>
    /// 预热对象池，提前加载需要频繁使用的预制体，避免游戏过程中出现卡顿
    /// </summary>
    public static void PrewarmPools()
    {
        // 预热BulletPoll，提前加载子弹预制体
        foreach (var b in bulletsDataDict)
        {
            BulletPool.Instance.Prewarm(b.Value.prefabString, 50);
        }

        // 预热ExpBallPool，提前加载经验球预制体
        ExpBallPool.Instance.Prewarm("expBall", 50);

        // 预热DeadFXPool，提前加载死亡特效预制体
        DeadFXPool.Instance.Prewarm("deadFX", 50);
        DeadFXPool.Instance.Prewarm("bigDeadFX", 50);
    }

    static void ConfigSkillCD()
    {
        playerSkillTypeCDDict[AirplaneType.Normal] = new AirplaneInfo
        {
            id = 0,
            name = "Normal",
            desc = "No special skill.",
            skillCD = 10f,
            iconString = "PlayerSkillTypeIcon/skill_invincible"
        };
        playerSkillTypeCDDict[AirplaneType.BlackHole] = new AirplaneInfo
        {
            id = 1,
            name = "Black Hole",
            desc = "Summon a black hole that pulls in nearby enemies.",
            skillCD = 30f,
            iconString = "PlayerSkillTypeIcon/skill_blackhole"
        };
        playerSkillTypeCDDict[AirplaneType.TimeStop] = new AirplaneInfo
        {
            id = 2,
            name = "Time Stop",
            desc = "Stop time for a short duration, freezing all enemies.",
            skillCD = 35f,
            iconString = "PlayerSkillTypeIcon/skill_timestop"
        };
        playerSkillTypeCDDict[AirplaneType.Rage] = new AirplaneInfo
        {
            id = 3,
            name = "Rage",
            desc = "Unleash a powerful attack that damages all enemies on screen.",
            skillCD = 60f,
            iconString = "PlayerSkillTypeIcon/skill_rage_nuke"
        };
    }
    static void LoadBulletConfig()
    {
        TextAsset csv = Resources.Load<TextAsset>("configs/Bullet");

        string[] lines = csv.text.Split('\n');

        bulletsDataDict.Clear();

        // 第一行是表头，所以从1开始
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string line = lines[i].Replace("\r", "");

            string[] row = line.Split(',');

            BulletData data = new BulletData();

            data.id = int.Parse(row[0]);
            data.moveSpeed = float.Parse(row[1]);
            data.distance = float.Parse(row[2]);
            data.damage = int.Parse(row[3]);
            data.prefabString = row[4];

            bulletsDataDict[data.id] = data;
        }
    }

    static void LoadEnemyConfig()
    {
        TextAsset csv = Resources.Load<TextAsset>("configs/Enemy");

        string[] lines = csv.text.Split('\n');

        enemyDataDict.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string line = lines[i].Replace("\r", "");

            string[] row = line.Split(',');

            EnemyData data = new EnemyData();

            data.id = int.Parse(row[0]);
            data.moveSpeed = float.Parse(row[1]);
            data.hp = int.Parse(row[2]);
            data.damage = float.Parse(row[3]);
            data.scale = float.Parse(row[4]);
            data.type = (EnemyType)int.Parse(row[5]);
            data.CurrentWeaponIndex = int.Parse(row[6]);

            enemyDataDict[data.id] = data;
        }
    }
    static void LoadUpgradeCsv()
    {
        TextAsset text = Resources.Load<TextAsset>("configs/upgrade");

        string[] lines = text.text.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] row = lines[i].Split(',');

            UpgradeData data = new UpgradeData();

            data.id = int.Parse(row[0]);

            data.name = row[1];

            data.tag = row[2];

            data.rarity = row[3];

            data.desc = row[4];

            data.value = float.Parse(row[5]);

            data.type = (UpgradeType)int.Parse(row[6]);

            DataManager.upgradeList.Add(data);
        }
    }
    static void LoadWeaponConfig()
    {
        TextAsset csv = Resources.Load<TextAsset>("configs/Weapon");

        string[] lines = csv.text.Split('\n');

        weaponDataDict.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string line = lines[i].Replace("\r", "");

            string[] row = line.Split(',');

            WeaponData data = new WeaponData();

            data.id = int.Parse(row[0]);
            data.FireInterval = float.Parse(row[1]);
            data.FireAngle = float.Parse(row[2]);
            data.CurrentUsedBulletIndex = int.Parse(row[3]);
            data.Attack = int.Parse(row[4]);
            data.type = (WeaponType)int.Parse(row[5]);
            data.Critical = float.Parse(row[6]);
            data.AttackRange = float.Parse(row[7]);
            weaponDataDict[data.id] = data;
        }
    }

    public static void Clear()
    {
        // 销毁所有敌人
        foreach (var enemy in DataManager.allEnemyDict)
        {
            if (enemy)
            {
                Object.Destroy(enemy);
            }
        }
        // 销毁所有伤害文本
        foreach (var damageText in DataManager.allDamageText)
        {
            if (damageText)
            {
                Object.Destroy(damageText);
            }
        }
        // 销毁所有经验球
        foreach (var expBall in DataManager.allExpBall)
        {
            if (expBall)
            {
                Object.Destroy(expBall);
            }
        }
        allEnemyDict.Clear();
        allDamageText.Clear();
        allExpBall.Clear();
    }

    public static Vector3[] GetFanDirections2D(Vector3 centerDir, int count)
    {
        Vector3[] directions = new Vector3[count];

        // 单发子弹
        if (count <= 1)
        {
            directions[0] = centerDir.normalized;
            return directions;
        }

        // 每发子弹之间间隔角度
        float angleStep = 8f;

        // 根据子弹数量自动计算总角度
        float totalAngle = angleStep * (count - 1);

        // 左右对称
        float startAngle = -totalAngle * 0.5f;

        // 基础方向角度
        float baseAngle = Mathf.Atan2(centerDir.y, centerDir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + startAngle + i * angleStep;

            float rad = angle * Mathf.Deg2Rad;

            directions[i] = new Vector3(
                Mathf.Cos(rad),
                Mathf.Sin(rad),
                0
            );
        }

        return directions;
    }
}