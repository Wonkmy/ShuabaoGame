using System.Collections;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class Bullet : MonoBehaviour
{
    public BulletData myBulletData;

    public bool CanMove { get; set; }

    Vector3 targetPosition;
    public void SetBullet(BulletData bulletData,Vector3 _dir)
    {
        myBulletData = new BulletData
        {
            damage = bulletData.damage,
            distance = bulletData.distance,
            moveSpeed = bulletData.moveSpeed
        };
        targetPosition = transform.position + _dir.normalized * myBulletData.distance;
    }
    void Update()
    {
        if (CanMove)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, myBulletData.moveSpeed * Time.deltaTime);
            CheckCollisionOnEnemy();
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                Destroy(gameObject);
            }
        }
    }

    void CheckCollisionOnEnemy()
    {
        for (int i = DataManager.allEnemyDict.Count - 1; i >= 0; i--)
        {
            float distance = Vector3.Distance(transform.position, DataManager.allEnemyDict[i].transform.position);
            if (distance < 0.7f)
            {
                // 这里可以添加对敌人造成伤害的逻辑
                Player player = GameManager.Instance.player.GetComponent<Player>();
                Weapon weapon = player.GetCurrentWeapon();
                int finalDamage = weapon.weaponData.Attack * (int)myBulletData.damage * player.playerData.Level * (int)player.playerData.power;// 伤害等于 武器攻击力 * 子弹伤害 * 玩家等级 * 当前游戏倍率
                DataManager.allEnemyDict[i].GetComponent<Enemy>().TakeDamage(finalDamage);
                Destroy(gameObject);
                break;
            }
        }
    }
}
