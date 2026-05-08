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
    public float scale;// 体型
    public EnemyType type;// 怪物类型
}
public enum WeaponType
{
   Normal
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
public struct WeaponData
{
    public int id;
    public float FireInterval;// 开火间隔
    public float FireAngle;// 开火攻击角度范围
    public int CurrentUsedBulletIndex;// 当前使用的子弹类型索引
    public int Attack;// 武器攻击力
}
public class PlayerData
{
    public int Level;// 玩家等级
    public float Hp;// 玩家血量
    public float power;// 玩家攻击倍率
    public float MoveSpeed;
    public WeaponType CurrentWeaponType;// 持有的武器类型
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