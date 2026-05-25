using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrageWeapon : Weapon
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
        if (entity == null || entity.EntityTag != "enemy")
        {
            base.AttackSector(fireDirection, firePos, currentBulletCount);
            return;
        }

        Enemy enemy = entity as Enemy;
        if (enemy != null) {
           if(enemy.enemyType != EnemyType.Boss && enemy.enemyType != EnemyType.Elite)
           {
                base.AttackSector(fireDirection, firePos, currentBulletCount);
                return;
            }
        }

        Vector3 mainDir = fireDirection.normalized;
        Vector3 sideDir = Vector3.Cross(mainDir, Vector3.forward).normalized;

        int baseBulletCount = Mathf.Max(currentBulletCount + 2, 8);
        Vector3[] mainFanDirections = DataManager.GetFanDirections2D(mainDir, baseBulletCount);

        float[] laneOffsets = new float[] { -0.55f, -0.25f, 0f, 0.25f, 0.55f };

        for (int lane = 0; lane < laneOffsets.Length; lane++)
        {
            Vector3 spawnPos = firePos + sideDir * laneOffsets[lane];

            for (int i = 0; i < mainFanDirections.Length; i++)
            {
                Vector3 finalDir = mainFanDirections[i];
                GameObject bullet = GameManager.Instance.SpwanBulletSingle(
                    bulletData,
                    finalDir,
                    spawnPos,
                    0.5f,
                    entity.EntityTag,
                    entity);

                Bullet bulletComp = bullet.GetComponent<Bullet>();
                if (bulletComp != null)
                {
                    bulletComp.PierceLeft = 1;

                    bool isCenterLane = lane == laneOffsets.Length / 2;
                    bool isCenterBullet = i == mainFanDirections.Length / 2;

                    if (isCenterLane && isCenterBullet)
                    {
                        bulletComp.canTriggerHitStop = true;
                        bulletComp.PierceLeft = 3;
                        bullet.transform.localScale = Vector3.one * 1.35f;
                    }
                    else if (isCenterLane || isCenterBullet)
                    {
                        bulletComp.PierceLeft = 2;
                        bullet.transform.localScale = Vector3.one * 1.15f;
                    }
                }

                spawnedBullets.Add(bullet);
            }
        }

        int extraWaveBulletCount = Mathf.Max(currentBulletCount, 3);
        Vector3[] extraFanDirections = DataManager.GetFanDirections2D(mainDir, extraWaveBulletCount);
        float frontOffset = 0.9f;

        for (int i = 0; i < extraFanDirections.Length; i++)
        {
            Vector3 spawnPos = firePos + mainDir * frontOffset;
            GameObject bullet = GameManager.Instance.SpwanBulletSingle(
                bulletData,
                extraFanDirections[i],
                spawnPos,
                    bulletSclae,
                entity.EntityTag,
                entity);

            Bullet bulletComp = bullet.GetComponent<Bullet>();
            if (bulletComp != null)
            {
                bulletComp.PierceLeft = 2;
                if (i == extraFanDirections.Length / 2)
                {
                    bulletComp.canTriggerHitStop = true;
                    bulletComp.PierceLeft = 3;
                    bullet.transform.localScale = Vector3.one * 1.25f;
                }
            }

            spawnedBullets.Add(bullet);
        }
    }

    public override void AttackCicle(Vector3 fireDirection, Vector3 firePos, int currentBulletCount)
    {
        base.AttackCicle(fireDirection, firePos, currentBulletCount);
    }
}
