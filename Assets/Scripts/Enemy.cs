using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using UnityEngine;

public class Enemy : Entity
{
    int currentHp = 0;
    int totalHp = 0;
    public EnemyType enemyType;
    public Transform target;

    private Weapon weapon;// 武器类

    AttackType attackType;
    public void SetEnemy(EnemyData enemyData)
    {
        enemyType = enemyData.type;
        moveSpeed = enemyData.moveSpeed;
        transform.localScale = Vector3.one * enemyData.scale;
        totalHp = enemyData.hp;
        currentHp = enemyData.hp;
        FirePos = transform;

        weapon = new NormalWeapon();
        weapon.Init(enemyData.CurrentWeaponIndex, this);
        weapon.ChangeAttackType(attackType, this);

        EntityTag = "enemy";
    }

    private void Update()
    {
        Rotate();
        if (weapon != null)
        {
            weapon.WeaponAttack();
            weapon.ChangeAttackType(attackType, this);
        }
        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
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

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        if (currentHp <= 0)
        {
            DataManager.allEnemyDict.Remove(gameObject);
            //for (int i = 0; i < 6; i++)
            //{
            //    float angle = (360.0f / 6) * i;
            //    BulletData bulletData = DataManager.bulletsDataDict[0];
            //    Vector3 dir = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
            //    GameManager.Instance.SpwanBulletSingle(bulletData, dir, transform.position, 0);
            //}
            Destroy(gameObject);
        }
    }
}
