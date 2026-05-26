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
    float attack = 0;// 武器的攻击力。单独拿出来是后面需要动态修改达到成长与爽感
    int bulletPierce = 0;// 武器的子弹穿透力。单独拿出来是后面需要动态修改达到成长与爽感
    bool attackWarningIssued = false;
    public float attackRange { get; set; }// 武器的攻击范围，超过这个范围就不攻击了

    public GameObject lockedTarget;// 锁定的目标实体，敌人

    protected float bulletSclae = 0f;// 子弹的缩放，后面技能可能会修改这个值来达到子弹变大变小的效果

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
             AttackRange = DataManager.weaponDataDict[weaponID].AttackRange
        };
        bulletData = new BulletData
        {
            id = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].id,
            moveSpeed = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].moveSpeed,
            distance = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].distance,
            damage = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].damage,
            prefabString = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].prefabString
        };
        entity = _entity;
        spawnedBullets = new List<GameObject>();
        lockedTarget = null;
        attack = weaponData.Attack;
        attackRange = weaponData.AttackRange;
        if (this.entity.EntityTag == "enemy")
        {
            fireInterval = weaponData.FireInterval + Random.Range(-0.1f, 0.1f);
            bulletPierce = 1;
            //Debug.Log("敌人武器攻击范围：" + attackRange);
        }
        else
        {
            fireInterval = weaponData.FireInterval;
            bulletPierce = 2;
            //Debug.Log("玩家武器攻击范围：" + attackRange);
        }
    }

    public void SetWeaponAttackRange(float v)
    {
        attackRange += v;
        if (attackRange >= 15)
        {
            attackRange = 15;
        }
        else if(attackRange <= 10)
        {
            attackRange = 10;
        }
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
            damage = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].damage,
            prefabString = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].prefabString
        };
    }
    /// <summary>
    /// 修改武器的子弹穿透力
    /// </summary>
    /// <param name="v"></param>
    public void ChangeBulletPierce(int v)
    {
        bulletPierce += v;
        if(bulletPierce >= 5)// 穿透力最大为5
        {
            bulletPierce = 5;
        }
    }
    public void ChangeAttack(int v)
    {
        attack += v;
        bulletSclae += 0.2f;
    }

    public void ChangeCritical(float v)
    {
        weaponData.Critical = Mathf.Clamp(weaponData.Critical + v, 0f, 0.65f);
    }

    public void ChangeBulletScale(float v)
    {
        if (bulletSclae >= 4.5f) return;
        bulletSclae = bulletSclae + v;
    }

    public float GetAttack()
    {
        return attack;
    }
    /// <summary>
    /// 修改武器的攻击频率。Note：数值越小，频率越高
    /// </summary>
    /// <param name="v"></param>
    public void ChangeFireInterval(float v)
    {
        fireInterval += v;
        if (fireInterval <= 0.1f)
        {
            fireInterval = 0.1f;
        }
    }

    public void SetFireInterval(float v)
    {
        fireInterval = Mathf.Max(0.1f, v);
    }

    public float GetFireInterval()
    {
        return fireInterval;
    }

    protected void TryApplyEnhancedShot(GameObject bulletObj)
    {
        if (entity == null || entity.EntityTag != "player" || bulletObj == null)
            return;

        Player player = entity as Player;
        if (player == null || !player.IsEnhancedShotActive)
            return;

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet == null)
            return;

        bullet.IsEnhancedShot = true;
        bullet.EnhancedShotDamageMultiplier = player.EnhancedShotDamageMultiplier;
        bullet.PierceLeft += player.EnhancedShotBonusPierce;

        bulletObj.transform.localScale *= player.EnhancedShotScaleMultiplier;

        SpriteRenderer sr = bulletObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(1f, 0.85f, 0.35f, 1f);
        }
    }
    public void WeaponUpdate()
    {
        fireTime += Time.deltaTime;
        // 目标失效
        if (lockedTarget != null)
        {
            Entity lockEntity = lockedTarget.GetComponent<Entity>();

            if (lockEntity == null || lockEntity.Dead)
            {
                lockedTarget = null;
                attackWarningIssued = false;
            }
        }
        if (entity.GetNearestTarget() == null && lockedTarget == null)
        {
            return;
        }
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

            // =========================
            // 战场激活判断
            // =========================
            Enemy enemy = ey.GetComponent<Enemy>();

            if (enemy != null)
            {
                // 敌人未真正进入战场
                if (!enemy.IsBattleActive)
                {
                    lockedTarget = null;

                    return;
                }
            }

            entity.RotateToDetination(ey.transform.position);

            if (entity != null && ey != null)
            {
                float warningLeadTime = GetAttackWarningLeadTime();
                if (!attackWarningIssued &&
                    warningLeadTime > 0f &&
                    fireTime >= Mathf.Max(0f, fireInterval - warningLeadTime) &&
                    Vector3.Distance(entity.transform.position, ey.transform.position) <= attackRange)
                {
                    attackWarningIssued = true;
                    OnBeforeAttackWarning();
                }

                if (fireTime >= fireInterval)
                {
                    if(Vector3.Distance(entity.transform.position, ey.transform.position) <= attackRange)
                    {
                        fireFlashTimer = 0.0f;

                        lockedTarget = ey;

                        OnBeforeProcessAttack();
                        ProcessAttack();
                        OnAfterProcessAttack();

                        fireTime = 0.0f;
                        attackWarningIssued = false;
                    }
                    else
                    {
                        lockedTarget = null;
                        attackWarningIssued = false;
                    }
                }
            }
        }
    }

    protected virtual float GetAttackWarningLeadTime()
    {
        return 0f;
    }

    protected virtual void OnBeforeAttackWarning()
    {
    }

    protected virtual void OnBeforeProcessAttack()
    {
    }

    protected virtual void OnAfterProcessAttack()
    {
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
        Player player = null;

        if (entity != null && entity.EntityTag == "player")
        {
            player = entity as Player;
            if (player != null)
            {
                player.ApplyFireRecoil(attackData.fireDirection, player != null && player.IsEnhancedShotActive ? 0.12f : 0.05f);
                player.BeginFireCast();
            }
        }

        AudioManager.instance.PlaySounds("shoot");

        // 枪口火花
        GameObject newExpBall = GameManager.Instance.SpwanMuzzleflash(attackData.firePos);
        newExpBall.transform.rotation = Quaternion.FromToRotation(Vector3.up, attackData.fireDirection);

        if (player != null)
        {
            if (player.IsEnhancedShotActive)
            {
                newExpBall.transform.localScale *= 1.35f;
                GameManager.Instance.ShakeMainCamera(0.06f, 0.08f);
            }
            else
            {
                float vv = Random.value;
                if (vv < 0.3f)
                {
                    GameManager.Instance.ShakeMainCamera(0.02f, 0.04f);
                }
            }
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

        if (player != null)
        {
            player.EndFireCast();
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
            var bullet = GameManager.Instance.SpwanBulletSingle(bulletData, fireDirection, firePos, bulletSclae, entity.EntityTag, entity);
            TryApplyEnhancedShot(bullet);
            spawnedBullets.Add(bullet);
        }
        else if (currentBulletCount <= 4)
        {
            for (int i = 0; i < currentBulletCount; i++)
            {
                // 计算currentBulletCount个数量子弹的每发子弹的偏移量，偏移量的方向垂直于攻击方向，大小为0.3f
                Vector3 offset = Vector3.Cross(fireDirection, Vector3.forward).normalized * 0.3f * (i - (currentBulletCount - 1) / 2.0f);
                var bullet = GameManager.Instance.SpwanBulletSingle(bulletData, fireDirection, firePos + offset, bulletSclae, entity.EntityTag, entity);
                TryApplyEnhancedShot(bullet);
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
            var bullet = GameManager.Instance.SpwanBulletSingle(bulletData, allDires[i], firePos, bulletSclae, entity.EntityTag, entity);

            // 如果i是总数的中间的那个子弹，则给这个子弹添加一个额外的效果
            if (i == currentBulletCount / 2)
            {
                bullet.GetComponent<Bullet>().canTriggerHitStop = true;
                bullet.GetComponent<Bullet>().PierceLeft = bulletPierce;
            }

            TryApplyEnhancedShot(bullet);
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
        Debug.Log("环形射击下需要多少颗子弹：" + currentBulletCount);
        for (int i = 0; i < currentBulletCount; i++)
        {
            float angle = (360.0f / currentBulletCount) * i;
            Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
            var bullet = GameManager.Instance.SpwanBulletSingle(bulletData, dir, firePos, bulletSclae, entity.EntityTag, entity);
            bullet.GetComponent<Bullet>().PierceLeft = bulletPierce;

            TryApplyEnhancedShot(bullet);
            spawnedBullets.Add(bullet);
        }
    }
}
