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

        if (player != null && player.IsEnhancedShotActive)
        {
            powerAttack *= player.EnhancedShotDamageMultiplier;
        }
        if (player.ChainedLightningActive)// 如果玩家的链式闪电技能处于激活状态
        {
            player.chainedTargets.Clear();
            // 只取前3个目标
            GameObject start = player.gameObject;
            GameObject middle = lockedTarget;
            // 找到距离为5以内的所有敌人中的随机一个作为链式闪电的下一个目标
            GameObject end = GameManager.Instance.FindCicleAllEnemysByDistance(lockedTarget.transform.position, 5).Where(e => e != lockedTarget).OrderBy(e => Random.value).FirstOrDefault();
            if (end != null)
            {
                player.chainedTargets = new List<GameObject> { start, middle, end };
                LightningManager.Instance.PlayChain(new List<Vector3> { start.transform.position, middle.transform.position, end.transform.position });
                end.GetComponent<Entity>().TakeDamage(Mathf.FloorToInt(powerAttack), false);
            }
            else
            {
                player.chainedTargets = new List<GameObject> { start, middle };
                LightningManager.Instance.PlayChain(new List<Vector3> { start.transform.position, middle.transform.position });
            }
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
