using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public BulletData myBulletData;
    public bool CanMove { get; set; }
    Vector3 targetPosition;

    Entity BelongWho;

    int pierceLeft;// 子弹的穿透次数，穿透一次就减1，减到0就销毁子弹
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
            CheckCollisionOnEntity();
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

    void CheckCollisionOnEntity()
    {
        for (int i = DataManager.allEnemyDict.Count - 1; i >= 0; i--)
        {
            Entity entity = DataManager.allEnemyDict[i].GetComponent<Entity>();
            if(entity.EntityTag == BelongWho.EntityTag || entity.Dead) continue;// 如果敌人和子弹属于同一方，则跳过碰撞检测。或者敌人已经死了，也跳过碰撞检测。
            float distance = Vector3.Distance(transform.position, DataManager.allEnemyDict[i].transform.position);
            if (distance < 0.7f)
            {
                if(BelongWho.EntityTag == "player")
                {
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
                    DataManager.allEnemyDict[i].GetComponent<Entity>().TakeDamage(Mathf.CeilToInt(finalDamage));

                    pierceLeft--;
                    if (pierceLeft <= 0)
                    {
                        Destroy(gameObject);
                    }
                }
                else
                {
                    GameManager.Instance.player.GetComponent<Player>().TakeDamage(2);
                }
                break;
            }
        }
    }
}
