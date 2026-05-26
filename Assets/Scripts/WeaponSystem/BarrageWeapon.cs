using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrageWeapon : Weapon
{
    int bossAttackIndex = 0;

    public override void Init(int weaponType, Entity _entity)
    {
        base.Init(weaponType, _entity);
    }

    protected override void OnBeforeProcessAttack()
    {
        Enemy enemy = entity as Enemy;
        if (enemy == null || enemy.enemyType != EnemyType.Boss)
            return;

        bossAttackIndex++;
        bool useCircle = bossAttackIndex % 2 == 0;
        enemy.attackType = useCircle ? AttackType.Cicle : AttackType.Sector;
        int bulletCount = useCircle ? 18 : 7;
        ChangeAttackType(enemy.attackType, enemy, bulletCount);

        Color pulseColor = useCircle
            ? new Color(1f, 0.2f, 0.12f, 0.5f)
            : new Color(1f, 0.65f, 0.18f, 0.45f);
        GameManager.Instance.SpwanEnemyAttackPulse(enemy.transform.position, pulseColor, useCircle ? 5.2f : 3.2f, 0.35f);
        GameManager.Instance.ShakeMainCamera(useCircle ? 0.14f : 0.1f, useCircle ? 0.16f : 0.11f);
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
        Enemy enemy = entity as Enemy;
        if (enemy == null || enemy.enemyType != EnemyType.Boss)
        {
            base.AttackCicle(fireDirection, firePos, currentBulletCount);
            return;
        }

        int outerCount = Mathf.Max(currentBulletCount, 18);
        int innerCount = Mathf.Max(outerCount / 2, 9);

        for (int i = 0; i < outerCount; i++)
        {
            float angle = (360.0f / outerCount) * i;
            Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
            GameObject bullet = GameManager.Instance.SpwanBulletSingle(
                bulletData,
                dir,
                firePos,
                0.65f,
                entity.EntityTag,
                entity);

            Bullet bulletComp = bullet.GetComponent<Bullet>();
            if (bulletComp != null)
            {
                bulletComp.PierceLeft = 2;
                if (i % 3 == 0)
                {
                    bulletComp.canTriggerHitStop = true;
                    bullet.transform.localScale = Vector3.one * 1.2f;
                }
            }

            spawnedBullets.Add(bullet);
        }

        float angleOffset = 360f / outerCount * 0.5f;
        for (int i = 0; i < innerCount; i++)
        {
            float angle = angleOffset + (360.0f / innerCount) * i;
            Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
            GameObject bullet = GameManager.Instance.SpwanBulletSingle(
                bulletData,
                dir,
                firePos + dir * 0.45f,
                0.35f,
                entity.EntityTag,
                entity);

            Bullet bulletComp = bullet.GetComponent<Bullet>();
            if (bulletComp != null)
            {
                bulletComp.PierceLeft = 1;
            }

            spawnedBullets.Add(bullet);
        }
    }
}
