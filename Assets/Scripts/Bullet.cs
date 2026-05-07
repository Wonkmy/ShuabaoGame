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
        myBulletData = bulletData;
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
                DataManager.allEnemyDict[i].GetComponent<Enemy>().TakeDamage(10);

                Destroy(gameObject);
                break;
            }
        }
    }
}
