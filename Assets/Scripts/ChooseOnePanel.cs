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
        Player player =
        GameManager.Instance.player.GetComponent<Player>();

        List<UpgradeData> options = GameManager.Instance.GetUpgradeOptions(3);

        for (int i = 0; i < optionButtons.Count; i++)
        {
            optionButtons[i].onClick.RemoveAllListeners();

            UpgradeData data = options[i];

            optionButtons[i].GetComponent<Image>().color = data.rarity switch
            {
                "normal" => Color.white,
                "rare" => Color.green,
                "epic" => Color.blue,
                "legendary" => new Color(1.0f, 0.0f, 1.0f),// 紫色
                _ => Color.white
            };
            optionButtons[i].GetComponentInChildren<Text>().text = data.name;

            optionButtons[i].onClick.AddListener(() =>
            {
                // 执行升级
                GameManager.Instance.ExecuteUpgrade(data);

                // 增加流派层数
                if (!player.buildDict.ContainsKey(data.tag))
                {
                    player.buildDict.Add(data.tag, 0);
                }

                player.buildDict[data.tag]++;

                // 检查流派
                player.CheckBuildCombo();

                // 关闭界面
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