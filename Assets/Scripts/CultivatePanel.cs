using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 局外金币永久升级界面
public class CultivatePanel : MonoBehaviour
{
    public List<Button> upgradeButtonsList;
    public Button closeButton;

    Dictionary<string,Button> upgradeButtons = new Dictionary<string, Button>();

    public void Init() {
        upgradeButtons.Add("Attack", upgradeButtonsList[0]);
        upgradeButtons.Add("HP", upgradeButtonsList[1]);
        upgradeButtons.Add("MoveSpeed", upgradeButtonsList[2]);
        upgradeButtons.Add("Crit", upgradeButtonsList[3]);

        closeButton.onClick.AddListener(() =>
        {
            GameManager.Instance.ShowCultivatePanel(false);
        });
        foreach (var item in upgradeButtons)
        {
            // 先显示当前培养数据
            switch (item.Key)
            {
                case "Attack":
                    item.Value.GetComponentInChildren<Text>().text = $"{item.Key.ToUpper()} .{DataManager.myGameData.PermanentAtk.ToString()}";
                    break;
                case "HP":
                    item.Value.GetComponentInChildren<Text>().text = $"{item.Key.ToUpper()} .{DataManager.myGameData.PermanentHp.ToString()}";
                    break;
                case "MoveSpeed":
                    item.Value.GetComponentInChildren<Text>().text = $"{item.Key.ToUpper()} .{DataManager.myGameData.PermanentMoveSpeed.ToString("F1")}";
                    break;
                case "Crit":
                    item.Value.GetComponentInChildren<Text>().text = $"{item.Key.ToUpper()} .{DataManager.myGameData.PermanentCrit.ToString("F1")}";
                    break;
            }

            item.Value.onClick.AddListener(() =>
            {
                switch (item.Key)
                {
                    case "Attack":
                        DataManager.myGameData.PermanentAtk += 1;
                        item.Value.GetComponentInChildren<Text>().text = $"{item.Key.ToUpper()} .{DataManager.myGameData.PermanentAtk.ToString()}";
                        break;
                    case "HP":
                        DataManager.myGameData.PermanentHp += 5;
                        item.Value.GetComponentInChildren<Text>().text = $"{item.Key.ToUpper()} .{DataManager.myGameData.PermanentHp.ToString()}";
                        break;
                    case "MoveSpeed":
                        DataManager.myGameData.PermanentMoveSpeed += 0.1f;
                        item.Value.GetComponentInChildren<Text>().text = $"{item.Key.ToUpper()} .{DataManager.myGameData.PermanentMoveSpeed.ToString("F1")}";
                        break;
                    case "Crit":
                        DataManager.myGameData.PermanentCrit += 0.1f;
                        item.Value.GetComponentInChildren<Text>().text = $"{item.Key.ToUpper()} .{DataManager.myGameData.PermanentCrit.ToString("F1")}";
                        break;
                }

                // 保存修改的培养数据
                GameManager.Instance.SaveGame();
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
        closeButton.onClick.RemoveAllListeners();
        upgradeButtons.Clear();
    }
}
