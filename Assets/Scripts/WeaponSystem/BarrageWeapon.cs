using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrageWeapon : Weapon
{
    int bossAttackIndex = 0;
    bool nextBossAttackUseCircle = false;

    public override void Init(int weaponType, Entity _entity)
    {
        base.Init(weaponType, _entity);
    }

    protected override float GetAttackWarningLeadTime()
    {
        Enemy enemy = entity as Enemy;
        return enemy != null && enemy.enemyType == EnemyType.Boss && enemy.IsFinalBoss
            ? GameManager.Instance.BalanceConfig.bossCombat.warningLeadTime
            : 0f;
    }

    protected override void OnBeforeAttackWarning()
    {
        Enemy enemy = entity as Enemy;
        if (enemy == null || enemy.enemyType != EnemyType.Boss || !enemy.IsFinalBoss)
            return;

        nextBossAttackUseCircle = bossAttackIndex % 2 == 1;
        int bulletCount = nextBossAttackUseCircle ? 18 + enemy.BossPhase * 2 : 7 + enemy.BossPhase;
        enemy.attackType = nextBossAttackUseCircle ? AttackType.Cicle : AttackType.Sector;
        ChangeAttackType(enemy.attackType, enemy, bulletCount);
        GameManager.Instance.SpwanBossAttackWarning(
            enemy.transform.position,
            enemy.FireDirection,
            nextBossAttackUseCircle,
            enemy.BossPhase,
            GetAttackWarningLeadTime());
        enemy.PlayBossChargeFlash(nextBossAttackUseCircle);
    }

    protected override void OnBeforeProcessAttack()
    {
        Enemy enemy = entity as Enemy;
        if (enemy == null || enemy.enemyType != EnemyType.Boss)
            return;

        bool useCircle = enemy.IsFinalBoss ? nextBossAttackUseCircle : bossAttackIndex % 2 == 1;
        bossAttackIndex++;
        enemy.attackType = useCircle ? AttackType.Cicle : AttackType.Sector;
        int bulletCount = useCircle ? 18 + enemy.BossPhase * 2 : 7 + enemy.BossPhase;
        ChangeAttackType(enemy.attackType, enemy, bulletCount);

        Color pulseColor = useCircle
            ? new Color(1f, 0.2f, 0.12f, 0.5f)
            : new Color(1f, 0.65f, 0.18f, 0.45f);
        GameManager.Instance.SpwanEnemyAttackPulse(enemy.transform.position, pulseColor, useCircle ? 5.2f : 3.2f, 0.35f);
        GameManager.Instance.ShakeMainCamera(useCircle ? 0.14f : 0.1f, useCircle ? 0.16f : 0.11f);
    }

    protected override void OnAfterProcessAttack()
    {
        Enemy enemy = entity as Enemy;
        if (enemy == null || enemy.enemyType != EnemyType.Boss || !enemy.IsFinalBoss)
            return;

        enemy.StartBossVulnerableWindow(GameManager.Instance.BalanceConfig.bossCombat.vulnerableDuration);
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

        int bossPhase = enemy != null && enemy.enemyType == EnemyType.Boss ? enemy.BossPhase : 1;
        int baseBulletCount = Mathf.Max(currentBulletCount + 2 + bossPhase, 8);
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
                    }
                    else if (isCenterLane || isCenterBullet)
                    {
                        bulletComp.PierceLeft = 2;
                    }
                }

                spawnedBullets.Add(bullet);
            }
        }

        int extraWaveBulletCount = Mathf.Max(currentBulletCount + bossPhase, 3);
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

        int outerCount = Mathf.Max(currentBulletCount + enemy.BossPhase * 2, 18);
        int innerCount = Mathf.Max(outerCount / 2 + enemy.BossPhase, 9);

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
