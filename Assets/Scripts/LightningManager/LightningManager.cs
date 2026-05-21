using System.Collections.Generic;
using UnityEngine;

public class LightningManager : MonoBehaviour
{
    public static LightningManager Instance;

    public AdvancedLightning lightningPrefab;

    // 单条闪电
    public AdvancedLightning lightningEntitiy { get; private set; }

    // 连锁闪电
    public List<AdvancedLightning> lightningList = new List<AdvancedLightning>();

    void Awake()
    {
        Instance = this;
    }

    // =========================
    // 单条闪电
    // =========================

    public void Play(Vector3 start,Vector3 end)
    {
        // 如果之前存在则删除
        if (lightningEntitiy != null)
        {
            Destroy(lightningEntitiy.gameObject);
        }

        lightningEntitiy = Instantiate(lightningPrefab, Vector3.zero, Quaternion.identity);

        lightningEntitiy.Init(start, end);
    }

    public void UpdateSinglePosition(Vector3 newStartPos,Vector3 newEndPos)
    {
        if (lightningEntitiy == null)
            return;

        lightningEntitiy.UpdatePosition(newStartPos, newEndPos);
    }

    // =========================
    // 连锁闪电
    // =========================

    public void PlayChain(
        List<Vector3> points
    )
    {
        if (points == null)
            return;

        if (points.Count < 2)
            return;

        // 清除旧闪电
        ClearChain();

        for (int i = 0; i < points.Count - 1; i++)
        {
            AdvancedLightning lightning =
                Instantiate(
                    lightningPrefab,
                    Vector3.zero,
                    Quaternion.identity
                );

            lightning.Init(
                points[i],
                points[i + 1]
            );

            lightningList.Add(lightning);
        }
    }

    public void UpdateChainPosition(
        List<Vector3> newPoints
    )
    {
        if (newPoints == null)
            return;

        if (newPoints.Count < 2)
            return;

        // 数量不一致直接重新生成
        if (lightningList.Count != newPoints.Count - 1)
        {
            PlayChain(newPoints);
            return;
        }

        for (int i = 0; i < lightningList.Count; i++)
        {
            if (lightningList[i] == null)
                continue;

            lightningList[i].UpdatePosition(newPoints[i], newPoints[i + 1]);
        }
    }

    // =========================
    // 清除
    // =========================

    public void ClearSingle()
    {
        if (lightningEntitiy != null)
        {
            Destroy(lightningEntitiy.gameObject);
        }

        lightningEntitiy = null;
    }

    public void ClearChain()
    {
        for (int i = 0; i < lightningList.Count; i++)
        {
            if (lightningList[i] != null)
            {
                Destroy(lightningList[i].gameObject);
            }
        }

        lightningList.Clear();
    }
}