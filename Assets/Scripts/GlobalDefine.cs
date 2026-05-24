using System.Collections.Generic;
using UnityEngine;

public struct BulletData
{
    public int id;
    /// <summary>
    /// 速度
    /// </summary>
    public float moveSpeed;
    /// <summary>
    /// 距离
    /// </summary>
    public float distance;
    /// <summary>
    /// 伤害
    /// </summary>
    public float damage;
    /// <summary>
    /// 子弹预制体名字字符串
    /// </summary>
    public string prefabString;
}
public enum EnemyType
{
    /// <summary>
    /// 普通怪
    /// </summary>
    Normal = 0,
    /// <summary>
    /// 快速怪
    /// </summary>
    Fast = 1,
    /// <summary>
    /// 血厚怪
    /// </summary>
    Thick = 2,
    /// <summary>
    ///  自爆怪
    /// </summary>
    SelfExplosion = 3,
    /// <summary>
    /// 精英怪
    /// </summary>
    Elite = 4,
    /// <summary>
    /// Boss怪
    /// </summary>
    Boss = 5
}
public struct EnemyData
{
    public int id;
    public float moveSpeed;// 移动速度
    public int hp;// 血量
    public float damage;// 伤害
    public float scale;// 体型
    public EnemyType type;// 怪物类型
    public int CurrentWeaponIndex;// 持有的武器id
}
public enum AttackType
{
    /// <summary>
    /// 线性攻击方式
    /// </summary>
    Liner,
    /// <summary>
    /// 扇形攻击方式
    /// </summary>
    Sector,
    /// <summary>
    /// 环形攻击方式
    /// </summary>
    Cicle
}
/// <summary>
/// 武器类型
/// 这里并不是定义的武器类型，而是具体的武器实体类。例如：这是Normal的武器，还是敌人专属武器，还是玩家专属，还是特殊敌人的专属武器。而不是步枪、散弹枪、狙击枪等武器类型
/// Normal类型代表所有角色都可使用
/// </summary>
public enum WeaponType
{
    Normal,
    /// <summary>
    /// 敌人专属武器，玩家无法获得
    /// </summary>
    Barrage,
    /// <summary>
    /// 精英怪专属武器，玩家无法获得
    /// </summary>
    EliteGun,
    /// <summary>
    /// 激光武器，玩家专属武器，具有特殊攻击方式和效果
    /// </summary>
    Laser,
}
public struct WeaponData
{
    public int id;
    public float FireInterval;// 开火间隔
    public float FireAngle;// 开火攻击角度范围
    public int CurrentUsedBulletIndex;// 当前使用的子弹类型索引
    public int Attack;// 武器攻击力
    public WeaponType type;
    public float Critical;// 暴击倍率
    public float AttackRange;// 武器攻击范围
}
public class PlayerData
{
    public int Level;// 玩家等级
    public float Hp;// 玩家血量
    public float Atk;// 玩家攻击力
    public float MoveSpeed;// 玩家移动速度
    public float Def;// 玩家防御力
}

public struct AirplaneInfo
{
    public int id;
    public string name;
    public string desc;
    public float skillCD;
}

public enum AirplaneType
{
    Normal,// 普通飞机
    BlackHole,// 黑洞技能飞机
    TimeStop,// 时间停止技能飞机
    Rage// 核爆（清屏）技能飞机
}

[System.Serializable]
public class GameData { 
    public int TotalCoinCount;// 总金币数
    public int PermanentAtk;// 永久攻击力
    public int PermanentHp;// 永久血量
    public float PermanentMoveSpeed = 0;// 永久移动速度
    public float PermanentCrit = 0;// 永久暴击
    public AirplaneType playerType;// 飞机类型
}

/// <summary>
/// 攻击数据包
/// </summary>
public struct AttackData
{
    public Vector3 fireDirection;
    public Vector3 firePos;
    public int currentBulletCount;
}

//升级数据结构
public class UpgradeData
{
    public string name;
    // 流派tag
    public string tag;
    public int id;

    public string desc;

    // normal rare epic legendary
    public string rarity;

    // attack bullet_count crit_explosion ...
    public UpgradeType type;

    public float value;
    public System.Action action;
}

public enum UpgradeType
{
    // 子弹数量增加
    BulletCount = 0,

    // 重型弹头：子弹变大并提升伤害
    HeavyBullet = 1,

    // 子弹穿透数量增加
    Pierce = 2,

    // 玩家最终攻击倍率增加
    AtkRatio = 3,

    // 大幅增加移速，但降低部分攻击
    MoveFast = 4,

    // 高伤害、低移速、高攻速模式
    HeavyMode = 5,

    // 暴击时触发范围爆炸
    CritExplosion = 6,

    // 穿透后触发范围爆炸
    PierceExplosion = 7,

    // 子弹数量减少，但单发伤害极高
    LowBulletHighDamage = 8,

    // 子弹命中后再次分裂
    LegendSplit = 9,

    // 大幅提升攻速
    LegendFire = 10,

    // 提高暴击率
    CritChance = 11,

    // 提高开火频率
    FireRate = 12,

    // 强化每隔数次触发的强力齐射
    EnhancedShot = 13,
}
