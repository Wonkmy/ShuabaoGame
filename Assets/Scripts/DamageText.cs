using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class DamageText : MonoBehaviour
//{
//    public TextMesh textMesh;

//    public float Life { get; set; }
//    public bool Dead { get; set; }

//    public void SetDamageText(int damage,float _life = 0.35f)
//    {
//        textMesh.text = "-" + damage.ToString();
//        Life = _life;
//        Dead = false;
//    }
//    public void DamageTextUpdate()
//    {
//        Life -= Time.deltaTime;
//        transform.position += new Vector3(0, 2.8f * Time.deltaTime, 0);
//        if(Life <= 0)
//        {
//            Life = 0;
//            Dead = true;
//        }
//    }
//}

public class DamageText : MonoBehaviour
{
    public TextMesh textMesh;

    public float Life { get; set; }

    public bool Dead { get; set; }

    private Vector3 moveDir;

    private Vector3 targetScale;

    public void SetDamageText(int damage, bool isCrit, float _life = 0.45f)
    {
        textMesh.text = "-" + damage.ToString();

        Life = _life;

        Dead = false;

        // 随机漂浮方向
        moveDir = new Vector3(
            Random.Range(-1.6f, 1.6f),
            Random.Range(3.5f, 3.5f),
            0);

        // 暴击
        if (isCrit)
        {
            textMesh.color = Color.red;

            transform.localScale = Vector3.one * 2.0f;

            targetScale = Vector3.one * 1.2f;
        }
        else
        {
            textMesh.color = Color.yellow;

            transform.localScale = Vector3.one;

            targetScale = Vector3.one * 0.7f;
        }
    }

    public void DamageTextUpdate()
    {
        Life -= Time.deltaTime;

        // 漂浮
        transform.position += moveDir * Time.deltaTime;

        // 缩放缓动
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * 10f);

        // 渐隐
        Color color = textMesh.color;

        color.a = Mathf.Clamp01(Life / 0.35f);

        textMesh.color = color;

        if (Life <= 0)
        {
            Life = 0;

            Dead = true;
        }
    }
}