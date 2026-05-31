using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpBallPool : MonoBehaviour, IPool
{
    public static ExpBallPool Instance;
    private Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();
    public Transform expBallRoot;
    public GameObject Get(string expBallId)
    {
        if (!poolDict.ContainsKey(expBallId))
        {
            poolDict.Add(expBallId, new Queue<GameObject>());
        }

        Queue<GameObject> pool = poolDict[expBallId];

        GameObject obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(Resources.Load<GameObject>("expBall"));
            obj.name = expBallId;
            obj.transform.SetParent(expBallRoot);
        }

        return obj;
    }

    public void Prewarm(string expBallId, int count)
    {
        if (!poolDict.ContainsKey(expBallId))
        {
            poolDict.Add(expBallId, new Queue<GameObject>());
        }

        Queue<GameObject> pool = poolDict[expBallId];

        for (int i = 0; i < count; i++)
        {
            GameObject newExpBall = Instantiate(Resources.Load<GameObject>("expBall"));
            newExpBall.name = expBallId;
            newExpBall.transform.SetParent(expBallRoot);
            newExpBall.SetActive(false);

            pool.Enqueue(newExpBall);
        }
    }

    public void Release(string expBallId, GameObject obj)
    {
        if (!poolDict.ContainsKey(expBallId))
        {
            poolDict.Add(expBallId, new Queue<GameObject>());
        }
        obj.transform.localScale = Vector3.one * 0.26f;// 重置经验球的缩放
        obj.SetActive(false);
        poolDict[expBallId].Enqueue(obj);
    }

    private void Awake()
    {
        Instance = this;
    }
}
