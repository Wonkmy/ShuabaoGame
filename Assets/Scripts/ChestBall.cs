using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 宝箱物体，只有一个功能：如果没有打开三选一界面，则打开三选一界面，之后销毁自己。如果已经是三选一界面，则等待玩家选择完上一个三选一界面后，再次打开三选一界面，之后销毁自己。
/// </summary>
public class ChestBall : MonoBehaviour
{
    GameObject _target;
    public void SetChestValue(GameObject target)
    {
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
        if (distance <= 1.5f)
        {
            transform.position = Vector3.MoveTowards(transform.position, _target.transform.position, 20f * Time.deltaTime);
            if (Vector3.Distance(transform.position, _target.transform.position) < 0.1f)
            {
                if (!GameManager.Instance.LevelUpPanelActive())
                {
                    GameManager.Instance.ShowLevelUpPanel(true);
                }
                Destroy(gameObject);
            }
        }
    }
}
