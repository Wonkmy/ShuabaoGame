using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public Transform p1;
    public Transform p2;

    public float cannonViewer = 60;// 炮口可视角度范围
    void Update()
    {
        Vector2 dir = p1.up;// 加农炮的朝向
        Vector2 toTarget = (p2.position - p1.position).normalized;

        // 计算炮口可视角度范围的一半的余弦值
        float cosViwerHalf = Mathf.Cos(cannonViewer * 0.5f * Mathf.Deg2Rad);

        float _dot = Vector2.Dot(dir, toTarget);
        if (_dot > cosViwerHalf)// 如果位于加农炮前方(dir)60度范围内
        {
            Debug.Log("检测到敌人");
        }
    }
}
