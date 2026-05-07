using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    float moveSpeed = 2.0f;

    int currentHp = 0;
    int totalHp = 0;
    public EnemyType enemyType;
    public Transform target;
    public void SetEnemy(EnemyData enemyData)
    {
        enemyType = enemyData.type;
        moveSpeed = enemyData.moveSpeed;
        transform.localScale = Vector3.one * enemyData.scale;
        totalHp = enemyData.hp;
        currentHp = enemyData.hp;
    }

    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
        if(Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            DataManager.allEnemyDict.Remove(gameObject);
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        if (currentHp <= 0)
        {
            DataManager.allEnemyDict.Remove(gameObject);


            // 分裂成6个子弹向周围移动
            for (int j = 0; j < 6; j++)
            {
                float angle = (360.0f / 6) * j;
                Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
                GameObject newBullet_Liner = Instantiate(Resources.Load<GameObject>("bullet"));
                newBullet_Liner.transform.position = transform.position;
                BulletData bulletData = DataManager.bulletsDataDict[0];
                newBullet_Liner.GetComponent<Bullet>().SetBullet(bulletData, dir);
                newBullet_Liner.GetComponent<Bullet>().CanMove = true;
            }
            Destroy(gameObject);
        }
    }
}
