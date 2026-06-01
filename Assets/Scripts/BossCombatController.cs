using System.Collections;
using UnityEngine;

public class BossCombatController : MonoBehaviour
{
    Enemy boss;
    int currentPhase = 1;
    bool initialized;
    bool isFinalBoss;
    bool isMiniBoss;
    bool miniBossEnraged;
    float miniBossPressureTimer;

    public void Init(Enemy enemy)
    {
        Init(enemy, true, false);
    }

    public void Init(Enemy enemy, bool finalBoss, bool miniBoss)
    {
        boss = enemy;
        isFinalBoss = finalBoss;
        isMiniBoss = miniBoss;
        initialized = true;
        currentPhase = 1;

        if (isFinalBoss)
        {
            boss.ApplyBossPhase(currentPhase);
            GameManager.Instance.BeginFinalBossAtmosphere();
            GameManager.Instance.SetFinalBossPhaseAtmosphere(currentPhase);
        }

        BossCombatTuning tuning = GameManager.Instance.BalanceConfig.bossCombat;
        miniBossPressureTimer = tuning.miniBossPressurePulseInterval;
        string startText = isFinalBoss ? "最终Boss战开始" : (boss.enemyType == EnemyType.Boss ? "Boss进入战场" : "小Boss进入战场");
        GameObject warning = GameManager.Instance.SpwanWorldTxt(startText, isFinalBoss ? 1.2f : 1.0f);
        GameManager.Instance.StartRuntimeCoroutine(GameManager.Instance.ShowFlashWarningTxt(warning));
    }

    void Update()
    {
        if (!initialized || boss == null || boss.Dead)
            return;

        BossCombatTuning tuning = GameManager.Instance.BalanceConfig.bossCombat;
        float hp = boss.GetHpProgress();

        if (isMiniBoss)
        {
            UpdateMiniBoss(tuning, hp);
            return;
        }

        if (!isFinalBoss)
            return;

        if (currentPhase == 1 && hp <= tuning.phase2HpPercent)
        {
            EnterPhase(2, "Boss进入第二阶段");
        }
        else if (currentPhase == 2 && hp <= tuning.phase3HpPercent)
        {
            EnterPhase(3, "Boss狂暴！");
        }
    }

    void UpdateMiniBoss(BossCombatTuning tuning, float hp)
    {
        miniBossPressureTimer -= Time.deltaTime;
        if (miniBossPressureTimer <= 0f)
        {
            miniBossPressureTimer = tuning.miniBossPressurePulseInterval;
            GameManager.Instance.ShakeMainCamera(tuning.miniBossPressurePulseShakeDuration, tuning.miniBossPressurePulseShakeStrength);
            boss.PlayCombatWeightPulse(new Color(1f, 0.38f, 0.08f, 0.34f), 2.4f, 0.28f);
        }

        if (!miniBossEnraged && hp <= tuning.miniBossEnrageHpPercent)
        {
            miniBossEnraged = true;
            boss.ApplyMiniBossEnrage();
            GameManager.Instance.ShakeMainCamera(tuning.phaseChangeShakeDuration * 0.65f, tuning.phase2ShakeStrength * 0.75f);
            GameObject warning = GameManager.Instance.SpwanWorldTxt("小Boss爆发", 1.05f);
            GameManager.Instance.StartRuntimeCoroutine(GameManager.Instance.ShowFlashWarningTxt(warning));
        }
    }

    void EnterPhase(int phase, string text)
    {
        StartCoroutine(EnterPhaseRoutine(phase, text));
    }

    IEnumerator EnterPhaseRoutine(int phase, string text)
    {
        initialized = false;
        BossCombatTuning tuning = GameManager.Instance.BalanceConfig.bossCombat;
        float shakeStrength = phase == 2 ? tuning.phase2ShakeStrength : tuning.phase3ShakeStrength;
        float titleSize = phase == 2 ? tuning.phase2TitleSize : tuning.phase3TitleSize;
        GameManager.Instance.ShakeMainCamera(tuning.phaseChangeShakeDuration, shakeStrength);
        boss.StartBossPhaseBreak(tuning.phaseBreakDuration, phase);
        GameObject warning = GameManager.Instance.SpwanWorldTxt(text, titleSize);
        GameManager.Instance.StartRuntimeCoroutine(GameManager.Instance.ShowFlashWarningTxt(warning));
        yield return new WaitForSeconds(tuning.phaseBreakDuration);
        yield return null;

        currentPhase = phase;
        boss.ApplyBossPhase(currentPhase);
        GameManager.Instance.SetFinalBossPhaseAtmosphere(currentPhase);
        initialized = true;
    }
}
