using UnityEngine;

public class PulseFx : MonoBehaviour
{
    float targetScale = 2f;
    float duration = 0.3f;
    float timer;
    SpriteRenderer sr;
    Color startColor;

    public void Init(float scale, float lifeTime)
    {
        targetScale = scale;
        duration = Mathf.Max(0.05f, lifeTime);
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        startColor = sr != null ? sr.color : Color.white;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        transform.localScale = Vector3.Lerp(Vector3.one * 0.35f, Vector3.one * targetScale, t);

        if (sr != null)
        {
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            sr.color = c;
        }

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}
