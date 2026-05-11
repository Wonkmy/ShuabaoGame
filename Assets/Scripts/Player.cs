using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.MemoryProfiler;
using UnityEngine;
public class Player : Entity
{
    int currentHp = 0;
    int totalHp = 0;
    int level = 1;
    public PlayerData playerData;
    private Transform fire;
    int totalExp = 0;
    int currentExp = 0;
    int needExp = 100;
    public void Init(PlayerData data)
    {
        fire = transform.Find("Fire");
        FirePos = fire.Find("firePos");

        CurrentBulletCount = 1;
        FireDirection = Vector3.up;
        attackType = AttackType.Liner;

        playerData = data;// 拿到玩家数据
        totalHp = (int)playerData.Hp;
        currentHp = (int)playerData.Hp;

        totalExp = 0;
        currentExp = 0;
        level = (int)playerData.Level;

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
    public void AddExp(int exp)
    {
        currentExp += exp;
        if(currentExp >= needExp)
        {
            level++;
            currentExp = currentExp - needExp;
            needExp = (int)(needExp * 1.25f);
        }
    }
    public float GetExpProgress()
    {
        return (float)currentExp / needExp;
    }

    public float GetHpProgress()
    {
        return (float)currentHp / totalHp;
    }
    public void PlayerUpdate()
    {
        if(Dead) { return; }

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
        #endregion
    }

    void Rotate()
    {
        if (weapon.lockedTarget == null)
        {
            Vector3 mpos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mpos.z = 0;
            RotateToDetination(mpos);
        }
        else
        {
            RotateToDetination(weapon.lockedTarget.transform.position);
        }
    }

    public override void RotateToDetination(Vector3 pos)
    {
        FireDirection = pos - transform.position;
        FireDirection = FireDirection.normalized;
        float angle = Mathf.Atan2(FireDirection.y, FireDirection.x) * Mathf.Rad2Deg;
        // 这里的旋转用缓动会更好看一些，直接设置角度会有点生硬
        transform.localEulerAngles = new Vector3(0, 0, angle - 90);
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        Vector3 dir = new Vector3(x, y, 0);

        transform.position += dir * moveSpeed * Time.deltaTime;

        Vector3 spos = GameManager.Instance.mainCamera.WorldToScreenPoint(transform.position);

        // 玩家半径（或者半宽半高）
        float offset = 0.5f;

        // 先转换一下偏移到屏幕距离
        Vector3 offsetScreen =
            GameManager.Instance.mainCamera.WorldToScreenPoint(new Vector3(offset, offset, 0)) -
            GameManager.Instance.mainCamera.WorldToScreenPoint(Vector3.zero);

        float ox = offsetScreen.x;
        float oy = offsetScreen.y;

        // 左右边界
        spos.x = Mathf.Clamp(spos.x, ox, Screen.width - ox);

        // 上下边界
        spos.y = Mathf.Clamp(spos.y, oy, Screen.height - oy);

        Vector3 wpos = GameManager.Instance.mainCamera.ScreenToWorldPoint(spos);

        wpos.z = transform.position.z;

        transform.position = wpos;
    }

    public override void TakeDamage(int damage)
    {
        currentHp -= damage;
        if (currentHp <= 0)
        {
            currentHp = 0;
            Dead = true;

            WeaponSystem.RemoveWeapon(weapon);// 先移除武器，避免在销毁敌人后还调用武器的Update方法
        }
    }
}
