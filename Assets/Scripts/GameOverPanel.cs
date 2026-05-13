using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameOverPanel : MonoBehaviour
{
    private Text infoText;
    private Button restartGameBtn;
    private Button revivalBtn;
    public void Init(float gameTime,int killCount,float difficulty, int pLevel)
    {
        infoText = transform.Find("Container/list/info").GetComponent<Text>();

        string timeStr = $"游戏时间：{gameTime.ToString("F2")}秒\n";
        string killCountStr = $"击杀数：{killCount}\n";
        string difficultyStr = $"难度系数：{difficulty}\n";
        string pLevelStr = $"玩家等级：{pLevel}\n";

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(timeStr + "\n");
        stringBuilder.Append(killCountStr + "\n");
        stringBuilder.Append(difficultyStr + "\n");
        stringBuilder.Append(pLevelStr);

        infoText.text = stringBuilder.ToString();   

        restartGameBtn = transform.Find("Container/list/Option1Btn").GetComponent<Button>();
        revivalBtn = transform.Find("Container/list/Option2Btn").GetComponent<Button>();

        restartGameBtn.onClick.AddListener(() =>
        {
            GameManager.Instance.RestartGame();
            GameManager.Instance.ShowGameOverPanel(false);
        });

        revivalBtn.onClick.AddListener(() =>
        {
            GameManager.Instance.Revival();
            GameManager.Instance.ShowGameOverPanel(false);
        });
    }
}
