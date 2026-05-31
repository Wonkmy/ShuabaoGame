using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadFX : MonoBehaviour
{
    public string FXId { get;private set; }

    Coroutine fxCoroutine;
    public void SetFXId(string id)
    {
        FXId = id;
    }

    private void Start()
    {
        fxCoroutine = StartCoroutine(DisableSelf());
    }

    IEnumerator DisableSelf()
    {
        yield return new WaitForSeconds(1.0f);
        DeadFXPool.Instance.Release(FXId, gameObject);
    }

    private void OnDisable()
    {
        if (fxCoroutine != null)
        {
            StopCoroutine(fxCoroutine);
        }
    }
}
