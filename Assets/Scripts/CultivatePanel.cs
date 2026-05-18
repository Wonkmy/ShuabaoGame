using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 局外金币永久升级界面
public class CultivatePanel : MonoBehaviour
{
    public List<Button> upgradeButtonsList;

    Dictionary<string,Button> upgradeButtons = new Dictionary<string, Button>();

    public void Init() {
        upgradeButtons.Add("Attack", upgradeButtonsList[0]);
        upgradeButtons.Add("HP", upgradeButtonsList[1]);
        upgradeButtons.Add("MoveSpeed", upgradeButtonsList[2]);
        upgradeButtons.Add("Crit", upgradeButtonsList[3]);

        foreach (var item in upgradeButtons)
        {
            item.Value.GetComponentInChildren<Text>().text = $"{item.Key.ToUpper()} .{DataManager.cultivateDict[item.Key]}";
            item.Value.onClick.AddListener(() =>
            {
                switch (item.Key)
                {
                    case "Attack":
                        DataManager.cultivateDict["Attack"]++;
                        break;
                    case "HP":
                        DataManager.cultivateDict["HP"]++;
                        break;
                    case "MoveSpeed":
                        DataManager.cultivateDict["MoveSpeed"]++;
                        break;
                    case "Crit":
                        DataManager.cultivateDict["Crit"]++;
                        break;
                }
            });
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < upgradeButtonsList.Count; i++)
        {
            upgradeButtonsList[i].GetComponentInChildren<Text>().text = "";
            upgradeButtonsList[i].onClick.RemoveAllListeners();
        }
        upgradeButtons.Clear();
    }
}
