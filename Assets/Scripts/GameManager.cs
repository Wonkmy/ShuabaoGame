using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject player;
    float spwanTime;
    float spwanInterval = 0.5f;

    private Camera mainCamera;

    // 敌人生成到屏幕外的偏移距离
    private float offset = 100f;
    void Start()
    {
        DataManager.Init();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        spwanTime += Time.deltaTime;
        if (spwanTime > spwanInterval) {
            spwanTime = 0;
            GenEnemy();
        }
    }

    void GenEnemy()
    {
        GameObject newEnemy = Instantiate(Resources.Load<GameObject>("enemy"));
        newEnemy.GetComponent<Enemy>().SetEnemy(DataManager.enemyDataDict[0]);
        newEnemy.GetComponent<Enemy>().target = player.transform;
        float x = 0;
        float y = 0;

        // 0 左 1 右 2 下 3 上
        int side = Random.Range(0, 4);

        switch (side)
        {
            case 0:
                x = -offset;
                y = Random.Range(0, Screen.height);
                break;

            case 1:
                x = Screen.width + offset;
                y = Random.Range(0, Screen.height);
                break;

            case 2:
                x = Random.Range(0, Screen.width);
                y = -offset;
                break;

            case 3:
                x = Random.Range(0, Screen.width);
                y = Screen.height + offset;
                break;
        }

        Vector3 wpos = GetWorldPosByScreenPos(new Vector3(x, y, 0));

        // 保持敌人在2D世界层级
        wpos.z = 0;

        newEnemy.transform.position = wpos;

        DataManager.allEnemyDict.Add(newEnemy);
    }

    Vector3 GetWorldPosByScreenPos(Vector3 screenPos)
    {
        screenPos.z = 0;
        return mainCamera.ScreenToWorldPoint(screenPos);
    }

    private void OnDisable()
    {
        DataManager.Clear();
    }

    private void OnDestroy()
    {
        DataManager.Clear();
    }

    private void OnApplicationQuit()
    {
        DataManager.Clear();
    }
}
