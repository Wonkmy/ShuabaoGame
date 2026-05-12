using System;
using System.Collections;
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
    int currentExp = 0;
    int needExp = 100;

    Vector3 MoveDir;
    float MoveAngle;
    public void Init(PlayerData data)
    {
        fire = transform.Find("Fire");
        FirePos = fire.Find("firePos");

        CurrentBulletCount = 3;
        FireDirection = Vector3.up;

        playerData = data;// 拿到玩家数据
        totalHp = (int)playerData.Hp;
        currentHp = (int)playerData.Hp;

        currentExp = 0;
        level = (int)playerData.Level;

        weapon = WeaponSystem.CreateWeapon(playerData.CurrentWeaponIndex, this);
        weapon.ChangeBullet(2);
        attackType = AttackType.Sector;
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

            // 升级啦!
            GameManager.Instance.ShowLevelUpPanel(true);
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
    }

    public override void ChangeWeaponAttackType(AttackType attackType, int _currentBulletCount = 3)
    {
        this.attackType = attackType;
        CurrentBulletCount = _currentBulletCount;
        weapon.ChangeAttackType(this.attackType, this, _currentBulletCount);
    }
    void Rotate()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 mpos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mpos.z = 0;
            RotateToDetination(mpos);
            return;
        }

        // 其次判断是否有锁定目标
        if (weapon.lockedTarget != null)
        {
            RotateToDetination(weapon.lockedTarget.transform.position);
        }
        else if (MoveDir != Vector3.zero)
        {
            MoveAngle = Mathf.Atan2(MoveDir.y, MoveDir.x) * Mathf.Rad2Deg;
            float targetAngle = MoveAngle - 90;
            float currentZ = transform.localEulerAngles.z;
            float smoothAngle = Mathf.LerpAngle(currentZ, targetAngle, Time.deltaTime * 60);
            transform.localEulerAngles = new Vector3(0, 0, smoothAngle);
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

        MoveDir = new Vector3(x, y, 0);

        transform.position += MoveDir * moveSpeed * Time.deltaTime;

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
        GetComponentInChildren<SpriteRenderer>().color = Color.red;
        StartCoroutine(ResetColor());

        float percent = Mathf.Clamp01((float)currentHp / totalHp);

        if (percent <= 0.3f)
        {
            GameManager.Instance.cameraEffect.intensity = Mathf.Clamp01(1 - (percent / 0.3f));
        }
        else
        {
            GameManager.Instance.cameraEffect.intensity = 0;
        }

        if (currentHp <= 0)
        {
            currentHp = 0;
            Dead = true;

            WeaponSystem.RemoveWeapon(weapon);
        }
    }
    IEnumerator ResetColor()
    {
        yield return new WaitForSeconds(0.1f);
        GetComponentInChildren<SpriteRenderer>().color = Color.white;
    }
}
