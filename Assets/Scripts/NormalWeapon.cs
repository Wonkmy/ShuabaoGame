using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalWeapon : Weapon
{
    public override void Init(int weaponType, Entity _entity)
    {
        base.Init(weaponType, _entity);
    }
    public override void AttackLiner(Vector3 fireDirection, Vector3 firePos, int currentBulletCount)
    {
        base.AttackLiner(fireDirection, firePos, currentBulletCount);
    }

    public override void AttackSector(float fireAngle, Vector3 fireDirection, Vector3 firePos, int currentBulletCount)
    {
        base.AttackSector(fireAngle, fireDirection, firePos, currentBulletCount);
    }

    public override void AttackCicle(Vector3 fireDirection, Vector3 firePos, int currentBulletCount)
    {
        base.AttackCicle(fireDirection, firePos, currentBulletCount);
    }
}
