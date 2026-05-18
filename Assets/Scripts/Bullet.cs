using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public BulletData myBulletData;

    public bool CanMove { get; set; }

    public int PierceLeft { get; set; }// 子弹穿透次数

    public bool canTriggerHitStop = true;// 是否允许触发顿帧

    private Vector3 targetPosition;

    private Vector3 moveDir;

    private bool isExecuteHitStop = false;

    private Entity BelongWho;

    private Player player;
    private string bulletPrefabId;

    public void SetBulletPrefabId(string id)
    {
        bulletPrefabId = id;
    }
    public void SetBullet(BulletData bulletData,Vector3 pos, Vector3 _dir, Entity belongWho)
    {
        ResetBullet();

        BelongWho = belongWho;

        player = GameManager.Instance.player.GetComponent<Player>();

        myBulletData = new BulletData
        {
            damage = bulletData.damage,
            distance = bulletData.distance,
            moveSpeed = bulletData.moveSpeed
        };

        // 敌人子弹增加随机角度
        if (BelongWho.EntityTag == "enemy")
        {
            float randomAngle = Random.Range(-4f, 4f);

            moveDir = Quaternion.Euler(0, 0, randomAngle) * _dir.normalized;

            targetPosition = pos + moveDir * myBulletData.distance;
        }
        else
        {
            moveDir = _dir.normalized;

            targetPosition = pos + moveDir * myBulletData.distance;
        }
    }

    public void ResetBullet()
    {
        CanMove = true;

        PierceLeft = 1;

        canTriggerHitStop = true;

        isExecuteHitStop = false;

        moveDir = Vector3.zero;

        BelongWho = null;

        player = null;
        myBulletData = default;
        transform.rotation = Quaternion.identity;

        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!gameObject.activeSelf)
            return;
        if (!CanMove)
            return;

        if (GameManager.Instance.IsTimeStop && BelongWho.EntityTag == "enemy")
        {
            return;
        }

        Move();

        Rotate();

        CheckCollision();
    }

    void Move()
    {
        if (BelongWho.EntityTag == "enemy")
        {
            MoveEnemyBullet();
        }
        else
        {
            MovePlayerBullet();
        }
    }

    void MoveEnemyBullet()
    {
        transform.position += moveDir * myBulletData.moveSpeed * Time.deltaTime;
        // 如果飞出了当前视口范围，则销毁子弹
        Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);

        if (viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1)
        {
            // 在视口范围内，不销毁
        }
        else
        {
            // 超出视口范围，销毁子弹
            //Destroy(gameObject);
            BulletPool.Instance.Release(bulletPrefabId, gameObject);
        }
    }

    void MovePlayerBullet()
    {
        transform.position += moveDir * myBulletData.moveSpeed * Time.deltaTime;
        Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);

        if (viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1)
        {
            // 在视口范围内，不销毁
        }
        else
        {
            // 超出视口范围，销毁子弹
            BulletPool.Instance.Release(bulletPrefabId, gameObject);
        }
    }

    void Rotate()
    {
        Vector3 fireDirection = targetPosition - transform.position;

        fireDirection = fireDirection.normalized;

        float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;

        transform.localEulerAngles = new Vector3(0, 0, angle - 90);
    }

    void CheckCollision()
    {
        CheckCollisionOnEnemy();

        CheckCollisionOnPlayer();
    }

    void CheckCollisionOnPlayer()
    {
        if (BelongWho.EntityTag != "enemy")
            return;

        float distance = Vector3.Distance(transform.position, GameManager.Instance.player.transform.position);

        if (distance < 0.7f)
        {
            Enemy enemy = (Enemy)BelongWho;
            player.TakeDamage(Mathf.CeilToInt(myBulletData.damage + enemy.Damage),false);

            //Destroy(gameObject);
            BulletPool.Instance.Release(bulletPrefabId, gameObject);
        }
    }

    void CheckCollisionOnEnemy()
    {
        if (BelongWho.EntityTag != "player")
            return;

        for (int i = DataManager.allEnemyDict.Count - 1; i >= 0; i--)
        {
            Entity entity = DataManager.allEnemyDict[i].GetComponent<Entity>();

            if (entity.EntityTag == BelongWho.EntityTag || entity.Dead)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, DataManager.allEnemyDict[i].transform.position);

            if (distance < 0.7f)
            {
                if (entity.EntityTag == "enemy")
                {
                    Enemy enemy = (Enemy)entity;
                    if (!enemy.HasEnterScreen)
                    {
                        continue;
                    }
                    if (enemy.hasShield)
                    {
                        enemy.RemoveShild();
                    }
                }
                HandleDamage(entity);
                break;
            }
        }
    }

    void HandleDamage(Entity entity)
    {
        Weapon weapon = player.GetCurrentWeapon();

        float critChance = weapon != null ? weapon.weaponData.Critical : 0;

        bool isCrit = false;

        float critDamageMultiplier = 1.0f;

        // 暴击
        if (critChance > 0 && Random.value < critChance)
        {
            isCrit = true;

            critDamageMultiplier = 1.35f;

            HandleCrit(entity);
        }

        // 基础伤害
        float attack = weapon.weaponData.Attack * (int)myBulletData.damage;

        float powerAttack = attack * player.playerData.power;

        Enemy enemy = (Enemy)entity;
        float penetrate = PierceLeft;
        float defence = 1.8f * (1.25f + (int)enemy.enemyType);// 敌人类型越高，防御越高
        float fValue = 100.0f / (100.0f + Mathf.Max(defence - penetrate, 0));

        fValue = Mathf.Max(fValue, 0.5f);

        float finalDamage = powerAttack * critDamageMultiplier * fValue;

        // 少弹高伤
        if (player.HasLowBulletHighDamage)
        {
            float bonus = Mathf.Max(1, 6 - player.CurrentBulletCount);

            finalDamage *= bonus;
        }

        // 主目标伤害
        entity.TakeDamage(Mathf.FloorToInt(finalDamage), isCrit);

        // 原本AOE
        HandleAOE(entity, finalDamage);

        // 暴击爆炸
        if (isCrit && player.HasCritExplosion)
        {
            HandleCritExplosion(entity, finalDamage);
        }

        PierceLeft--;

        // 穿透爆炸
        if (player.HasPierceExplosion)
        {
            HandlePierceExplosion(entity, finalDamage);
        }

        if (PierceLeft <= 0)
        {
            //Destroy(gameObject);
            BulletPool.Instance.Release(bulletPrefabId, gameObject);
        }
    }

    void HandleCrit(Entity entity)
    {
        Enemy enemy = entity as Enemy;
        if (enemy.enemyType == EnemyType.Boss || enemy.enemyType == EnemyType.Elite)
        {
            GameManager.Instance.ShakeMainCamera(0.2f, 0.3f);
        }
        if (!isExecuteHitStop && canTriggerHitStop)
        {
            isExecuteHitStop = true;

            if (GameManager.Instance.HitStopIntensity <= 0)
            {
                GameManager.Instance.HitStopIntensity = 0.08f;

                GameManager.Instance.HitStopDuration = 0.05f;
            }
        }
    }

    //void HandleAOE(Entity entity, float finalDamage)
    //{
    //    List<GameObject> allEnemys = GameManager.Instance.FindCicleAllEnemysByDistance(entity.transform.position, 2.0f);

    //    foreach (var e in allEnemys)
    //    {
    //        if (e == entity.gameObject)
    //            continue;

    //        e.GetComponent<Entity>().TakeDamage(Mathf.FloorToInt(finalDamage * 0.5f),false);
    //    }
    //}

    //void HandleCritExplosion(Entity entity, float finalDamage)
    //{
    //    List<GameObject> allEnemys = GameManager.Instance.FindCicleAllEnemysByDistance(entity.transform.position, 3.0f);

    //    foreach (var e in allEnemys)
    //    {
    //        if (e == entity.gameObject)
    //            continue;

    //        e.GetComponent<Entity>().TakeDamage(Mathf.FloorToInt(finalDamage * 0.8f),true);
    //    }
    //}

    //void HandlePierceExplosion(float finalDamage)
    //{
    //    List<GameObject> allEnemys = GameManager.Instance.FindCicleAllEnemysByDistance(transform.position, 1.5f);

    //    foreach (var e in allEnemys)
    //    {
    //        e.GetComponent<Entity>().TakeDamage(Mathf.FloorToInt(finalDamage * 0.3f), false);
    //    }
    //}
    void HandleAOE(Entity entity, float finalDamage)
    {
        List<GameObject> allEnemys =
            GameManager.Instance.FindCicleAllEnemysByDistance(
                entity.transform.position,
                1.2f);

        int hitCount = 0;

        foreach (var e in allEnemys)
        {
            if (e == entity.gameObject)
                continue;

            Entity enemyEntity =
                e.GetComponent<Entity>();

            if (enemyEntity == null || enemyEntity.Dead)
                continue;

            enemyEntity.TakeDamage(
                Mathf.FloorToInt(finalDamage * 0.35f),
                false);

            hitCount++;

            // 最多影响3个敌人
            if (hitCount >= 3)
                break;
        }
    }

    void HandleCritExplosion(Entity entity, float finalDamage)
    {
        List<GameObject> allEnemys =
            GameManager.Instance.FindCicleAllEnemysByDistance(
                entity.transform.position,
                1.5f);

        int hitCount = 0;

        foreach (var e in allEnemys)
        {
            if (e == entity.gameObject)
                continue;

            Entity enemyEntity =
                e.GetComponent<Entity>();

            if (enemyEntity == null || enemyEntity.Dead)
                continue;

            enemyEntity.TakeDamage(
                Mathf.FloorToInt(finalDamage * 0.6f),
                true);

            hitCount++;

            // 暴击爆炸最多5个
            if (hitCount >= 5)
                break;
        }
    }

    void HandlePierceExplosion(Entity entity, float finalDamage)
    {
        List<GameObject> allEnemys = GameManager.Instance.FindCicleAllEnemysByDistance(entity.transform.position, 0.8f);

        int hitCount = 0;

        foreach (var e in allEnemys)
        {
            if (e == entity.gameObject)
                continue;

            Entity enemyEntity =
                e.GetComponent<Entity>();

            if (enemyEntity == null || enemyEntity.Dead)
                continue;

            enemyEntity.TakeDamage(
                Mathf.FloorToInt(finalDamage * 0.2f),
                false);

            hitCount++;

            // 穿透爆炸限制更严格
            if (hitCount >= 2)
                break;
        }
    }
}