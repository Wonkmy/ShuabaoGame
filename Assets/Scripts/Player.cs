using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using UnityEngine;
public class Player : MonoBehaviour
{
    public float moveSpeed { get; set; }
    public Transform FirePos { get; set; }
    public Vector3 FireDirection { get; set; }// 朝向
    public int CurrentBulletCount { get; set; }// 当前子弹数量

    public PlayerData playerData;

    private Transform fire;
    private Weapon weapon;// 武器类

    AttackType attackType;
    public void Init(PlayerData data)
    {
        fire = transform.Find("Fire");
        FirePos = fire.Find("firePos");

        CurrentBulletCount = 1;
        FireDirection = Vector3.up;
        attackType = AttackType.Liner;

        playerData = data;// 拿到玩家数据
        weapon = new NormalWeapon();
        weapon.Init(playerData.CurrentWeaponType);
        weapon.ChangeAttackType(attackType, this);

        moveSpeed = playerData.MoveSpeed;
    }
    public Weapon GetCurrentWeapon()
    {
        return weapon;
    }
    public void PlayerUpdate()
    {
        Move();
        Rotate();

        if (weapon != null)
        {
            weapon.WeaponAttack();
        }

        #region 一些测试用的代码
        if (Input.GetAxis("Mouse ScrollWheel") > 0f)
        {
            CurrentBulletCount = CurrentBulletCount + 1;
        }
        else if(Input.GetAxis("Mouse ScrollWheel") < 0f)
        {
            CurrentBulletCount = CurrentBulletCount - 1;
        }
        if (weapon != null) {
            //if (Input.GetKeyDown(KeyCode.Q))
            //{
            //    attackType = AttackType.Liner;
            //}
            //if (Input.GetKeyDown(KeyCode.W))
            //{
            //    attackType = AttackType.Sector;
            //}
            //if (Input.GetKeyDown(KeyCode.E))
            //{
            //    attackType = AttackType.Cicle;
            //}
            weapon.ChangeAttackType(attackType, this);
        }
        #endregion
    }

    void Rotate()
    {
        Vector3 mpos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mpos.z = 0;
        FireDirection = mpos - transform.position;
        FireDirection = FireDirection.normalized;
        float angle = Mathf.Atan2(FireDirection.y, FireDirection.x) * Mathf.Rad2Deg;
        fire.localEulerAngles = new Vector3(0, 0, angle - 90);
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        Vector3 dir = new Vector3(x, y, 0);
        transform.position = Vector3.Lerp(transform.position, transform.position + dir, moveSpeed * Time.deltaTime);
    }
}
