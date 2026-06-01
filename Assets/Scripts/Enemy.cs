using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyMoveIntent
{
    Chase,
    Strafe,
    HoldRange,
    Reposition
}

public class Enemy : Entity
{
    int baseHp = 0;
    int currentHp = 0;
    int totalHp = 0;
    public EnemyType enemyType;
    public Transform target;
    public bool hasShield = false;
    public bool IsSpecialEnemy { get; set; }// 是否是特殊怪物（精英怪和Boss）
    public float Damage { get; set; }
    public bool HasEnterScreen { get; set; }// 是否已经进入屏幕（用于怪物在视野内才能被攻击）
    public bool IsBattleActive { get; set; }
    public Transform hp { get; set; }
    public int BossPhase { get; set; } = 1;
    public bool IsFinalBoss { get; private set; }
    public bool IsChapterMiniBoss { get; private set; }
    public bool IsBossCombatEncounter { get; private set; }
    public bool IsBossVulnerable { get; private set; }
    public bool IsBossPhaseBreaking { get; private set; }
    public bool IsFinalBossStateRepositioning { get; private set; }
    public Vector3 EstimatedVelocity { get; private set; }
    float baseMoveSpeed = 0f;

    float attackRange = 0f;// 攻击范围，也就是敌人停止移动开始攻击的距离
    float findTargetRange = 10f;// 寻找目标的范围
    EnemyMoveIntent moveIntent = EnemyMoveIntent.Chase;
    float movementThinkTimer = 0f;
    float strafeSign = 1f;
    Vector3 repositionDestination;
    Vector3 finalBossStateDestination;
    float finalBossStateRepositionTimer;

    public void SetEnemy(EnemyData enemyData)
    {
        view = GetComponentInChildren<SpriteRenderer>();
        enemyType = enemyData.type;
        moveSpeed = enemyData.moveSpeed;
        baseMoveSpeed = moveSpeed;
        transform.localScale = Vector3.one * enemyData.scale;

        baseHp = enemyData.hp;

        currentHp = Mathf.FloorToInt(baseHp * GameManager.Instance.currentEnemyHpFactor);
        totalHp = currentHp;



        hp = transform.Find("hp");
        hp.Find("slider").localScale = new Vector3((float)currentHp / totalHp, 1, 1);
        if (enemyType == EnemyType.Boss)
        {
            hp.gameObject.SetActive(true);
        }else if(enemyType == EnemyType.Elite)
        {
            hp.gameObject.SetActive(true);
        }
        else
        {
            hp.gameObject.SetActive(false);
        }

        Damage = Mathf.FloorToInt(enemyData.damage * GameManager.Instance.currentEnemyAtkFactor);

        view.sprite = Resources.Load<Sprite>("sprites/" + enemyType.ToString().ToLower());

        FirePos = transform;
        attackType = AttackType.Sector;
        CurrentBulletCount = 3;
        EntityTag = "enemy";

        if (enemyType != EnemyType.SelfExplosion) {
            weapon = WeaponSystem.CreateWeapon(enemyData.CurrentWeaponIndex, this);
            attackRange = weapon.attackRange;
            if (enemyType == EnemyType.Boss)
            {
                weapon.ChangeFireInterval(0.4f);
                weapon.ChangeBullet(2);
                attackRange += 5f;// Boss的攻击范围更大一些
                findTargetRange += 3f;// Boss的寻找目标范围更大一些
            }
        }
        CanMove = true;
        Dead = false;
        ResetMovementBrain();
    }

    public void ConfigureFinalBossCombat(bool isFinalBoss)
    {
        ConfigureChapterBossCombat(isFinalBoss, isFinalBoss);
    }

    public void ConfigureChapterBossCombat(bool isFinalBoss, bool isChapterBoss)
    {
        IsFinalBoss = isFinalBoss;
        IsChapterMiniBoss = isChapterBoss && enemyType == EnemyType.Elite;
        IsBossCombatEncounter = enemyType == EnemyType.Boss || IsChapterMiniBoss;

        if (!IsBossCombatEncounter)
            return;

        BossCombatController controller = GetComponent<BossCombatController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<BossCombatController>();
        }

        controller.Init(this, IsFinalBoss, IsChapterMiniBoss || (enemyType == EnemyType.Boss && !IsFinalBoss));
    }

    public void EnemyUpdate()
    {
        if (Dead) { return; }
        if (GameManager.Instance.IsTimeStop)
            return;

        Rotate();

        Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);

        if (!HasEnterScreen)
        {
            if (viewPos.x >= 0 &&viewPos.x <= 1 &&viewPos.y >= 0 && viewPos.y <= 1)
            {
                if(enemyType == EnemyType.Elite || enemyType == EnemyType.Boss)
                {
                    var specialEventObj = GameManager.Instance.SpwanWorldTxt($"{enemyType.ToString()}来袭！", 1.0f);
                    GameManager.Instance.StartRuntimeCoroutine(GameManager.Instance.ShowFlashWarningTxt(specialEventObj));
                }

                HasEnterScreen = true;
                IsBattleActive = true;
                // 如果是boss进场，则时间放慢为0.25倍速，增加紧张感。0.2秒钟之后恢复正常速度
                if (enemyType == EnemyType.Boss)
                {
                    Time.timeScale = 0.5f;
                    GameManager.Instance.StartCoroutine(ResetTimeScale());
                }
            }
        }

        if(enemyType != EnemyType.SelfExplosion)
        {
            NormalMove();// 普通移动
        }
        else
        {
            ExplosionMove();// 自爆怪的移动
        }
    }

    void ExplosionMove()
    {
        if (target != null && CanMove)
        {
            MoveToPosition(Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime));
            if (Vector3.Distance(transform.position, target.position) <= 0.1f)
            {
                // 造成伤害
                Player player = target.GetComponent<Player>();
                if (player != null)
                {
                    player.TakeDamage(Mathf.FloorToInt(Damage), false);
                }
                Dead = true;
                CanMove = false;

                SpwanExpBall(false);
                StartCoroutine(DeathEffect());
            }
        }
    }

    void NormalMove()
    {
        if (GameManager.Instance.IsBlackHole && enemyType != EnemyType.Boss)
        {
            MoveToPosition(Vector3.MoveTowards(transform.position, GameManager.Instance.BlackHolePos, 8f * Time.deltaTime));
            return;
        }

        if (target == null || !CanMove)
            return;

        if (IsFinalBossStateRepositioning)
        {
            MoveFinalBossStateReposition();
            return;
        }

        EnemyMovementTuning tuning = GameManager.Instance.BalanceConfig.enemyMovement;
        if (tuning == null || !tuning.enableMovementBrain)
        {
            if (Vector3.Distance(transform.position, target.position) > findTargetRange)
            {
                MoveToPosition(Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime));
            }
            return;
        }

        UpdateMovementBrain(tuning);
        Vector3 moveDirection = GetMovementDirection(tuning);
        if (moveDirection.sqrMagnitude <= 0.001f)
            return;

        float speedMultiplier = tuning.GetMoveSpeedMultiplier(enemyType);
        MoveToPosition(transform.position + moveDirection.normalized * moveSpeed * speedMultiplier * Time.deltaTime);
    }

    void MoveToPosition(Vector3 nextPosition)
    {
        if (Time.deltaTime > 0f)
        {
            EstimatedVelocity = (nextPosition - transform.position) / Time.deltaTime;
        }
        transform.position = nextPosition;
    }

    void MoveFinalBossStateReposition()
    {
        BossCombatTuning tuning = GameManager.Instance.BalanceConfig.bossCombat;
        Vector3 toDestination = finalBossStateDestination - transform.position;
        toDestination.z = 0f;
        finalBossStateRepositionTimer -= Time.deltaTime;

        if (toDestination.magnitude <= tuning.finalBossRepositionArrivalDistance || finalBossStateRepositionTimer <= 0f)
        {
            IsFinalBossStateRepositioning = false;
            movementThinkTimer = 0f;
            EstimatedVelocity = Vector3.zero;
            return;
        }

        Vector3 nextPosition = transform.position + toDestination.normalized * moveSpeed * tuning.finalBossRepositionSpeedMultiplier * Time.deltaTime;
        MoveToPosition(nextPosition);
    }

    void ResetMovementBrain()
    {
        moveIntent = EnemyMoveIntent.Chase;
        movementThinkTimer = Random.Range(0.05f, 0.35f);
        strafeSign = Random.value > 0.5f ? 1f : -1f;
        repositionDestination = transform.position;
        finalBossStateDestination = transform.position;
        finalBossStateRepositionTimer = 0f;
    }

    void UpdateMovementBrain(EnemyMovementTuning tuning)
    {
        movementThinkTimer -= Time.deltaTime;
        if (movementThinkTimer > 0f)
            return;

        movementThinkTimer = tuning.GetThinkInterval(enemyType, BossPhase);
        if (Random.value < 0.28f)
        {
            strafeSign *= -1f;
        }

        float distance = Vector3.Distance(transform.position, target.position);
        float desiredRange = GetDesiredMovementRange(tuning);

        if (enemyType == EnemyType.Thick)
        {
            ChooseThickMoveIntent(distance, desiredRange, tuning);
        }
        else if (enemyType == EnemyType.Elite)
        {
            ChooseEliteMoveIntent(distance, desiredRange, tuning);
        }
        else if (enemyType == EnemyType.Boss)
        {
            ChooseBossMoveIntent(distance, desiredRange, tuning);
        }
        else
        {
            ChooseNormalMoveIntent(distance, desiredRange);
        }
    }

    void ChooseNormalMoveIntent(float distance, float desiredRange)
    {
        moveIntent = distance > desiredRange ? EnemyMoveIntent.Chase : EnemyMoveIntent.Strafe;
    }

    void ChooseThickMoveIntent(float distance, float desiredRange, EnemyMovementTuning tuning)
    {
        float tolerance = desiredRange * tuning.rangeToleranceRatio;
        if (distance > desiredRange + tolerance)
        {
            moveIntent = EnemyMoveIntent.Chase;
        }
        else if (distance < desiredRange - tolerance)
        {
            moveIntent = EnemyMoveIntent.HoldRange;
        }
        else
        {
            moveIntent = Random.value > 0.35f ? EnemyMoveIntent.Strafe : EnemyMoveIntent.HoldRange;
        }
    }

    void ChooseEliteMoveIntent(float distance, float desiredRange, EnemyMovementTuning tuning)
    {
        if (distance > desiredRange * 1.22f)
        {
            moveIntent = EnemyMoveIntent.Chase;
            return;
        }

        if (Random.value < tuning.eliteRepositionChance)
        {
            PickRepositionDestination(desiredRange, 40f, 85f);
            moveIntent = EnemyMoveIntent.Reposition;
        }
        else
        {
            moveIntent = Random.value > 0.25f ? EnemyMoveIntent.Strafe : EnemyMoveIntent.HoldRange;
        }
    }

    void ChooseBossMoveIntent(float distance, float desiredRange, EnemyMovementTuning tuning)
    {
        if (distance < desiredRange * tuning.bossBackoffRangeRatio)
        {
            PickRepositionDestination(desiredRange, 95f, 145f);
            moveIntent = EnemyMoveIntent.Reposition;
            return;
        }

        if (distance > desiredRange * 1.28f)
        {
            moveIntent = EnemyMoveIntent.Chase;
            return;
        }

        float phaseRepositionChance = Mathf.Clamp01(tuning.bossRepositionChance + (BossPhase - 1) * 0.08f);
        if (Random.value < phaseRepositionChance)
        {
            PickRepositionDestination(desiredRange, 55f, 105f);
            moveIntent = EnemyMoveIntent.Reposition;
        }
        else
        {
            moveIntent = Random.value > 0.35f ? EnemyMoveIntent.Strafe : EnemyMoveIntent.HoldRange;
        }
    }

    Vector3 GetMovementDirection(EnemyMovementTuning tuning)
    {
        Vector3 toTarget = target.position - transform.position;
        toTarget.z = 0f;
        if (toTarget.sqrMagnitude <= 0.001f)
            return Vector3.zero;

        float distance = toTarget.magnitude;
        Vector3 toward = toTarget / distance;
        Vector3 away = -toward;
        Vector3 tangent = new Vector3(-toward.y, toward.x, 0f) * strafeSign;
        float desiredRange = GetDesiredMovementRange(tuning);

        if (moveIntent == EnemyMoveIntent.Chase)
        {
            float drift = enemyType == EnemyType.Thick ? tuning.thickStrafeWeight * 0.35f : tuning.normalDriftWeight;
            return toward + tangent * drift;
        }

        if (moveIntent == EnemyMoveIntent.Reposition)
        {
            Vector3 toDestination = repositionDestination - transform.position;
            toDestination.z = 0f;
            if (toDestination.sqrMagnitude < 0.25f)
            {
                moveIntent = EnemyMoveIntent.Strafe;
                return tangent;
            }
            return toDestination.normalized;
        }

        float correction = GetRangeCorrection(distance, desiredRange, tuning.rangeToleranceRatio);
        if (moveIntent == EnemyMoveIntent.HoldRange)
        {
            return tangent * 0.22f + toward * correction;
        }

        float strafeWeight = GetStrafeWeight(tuning);
        return tangent * strafeWeight + toward * correction;
    }

    float GetRangeCorrection(float distance, float desiredRange, float toleranceRatio)
    {
        float tolerance = desiredRange * toleranceRatio;
        if (distance > desiredRange + tolerance)
            return 0.65f;
        if (distance < desiredRange - tolerance)
            return -0.75f;
        return 0f;
    }

    float GetStrafeWeight(EnemyMovementTuning tuning)
    {
        if (enemyType == EnemyType.Thick)
            return tuning.thickStrafeWeight;
        if (enemyType == EnemyType.Elite)
            return tuning.eliteStrafeWeight;
        if (enemyType == EnemyType.Boss)
            return tuning.bossStrafeWeight + Mathf.Max(0, BossPhase - 1) * 0.08f;
        return tuning.normalDriftWeight;
    }

    float GetDesiredMovementRange(EnemyMovementTuning tuning)
    {
        float baseRange = Mathf.Max(1.5f, attackRange);
        return Mathf.Max(1.2f, baseRange * tuning.GetDesiredRangeRatio(enemyType));
    }

    void PickRepositionDestination(float desiredRange, float minAngle, float maxAngle)
    {
        Vector3 fromTarget = transform.position - target.position;
        fromTarget.z = 0f;
        if (fromTarget.sqrMagnitude <= 0.001f)
        {
            fromTarget = Vector3.right;
        }

        float angle = Random.Range(minAngle, maxAngle) * strafeSign;
        Vector3 dir = Quaternion.Euler(0f, 0f, angle) * fromTarget.normalized;
        repositionDestination = target.position + dir * desiredRange;
    }

    IEnumerator ResetTimeScale()
    {
        yield return new WaitForSeconds(0.25f);
        Time.timeScale = 1f;
    }
    public void AddShield()
    {
        hasShield = true;
        GameObject newShield = Instantiate(Resources.Load<GameObject>("shield"), transform.Find("view"));
        newShield.transform.localPosition = new Vector3(0, 1, 0);
    }
    public void RemoveShild()
    {
        Transform shield = transform.Find("view/shield(Clone)");
        if (shield != null)
        {
            Destroy(shield.gameObject);
        }
        hasShield = false;
    }
    public override void ChangeWeaponAttackType(AttackType attackType, int _currentBulletCount = 3)
    {
        this.attackType = attackType;
        weapon.ChangeAttackType(this.attackType, this, CurrentBulletCount);
    }
    void Rotate()
    {
        FireDirection = target.position - transform.position;
        FireDirection = FireDirection.normalized;
        float angle = Mathf.Atan2(FireDirection.y, FireDirection.x) * Mathf.Rad2Deg;
        transform.Find("view").localEulerAngles = new Vector3(0, 0, angle - 90);
    }

    public override Entity GetNearestTarget()
    {
        if (IsFinalBossStateRepositioning)
            return null;

        return target.GetComponent<Entity>();
    }

    public override void TakeDamage(int damage, bool isCrit)
    {
        BossCombatTuning bossTuning = GameManager.Instance.BalanceConfig.bossCombat;
        if (enemyType == EnemyType.Boss && IsFinalBoss)
        {
            if (IsFinalBossStateRepositioning && bossTuning.finalBossInvincibleDuringReposition)
            {
                return;
            }

            if (IsBossPhaseBreaking)
            {
                damage = Mathf.RoundToInt(damage * bossTuning.phaseBreakDamageTakenRatio);
            }
            else if (IsBossVulnerable)
            {
                damage = Mathf.RoundToInt(damage * bossTuning.finalBossVulnerableDamageMultiplier);
            }
            else
            {
                damage = Mathf.RoundToInt(damage * bossTuning.finalBossGuardedDamageTakenRatio);
            }
        }
        else if (IsChapterMiniBoss)
        {
            damage = Mathf.RoundToInt(damage * bossTuning.miniBossGuardedDamageTakenRatio);
        }

        damage = Mathf.Max(1, damage);
        currentHp -= damage;

        //GameManager.Instance.SpwanHitFx(transform.position);//  命中特效

        hp.Find("slider").localScale = new Vector3((float)currentHp / (float)totalHp, 1, 1);
        GetComponentInChildren<SpriteRenderer>().color = Color.red;
        StartCoroutine(ResetColor());

        Transform _canvas = GameObject.Find("Canvas").transform;
        GameObject newdamage = Instantiate(Resources.Load<GameObject>("damage_txt"), _canvas);
        RectTransform canvasRect = _canvas.GetComponent<RectTransform>();

        

        float randomOffsetX = Random.Range(-0.3f, 0.3f);
        float randomOffsetY = Random.Range(0.3f, 0.7f);
        Vector3 worldPos = transform.position + new Vector3(randomOffsetX, randomOffsetY, 0);
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPos);

        Vector2 localPoint;
        // 关键转换API
        bool isInside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,   
            screenPoint,  
            null,
            out localPoint
        );

        newdamage.GetComponent<RectTransform>().anchoredPosition = localPoint;
        newdamage.GetComponent<DamageText>().SetDamageText(damage, isCrit);
        DataManager.allDamageText.Add(newdamage);
        if (currentHp <= 0)
        {
            Dead = true;
            CanMove = false;

            SpwanExpBall(isCrit);
            // 生成金币或宝箱
            SpwanCoinAndChest();
            // 旋转缩小然后死亡
            StartCoroutine(DeathEffect());
        }
    }
    private void SpwanCoinAndChest() {

        if (GameManager.Instance.isWave)
        {
            RewardTuning rewardTuning = GameManager.Instance.BalanceConfig.reward;
            int waveCoinCount = rewardTuning.GetWaveCoinCount();
            for (int i = 0; i < waveCoinCount; i++)
            {
                float angle = i * (360f / waveCoinCount);
                Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * 0.55f;
                Vector3 randomOffset = new Vector3(Random.Range(-0.12f, 0.2f), Random.Range(-0.2f, 0.35f), 0);
                GameManager.Instance.SpwanCoin(transform.position + offset + randomOffset, rewardTuning.GetWaveCoinValue());
            }
        }
        else
        {
            if (enemyType != EnemyType.Elite && enemyType != EnemyType.Boss) return;
            RewardTuning rewardTuning = GameManager.Instance.BalanceConfig.reward;
            if (Random.value < rewardTuning.eliteBossChestChance)
            {
                GameManager.Instance.SpwanChest(transform.position);
            }
            else
            {
                int baseCoinCount = enemyType == EnemyType.Elite ? rewardTuning.GetEliteCoinCount() : rewardTuning.GetBossCoinCount();// 数量
                int baseCoinValue = enemyType == EnemyType.Elite ? rewardTuning.GetEliteCoinValue() : rewardTuning.GetBossCoinValue();// 价值
                if (baseCoinCount > 0)
                {
                    for (int i = 0; i < baseCoinCount; i++)
                    {
                        float angle = i * (360f / baseCoinCount);
                        Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * 0.55f;
                        Vector3 randomOffset = new Vector3(Random.Range(-0.12f, 0.2f), Random.Range(-0.2f, 0.35f), 0);
                        GameManager.Instance.SpwanCoin(transform.position + offset + randomOffset, baseCoinValue);
                    }
                }
            }
        }  
    }

    public float GetHpProgress()
    {
        if (totalHp <= 0)
            return 0f;

        return Mathf.Clamp01((float)currentHp / totalHp);
    }

    public void ApplyFinalBossDynamicHp(Player player)
    {
        if (enemyType != EnemyType.Boss || !IsFinalBoss || player == null)
            return;

        BossCombatTuning tuning = GameManager.Instance.BalanceConfig.bossCombat;
        float attackAboveStart = Mathf.Max(0f, player.playerData.Atk - GameManager.Instance.BalanceConfig.player.startAttack);
        int levelAboveStart = Mathf.Max(0, player.GetCurrentLevel() - 1);
        int buildStackCount = 0;
        int buildTagCount = 0;

        if (player.buildDict != null)
        {
            buildTagCount = player.buildDict.Count;
            foreach (KeyValuePair<string, int> pair in player.buildDict)
            {
                buildStackCount += Mathf.Max(0, pair.Value);
            }
        }

        float extraRatio =
            tuning.GetFinalBossExtraHpBaseRatio() +
            attackAboveStart * tuning.finalBossExtraHpPerAttack +
            levelAboveStart * tuning.finalBossExtraHpPerLevel +
            buildStackCount * tuning.finalBossExtraHpPerBuildStack +
            buildTagCount * tuning.finalBossExtraHpPerBuildTag +
            player.GetHpProgress() * tuning.finalBossExtraHpByPlayerHpProgress;

        extraRatio = Mathf.Clamp(extraRatio, 0f, tuning.GetFinalBossExtraHpMaxRatio());
        int extraHp = Mathf.FloorToInt(totalHp * extraRatio);
        if (extraHp <= 0)
            return;

        totalHp += extraHp;
        currentHp += extraHp;
        if (hp != null)
        {
            hp.Find("slider").localScale = new Vector3((float)currentHp / totalHp, 1, 1);
        }
    }

    public void ApplyBossPhase(int phase)
    {
        if (enemyType != EnemyType.Boss || !IsFinalBoss)
            return;

        BossPhase = Mathf.Clamp(phase, 1, 3);
        if (weapon != null)
        {
            weapon.SetFireInterval(GameManager.Instance.BalanceConfig.bossCombat.GetFireInterval(BossPhase));
        }
    }

    public void ApplyMiniBossEnrage()
    {
        if (!IsBossCombatEncounter || IsFinalBoss)
            return;

        BossCombatTuning tuning = GameManager.Instance.BalanceConfig.bossCombat;
        moveSpeed = baseMoveSpeed * tuning.miniBossEnrageMoveSpeedMultiplier;
        Weapon currentWeapon = GetCurrentWeapon();
        if (currentWeapon != null)
        {
            currentWeapon.ChangeFireInterval(-tuning.miniBossEnrageFireIntervalReduce);
        }

        PlayCombatWeightPulse(new Color(1f, 0.42f, 0.12f, 0.48f), 3.2f, 0.32f);
    }

    public void PlayBossAttackImpact(bool circleAttack)
    {
        if (enemyType != EnemyType.Boss)
            return;

        BossCombatTuning tuning = GameManager.Instance.BalanceConfig.bossCombat;
        float shakeStrength = circleAttack ? tuning.GetCircleAttackImpactShakeStrength() : tuning.GetSectorAttackImpactShakeStrength();
        GameManager.Instance.ShakeMainCamera(tuning.GetAttackImpactShakeDuration(), shakeStrength);
        PlayCombatWeightPulse(
            circleAttack ? new Color(1f, 0.14f, 0.06f, 0.46f) : new Color(1f, 0.62f, 0.08f, 0.42f),
            circleAttack ? 5.8f : 3.8f,
            0.34f);
    }

    public void BeginFinalBossStateReposition()
    {
        if (enemyType != EnemyType.Boss || !IsFinalBoss || Dead || target == null)
            return;

        BossCombatTuning tuning = GameManager.Instance.BalanceConfig.bossCombat;
        if (!tuning.finalBossRepositionAfterAttack)
            return;

        Vector3 fromPlayer = transform.position - target.position;
        fromPlayer.z = 0f;
        if (fromPlayer.sqrMagnitude <= 0.001f)
        {
            fromPlayer = Random.value > 0.5f ? Vector3.right : Vector3.up;
        }

        float side = Random.value > 0.5f ? 1f : -1f;
        float angle = Random.Range(tuning.finalBossRepositionMinAngle, tuning.finalBossRepositionMaxAngle) * side;
        float desiredRange = Mathf.Max(2.5f, attackRange * tuning.finalBossRepositionRangeRatio);
        Vector3 dir = Quaternion.Euler(0f, 0f, angle) * fromPlayer.normalized;
        finalBossStateDestination = target.position + dir * desiredRange;
        finalBossStateRepositionTimer = tuning.finalBossRepositionMaxDuration;
        IsFinalBossStateRepositioning = true;
        moveIntent = EnemyMoveIntent.Reposition;

        if (weapon != null)
        {
            weapon.lockedTarget = null;
        }

        PlayCombatWeightPulse(new Color(1f, 0.36f, 0.08f, 0.32f), 2.4f, 0.24f);
    }

    public void PlayCombatWeightPulse(Color color, float scale, float duration)
    {
        GameManager.Instance.SpwanEnemyAttackPulse(transform.position, color, scale, duration);
    }

    public void StartBossPhaseBreak(float duration, int phase)
    {
        if (enemyType != EnemyType.Boss || !IsFinalBoss)
            return;

        StartCoroutine(BossPhaseBreak(duration, phase));
    }

    IEnumerator BossPhaseBreak(float duration, int phase)
    {
        IsBossPhaseBreaking = true;
        bool wasCanMove = CanMove;
        CanMove = false;
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        Color startColor = sr != null ? sr.color : Color.white;
        Vector3 originalScale = transform.localScale;
        BossCombatTuning tuning = GameManager.Instance.BalanceConfig.bossCombat;
        GameManager.Instance.SpwanEnemyAttackPulse(transform.position, new Color(1f, 0.08f, 0.04f, 0.5f), tuning.phaseBreakPulseScale + phase * 0.45f, duration);

        float timer = 0f;
        while (timer < duration && !Dead)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float pulse = Mathf.Sin(t * Mathf.PI);
            transform.localScale = Vector3.Lerp(originalScale, originalScale * (1f + phase * 0.08f), pulse);
            if (sr != null)
            {
                sr.color = Color.Lerp(startColor, new Color(1f, 0.26f, 0.1f, 1f), Mathf.Abs(Mathf.Sin(timer * 16f)));
            }
            yield return null;
        }

        transform.localScale = originalScale;
        if (sr != null && !Dead)
        {
            sr.color = Color.white;
        }
        CanMove = wasCanMove && !Dead;
        IsBossPhaseBreaking = false;
    }

    public void PlayBossChargeFlash(bool circleAttack)
    {
        if (enemyType != EnemyType.Boss || !IsFinalBoss)
            return;

        if (bossChargeCoroutine != null)
        {
            StopCoroutine(bossChargeCoroutine);
        }

        bossChargeCoroutine = StartCoroutine(BossChargeFlash(circleAttack));
    }

    IEnumerator BossChargeFlash(bool circleAttack)
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
            yield break;

        Color startColor = sr.color;
        Color chargeColor = circleAttack ? new Color(1f, 0.22f, 0.12f, 1f) : new Color(1f, 0.72f, 0.18f, 1f);
        float timer = 0f;
        BossCombatTuning tuning = GameManager.Instance.BalanceConfig.bossCombat;
        float duration = tuning.chargeFlashDuration;
        bool wasCanMove = CanMove;
        Vector3 originalScale = transform.localScale;
        if (tuning.lockMoveDuringCharge)
        {
            CanMove = false;
        }
        while (timer < duration && !Dead)
        {
            timer += Time.deltaTime;
            float normalized = Mathf.Clamp01(timer / duration);
            float pulse = Mathf.Sin(normalized * Mathf.PI);
            float t = Mathf.Abs(Mathf.Sin(timer * 18f));
            sr.color = Color.Lerp(startColor, chargeColor, t);
            transform.localScale = Vector3.Lerp(originalScale, originalScale * tuning.chargeScalePulse, pulse);
            yield return null;
        }

        transform.localScale = originalScale;
        if (!Dead)
        {
            sr.color = Color.white;
        }
        CanMove = wasCanMove && !Dead;

        bossChargeCoroutine = null;
    }

    public void StartBossVulnerableWindow(float duration)
    {
        if (bossVulnerableCoroutine != null)
        {
            StopCoroutine(bossVulnerableCoroutine);
        }

        bossVulnerableCoroutine = StartCoroutine(BossVulnerableWindow(duration));
    }

    IEnumerator BossVulnerableWindow(float duration)
    {
        if (enemyType != EnemyType.Boss || !IsFinalBoss || Dead)
            yield break;

        IsBossVulnerable = true;
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;
        if (sr != null)
        {
            sr.color = new Color(0.65f, 0.9f, 1f, 1f);
        }

        yield return new WaitForSeconds(duration);

        IsBossVulnerable = false;
        if (sr != null && !Dead)
        {
            sr.color = originalColor;
        }

        bossVulnerableCoroutine = null;
    }
    private void SpwanExpBall(bool isCrit)
    {
        RewardTuning rewardTuning = GameManager.Instance.BalanceConfig.reward;
        EnemyReward reward = rewardTuning.GetEnemyReward(enemyType);
        float finalExp = reward.baseExp * rewardTuning.GetExpMultiplier(isCrit);
        int expValue = Mathf.FloorToInt(finalExp);
        int expBallCount = rewardTuning.GetExpBallCount(reward);
        float spreadRadius = rewardTuning.GetSpreadRadius(reward);

        if (expBallCount <= 1 || spreadRadius <= 0f)
        {
            GameManager.Instance.SpwanExpBall(transform.position, enemyType, expValue);
            return;
        }

        for (int i = 0; i < expBallCount; i++)
        {
            float angle = i * (360f / expBallCount);
            Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * spreadRadius;
            Vector3 randomOffset = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f), 0);
            GameManager.Instance.SpwanExpBall(transform.position + offset + randomOffset, enemyType, expValue);
        }
    }

    IEnumerator ResetColor()
    {
        yield return new WaitForSeconds(0.1f);
        GetComponentInChildren<SpriteRenderer>().color = Color.white;
    }

    IEnumerator DeathEffect()
    {
        if (bossChargeCoroutine != null)
        {
            StopCoroutine(bossChargeCoroutine);
            bossChargeCoroutine = null;
        }

        if (bossVulnerableCoroutine != null)
        {
            StopCoroutine(bossVulnerableCoroutine);
            bossVulnerableCoroutine = null;
        }

        WeaponSystem.RemoveWeapon(weapon);// 先移除武器，避免在销毁敌人后还调用武器的Update方法

        if (enemyType == EnemyType.Elite || enemyType == EnemyType.Boss)
        {
            yield return StartCoroutine(HeavyDeathCollapse());
        }
        else
        {
            yield return StartCoroutine(LightDeathCollapse());
        }

        // 增加击杀统计
        GameManager.Instance.RecordEnemyKilled(enemyType);
        GameManager.Instance.GetPlayer().AddKilledCount();
        
        if (IsSpecialEnemy)
        {
            GameManager.Instance.IsSpecialEvent = false;// 结束特殊事件
            // 特殊事件结束后，重新计算下一次特殊事件间隔
            GameManager.Instance.nextSpecialEventInterval = GameManager.Instance.CalculateDynamicSpecialEventInterval();
            //GameManager.Instance.player.GetComponent<Player>().ResetWeaponAttackRange();// 重置玩家的武器攻击范围
            GameManager.Instance.cameraEffect.darkIntensity = 0.0f;
        }

        if (enemyType == EnemyType.Boss && IsFinalBoss)
        {
            GameManager.Instance.PlayFinalBossDeathReward(transform.position);
            GameManager.Instance.EndFinalBossAtmosphere();
        }

        view.gameObject.SetActive(false);// 先隐藏敌人，等特效播放完再销毁
        yield return new WaitForSeconds(0.2f);
        // 如果是精英怪或血厚怪，生成一个加血道具
        if (enemyType == EnemyType.Elite || enemyType == EnemyType.Thick)
        {
            float r = Random.Range(0f, 1f);
            if(r < 0.168f)
            {
                GameObject newAddHp = Instantiate(Resources.Load<GameObject>("add_hp"), transform.position, Quaternion.identity);
                newAddHp.GetComponent<AddHP>().SetAddHP(10, GameManager.Instance.player, true);
            }
            //Instantiate(Resources.Load<GameObject>("bigDeadFX"), transform.position, Quaternion.identity);
            GameObject bigDeaddFX = DeadFXPool.Instance.Get("bigDeadFX");
            if (bigDeaddFX != null)
            {
                bigDeaddFX.transform.position = transform.position;
            }
        }
        else
        {
            //Instantiate(Resources.Load<GameObject>("deadFX"), transform.position, Quaternion.identity);
            GameObject deadFX = DeadFXPool.Instance.Get("deadFX");
            if (deadFX != null)
            {
                deadFX.transform.position = transform.position;
            }
        }


        DataManager.allEnemyDict.Remove(gameObject);// 从敌人字典中移除
        if (GameManager.Instance.GetPlayer().chainedTargets.Contains(gameObject))
        {
            GameManager.Instance.GetPlayer().chainedTargets.Remove(gameObject);// 从玩家的连锁目标列表中移除
        }
        Destroy(gameObject);
    }

    IEnumerator LightDeathCollapse()
    {
        EnemyDeathEffectTuning tuning = GameManager.Instance.BalanceConfig.deathEffect;
        float duration = Mathf.Max(0.05f, tuning.GetNormalDeathDuration());
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;
        Quaternion originalRotation = transform.rotation;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.rotation = Quaternion.Euler(0, 0, t * 360) * originalRotation;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.one * tuning.normalDeathExpandScale, t);
            yield return null;
            transform.localScale = Vector3.Lerp(Vector3.one * tuning.normalDeathExpandScale, Vector3.zero, t);
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0, 0, 360) * originalRotation;
        transform.localScale = Vector3.zero;
    }

    IEnumerator HeavyDeathCollapse()
    {
        EnemyDeathEffectTuning tuning = GameManager.Instance.BalanceConfig.deathEffect;
        bool isBoss = enemyType == EnemyType.Boss;
        Vector3 originalScale = transform.localScale;
        Quaternion originalRotation = transform.rotation;
        float shakeStrength = isBoss ? tuning.GetBossShakeStrength() : tuning.GetEliteShakeStrength();
        float chargeDuration = Mathf.Max(0.05f, isBoss ? tuning.GetBossChargeDuration() : tuning.GetEliteChargeDuration());
        float collapseDuration = Mathf.Max(0.05f, isBoss ? tuning.GetBossCollapseDuration() : tuning.GetEliteCollapseDuration());

        GameManager.Instance.ShakeMainCamera(
            isBoss ? tuning.GetBossChargeShakeDuration() : tuning.GetEliteChargeShakeDuration(),
            isBoss ? tuning.GetBossChargeShakeStrength() : tuning.GetEliteChargeShakeStrength());

        float elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / chargeDuration;
            float pulse = Mathf.Sin(t * Mathf.PI);
            Vector3 shake = new Vector3(Random.Range(-shakeStrength, shakeStrength), Random.Range(-shakeStrength, shakeStrength), 0f) * (1f - t);
            transform.position += shake * Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * tuning.heavyChargeExpandScale, pulse);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 4f) * 8f) * originalRotation;
            yield return null;
        }

        GameManager.Instance.ShakeMainCamera(
            isBoss ? tuning.GetBossCollapseShakeDuration() : tuning.GetEliteCollapseShakeDuration(),
            isBoss ? tuning.GetBossCollapseShakeStrength() : tuning.GetEliteCollapseShakeStrength());

        elapsed = 0f;
        Vector3 wideScale = new Vector3(originalScale.x * tuning.heavyWideScaleX, originalScale.y * tuning.heavyWideScaleY, originalScale.z);
        while (elapsed < collapseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collapseDuration;
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            Vector3 squash = Vector3.Lerp(wideScale, Vector3.zero, eased);
            squash.y *= Mathf.Lerp(1f, tuning.heavyFinalScaleY, t);
            transform.localScale = squash;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, isBoss ? tuning.bossCollapseRotateAngle : tuning.eliteCollapseRotateAngle, eased)) * originalRotation;

            if (elapsed > collapseDuration * 0.48f && elapsed - Time.deltaTime <= collapseDuration * 0.48f)
            {
                GameManager.Instance.SpwanEnemyAttackPulse(
                    transform.position,
                    new Color(1f, 0.24f, 0.08f, 0.42f),
                    isBoss ? tuning.GetBossDeathPulseScale() : tuning.GetEliteDeathPulseScale(),
                    tuning.GetHeavyDeathPulseDuration());
            }

            yield return null;
        }

        transform.rotation = originalRotation;
        transform.localScale = Vector3.zero;
    }

    // 受击脉冲
    Coroutine hitPunchCoroutine;
    Coroutine bossChargeCoroutine;
    Coroutine bossVulnerableCoroutine;

    public void PlayHitPunch(Vector3 hitDir)
    {
        if (hitPunchCoroutine != null)
            StopCoroutine(hitPunchCoroutine);

        hitPunchCoroutine = StartCoroutine(HitPunch(hitDir));
    }

    IEnumerator HitPunch(Vector3 hitDir)
    {
        Vector3 startPos = transform.position;
        Vector3 punchPos = startPos + hitDir.normalized * 0.28f;

        Vector3 startScale = transform.localScale;
        Vector3 punchScale = startScale * 1.08f;

        float t = 0f;
        while (t < 0.06f)
        {
            t += Time.deltaTime;
            float k = t / 0.06f;
            transform.position = Vector3.Lerp(startPos, punchPos, k);
            transform.localScale = Vector3.Lerp(startScale, punchScale, k);
            yield return null;
        }

        t = 0f;
        while (t < 0.08f)
        {
            t += Time.deltaTime;
            float k = t / 0.08f;
            transform.position = Vector3.Lerp(punchPos, startPos, k);
            transform.localScale = Vector3.Lerp(punchScale, startScale, k);
            yield return null;
        }

        transform.position = startPos;
        transform.localScale = startScale;
    }
}
