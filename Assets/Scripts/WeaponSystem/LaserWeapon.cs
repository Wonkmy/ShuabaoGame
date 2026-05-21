using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LaserWeapon : Weapon
{
    public override void Init(int weaponType, Entity _entity)
    {
        base.Init(weaponType, _entity);
    }
    public override void AttackLiner(Vector3 fireDirection, Vector3 firePos, int currentBulletCount)
    {
        base.AttackLiner(fireDirection, firePos, currentBulletCount);
    }

    public override void AttackSector(Vector3 fireDirection, Vector3 firePos, int currentBulletCount)
    {
        Player player = null;
        float entityAttack = 0;
        if (entity.EntityTag == "player")
        {
            player = entity as Player;
            entityAttack = player.playerData.Atk;
        }
        float attack = weaponData.Attack + GetAttack();
        float powerAttack = attack * Mathf.Max(1, entityAttack);
        if (player.ChainedLightningActive)// 如果玩家的链式闪电技能处于激活状态
        {
            // 只取前3个目标
            Transform start = player.transform;
            Transform middle = lockedTarget.transform;
            Transform end = GameManager.Instance.FindCicleAllEnemysByDistance(lockedTarget.transform.position, 5f).FirstOrDefault()?.transform;
            player.UpdateChaineLaser(new List<Transform> { start, middle, end });
            LightningManager.Instance.PlayChain(new List<Vector3> { start.position, middle.position, end != null ? end.position : middle.position });
            end.GetComponent<Entity>().TakeDamage(Mathf.FloorToInt(powerAttack), false);
        }
        else
        {
            LightningManager.Instance.Play(entity.transform.position, lockedTarget.transform.position);
        }
        lockedTarget.GetComponent<Entity>().TakeDamage(Mathf.FloorToInt(powerAttack), false);
    }

    public override void AttackCicle(Vector3 fireDirection, Vector3 firePos, int currentBulletCount)
    {
        base.AttackCicle(fireDirection, firePos, currentBulletCount);
    }
}
