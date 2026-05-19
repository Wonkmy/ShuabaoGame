using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UtilsTool
{
    // 创建一个菜单项，点击后会清除游戏数据
    [MenuItem("Tool/ClearGameData")]
    public static void ClearGameData()
    {
		try
		{
			PlayerPrefs.DeleteAll();
			Debug.Log("游戏数据已清除！");
        }
		catch (System.Exception e)
		{
			Debug.LogError("清除游戏数据时发生错误: " + e.Message);
        }
    }
}
