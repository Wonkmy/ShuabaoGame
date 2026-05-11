using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    public TextMesh textMesh;

    public float Life { get; set; }
    public bool Dead { get; set; }

    public void SetDamageText(int damage,float _life = 0.35f)
    {
        textMesh.text = "-" + damage.ToString();
        Life = _life;
        Dead = false;
    }
    public void DamageTextUpdate()
    {
        Life -= Time.deltaTime;
        transform.position += new Vector3(0, 1.8f * Time.deltaTime, 0);
        if(Life <= 0)
        {
            Life = 0;
            Dead = true;
        }
    }
}
