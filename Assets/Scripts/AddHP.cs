using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddHP : MonoBehaviour
{
    GameObject _target;
        int _AddHpValue;
    public void SetAddHP(int addValue, GameObject target)
    {
        _AddHpValue = addValue;
        _target = target;
    }

    private void Update()
    {
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }
        if (Vector3.Distance(transform.position, _target.transform.position) < 1f)
        {
            GameManager.Instance.player.GetComponent<Player>().AddHP(_AddHpValue);
            DataManager.allExpBall.Remove(gameObject);
            Destroy(gameObject);
        }
    }
}
