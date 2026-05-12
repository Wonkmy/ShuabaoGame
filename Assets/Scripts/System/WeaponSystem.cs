using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;
// 武器系统，用来创建和管理所有武器类的实例
public static class WeaponSystem
{
    public static List<Weapon> weapons = new List<Weapon>();
    public static Weapon CreateWeapon(int weaponId,Entity entity)
    {
        WeaponType weaponType = DataManager.weaponDataDict[weaponId].type;
        Weapon weapon = (Weapon)System.Activator.CreateInstance(Type.GetType(weaponType.ToString() + "Weapon"));
        weapon.Init(weaponId, entity);
        weapon.ChangeAttackType(AttackType.Liner, entity,entity.CurrentBulletCount);
        weapons.Add(weapon);
        return weapon;
    }

    public static void RemoveWeapon(Weapon weapon)
    {
        if (weapons.Contains(weapon))
        {
            weapon.spawnedBullets.ForEach(bullet => GameObject.Destroy(bullet));
            weapon.spawnedBullets.Clear();
            weapons.Remove(weapon);
        }
    }
    public static void UpdateWeapons()
    {
        for(int i = weapons.Count - 1; i >= 0; i--)
        {
            weapons[i].WeaponUpdate();
            for (int j = 0; j < weapons[i].spawnedBullets.Count; j++)
            {
                if (weapons[i].spawnedBullets[j] != null)
                {
                    Bullet bullet = weapons[i].spawnedBullets[j].GetComponent<Bullet>();
                    if (bullet != null)
                    {
                        bullet.BulletUpdate();
                    }
                }
            }
            weapons[i].ChangeAttackType(weapons[i].entity.attackType, weapons[i].entity, weapons[i].entity.CurrentBulletCount);
        }
    }

    public static void Clear()
    {
        weapons.Clear();
    }
}
