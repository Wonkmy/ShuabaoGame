using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public BulletData myBulletData;
    public bool CanMove { get; set; }
    Vector3 targetPosition;

    Entity BelongWho;

    int pierceLeft;// 子弹的穿透次数，穿透一次就减1，减到0就销毁子弹
    bool isExecuteHitStop = false;// 是否已经执行过命中顿帧，防止同一颗子弹多次命中时重复执行顿帧

    public bool canTriggerHitStop = true;// 是否可以触发命中顿帧，防止扇形发射的子弹每一颗都触发顿帧导致游戏卡顿
    public void SetBullet(BulletData bulletData,Vector3 _dir, Entity belongWho)
    {
        BelongWho = belongWho;
        myBulletData = new BulletData
        {
            damage = bulletData.damage,
            distance = bulletData.distance,
            moveSpeed = bulletData.moveSpeed
        };
        targetPosition = transform.position + _dir.normalized * myBulletData.distance;
        pierceLeft = 2;
    }
    public void BulletUpdate()
    {
        if (CanMove)
        {
            Rotate();
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, myBulletData.moveSpeed * Time.deltaTime);
            CheckCollisionOnEnemy();
            CheckCollisionOnPlayer();
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                Destroy(gameObject);
            }
        }
    }

    void Rotate()
    {
        var FireDirection = targetPosition - transform.position;
        FireDirection = FireDirection.normalized;
        float angle = Mathf.Atan2(FireDirection.y, FireDirection.x) * Mathf.Rad2Deg;
        transform.localEulerAngles = new Vector3(0, 0, angle - 90);
    }
    void CheckCollisionOnPlayer() {
        if (BelongWho.EntityTag == "enemy") {
            float distance = Vector3.Distance(transform.position, GameManager.Instance.player.transform.position);
            if (distance < 0.7f)
            {
                GameManager.Instance.player.GetComponent<Player>().TakeDamage(2);
                Destroy(gameObject);
            }
        }
    }
    void CheckCollisionOnEnemy()
    {
        if (BelongWho.EntityTag == "player")
        {
            for (int i = DataManager.allEnemyDict.Count - 1; i >= 0; i--)
            {
                Entity entity = DataManager.allEnemyDict[i].GetComponent<Entity>();
                if (entity.EntityTag == BelongWho.EntityTag || entity.Dead) continue;// 如果敌人和子弹属于同一方，则跳过碰撞检测。或者敌人已经死了，也跳过碰撞检测。

                float distance = Vector3.Distance(transform.position, DataManager.allEnemyDict[i].transform.position);
                if (distance < 0.7f)
                {
                    // 溅射伤害，先获得当前被命中的敌人的周围一定范围内的所有敌人，然后对这些敌人造成伤害，伤害值是被命中敌人伤害值的一半
                    List<GameObject> allEnemys = GameManager.Instance.FindCicleAllEnemysByDistance(entity.transform.position, 2.0f);
                    // 这里可以添加对敌人造成伤害的逻辑
                    Player player = GameManager.Instance.player.GetComponent<Player>();
                    Weapon weapon = player.GetCurrentWeapon();
                    float critChance = weapon != null ? weapon.weaponData.Critical : 0;// 如果玩家有武器，就用武器的暴击率，否则暴击率为0
                    float critDamageMultiplier = 1.0f;
                    // 根据武器的暴击率来决定是否暴击，暴击伤害是普通伤害的1.5倍，并且暴击会震屏
                    if (critChance > 0 && Random.value < critChance)
                    {
                        critDamageMultiplier = 1.5f;
                        GameManager.Instance.ShakeMainCamera(0.2f, 0.3f);
                    }
                    float finalDamage = weapon.weaponData.Attack * critDamageMultiplier * (int)myBulletData.damage * player.playerData.Level * (int)player.playerData.power;// 伤害等于 武器攻击力 * 武器暴击伤害倍率 * 子弹伤害 * 玩家等级 * 游戏倍率
                    entity.TakeDamage(Mathf.CeilToInt(finalDamage));
                    foreach (var _e in allEnemys)
                    {
                        if(_e == entity.gameObject) continue;// 如果是被命中的敌人，就跳过，不要对它造成两次伤害
                        _e.GetComponent<Entity>().TakeDamage((int)(finalDamage * 0.5f));
                    }
                    // 这里加一个命中时的顿帧效果
                    if (!isExecuteHitStop && canTriggerHitStop)
                    {
                        isExecuteHitStop = true;
                        if (GameManager.Instance.HitStopIntensity <= 0)
                        {
                            GameManager.Instance.HitStopIntensity = 0.08f;
                            GameManager.Instance.HitStopDuration = 0.06f;
                        }
                    }
                    pierceLeft--;
                    if (pierceLeft <= 0)
                    {
                        Destroy(gameObject);
                    }
                    break;
                }
            }
        }
    }
}
