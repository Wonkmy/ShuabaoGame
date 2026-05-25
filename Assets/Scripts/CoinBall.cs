using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinBall : MonoBehaviour
{
    int _coinValue;
    GameObject _target;
    public void SetCoinValue(int expValue, GameObject target)
    {
        _coinValue = expValue;
        _target = target;
    }

    public void Update()
    {
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }
        float distance = Vector3.Distance(transform.position, _target.transform.position);
        if (distance <= 8)
        {
            transform.position = Vector3.MoveTowards(transform.position, _target.transform.position, 20f * Time.deltaTime);
            if (Vector3.Distance(transform.position, _target.transform.position) < 0.1f)
            {
                GameManager.Instance.RecordCoinCollected(_coinValue);
                DataManager.myGameData.TotalCoinCount += _coinValue;
                Destroy(gameObject);
            }
        }
    }
}
