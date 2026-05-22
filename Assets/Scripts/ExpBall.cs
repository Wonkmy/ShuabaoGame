using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpBall : MonoBehaviour
{
    int _expValue;
    GameObject _target;
    public void SetExpValue(int expValue,GameObject target)
    {
        _expValue = expValue;
        _target = target;
    }

    public void ExpBallUpdate()
    {
        if (_target == null)
        {
            DataManager.allExpBall.Remove(gameObject);
            Destroy(gameObject);
            return;
        }
        float distance = Vector3.Distance(transform.position, _target.transform.position);
        if (distance <= 6)
        {
            //transform.position = Vector3.MoveTowards(transform.position, _target.transform.position, 20f * Time.deltaTime);
            transform.position += (_target.transform.position - transform.position).normalized * 20f * Time.deltaTime;
            if (Vector3.Distance(transform.position, _target.transform.position) < 0.1f)
            {
                GameManager.Instance.player.GetComponent<Player>().AddExp(_expValue);
                DataManager.allExpBall.Remove(gameObject);
                Destroy(gameObject);
            }
        }
    }
}
