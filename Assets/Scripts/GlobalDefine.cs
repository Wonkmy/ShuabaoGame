public enum BulletType
{
    /// <summary>
    /// 线性子弹
    /// </summary>
    Liner,
    /// <summary>
    /// 扇形子弹 
    /// </summary>
    Sector,
    /// <summary>
    /// 环形子弹
    /// </summary>
    Cicle
}
public struct BulletData
{
    /// <summary>
    /// 速度
    /// </summary>
    public float moveSpeed;
    /// <summary>
    /// 距离
    /// </summary>
    public float distance;

    public BulletType type;
}
public enum EnemyType
{
    /// <summary>
    /// 普通怪
    /// </summary>
    Normal,
    /// <summary>
    /// 快速怪
    /// </summary>
    Fast,
    /// <summary>
    /// 血厚怪
    /// </summary>
    Thick,
    /// <summary>
    ///  自爆怪
    /// </summary>
    SelfExplosion,
    /// <summary>
    /// 精英怪
    /// </summary>
    Elite,
    /// <summary>
    /// Boss怪
    /// </summary>
    Boss
}
public struct EnemyData
{
    public float moveSpeed;// 移动速度
    public int hp;// 血量
    public float scale;// 体型
    public EnemyType type;// 怪物类型
}