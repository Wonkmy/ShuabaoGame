using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPool
{
    void Prewarm(string prefabId, int count);
    GameObject Get(string prefabId);
    void Release(string prefabId, GameObject obj);
}
