using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon
{
    public WeaponData weaponData;
    public BulletData bulletData;
    private AttackType weaponAttackType;// 此武器当前的攻击方式
    private AttackData attackData;// 攻击数据包，包含攻击方向、攻击位置、当前子弹数量等信息
    float fireTime = 0.0f;

    public virtual void Init(WeaponType weaponType)
    {
        weaponData = new WeaponData
        {
             id = DataManager.weaponDataDict[(int)weaponType].id,
             FireInterval = DataManager.weaponDataDict[(int)weaponType].FireInterval,
             FireAngle = DataManager.weaponDataDict[(int)weaponType].FireAngle,
             CurrentUsedBulletIndex = DataManager.weaponDataDict[(int)weaponType].CurrentUsedBulletIndex,
             Attack = DataManager.weaponDataDict[(int)weaponType].Attack
        };
        bulletData = new BulletData { 
            id = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].id,
            moveSpeed = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].moveSpeed,
            distance = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].distance,
            damage = DataManager.bulletsDataDict[weaponData.CurrentUsedBulletIndex].damage
        };
    }
    public void WeaponAttack()
    {
        fireTime += Time.deltaTime;
        if (fireTime >= weaponData.FireInterval)
        {
            ProcessAttack();
            fireTime = 0.0f;
        }
    }

    public void ChangeAttackType(AttackType attackType, Player player)
    {
        weaponAttackType = attackType;
        attackData = new AttackData
        {
            firePos = attackType == AttackType.Cicle ? player.transform.position : player.FirePos.position,
            fireDirection = player.FireDirection,
            currentBulletCount = player.CurrentBulletCount
        };
    }
    void ProcessAttack()
    {
        switch (weaponAttackType)
        {
            case AttackType.Liner:
                AttackLiner(attackData.fireDirection, attackData.firePos, attackData.currentBulletCount);
                break;
            case AttackType.Sector:
                AttackSector(weaponData.FireAngle, attackData.fireDirection, attackData.firePos, attackData.currentBulletCount);
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
            GameManager.Instance.SpwanBulletSingle(bulletData, fireDirection, firePos, weaponData.CurrentUsedBulletIndex);
        }
        else if (currentBulletCount <= 4)
        {
            for (int i = 0; i < currentBulletCount; i++)
            {
                // 计算currentBulletCount个数量子弹的每发子弹的偏移量，偏移量的方向垂直于攻击方向，大小为0.3f
                Vector3 offset = Vector3.Cross(fireDirection, Vector3.forward).normalized * 0.3f * (i - (currentBulletCount - 1) / 2.0f);
                GameManager.Instance.SpwanBulletSingle(bulletData, fireDirection, firePos + offset, weaponData.CurrentUsedBulletIndex);
            }
        }
        // 如果子弹数量大于4，则转为扇形攻击方式
        else
        {
            AttackSector(120, fireDirection, firePos, currentBulletCount);
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
    public virtual void AttackSector(float fireAngle, Vector3 fireDirection, Vector3 firePos, int currentBulletCount)
    {
        var allDires = DataManager.GetFanDirections2D(fireDirection, fireAngle, fireAngle / (currentBulletCount - 1));
        for (int i = 0; i < allDires.Length; i++)
        {
            GameManager.Instance.SpwanBulletSingle(bulletData, allDires[i], firePos, weaponData.CurrentUsedBulletIndex);
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
            GameManager.Instance.SpwanBulletSingle(bulletData, dir, firePos, weaponData.CurrentUsedBulletIndex);
        }
    }
}
