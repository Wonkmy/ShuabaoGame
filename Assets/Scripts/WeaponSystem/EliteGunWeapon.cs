using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EliteGunWeapon : Weapon
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
        if (enemy == null || enemy.enemyType != EnemyType.Elite)
        {
            base.AttackSector(fireDirection, firePos, currentBulletCount);
            return;
        }

        GameManager.Instance.SpwanEnemyAttackPulse(
            enemy.transform.position,
            new Color(1f, 0.35f, 0.18f, 0.4f),
            2.5f,
            0.28f);
        GameManager.Instance.ShakeMainCamera(0.06f, 0.07f);

        Vector3 mainDir = fireDirection.normalized;
        Vector3 sideDir = Vector3.Cross(mainDir, Vector3.forward).normalized;

        // 精英怪：比普通怪更夸张，但比Boss收敛
        int centerFanCount = Mathf.Clamp(currentBulletCount + 3, 5, 9);
        int sideFanCount = Mathf.Clamp(currentBulletCount + 1, 3, 6);

        float[] laneOffsets = new float[] { -0.45f, 0f, 0.45f };
        float[] laneAngleOffsets = new float[] { -12f, 0f, 12f };

        for (int lane = 0; lane < laneOffsets.Length; lane++)
        {
            float laneOffset = laneOffsets[lane];
            float laneAngle = laneAngleOffsets[lane];

            Vector3 spawnPos = firePos + sideDir * laneOffset;
            Vector3 laneDir = Quaternion.Euler(0, 0, laneAngle) * mainDir;

            int fanCount = lane == 1 ? centerFanCount : sideFanCount;
            Vector3[] fanDirs = DataManager.GetFanDirections2D(laneDir, fanCount);

            for (int i = 0; i < fanDirs.Length; i++)
            {
                GameObject bullet = GameManager.Instance.SpwanBulletSingle(
                    bulletData,
                    fanDirs[i],
                    spawnPos,
                    0.2f,
                    "1",
                    entity);

                Bullet bulletComp = bullet != null ? bullet.GetComponent<Bullet>() : null;
                if (bulletComp != null)
                {
                    bool isCenterLane = lane == 1;
                    bool isCenterBullet = i == fanDirs.Length / 2;

                    bulletComp.PierceLeft = 1;

                    if (isCenterLane && isCenterBullet)
                    {
                        bulletComp.canTriggerHitStop = true;
                        bulletComp.PierceLeft = 3;
                    }
                    else if (isCenterLane || isCenterBullet)
                    {
                        bulletComp.PierceLeft = 2;
                    }
                }

                spawnedBullets.Add(bullet);
            }
        }

        // 前压补射：让精英怪的扇形更有“压迫推进感”
        int extraCount = Mathf.Clamp(currentBulletCount, 5, 8);
        Vector3[] extraDirs = DataManager.GetFanDirections2D(mainDir, extraCount);
        Vector3 extraSpawnPos = firePos + mainDir * 0.75f;

        for (int i = 0; i < extraDirs.Length; i++)
        {
            GameObject bullet = GameManager.Instance.SpwanBulletSingle(
                bulletData,
                extraDirs[i],
                extraSpawnPos,
                0.2f,
                "1",
                entity);
            Bullet bulletComp = bullet != null ? bullet.GetComponent<Bullet>() : null;
            if (bulletComp != null)
            {
                bulletComp.PierceLeft = 1;

                if (i == extraDirs.Length / 2)
                {
                    bulletComp.canTriggerHitStop = true;
                    bulletComp.PierceLeft = 2;
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
