using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class Weapon
{
    public WeaponData weaponData;
    public BulletData bulletData;
    private AttackType weaponAttackType;// 此武器当前的攻击方式
    private AttackData attackData;// 攻击数据包，包含攻击方向、攻击位置、当前子弹数量等信息
    float fireTime = 0.0f;

    public Entity entity { get; set; }// 武器所属的实体，玩家或敌人

    public List<GameObject> spawnedBullets;// 此武器生成的子弹列表

    float fireFlashDuration = 0.2f;// 枪口火花持续时间
    float fireFlashTimer = 0.0f;// 枪口火花计时器
    float fireInterval = 0;// 武器的攻击频率。单独拿出来是后面需要动态修改达到成长与爽感

    public GameObject lockedTarget;// 锁定的目标实体，敌人

    public virtual void Init(int weaponID,Entity _entity)
    {
        weaponData = new WeaponData
        {
             id = DataManager.weaponDataDict[weaponID].id,
             FireInterval = DataManager.weaponDataDict[weaponID].FireInterval,
             FireAngle = DataManager.weaponDataDict[weaponID].FireAngle,
             CurrentUsedBulletIndex = DataManager.weaponDataDict[weaponID].CurrentUsedBulletIndex,
             Attack = DataManager.weaponDataDict[weaponID].Attack,
             type = DataManager.weaponDataDict[weaponID].type,
             Critical = DataManager.weaponDataDict[weaponID].Critical,
        };
        bulletData = new BulletData { 
            id = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].id,
            moveSpeed = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].moveSpeed,
            distance = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].distance,
            damage = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].damage
        };
        entity = _entity;
        spawnedBullets = new List<GameObject>();
        lockedTarget = null;
        fireInterval = weaponData.FireInterval;
    }
    /// <summary>
    /// 更换子弹数据，传入新的子弹ID，根据ID从DataManager中获取新的子弹数据，并更新当前武器的bulletData
    /// 游戏中呈现：玩家通过某些方式（如拾取道具）更换武器的子弹类型，调用此方法来更新武器的子弹数据，使得玩家在攻击时使用新的子弹属性进行攻击
    /// </summary>
    /// <param name="bulletID"></param>
    public void ChangeBullet(int bulletID)
    {
        weaponData.CurrentUsedBulletIndex = bulletID;
        bulletData = new BulletData
        {
            id = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].id,
            moveSpeed = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].moveSpeed,
            distance = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].distance,
            damage = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].damage
        };
    }

    /// <summary>
    /// 修改武器的攻击频率。Note：数值越小，频率越高
    /// </summary>
    /// <param name="v"></param>
    public void ChangeFireInterval(float v)
    {
        fireInterval -= v;
        if (fireInterval <= 0.1f)
        {
            fireInterval = 0.1f;
        }
    }

    public void WeaponUpdate()
    {
        fireTime += Time.deltaTime;
        if (entity.GetNearestTarget() != null)
        {
            GameObject ey = null;
            if (lockedTarget == null)
            {
                ey = entity.GetNearestTarget().gameObject;
            }
            else
            {
                ey = lockedTarget;
            }
            entity.RotateToDetination(ey.transform.position);// 发现目标后立刻转向目标，防止出现未转向目标就攻击的情况
            if (entity != null && ey != null)
            {
                if (fireTime >= fireInterval && Vector3.Distance(entity.transform.position, ey.transform.position) <= 10.0f)
                {
                    fireFlashTimer = 0.0f;
                    lockedTarget = ey;
                    ProcessAttack();
                    fireTime = 0.0f;
                }
            }
        }
    }

    public void ChangeAttackType(AttackType attackType, Entity entity,int _currentBulletCount = 3)
    {
        weaponAttackType = attackType;
        if (entity && entity.gameObject != null)
        {
            attackData = new AttackData
            {
                firePos = attackType == AttackType.Cicle ? entity.transform.position : entity.FirePos.position,
                fireDirection = entity.FireDirection,
                currentBulletCount = _currentBulletCount
            };
        }
    }
    void ProcessAttack()
    {
        // 枪口火花，一个黄色的小球来表示，0.2秒后销毁
        GameObject newExpBall = GameManager.Instance.SpwanSingleCircle(attackData.firePos);
        newExpBall.GetComponent<SpriteRenderer>().color = Color.yellow;
        if (newExpBall)
        {
            while (fireFlashTimer < fireFlashDuration)
            {
                fireFlashTimer += Time.deltaTime;
                // 这里可以添加枪口火花的动画效果，比如缩放和颜色变化
                newExpBall.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one * 0.4f, fireFlashTimer / fireFlashDuration);
            }
            Object.Destroy(newExpBall);
        }

        switch (weaponAttackType)
        {
            case AttackType.Liner:
                AttackLiner(attackData.fireDirection, attackData.firePos, attackData.currentBulletCount);
                break;
            case AttackType.Sector:
                AttackSector(attackData.fireDirection, attackData.firePos, attackData.currentBulletCount);
                break;
            case AttackType.Cicle:
                AttackCicle(attackData.fireDirection, attackData.firePos, attackData.currentBulletCount);
                break;
            default:
                break;
        }
    }
    /// <summary>
    /// 线性单发攻击方式
    /// </summary>
    /// <param name="bulletData"></param>
    /// <param name="fireDirection"></param>
    /// <param name="firePos"></param>
    /// <param name="currentBulletCount"></param>
    public virtual void AttackLiner(Vector3 fireDirection, Vector3 firePos, int currentBulletCount)
    {
        if (currentBulletCount <= 1)
        {
            var bullet = GameManager.Instance.SpwanBulletSingle(bulletData, fireDirection, firePos, 0, entity);
            spawnedBullets.Add(bullet);
        }
        else if (currentBulletCount <= 4)
        {
            for (int i = 0; i < currentBulletCount; i++)
            {
                // 计算currentBulletCount个数量子弹的每发子弹的偏移量，偏移量的方向垂直于攻击方向，大小为0.3f
                Vector3 offset = Vector3.Cross(fireDirection, Vector3.forward).normalized * 0.3f * (i - (currentBulletCount - 1) / 2.0f);
                var bullet = GameManager.Instance.SpwanBulletSingle(bulletData, fireDirection, firePos + offset, 0, entity);
                spawnedBullets.Add(bullet);
            }
        }
        // 如果子弹数量大于4，则转为扇形攻击方式
        else
        {
            AttackSector(fireDirection, firePos, currentBulletCount);
        }
    }

    /// <summary>
    /// 扇形攻击方式，发出currentBulletCount发子弹，子弹之间的夹角为fireAngle/currentBulletCount
    /// </summary>
    /// <param name="bulletData"></param>
    /// <param name="fireAngle"></param>
    /// <param name="fireDirection"></param>
    /// <param name="firePos"></param>
    /// <param name="currentBulletCount"></param>
    public virtual void AttackSector(Vector3 fireDirection, Vector3 firePos, int currentBulletCount)
    {
        var allDires = DataManager.GetFanDirections2D(fireDirection, currentBulletCount);
        for (int i = 0; i < allDires.Length; i++)
        {
            var bullet = GameManager.Instance.SpwanBulletSingle(bulletData, allDires[i], firePos, 0, entity);
            // 如果i是总数的中间的那个子弹，则给这个子弹添加一个额外的效果
            if(i == currentBulletCount / 2)
            {
                bullet.GetComponent<Bullet>().canTriggerHitStop = true;
            }
            spawnedBullets.Add(bullet);
        }
    }

    /// <summary>
    /// 环形攻击方式，发出currentBulletCount发子弹，子弹之间的夹角为360/currentBulletCount
    /// </summary>
    /// <param name="bulletData"></param>
    /// <param name="fireDirection"></param>
    /// <param name="firePos"></param>
    /// <param name="currentBulletCount"></param>
    public virtual void AttackCicle(Vector3 fireDirection, Vector3 firePos, int currentBulletCount)
    {
        for (int i = 0; i < currentBulletCount; i++)
        {
            float angle = (360.0f / currentBulletCount) * i;
            Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
            var bullet = GameManager.Instance.SpwanBulletSingle(bulletData, dir, firePos, 0, entity);
            spawnedBullets.Add(bullet);
        }
    }
}
