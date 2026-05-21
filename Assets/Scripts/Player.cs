using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.MemoryProfiler;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
public class Player : Entity
{
    int currentHp = 0;
    int totalHp = 0;
    int level = 1;
    public PlayerData playerData;
    private Transform fire;
    int currentExp = 0;
    int needExp = 50;

    Vector3 MoveDir;
    float MoveAngle;
    public float PlayerPower { get; set; }

    public Dictionary<string, int> buildDict = new Dictionary<string, int>();
    // =========================
    // 流派系统 逻辑在407行左右
    // =========================
    // 是否形成核爆流
    public bool HasNuclearBuild;

    // 是否形成裂变流
    public bool HasSplitBuild;

    // 是否形成无限火力流
    public bool HasFireBuild;

    public int KilledCount { get;private set; }
    public bool HasLegendSplit { get; set; }
    public bool HasCritExplosion { get; set; }
    public bool HasPierceExplosion { get; set; }
    public bool HasLowBulletHighDamage { get; set; }

    // 是否是无敌状态，测试用.
    public bool IsInvincible { get; set; }

    public bool ChainedLightningActive { get; set; }
    List<Transform> chainedTargets = new List<Transform>();
    // 冲刺相关
    public bool IsDash { get; set; }
    float dashTimer = 0;
    float dashDuration = 0.18f;
    float dashCooldown = 0;
    float dashCooldownTime = 1.2f;
    Vector3 dashDir;
    // Dash残影
    float ghostTimer = 0;

    /// <summary>
    /// 飞机类型，决定了飞机的技能
    /// </summary>
    public PlayerType playerType { get; set; }
    public void AddKilledCount()
    {
        KilledCount++;
    }
    public void AddKilledCount(int count)
    {
        KilledCount += count;
    }
    public void Init(PlayerData data)
    {
        fire = transform.Find("Fire");
        FirePos = fire.Find("firePos");

        CurrentBulletCount = 3;
        FireDirection = Vector3.up;

        playerData = data;// 拿到玩家数据
        totalHp = (int)playerData.Hp;
        currentHp = (int)playerData.Hp;
        PlayerPower = playerData.Atk;
        currentExp = 0;
        level = (int)playerData.Level;
        playerType = DataManager.myGameData.playerType;// 这里后面需要在局外系统中设置成玩家选择的类型

        transform.Find("Fire/view").GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"sprites/PlayerTypeIcon/{(int)playerType}");
        EntityTag = "player";
        weapon = WeaponSystem.CreateWeapon(playerData.CurrentWeaponIndex, this);
        weapon.ChangeBullet(2);
        attackType = AttackType.Sector;
        moveSpeed = playerData.MoveSpeed;
    }

    public void SetWeaponAttackRange(float v)
    {
        weapon.SetWeaponAttackRange(v);
    }

    public void ResetWeaponAttackRange()
    {
        weapon.SetWeaponAttackRange(weapon.weaponData.AttackRange);
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
    public void FilledTotalHp()
    {
        // 直接加满血
        currentHp = totalHp;
    }
    public void AddHP(int v)
    {
        currentHp += v;
        if (currentHp > totalHp)
        {
            currentHp = totalHp;
        }
    }
    public void AddExp(int exp)
    {
        currentExp += exp;
        bool isLevelUp = false;

        // 允许连续升级
        while (currentExp >= needExp)
        {
            currentExp -= needExp;

            level++;

            isLevelUp = true;

            // 经验需求增长
            needExp = Mathf.CeilToInt(needExp * 1.38f);

            if (!GameManager.Instance.levelPanel.activeSelf)
            {
                GameManager.Instance.ShowLevelUpPanel(true);
            }
            else
            {
                break;
            }
        }

        // 本次AddExp只增加一次移速
        if (isLevelUp)
        {
            moveSpeed += 0.05f;
        }
    }
    public int GetCurrentLevel()
    {
        return level;
    }
    public int SetCurrentLevel(int newLevel)
    {
        level = newLevel;
        return level;
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

        UpdateDash();
        Move();
        Rotate();

        if(weapon != null && weapon.lockedTarget != null)
        {
            if (ChainedLightningActive)
            {
                var points = new List<Vector3>();
                for (int i = 0; i < chainedTargets.Count; i++)
                {
                    points.Add(chainedTargets[i].position);
                }
                LightningManager.Instance.UpdateChainPosition(points);
            }
            else
            {
                LightningManager.Instance.UpdateSinglePosition(transform.position, weapon.lockedTarget.transform.position);
            }
        }
        else
        {
            if(ChainedLightningActive)
            {
                LightningManager.Instance.ClearChain();
            }
            else
            {
                LightningManager.Instance.ClearSingle();
            }
        }
    }
    public void UpdateChaineLaser(List<Transform> transforms)
    {
        chainedTargets = transforms;
    }
    public void ChangeWhenInWave(bool state)
    {
        if(weapon.weaponData.type == WeaponType.Laser)
        {
            if(state == false)
            {
                // 直接将激光武器改成chain模式
                LightningManager.Instance.ClearChain();
            }
            else
            {
                ChainedLightningActive = state;
            }
            LightningManager.Instance.ClearSingle();
        }
        else
        {
            CurrentBulletCount = CurrentBulletCount + (state == true ? 10 : -10);
        }
    }
    public override void ChangeWeaponAttackType(AttackType attackType, int _currentBulletCount = 3)
    {
        this.attackType = attackType;
        weapon.ChangeAttackType(this.attackType, this, CurrentBulletCount);
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

        if (IsDash)
            return;

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

    void UpdateDash()
    {
        // CD
        if (dashCooldown > 0)
        {
            dashCooldown -= Time.deltaTime;
        }

        // Dash期间
        if (IsDash)
        {
            dashTimer -= Time.deltaTime;
            transform.position += dashDir * 25f * Time.deltaTime;

            // 残影生成
            ghostTimer -= Time.deltaTime;

            if (ghostTimer <= 0)
            {
                ghostTimer = 0.03f;

                SpawnDashGhost();
            }

            if (dashTimer <= 0)
            {
                dashTimer = 0;

                IsDash = false;
            }

            return;
        }

        // 触发Dash
        if (Input.GetKeyDown(KeyCode.Space) &&
            dashCooldown <= 0)
        {
            

            dashDir = MoveDir;

            IsDash = true;

            dashTimer = dashDuration;

            dashCooldown = dashCooldownTime;
        }
    }

    void SpawnDashGhost()
    {
        GameObject ghost = new GameObject("DashGhost");

        ghost.transform.position = transform.position;

        ghost.transform.rotation = transform.rotation;

        ghost.transform.localScale = transform.localScale;

        SpriteRenderer ghostSr = ghost.AddComponent<SpriteRenderer>();

        SpriteRenderer mySr = GetComponentInChildren<SpriteRenderer>();

        ghostSr.sprite = mySr.sprite;

        ghostSr.flipX = mySr.flipX;

        ghostSr.sortingLayerID = mySr.sortingLayerID;

        ghostSr.sortingOrder = mySr.sortingOrder - 1;

        ghostSr.color = new Color(0.5f, 0.8f, 1f, 0.5f);

        StartCoroutine(FadeGhost(ghostSr));
    }
    IEnumerator FadeGhost(SpriteRenderer sr)
    {
        if (sr == null) yield break;
        float life = 0.2f;

        while (life > 0)
        {
            life -= Time.deltaTime;

            Color c = sr.color;

            c.a = life / 0.2f;

            sr.color = c;

            sr.transform.localScale += Vector3.one * 1.5f * Time.deltaTime;

            yield return null;
        }
        if (sr != null)
        {
            Destroy(sr.gameObject);
        }
    }

    public override void TakeDamage(int damage,bool isCrit)
    {
        if (IsInvincible) return;
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
            
            GameManager.Instance.GameOver = true;
            GameManager.Instance.IsGameStarted = false;
            // 显示游戏结束界面
            GameManager.Instance.ShowGameOverPanel(true);
        }
    }
    IEnumerator ResetColor()
    {
        yield return new WaitForSeconds(0.1f);
        GetComponentInChildren<SpriteRenderer>().color = Color.white;
    }

    public void CheckBuildCombo()
    {
        // =========================
        // 核爆流
        // 暴击爆炸 + 穿透爆炸
        // =========================

        if (HasCritExplosion && HasPierceExplosion)
        {
            HasNuclearBuild = true;
        }

        // =========================
        // 裂变流
        // 裂变 + 多子弹
        // =========================

        if (HasLegendSplit && CurrentBulletCount >= 6)
        {
            HasSplitBuild = true;
        }

        // =========================
        // 无限火力流
        // 高攻速 + 高子弹数
        // =========================

        if (CurrentBulletCount >= 8 && GetCurrentWeapon().GetFireInterval() <= 0.12f)
        {
            HasFireBuild = true;
        }
    }
}
