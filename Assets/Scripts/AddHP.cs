using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddHP : MonoBehaviour
{
    GameObject _target;
    Player _player;
    int _AddHpValue;
    bool _isFilledHp;
    public void SetAddHP(int addValue, GameObject target, bool isFilledHp = false)
    {
        _isFilledHp = isFilledHp;
        _target = target;
        _player = _target.GetComponent<Player>();
        if (!isFilledHp)
        {
            _AddHpValue = addValue;
        }
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
            if(_isFilledHp)
            {
                _player.FilledTotalHp();
            }
            else
            {
                _player.AddHP(_AddHpValue);
            }

            DataManager.allExpBall.Remove(gameObject);
            Destroy(gameObject);
        }
    }
}
