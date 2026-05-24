using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    float attackRange = 0f;// 攻击范围，也就是敌人停止移动开始攻击的距离
    float findTargetRange = 10f;// 寻找目标的范围
    public void SetEnemy(EnemyData enemyData)
    {
        view = GetComponentInChildren<SpriteRenderer>();
        enemyType = enemyData.type;
        moveSpeed = enemyData.moveSpeed;
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
        weapon = WeaponSystem.CreateWeapon(enemyData.CurrentWeaponIndex, this);
        attackRange = weapon.attackRange;
        if (enemyType == EnemyType.Boss)
        {
            weapon.ChangeFireInterval(0.4f);
            weapon.ChangeBullet(2);
            attackRange += 5f;// Boss的攻击范围更大一些
            findTargetRange += 3f;// Boss的寻找目标范围更大一些
        }


        CanMove = true;
        Dead = false;
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
                HasEnterScreen = true;
                IsBattleActive = true;
                // 如果是boss进场，则时间放慢为0.25倍速，增加紧张感。0.2秒钟之后恢复正常速度
                if (enemyType == EnemyType.Boss)
                {
                    Time.timeScale = 0.25f;
                    GameManager.Instance.StartCoroutine(ResetTimeScale());
                }
            }
        }

        if (GameManager.Instance.IsBlackHole)
        {
            transform.position = Vector3.MoveTowards(transform.position, GameManager.Instance.BlackHolePos, 8f * Time.deltaTime);

            return;
        }
        if (target != null && CanMove && Vector3.Distance(transform.position,target.position) > findTargetRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
        }
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
        return target.GetComponent<Entity>();
    }

    public override void TakeDamage(int damage, bool isCrit)
    {
        currentHp -= damage;

        GameManager.Instance.SpwanHitFx(transform.position);//  命中特效

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
            for (int i = 0; i < 2; i++)
            {
                float angle = i * (360f / 2);
                Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * 0.55f;
                Vector3 randomOffset = new Vector3(Random.Range(-0.12f, 0.2f), Random.Range(-0.2f, 0.35f), 0);
                GameManager.Instance.SpwanCoin(transform.position + offset + randomOffset, 1);
            }
        }
        else
        {
            if (enemyType != EnemyType.Elite && enemyType != EnemyType.Boss) return;
            int spwanType = Random.Range(0, 2);
            if (spwanType == 0)
            {
                GameManager.Instance.SpwanChest(transform.position);
            }
            else if (spwanType == 1)
            {
                int baseCoinCount = enemyType == EnemyType.Elite ? 5 : 8;// 精英怪生成2个金币，Boss生成5个金币
                int baseCoinValue = enemyType == EnemyType.Elite ? 2 : 4;// 精英怪生成的金币价值1，Boss生成的金币价值2
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
    private void SpwanExpBall(bool isCrit)
    {
        float baseExp;
        switch (enemyType)
        {
            case EnemyType.Normal: baseExp = 2.3f; break;
            case EnemyType.Fast: baseExp = 3.5f; break;
            case EnemyType.Elite: baseExp = 3.8f; break;
            case EnemyType.Thick: baseExp = 2.5f; break;
            case EnemyType.Boss: baseExp = 6.5f; break;
            default: baseExp = 0; break;
        }
        float finalExp = (baseExp * (isCrit ? 1.25f : 1f));
        if (enemyType == EnemyType.Elite)
        {
            // 如果是精英怪，生成大量经验球。这里默认生成8个，分散在敌人周围
            int eliteExpCount = 12;
            for (int i = 0; i < eliteExpCount; i++)
            {
                float angle = i * (360f / eliteExpCount);
                Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * 1.5f;
                Vector3 randomOffset = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0);
                GameManager.Instance.SpwanExpBall(transform.position + offset + randomOffset, enemyType, Mathf.FloorToInt(finalExp));
            }
        }
        else if (enemyType == EnemyType.Thick) {
            int thickExpCount = 15;
            for (int i = 0; i < thickExpCount; i++)
            {
                float angle = i * (360f / thickExpCount);
                Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * 1.85f;
                Vector3 randomOffset = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f), 0);
                GameManager.Instance.SpwanExpBall(transform.position + offset + randomOffset, enemyType, Mathf.FloorToInt(finalExp));
            }
        }
        else if (enemyType == EnemyType.Boss)
        {
            int bossExpCount = 18;
            for (int i = 0; i < bossExpCount; i++)
            {
                float angle = i * (360f / bossExpCount);
                Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * 2.35f;
                Vector3 randomOffset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0);
                GameManager.Instance.SpwanExpBall(transform.position + offset + randomOffset, enemyType, Mathf.FloorToInt(finalExp));
            }
        }
        else
        {
            GameManager.Instance.SpwanExpBall(transform.position, enemyType, Mathf.FloorToInt(finalExp));
        }
    }

    IEnumerator ResetColor()
    {
        yield return new WaitForSeconds(0.1f);
        GetComponentInChildren<SpriteRenderer>().color = Color.white;
    }

    IEnumerator DeathEffect()
    {
        WeaponSystem.RemoveWeapon(weapon);// 先移除武器，避免在销毁敌人后还调用武器的Update方法

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
            transform.localScale = Vector3.Lerp(originalScale, Vector3.one * 1.25f, t);
            yield return null;
            // 再恢复，有一种膨胀后爆炸的感觉
            transform.localScale = Vector3.Lerp(Vector3.one * 1.25f, Vector3.zero, t);
            yield return null;
        }
        // 确保最终状态
        transform.rotation = Quaternion.Euler(0, 0, 360) * originalRotation;
        transform.localScale = Vector3.zero;

        // 增加击杀统计
        GameManager.Instance.GetPlayer().AddKilledCount();
        
        if (IsSpecialEnemy)
        {
            GameManager.Instance.IsSpecialEvent = false;// 结束特殊事件
            // 特殊事件结束后，重新计算下一次特殊事件间隔
            GameManager.Instance.nextSpecialEventInterval = GameManager.Instance.CalculateDynamicSpecialEventInterval();
            //GameManager.Instance.player.GetComponent<Player>().ResetWeaponAttackRange();// 重置玩家的武器攻击范围
            GameManager.Instance.cameraEffect.darkIntensity = 0.0f;
        }

        // 如果是精英怪或血厚怪，生成一个加血道具
        if (enemyType == EnemyType.Elite || enemyType == EnemyType.Thick)
        {
            float r = Random.Range(0f, 1f);
            if(r < 0.333f)
            {
                GameObject newAddHp = Instantiate(Resources.Load<GameObject>("add_hp"), transform.position, Quaternion.identity);
                newAddHp.GetComponent<AddHP>().SetAddHP(10, GameManager.Instance.player, true);
            }
        }

        Instantiate(Resources.Load<GameObject>("deadFX"), transform.position, Quaternion.identity);

        DataManager.allEnemyDict.Remove(gameObject);// 从敌人字典中移除
        if (GameManager.Instance.GetPlayer().chainedTargets.Contains(gameObject))
        {
            GameManager.Instance.GetPlayer().chainedTargets.Remove(gameObject);// 从玩家的连锁目标列表中移除
        }
        Destroy(gameObject);
    }

    // 受击脉冲
    Coroutine hitPunchCoroutine;

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
