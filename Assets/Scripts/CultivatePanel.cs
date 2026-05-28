using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 局外金币永久升级界面
public class CultivatePanel : MonoBehaviour,IDisposable
{
    public List<Button> upgradeButtonsList;
    public List<Button> playerTypeChooses;
    public Button closeButton;
    public Text goldNumLabel;

    Dictionary<string,Button> upgradeButtons = new Dictionary<string, Button>();
    Dictionary<AirplaneType,Button> playerTypeChooseButtons = new Dictionary<AirplaneType, Button>();

    public void Init() {
        closeButton.onClick.RemoveAllListeners();

        upgradeButtons["Attack"] = upgradeButtonsList[0];
        upgradeButtons["HP"] = upgradeButtonsList[1];
        upgradeButtons["MoveSpeed"] = upgradeButtonsList[2];
        upgradeButtons["Crit"] = upgradeButtonsList[3];

        goldNumLabel.text = "X " + DataManager.myGameData.TotalCoinCount.ToString();

        for (int i = 0; i < playerTypeChooses.Count; i++)
        {
            AirplaneType type = (AirplaneType)i;
            playerTypeChooseButtons[type] = playerTypeChooses[i];
            playerTypeChooses[i].transform.Find("icon").GetComponent<Image>().sprite = Resources.Load<Sprite>($"sprites/PlayerTypeIcon/{i}");
            playerTypeChooses[i].transform.Find("name").GetComponent<Text>().text = DataManager.playerSkillTypeCDDict[type].name;
        }

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
                if(DataManager.myGameData.TotalCoinCount <= 0)
                {
                    Debug.Log("金币不足");
                    return;
                }
                switch (item.Key)
                {
                    case "Attack":
                        if(DataManager.myGameData.TotalCoinCount < 10)
                        {
                            Debug.Log("金币不足");
                            return;
                        }
                        DataManager.myGameData.PermanentAtk += 1;
                        DataManager.myGameData.TotalCoinCount -= 10;
                        item.Value.GetComponentInChildren<Text>().text = $"{item.Key.ToUpper()} .{DataManager.myGameData.PermanentAtk.ToString()}";
                        break;
                    case "HP":
                        if(DataManager.myGameData.TotalCoinCount < 20)
                        {
                                Debug.Log("金币不足");
                                return;
                        }
                        DataManager.myGameData.PermanentHp += 5;
                        DataManager.myGameData.TotalCoinCount -= 20;
                        item.Value.GetComponentInChildren<Text>().text = $"{item.Key.ToUpper()} .{DataManager.myGameData.PermanentHp.ToString()}";
                        break;
                    case "MoveSpeed":
                        if(DataManager.myGameData.TotalCoinCount < 15)
                        {
                            Debug.Log("金币不足");
                            return;
                        }
                        DataManager.myGameData.PermanentMoveSpeed += 0.1f;
                        DataManager.myGameData.TotalCoinCount -= 15;
                        item.Value.GetComponentInChildren<Text>().text = $"{item.Key.ToUpper()} .{DataManager.myGameData.PermanentMoveSpeed.ToString("F1")}";
                        break;
                    case "Crit":
                        if(DataManager.myGameData.TotalCoinCount < 25)
                        {
                            Debug.Log("金币不足");
                            return;
                        }
                        DataManager.myGameData.PermanentCrit += 0.1f;
                        DataManager.myGameData.TotalCoinCount -= 25;
                        item.Value.GetComponentInChildren<Text>().text = $"{item.Key.ToUpper()} .{DataManager.myGameData.PermanentCrit.ToString("F1")}";
                        break;
                }

                // 保存修改的培养数据
                GameManager.Instance.SaveGame();
            });   
        }

        foreach (var item in playerTypeChooseButtons)
        {
            item.Value.interactable = !(item.Key == DataManager.myGameData.playerType);
            item.Value.transform.Find("check").gameObject.SetActive(item.Key == DataManager.myGameData.playerType);
            item.Value.onClick.AddListener(() =>
            {
                item.Value.interactable = false;
                item.Value.transform.Find("check").gameObject.SetActive(true);
                DataManager.myGameData.playerType = item.Key;

                ResetOtherplayerTypeChooseButtons();
                // 保存修改的玩家类型
                GameManager.Instance.SaveGame();
            });
        }
    }
    void ResetOtherplayerTypeChooseButtons() {
        foreach (var item in playerTypeChooseButtons) { 
            if(item.Key != DataManager.myGameData.playerType)
            {
                item.Value.interactable = true;
                item.Value.transform.Find("check").gameObject.SetActive(!item.Value.interactable);
            }
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
        playerTypeChooseButtons.Clear();
    }

    public void Dispose()
    {
        for (int i = 0; i < upgradeButtonsList.Count; i++)
        {
            upgradeButtonsList[i].GetComponentInChildren<Text>().text = "";
            upgradeButtonsList[i].onClick.RemoveAllListeners();
        }
        closeButton.onClick.RemoveAllListeners();
        upgradeButtons.Clear();
        playerTypeChooseButtons.Clear();
    }
}
