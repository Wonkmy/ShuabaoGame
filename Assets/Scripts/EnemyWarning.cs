using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWarning : MonoBehaviour
{
    public Transform target;

    Camera mainCamera;
    GameObject view;

    void Start()
    {
        mainCamera = Camera.main;

        view = transform.Find("view").gameObject;
        view.SetActive(false);
        StartCoroutine(ShowFlashWarningTxt());
    }

    IEnumerator ShowFlashWarningTxt()
    {
        view.SetActive(true);
        float timer = 0;
        while (timer < 2)
        {
            timer += Time.deltaTime;
            // 每0.2秒闪烁一次
            if (Mathf.FloorToInt(timer * 5) % 2 == 0)
            {
                view.SetActive(true);
            }
            else
            {
                view.SetActive(false);
            }
            yield return null;
        }
        view.SetActive(false);
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 screenPos =
            mainCamera.WorldToScreenPoint(target.position);

        float padding = 80f;

        screenPos.x =
            Mathf.Clamp(
                screenPos.x,
                padding,
                Screen.width - padding);

        screenPos.y =
            Mathf.Clamp(
                screenPos.y,
                padding,
                Screen.height - padding);

        Vector3 worldPos =
            mainCamera.ScreenToWorldPoint(screenPos);

        worldPos.z = 0;

        transform.position = worldPos;
    }
}
