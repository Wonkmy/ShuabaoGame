using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager
{
    public static Dictionary<int, BulletData> bulletsDataDict = new Dictionary<int, BulletData>();// 子弹数据字典，将所有子弹全部抽象成可配置的数据来进行配置
    public static Dictionary<int, EnemyData> enemyDataDict = new Dictionary<int, EnemyData>();// 怪物数据字典，将所有怪物全部抽象成可配置的数据来进行配置
    public static List<GameObject> allEnemyDict = new List<GameObject>();// 所有敌人字典
    public static void Init()
    {
        // ======= 子弹数据配置 =======
        bulletsDataDict.Add(0, new BulletData
        {
            moveSpeed = 8,// 子弹移动速度
            distance = 15,// 子弹飞行距离
        });

        bulletsDataDict.Add(1, new BulletData
        {
            moveSpeed = 12,// 子弹移动速度
            distance = 18,// 子弹飞行距离
        });
        bulletsDataDict.Add(2, new BulletData
        {
            moveSpeed = 15,// 子弹移动速度
            distance = 20,// 子弹飞行距离
        });

        // ======= 怪物数据配置 =======
        enemyDataDict.Add(0, new EnemyData
        {
            moveSpeed = 3,// 怪物移动速度
            hp = 10,// 怪物血量
            scale = 1.0f,
            type = EnemyType.Normal// 怪物类型
        });

        enemyDataDict.Add(1, new EnemyData {
            moveSpeed = 6,// 怪物移动速度
            hp = 5,// 怪物血量
            scale = 0.85f,
            type = EnemyType.Fast// 怪物类型
        });
        enemyDataDict.Add(2, new EnemyData
        {
            moveSpeed = 1,// 怪物移动速度
            hp = 50,// 怪物血量
            scale = 1.5f,
            type = EnemyType.Thick// 怪物类型
        });

        enemyDataDict.Add(3, new EnemyData
        {
            moveSpeed = 4.5f,// 怪物移动速度
            hp = 12,// 怪物血量
            scale = 1.2f,
            type = EnemyType.SelfExplosion// 怪物类型
        });
        enemyDataDict.Add(4, new EnemyData
        {
            moveSpeed = 5,// 怪物移动速度
            hp = 20,// 怪物血量
            scale = 1.3f,
            type = EnemyType.Elite// 怪物类型
        });

        enemyDataDict.Add(5, new EnemyData
        {
            moveSpeed = 4,// 怪物移动速度
            hp = 100,// 怪物血量
            scale = 1.8f,
            type = EnemyType.Boss// 怪物类型
        });
    }

    public static void Clear()
    {
        bulletsDataDict.Clear();
        allEnemyDict.Clear();
    }

    public static Vector3[] GetFanDirections2D(Vector3 centerDir, float totalAngle = 60f, float angleStep = 15f, bool forwardCount = false)
    {
        int count;
        float startAngle;

        if (forwardCount)
        {
            count = Mathf.FloorToInt(totalAngle / angleStep) + 1;
            startAngle = 0f;
        }
        else
        {
            float halfAngle = totalAngle * 0.5f;
            count = Mathf.FloorToInt(totalAngle / angleStep) + 1;
            startAngle = -halfAngle;
        }

        Vector3[] directions = new Vector3[count];

        // 计算中心方向的基础角度（从X轴正方向逆时针）
        float baseAngle = Mathf.Atan2(centerDir.y, centerDir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + startAngle + i * angleStep;
            float rad = angle * Mathf.Deg2Rad;
            directions[i] = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
        }

        return directions;
    }
}
