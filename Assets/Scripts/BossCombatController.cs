using UnityEngine;

public class BossCombatController : MonoBehaviour
{
    Enemy boss;
    int currentPhase = 1;
    bool initialized;

    public void Init(Enemy enemy)
    {
        boss = enemy;
        initialized = true;
        currentPhase = 1;
        boss.ApplyBossPhase(currentPhase);
        GameManager.Instance.BeginFinalBossAtmosphere();
        GameManager.Instance.SetFinalBossPhaseAtmosphere(currentPhase);
        GameObject warning = GameManager.Instance.SpwanWorldTxt("最终Boss战开始", 1.2f);
        GameManager.Instance.StartRuntimeCoroutine(GameManager.Instance.ShowFlashWarningTxt(warning));
    }

    void Update()
    {
        if (!initialized || boss == null || boss.Dead)
            return;

        float hp = boss.GetHpProgress();
        BossCombatTuning tuning = GameManager.Instance.BalanceConfig.bossCombat;
        if (currentPhase == 1 && hp <= tuning.phase2HpPercent)
        {
            EnterPhase(2, "Boss进入第二阶段");
        }
        else if (currentPhase == 2 && hp <= tuning.phase3HpPercent)
        {
            EnterPhase(3, "Boss狂暴！");
        }
    }

    void EnterPhase(int phase, string text)
    {
        currentPhase = phase;
        boss.ApplyBossPhase(currentPhase);
        GameManager.Instance.SetFinalBossPhaseAtmosphere(currentPhase);
        BossCombatTuning tuning = GameManager.Instance.BalanceConfig.bossCombat;
        float shakeStrength = phase == 2 ? tuning.phase2ShakeStrength : tuning.phase3ShakeStrength;
        float titleSize = phase == 2 ? tuning.phase2TitleSize : tuning.phase3TitleSize;
        GameManager.Instance.ShakeMainCamera(tuning.phaseChangeShakeDuration, shakeStrength);
        GameObject warning = GameManager.Instance.SpwanWorldTxt(text, titleSize);
        GameManager.Instance.StartRuntimeCoroutine(GameManager.Instance.ShowFlashWarningTxt(warning));
    }
}
