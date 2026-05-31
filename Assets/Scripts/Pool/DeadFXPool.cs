using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadFXPool : MonoBehaviour, IPool
{
    public static DeadFXPool Instance;
    private Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();
    public Transform fxRoot;

    private void Awake()
    {
        Instance = this;
    }
    public GameObject Get(string fxId)
    {
        if (!poolDict.ContainsKey(fxId))
        {
            poolDict.Add(fxId, new Queue<GameObject>());
        }

        Queue<GameObject> pool = poolDict[fxId];

        GameObject obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(Resources.Load<GameObject>(fxId));
            obj.name = fxId;
            obj.GetComponent<DeadFX>().SetFXId(fxId);
            obj.transform.SetParent(fxRoot);
            DataManager.allDeadFx.Add(obj);
        }

        return obj;
    }

    public void Prewarm(string fxId, int count)
    {
        if (!poolDict.ContainsKey(fxId))
        {
            poolDict.Add(fxId, new Queue<GameObject>());
        }

        Queue<GameObject> pool = poolDict[fxId];

        for (int i = 0; i < count; i++)
        {
            GameObject newFX = Instantiate(Resources.Load<GameObject>(fxId));
            newFX.name = fxId;
            newFX.GetComponent<DeadFX>().SetFXId(fxId);
            newFX.transform.SetParent(fxRoot);
            newFX.SetActive(false);

            pool.Enqueue(newFX);
        }
    }

    public void Release(string fxId, GameObject obj)
    {
        if (!poolDict.ContainsKey(fxId))
        {
            poolDict.Add(fxId, new Queue<GameObject>());
        }
        obj.SetActive(false);
        poolDict[fxId].Enqueue(obj);
    }
}
