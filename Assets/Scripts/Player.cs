using System;
using UnityEditor.MemoryProfiler;
using UnityEngine;
public class Player : Entity
{
    public PlayerData playerData;

    private Transform fire;
    private Weapon weapon;// 武器类
    public void Init(PlayerData data)
    {
        fire = transform.Find("Fire");
        FirePos = fire.Find("firePos");

        CurrentBulletCount = 1;
        FireDirection = Vector3.up;
        attackType = AttackType.Liner;

        playerData = data;// 拿到玩家数据

        weapon = WeaponSystem.CreateWeapon(playerData.CurrentWeaponIndex, this);
        weapon.ChangeBullet(2);

        moveSpeed = playerData.MoveSpeed;

        EntityTag = "player";
    }

    /// <summary>
    /// 更换武器
    /// 游戏中呈现：玩家可以通过某些方式（比如按键、拾取武器等）来更换当前使用的武器。每种武器都会使用不同的子弹。但是每种武器都有最基础的三种攻击方式：线性攻击、扇形攻击和环形攻击
    /// </summary>
    /// <param name="newWeaponId"></param>
    public void ChangeWeapon(int newWeaponId)
    {
        playerData.CurrentWeaponIndex = newWeaponId;
        weapon.Init(newWeaponId, this);
    }

    public override Entity GetNearestTarget() {
        return GameManager.Instance.FindClosedEnemy(transform.position)?.GetComponent<Entity>();
    }
    public Weapon GetCurrentWeapon()
    {
        return weapon;
    }
    public void PlayerUpdate()
    {
        Move();
        Rotate();

        #region 一些测试用的代码
        if (Input.GetAxis("Mouse ScrollWheel") > 0f)
        {
            CurrentBulletCount = CurrentBulletCount + 1;
        }
        else if(Input.GetAxis("Mouse ScrollWheel") < 0f)
        {
            CurrentBulletCount = CurrentBulletCount - 1;
        }
        //if (weapon != null) {
        //    weapon.ChangeAttackType(attackType, this);
        //}
        #endregion
    }

    void Rotate()
    {
        Vector3 mpos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mpos.z = 0;
        
        FireDirection = mpos - transform.position;
        FireDirection = FireDirection.normalized;
        float angle = Mathf.Atan2(FireDirection.y, FireDirection.x) * Mathf.Rad2Deg;
        // 这里的旋转用缓动会更好看一些，直接设置角度会有点生硬
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
