using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWarning : MonoBehaviour
{
    public Transform target;

    Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
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
