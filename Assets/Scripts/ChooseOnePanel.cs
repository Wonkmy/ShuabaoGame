using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChooseOnePanel : MonoBehaviour
{
    // 选项按钮列表
    public List<Button> optionButtons;
    public void Init()
    {
        Player player = GameManager.Instance.player.GetComponent<Player>();

        // 临时升级池
        List<UpgradeData> tempList = new List<UpgradeData>();

        // 原始升级池
        tempList.AddRange(DataManager.upgradeList);

        // =========================
        // 传奇词条条件过滤
        // =========================
        for (int i = tempList.Count - 1; i >= 0; i--)
        {
            UpgradeData data = tempList[i];

            // 暴击爆炸
            if (data.name == "暴击爆炸")
            {
                if (!player.buildDict.ContainsKey("crit") ||
                    player.buildDict["crit"] < 5)
                {
                    tempList.RemoveAt(i);
                    continue;
                }
            }

            // 穿透爆炸
            if (data.name == "穿透爆炸")
            {
                if (!player.buildDict.ContainsKey("pierce") ||
                    player.buildDict["pierce"] < 5)
                {
                    tempList.RemoveAt(i);
                    continue;
                }
            }

            // 子弹风暴
            if (data.name == "子弹风暴")
            {
                if (!player.buildDict.ContainsKey("bullet") ||
                    player.buildDict["bullet"] < 8)
                {
                    tempList.RemoveAt(i);
                    continue;
                }
            }
        }

        // 根据当前流派增加权重
        for (int i = 0; i < DataManager.upgradeList.Count; i++)
        {
            UpgradeData data = DataManager.upgradeList[i];

            // 如果玩家已经拥有这个流派
            if (player.buildDict.ContainsKey(data.tag))
            {
                // 根据层数增加额外权重
                int count = player.buildDict[data.tag];

                for (int j = 0; j < count; j++)
                {
                    tempList.Add(data);
                }
            }
        }

        for (int i = 0; i < optionButtons.Count; i++)
        {
            optionButtons[i].onClick.RemoveAllListeners();

            int id = Random.Range(0, tempList.Count);

            UpgradeData data = tempList[id];

            // 防止重复出现
            tempList.Remove(data);

            optionButtons[i].GetComponentInChildren<Text>().text = data.name;

            optionButtons[i].onClick.AddListener(() =>
            {
                // 执行升级
                data.action.Invoke();

                // 增加流派层数
                if (!player.buildDict.ContainsKey(data.tag))
                {
                    player.buildDict.Add(data.tag, 0);
                }

                player.buildDict[data.tag]++;

                GameManager.Instance.ShowLevelUpPanel(false);
            });
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            optionButtons[i].GetComponentInChildren<Text>().text = "";
            optionButtons[i].onClick.RemoveAllListeners();
        }
    }
}