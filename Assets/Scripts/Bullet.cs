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

    private float lifeTime = 0f;

    private bool isExecuteHitStop = false;

    private Entity BelongWho;

    private Player player;

    public void SetBullet(BulletData bulletData, Vector3 _dir, Entity belongWho)
    {
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

            targetPosition = transform.position + moveDir * myBulletData.distance;
        }
        else
        {
            moveDir = _dir.normalized;

            targetPosition = transform.position + moveDir * myBulletData.distance;
        }

        lifeTime = 2.0f;
    }

    void Update()
    {
        if (!CanMove)
            return;

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

        lifeTime -= Time.deltaTime;

        if (lifeTime <= 0)
        {
            Destroy(gameObject);
        }
    }

    void MovePlayerBullet()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, myBulletData.moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            Destroy(gameObject);
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
            player.TakeDamage(Mathf.CeilToInt(myBulletData.damage));

            Destroy(gameObject);
        }
    }

    void CheckCollisionOnEnemy()
    {
        if (BelongWho.EntityTag != "player")
            return;

        for (int i = DataManager.allEnemyDict.Count - 1; i >= 0; i--)
        {
            Entity entity = DataManager.allEnemyDict[i].GetComponent<Entity>();

            if (entity.EntityTag == "enemy")
            {
                Enemy enemy = (Enemy)entity;

                if (enemy.hasShield)
                {
                    enemy.RemoveShild();

                    continue;
                }
            }

            if (entity.EntityTag == BelongWho.EntityTag || entity.Dead)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, DataManager.allEnemyDict[i].transform.position);

            if (distance < 0.7f)
            {
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

            critDamageMultiplier = 1.5f;

            HandleCrit();
        }

        // 基础伤害
        float attack = weapon.weaponData.Attack * (int)myBulletData.damage * player.playerData.Level;

        float powerAttack = attack * player.playerData.power;

        float defence = player.playerData.Level * 5;

        float penetrate = (int)myBulletData.damage * 2;

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
        entity.TakeDamage(Mathf.CeilToInt(finalDamage));

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
            HandlePierceExplosion(finalDamage);
        }

        if (PierceLeft <= 0)
        {
            Destroy(gameObject);
        }
    }

    void HandleCrit()
    {
        GameManager.Instance.ShakeMainCamera(0.2f, 0.3f);

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

    void HandleAOE(Entity entity, float finalDamage)
    {
        List<GameObject> allEnemys = GameManager.Instance.FindCicleAllEnemysByDistance(entity.transform.position, 2.0f);

        foreach (var e in allEnemys)
        {
            if (e == entity.gameObject)
                continue;

            e.GetComponent<Entity>().TakeDamage(Mathf.CeilToInt(finalDamage * 0.5f));
        }
    }

    void HandleCritExplosion(Entity entity, float finalDamage)
    {
        List<GameObject> allEnemys = GameManager.Instance.FindCicleAllEnemysByDistance(entity.transform.position, 3.0f);

        foreach (var e in allEnemys)
        {
            if (e == entity.gameObject)
                continue;

            e.GetComponent<Entity>().TakeDamage(Mathf.CeilToInt(finalDamage * 0.8f));
        }
    }

    void HandlePierceExplosion(float finalDamage)
    {
        List<GameObject> allEnemys = GameManager.Instance.FindCicleAllEnemysByDistance(transform.position, 1.5f);

        foreach (var e in allEnemys)
        {
            e.GetComponent<Entity>().TakeDamage(Mathf.CeilToInt(finalDamage * 0.3f));
        }
    }
}