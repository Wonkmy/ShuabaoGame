using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChooseOnePanel : MonoBehaviour
{
    public List<Button> optionButtons;// 选项按钮列表
    public void Init() { 
        for(int i = 0; i < optionButtons.Count; i++)
        {
            int id = Random.Range(0, DataManager.upgradeList.Count);
            UpgradeData data = DataManager.upgradeList[id];
            optionButtons[i].GetComponentInChildren<Text>().text = data.name;
            optionButtons[i].onClick.AddListener(() => {
                data.action.Invoke();
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