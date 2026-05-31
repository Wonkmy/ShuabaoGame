using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour,IPool
{
    public static BulletPool Instance;
    private Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();
    public Transform bulletRoot;

    private void Awake()
    {
        Instance = this;
    }
    public void Prewarm(string bulletPrefabId, int count)
    {
        if (!poolDict.ContainsKey(bulletPrefabId))
        {
            poolDict.Add(bulletPrefabId,new Queue<GameObject>());
        }

        Queue<GameObject> pool = poolDict[bulletPrefabId];

        for (int i = 0; i < count; i++)
        {
            GameObject obj =Instantiate(Resources.Load<GameObject>("bullets/" + bulletPrefabId));
            obj.name = bulletPrefabId;
            obj.transform.SetParent(bulletRoot);
            obj.SetActive(false);

            pool.Enqueue(obj);
        }
    }
    public GameObject Get(string bulletPrefabId)
    {
        if (!poolDict.ContainsKey(bulletPrefabId))
        {
            poolDict.Add(bulletPrefabId, new Queue<GameObject>());
        }

        Queue<GameObject> pool = poolDict[bulletPrefabId];

        GameObject obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(Resources.Load<GameObject>("bullets/" + bulletPrefabId));
            obj.transform.SetParent(bulletRoot);
        }

        return obj;
    }

    public void Release(string bulletPrefabId, GameObject obj)
    {
        if (!poolDict.ContainsKey(bulletPrefabId))
        {
            poolDict.Add(bulletPrefabId, new Queue<GameObject>());
        }
        obj.GetComponent<Bullet>().SetFxScaleToDefalut();
        obj.SetActive(false);
        poolDict[bulletPrefabId].Enqueue(obj);
    }
}