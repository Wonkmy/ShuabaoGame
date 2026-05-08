using System;
using UnityEngine;
public class Player : Entity
{
    public PlayerData playerData;

    private Transform fire;
    private NormalWeapon weapon;// 武器类

    AttackType attackType;
    public void Init(PlayerData data)
    {
        fire = transform.Find("Fire");
        FirePos = fire.Find("firePos");

        CurrentBulletCount = 1;
        FireDirection = Vector3.up;
        attackType = AttackType.Liner;

        playerData = data;// 拿到玩家数据
        //weapon = new NormalWeapon();

        // 根据武器类型，然后通过反射技术来实例化武器类，并传入玩家数据
        WeaponType weaponType = DataManager.weaponDataDict[playerData.CurrentWeaponIndex].type;
        weapon = (NormalWeapon)System.Activator.CreateInstance(Type.GetType(weaponType.ToString() + "Weapon")) ;

        weapon.Init(playerData.CurrentWeaponIndex, this);
        weapon.ChangeAttackType(attackType, this);

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
