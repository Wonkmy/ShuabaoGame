using System;
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
            weapon.spawnedBullets.Clear();
            weapons.Remove(weapon);
        }
    }
    public static void UpdateWeapons()
    {
        for(int i = weapons.Count - 1; i >= 0; i--)
        {
            weapons[i].WeaponUpdate();
            weapons[i].ChangeAttackType(weapons[i].entity.attackType, weapons[i].entity, weapons[i].entity.GetWeaponAttackBulletCount());
        }
    }

    public static void Clear()
    {
        weapons.Clear();
    }
}
