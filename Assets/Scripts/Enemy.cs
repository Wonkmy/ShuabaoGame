using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Entity
{
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
        FirePos = transform;
        attackType = AttackType.Liner;

        weapon = WeaponSystem.CreateWeapon(enemyData.CurrentWeaponIndex, this);
        EntityTag = "enemy";

        CanMove = true;
        Dead = false;
    }

    public void EnemyUpdate()
    {
        if (Dead) { return; }
        Rotate();
        if (CanMove)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
        }
        if(Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            DataManager.allEnemyDict.Remove(gameObject);
            Destroy(gameObject);
        }
    }

    void Rotate()
    {
        FireDirection = target.position - transform.position;
        FireDirection = FireDirection.normalized;
        float angle = Mathf.Atan2(FireDirection.y, FireDirection.x) * Mathf.Rad2Deg;
        transform.localEulerAngles = new Vector3(0, 0, angle - 90);
    }

    public override Entity GetNearestTarget()
    {
        return target.GetComponent<Entity>();
    }

    public override void TakeDamage(int damage)
    {
        currentHp -= damage;
        GameObject newBullet = Instantiate(Resources.Load<GameObject>("damage_txt"));
        newBullet.transform.position = transform.position + new Vector3(0, 0.5f, 0);
        newBullet.GetComponent<DamageText>().SetDamageText(damage);
        DataManager.allDamageText.Add(newBullet);
        if (currentHp <= 0)
        {
            Dead = true;
            CanMove = false;
            GameManager.Instance.SpwanExpBall(transform.position, Mathf.CeilToInt(totalHp / 10.0f));
            // 旋转缩小然后死亡
            StartCoroutine(DeathEffect());
        }
    }

    IEnumerator DeathEffect()
    {
        float duration = 0.4f;
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;
        Quaternion originalRotation = transform.rotation;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // 旋转
            transform.rotation = Quaternion.Euler(0, 0, t * 360) * originalRotation;
            // 缩小
            transform.localScale = Vector3.Lerp(originalScale, Vector3.one * 2.5f, t);
            yield return null;
            // 再恢复，有一种膨胀后爆炸的感觉
            transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.zero, t);
            yield return null;
        }
        // 确保最终状态
        transform.rotation = Quaternion.Euler(0, 0, 360) * originalRotation;
        transform.localScale = Vector3.zero;


        WeaponSystem.RemoveWeapon(weapon);// 先移除武器，避免在销毁敌人后还调用武器的Update方法
        DataManager.allEnemyDict.Remove(gameObject);// 从敌人字典中移除
        Destroy(gameObject);
    }
}
