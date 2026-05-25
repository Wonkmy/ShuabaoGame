using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Player : Entity
{
    int currentHp = 0;
    int totalHp = 0;
    int level = 1;
    public PlayerData playerData;
    private Transform fire;
    int currentExp = 0;
    int needExp = 48;
    float needExpGrowth = 1.34f;

    Vector3 MoveDir;
    float MoveAngle;
    public float PlayerPower { get; set; }
    public float playerDefence { get; set; }

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
    public List<GameObject> chainedTargets = new List<GameObject>();
    // 冲刺相关
    public bool IsDash { get; set; }
    float dashTimer = 0;
    float dashDuration = 0.18f;
    float dashCooldown = 0;
    float dashCooldownTime = 1.2f;
    Vector3 dashDir;
    // Dash残影
    float ghostTimer = 0;

    // 每第N次开火触发一次强化齐射（按一次完整开火算，不按单颗子弹算）
    public int EnhancedShotInterval { get; set; } = 5;
    public float EnhancedShotDamageMultiplier { get; set; } = 1.8f;
    public int EnhancedShotBonusPierce { get; set; } = 2;
    public float EnhancedShotScaleMultiplier { get; set; } = 1.2f;
    public bool IsEnhancedShotActive { get; private set; }
    public int FireCastCount { get; private set; }
    /// <summary>
    /// 飞机类型，决定了飞机的技能
    /// </summary>
    public AirplaneType playerType { get; set; }

    // 速度、阻尼、惯性相关
    Vector3 velocity;// 当前速度
    [SerializeField]private float acceleration = 10;// 加速度
    [SerializeField] private float drag = 8f;// 阻力

    [SerializeField] private float moveRotateSpeed = 8f;
    [SerializeField] private float aimRotateSpeed = 36f;

    Transform _canvas;
    RectTransform canvasRect;
    public void AddKilledCount()
    {
        KilledCount++;
    }
    public void AddKilledCount(int count)
    {
        KilledCount += count;
    }
    public void BeginFireCast()
    {
        FireCastCount++;
        IsEnhancedShotActive = EnhancedShotInterval > 0 && FireCastCount % EnhancedShotInterval == 0;
    }
    public void EndFireCast()
    {
        IsEnhancedShotActive = false;
    }
    public void Init(PlayerData data)
    {
        fire = transform.Find("Fire");
        FirePos = fire.Find("view/firePos");
        view = transform.Find("Fire/view").GetComponent<SpriteRenderer>();

        CurrentBulletCount = 3;
        FireDirection = Vector3.up;

        playerData = data;// 拿到玩家数据
        totalHp = (int)playerData.Hp;
        currentHp = (int)playerData.Hp;
        PlayerPower = playerData.Atk;
        level = (int)playerData.Level;
        playerDefence = playerData.Def;
        playerType = DataManager.myGameData.playerType;
        currentExp = 0;
        needExp = GameManager.Instance.BalanceConfig.player.firstLevelExp;
        needExpGrowth = GameManager.Instance.BalanceConfig.player.expGrowth;

        FireCastCount = 0;
        IsEnhancedShotActive = false;

        view.sprite = Resources.Load<Sprite>($"sprites/PlayerTypeIcon/{(int)playerType}");
        EntityTag = "player";
        weapon = WeaponSystem.CreateWeapon((int)playerType, this);
        attackType = AttackType.Sector;
        moveSpeed = playerData.MoveSpeed;

        _canvas = GameObject.Find("Canvas").transform;
        canvasRect = _canvas.GetComponent<RectTransform>();

        GameManager.Instance.dash_slider.GetComponent<Image>().fillAmount = 0;
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
            GameManager.Instance.RecordLevelUp(level);

            isLevelUp = true;

            // 经验需求增长
            needExp = Mathf.CeilToInt(needExp * needExpGrowth);

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
    public int GetCurrentExp()
    {
        return currentExp;
    }
    public int GetNeedExp()
    {
        return needExp;
    }
    public int SetCurrentLevel(int newLevel)
    {
        level = newLevel;
        return level;
    }

    public void DebugSetProgression(int newLevel, float expRatio, float hpRatio)
    {
        level = Mathf.Max(1, newLevel);

        needExp = GameManager.Instance.BalanceConfig.player.firstLevelExp;
        for (int i = 1; i < level; i++)
        {
            needExp = Mathf.CeilToInt(needExp * needExpGrowth);
        }

        currentExp = Mathf.Clamp(Mathf.FloorToInt(needExp * expRatio), 0, needExp - 1);
        currentHp = Mathf.Clamp(Mathf.RoundToInt(totalHp * hpRatio), 1, totalHp);
        Dead = false;
    }

    public float GetExpProgress()
    {
        return (float)currentExp / needExp;
    }

    public float GetHpProgress()
    {
        return (float)currentHp / totalHp;
    }

    public void AddDefence(float def)
    {
        playerDefence += def;
    }
    public void PlayerUpdate()
    {
        if(Dead) { return; }

        UpdateDash();
        Move();
        //DashSliderFollow();
        Rotate();

        if(weapon != null && weapon.lockedTarget != null)
        {
            if (ChainedLightningActive)
            {
                var points = new List<Vector3>();
                for (int i = 0; i < chainedTargets.Count; i++)
                {
                    points.Add(chainedTargets[i].transform.position);
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
                chainedTargets.Clear();
            }
            else
            {
                LightningManager.Instance.ClearSingle();
            }
        }
    }

    void DashSliderFollow()
    {
        Vector3 worldPos = transform.position - new Vector3(0, 0.5f, 0);
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPos);

        Vector2 localPoint;
        // 关键转换API
        bool isInside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            null,
            out localPoint
        );

        GameManager.Instance.dash_slider.GetComponent<RectTransform>().anchoredPosition = localPoint;
    }
    
    public void ChangeWhenInWave(bool state)
    {
        if(weapon.weaponData.type == WeaponType.Laser)
        {
            if(state == false)
            {
                LightningManager.Instance.ClearChain();
                chainedTargets.Clear();
            }
            ChainedLightningActive = state;
            LightningManager.Instance.ClearSingle();
        }
        else
        {
            //if (state)
            //{
            //    CurrentBulletCount += 10;
            //}
            //else
            //{
            //    CurrentBulletCount -= 10;
            //    if (CurrentBulletCount < 3)
            //    {
            //        CurrentBulletCount = 3;
            //    }
            //}
            if (state)
            {
                ChangeWeaponAttackType(AttackType.Cicle, 15);
            }
            else
            {
                ChangeWeaponAttackType(AttackType.Sector,CurrentBulletCount);
            }
        }
    }
    public override void ChangeWeaponAttackType(AttackType attackType, int _currentBulletCount)
    {
        this.attackType = attackType;
        weapon.ChangeAttackType(this.attackType, this, _currentBulletCount <= 3 ? CurrentBulletCount : _currentBulletCount);
    }

    /// <summary>
    /// 开火时的后坐力效果
    /// </summary>
    /// <param name="fireDir"></param>
    /// <param name="strength"></param>
    public void ApplyFireRecoil(Vector3 fireDir, float strength = 0.06f)
    {
        transform.position -= fireDir.normalized * strength;
    }

    void Rotate()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 mpos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mpos.z = 0;
            RotateToDetination(mpos, aimRotateSpeed);
            return;
        }

        if (weapon.lockedTarget != null)
        {
            RotateToDetination(weapon.lockedTarget.transform.position, aimRotateSpeed);
            return;
        }

        if (velocity.sqrMagnitude > 0.01f)
        {
            MoveAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            float targetAngle = MoveAngle - 90;
            float currentZ = view.transform.localEulerAngles.z;

            float smoothAngle = Mathf.LerpAngle(
                currentZ,
                targetAngle,
                Time.deltaTime * moveRotateSpeed
            );

            view.transform.localEulerAngles = new Vector3(0, 0, smoothAngle);
        }
    }
    public void RotateToDetination(Vector3 pos, float rotateSpeed)
    {
        Vector3 dir = pos - transform.position;

        if (dir.sqrMagnitude <= 0.0001f)
            return;

        FireDirection = dir.normalized;

        float angle = Mathf.Atan2(FireDirection.y, FireDirection.x) * Mathf.Rad2Deg;
        float targetAngle = angle - 90;
        float currentZ = view.transform.localEulerAngles.z;

        float smoothAngle = Mathf.LerpAngle(
            currentZ,
            targetAngle,
            Time.deltaTime * rotateSpeed
        );

        view.transform.localEulerAngles = new Vector3(0, 0, smoothAngle);
    }
    public override void RotateToDetination(Vector3 pos)
    {
        RotateToDetination(pos, aimRotateSpeed);
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        MoveDir = new Vector3(x, y, 0).normalized;
        if (MoveDir.magnitude > 1f)
            MoveDir.Normalize();

        if (IsDash)
            return;

        Vector3 targetVelocity = MoveDir * moveSpeed;

        float accel = MoveDir.sqrMagnitude > 0.01f ? acceleration : drag;

        velocity = Vector3.MoveTowards(
            velocity,
            targetVelocity,
            accel * Time.deltaTime
        );

        transform.position += velocity * Time.deltaTime;

        ClampToScreen();
    }

    private void ClampToScreen()
    {
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
            GameManager.Instance.dash_slider.GetComponent<Image>().fillAmount = dashCooldown / dashCooldownTime;
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
        if (Input.GetKeyDown(KeyCode.Space) && dashCooldown <= 0)
        {
            dashDir = MoveDir;

            IsDash = true;

            dashTimer = dashDuration;

            dashCooldown = dashCooldownTime;
            GameManager.Instance.dash_slider.GetComponent<Image>().fillAmount = 1;
        }
    }

    void SpawnDashGhost()
    {
        GameObject ghost = new GameObject("DashGhost");

        ghost.transform.position = transform.position;

        ghost.transform.localEulerAngles = view.transform.localEulerAngles;

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

        // 计算实际伤害。需要考虑玩家的防御力，公式为：实际伤害 = 伤害 * (100 / (100 + 防御力))
        int actualDamage = Mathf.RoundToInt(damage * (100f / (100f + playerDefence)));
        GameManager.Instance.RecordPlayerDamageTaken(actualDamage);
        currentHp -= actualDamage;
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
