using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        float distance = Vector3.Distance(transform.position, _target.transform.position);
        if (distance <= 3)
        {
            float t = 1f - Mathf.Clamp01(distance / 3f);
            float speed = Mathf.Lerp(8f, 32f, t);
            transform.position = Vector3.MoveTowards(transform.position, _target.transform.position, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, _target.transform.position) < 1f)
            {
                if (_isFilledHp)
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
}
