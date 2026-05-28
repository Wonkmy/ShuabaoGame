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
    public bool IsEnhancedShot { get; set; }
    public float EnhancedShotDamageMultiplier { get; set; }
    public bool IsSplitShot { get; set; }
    public float SplitShotDamageMultiplier { get; set; }

    public Vector3 originalLocalScale = Vector3.one;

    public float currentScale { get; set; }
    void Awake()
    {
        originalLocalScale = transform.Find("fx").localScale;
    }

    public void SetFxScaleToDefalut()
    {
        transform.Find("fx").localScale = originalLocalScale;
        BelongWho.GetCurrentWeapon().spawnedBullets.Remove(gameObject);
    }
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
            moveSpeed = bulletData.moveSpeed,
            prefabString = bulletData.prefabString
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

        IsEnhancedShot = false;
        EnhancedShotDamageMultiplier = 1f;
        IsSplitShot = false;
        SplitShotDamageMultiplier = 1f;

        isExecuteHitStop = false;

        moveDir = Vector3.zero;

        BelongWho = null;

        player = null;
        myBulletData = default;
        transform.rotation = Quaternion.identity;
        transform.Find("fx").localScale = originalLocalScale;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.white;
        }

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
            float destinationDis = 0.7f + 0.7f * currentScale;
            if (distance < destinationDis)// 这个数值要根据子弹大小调整大小的
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
        float totalCrit = DataManager.myGameData.PermanentCrit;
        // 暴击
        if (critChance > 0 && Random.value < critChance)
        {
            isCrit = true;

            critDamageMultiplier = 1.35f + totalCrit;

            HandleCrit(entity);
        }

        // 基础伤害
        float attack = weapon.GetAttack() * (int)myBulletData.damage;// （武器原始攻击力 + 武器升级增加的攻击力） * 子弹伤害

        float powerAttack = attack * player.playerData.Atk;// 武器攻击力 * 玩家攻击力(玩家攻击力为全局永久攻击力)

        Enemy enemy = (Enemy)entity;
        float penetrate = PierceLeft;
        float defence = 1.9f * (1.55f + (int)enemy.enemyType);// 敌人类型越高，防御越高
        float fValue = 100.0f / (100.0f + Mathf.Max(defence - penetrate, 0));
        fValue = Mathf.Max(fValue, 0.5f);

        //float finalDamage = powerAttack * critDamageMultiplier * fValue;
        float finalDamage = powerAttack * critDamageMultiplier * fValue;

        if (IsEnhancedShot)
        {
            finalDamage *= Mathf.Max(1f, EnhancedShotDamageMultiplier);
        }

        if (IsSplitShot)
        {
            finalDamage *= Mathf.Clamp(SplitShotDamageMultiplier, 0.25f, 1f);
        }

        // 少弹高伤
        if (player.HasLowBulletHighDamage)
        {
            float bonus = Mathf.Max(1, 6 - player.CurrentBulletCount);

            finalDamage *= bonus;
        }
        // 核弹增伤
        if (player.HasNuclearBuild)
        {
            finalDamage *= 1.5f;
        }
        // 主目标伤害
        entity.TakeDamage(Mathf.FloorToInt(finalDamage), isCrit);

        enemy.PlayHitPunch(moveDir);
        TriggerHitStop(0.018f, 0.03f);

        if (player.HasLegendSplit && !IsSplitShot)
        {
            SpawnSplitBullets(entity.transform.position);
        }

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
            BulletPool.Instance.Release(bulletPrefabId, gameObject);
        }
    }

    void SpawnSplitBullets(Vector3 splitPos)
    {
        Vector3 leftDir = Quaternion.Euler(0, 0, 28f) * moveDir;
        Vector3 rightDir = Quaternion.Euler(0, 0, -28f) * moveDir;

        SpawnSingleSplitBullet(splitPos, leftDir);
        SpawnSingleSplitBullet(splitPos, rightDir);
    }

    void SpawnSingleSplitBullet(Vector3 splitPos, Vector3 splitDir)
    {
        GameObject bulletObj = GameManager.Instance.SpwanBulletSingle(
            myBulletData,
            splitDir,
            splitPos,
            0f,
            BelongWho.EntityTag,
            BelongWho);

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        bullet.IsSplitShot = true;
        bullet.SplitShotDamageMultiplier = 0.45f;
        bullet.PierceLeft = 1;
        bullet.canTriggerHitStop = false;
        bulletObj.transform.localScale *= 0.8f;
    }

    void HandleCrit(Entity entity)
    {
        Enemy enemy = entity as Enemy;
        if (enemy.enemyType == EnemyType.Boss || enemy.enemyType == EnemyType.Elite)
        {
            GameManager.Instance.ShakeMainCamera(0.18f, 0.22f);
            TriggerHitStop(0.06f, 0.1f);
        }
        if (!isExecuteHitStop && canTriggerHitStop)
        {
            isExecuteHitStop = true;

            TriggerHitStop(0.045f, 0.08f);
        }
    }
    void TriggerHitStop(float duration, float intensity)
    {
        if (GameManager.Instance.HitStopIntensity <= 0)
        {
            GameManager.Instance.HitStopDuration = duration;
            GameManager.Instance.HitStopIntensity = intensity;
        }
    }
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
