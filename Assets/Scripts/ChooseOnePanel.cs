using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChooseOnePanel : MonoBehaviour,IDisposable
{
    // 选项按钮列表
    public List<Button> optionButtons;
    public Sprite white;
    public Sprite green;
    public Sprite blue;
    public Sprite purple;

    public void Dispose()
    {
        for (int i = 0; i < optionButtons.Count; i++)
        {
            optionButtons[i].GetComponentInChildren<Text>().text = "";
            optionButtons[i].onClick.RemoveAllListeners();
        }
    }

    public void Init()
    {
        List<UpgradeData> options = GameManager.Instance.GetUpgradeOptions(3);

        for (int i = 0; i < optionButtons.Count; i++)
        {
            optionButtons[i].onClick.RemoveAllListeners();

            UpgradeData data = options[i];

            optionButtons[i].GetComponent<Image>().sprite = data.rarity switch
            {
                "normal" => white,
                "rare" => green,
                "epic" => blue,
                "legendary" => purple,
                _ => white
            };
            optionButtons[i].GetComponentInChildren<Text>().text = data.name;

            optionButtons[i].onClick.AddListener(() =>
            {
                // 执行升级
                GameManager.Instance.ApplyUpgrade(data);

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
