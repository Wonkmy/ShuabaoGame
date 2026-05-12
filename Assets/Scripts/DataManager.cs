using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager
{
    public static Dictionary<int, BulletData> bulletsDataDict = new Dictionary<int, BulletData>();// 子弹数据字典
    public static Dictionary<int, EnemyData> enemyDataDict = new Dictionary<int, EnemyData>();// 怪物数据字典
    public static List<GameObject> allEnemyDict = new List<GameObject>();// 敌人实体字典
    public static List<GameObject> allDamageText =  new List<GameObject>();// 伤害文本字典
    public static List<GameObject> allExpBall = new List<GameObject>();// 经验球字典
    public static Dictionary<int, WeaponData> weaponDataDict = new Dictionary<int, WeaponData>();// 武器数据字典
    public static List<UpgradeData> upgradeList = new List<UpgradeData>();// 升级选项列表

    public static void Init()
    {
        LoadBulletConfig();
        LoadEnemyConfig();
        LoadWeaponConfig();
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
            data.scale = float.Parse(row[3]);

            // CSV中直接写数字
            // 0 Normal
            // 1 Fast
            // 2 Thick
            data.type = (EnemyType)int.Parse(row[4]);
            data.CurrentWeaponIndex = int.Parse(row[5]);

            enemyDataDict[data.id] = data;
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

            weaponDataDict[data.id] = data;
        }
    }

    public static void Clear()
    {
        bulletsDataDict.Clear();
        enemyDataDict.Clear();
        allEnemyDict.Clear();
        weaponDataDict.Clear();
        allDamageText.Clear();
        allExpBall.Clear();
        upgradeList.Clear();
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