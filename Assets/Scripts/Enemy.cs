using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Entity
{
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
    public void SetEnemy(EnemyData enemyData)
    {
        view = GetComponentInChildren<SpriteRenderer>();
        enemyType = enemyData.type;
        moveSpeed = enemyData.moveSpeed;
        transform.localScale = Vector3.one * enemyData.scale;

        totalHp = enemyData.hp;
        currentHp = enemyData.hp;

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



        Damage = enemyData.damage;

        view.sprite = Resources.Load<Sprite>("sprites/" + enemyType.ToString().ToLower());

        FirePos = transform;
        attackType = AttackType.Sector;
        CurrentBulletCount = 3;
        weapon = WeaponSystem.CreateWeapon(enemyData.CurrentWeaponIndex, this);
        weapon.SetWeaponAttackRange(enemyData.attackRange);
        attackRange = enemyData.attackRange;
        if (enemyType == EnemyType.Boss)
        {
            weapon.ChangeFireInterval(0.4f);
            weapon.ChangeBullet(2);
        }

        EntityTag = "enemy";

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
            }
        }

        if (GameManager.Instance.IsBlackHole)
        {
            transform.position =
                Vector3.MoveTowards(transform.position, GameManager.Instance.BlackHolePos, 8f * Time.deltaTime);

            return;
        }
        if (target != null && CanMove && Vector3.Distance(transform.position,target.position) > attackRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
        }
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
            float baseExp;
            switch (enemyType)
            {
                case EnemyType.Normal: baseExp = 2; break;
                case EnemyType.Fast: baseExp = 3; break;
                case EnemyType.Elite: baseExp = 6; break;
                case EnemyType.Thick: baseExp = 8; break;
                case EnemyType.Boss: baseExp = 10; break;
                default: baseExp = 0; break;
            }
            int finalExp = (int)(baseExp * (isCrit ? 1.25f : 1f));
            GameManager.Instance.SpwanExpBall(transform.position, Mathf.FloorToInt(finalExp));
            // 旋转缩小然后死亡
            StartCoroutine(DeathEffect());
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
        GameManager.Instance.player.GetComponent<Player>().AddKilledCount();
        if(enemyType == EnemyType.Thick)
        {
            GameManager.Instance.player.GetComponent<Player>().AddHP(20);// 击杀厚皮怪物回复20点生命值
        }
        if (IsSpecialEnemy)
        {
            GameManager.Instance.IsSpecialEvent = false;// 结束特殊事件
            GameManager.Instance.player.GetComponent<Player>().ResetWeaponAttackRange();
        }

        // 如果是精英怪或血厚怪，生成一个加血道具
        if (enemyType == EnemyType.Elite || enemyType == EnemyType.Thick)
        {
            float r = Random.Range(0f, 1f);
            if(r < 0.233f)
            {
                GameObject newAddHp = Instantiate(Resources.Load<GameObject>("add_hp"), transform.position, Quaternion.identity);
                newAddHp.GetComponent<AddHP>().SetAddHP(10, GameManager.Instance.player, true);
            }
        }

        Instantiate(Resources.Load<GameObject>("deadFX"), transform.position, Quaternion.identity);

        DataManager.allEnemyDict.Remove(gameObject);// 从敌人字典中移除
        Destroy(gameObject);
    }
}
