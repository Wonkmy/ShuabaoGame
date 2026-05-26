using UnityEngine;

public class SpaceBackgroundController : MonoBehaviour
{
    const int TextureSize = 512;
    SpriteRenderer backgroundRenderer;
    SpriteRenderer starLayerRenderer;
    SpriteRenderer twinkleLayerRenderer;
    float scroll;
    float twinkleTimer;

    void Start()
    {
        CreateBackgroundLayer();
        CreateStarLayer();
        CreateTwinkleLayer();
    }

    void LateUpdate()
    {
        if (backgroundRenderer == null)
            return;

        backgroundRenderer.transform.localPosition = new Vector3(0f, 0f, 20f);
        starLayerRenderer.transform.localPosition = new Vector3(0f, 0f, 19.9f);
        twinkleLayerRenderer.transform.localPosition = new Vector3(0f, 0f, 19.8f);

        scroll += Time.deltaTime * 0.18f;
        starLayerRenderer.transform.localPosition += new Vector3(Mathf.Sin(scroll) * 0.002f, -0.006f, 0f);

        twinkleTimer += Time.deltaTime;
        float alpha = 0.22f + Mathf.Abs(Mathf.Sin(twinkleTimer * 2.8f)) * 0.42f;
        float scale = 92f + Mathf.Sin(twinkleTimer * 3.7f) * 0.35f;
        twinkleLayerRenderer.color = new Color(1f, 1f, 1f, alpha);
        twinkleLayerRenderer.transform.localScale = Vector3.one * scale;
        twinkleLayerRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(twinkleTimer * 0.4f) * 0.25f);
    }

    void CreateBackgroundLayer()
    {
        GameObject obj = new GameObject("ProceduralSpaceBackground");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(0f, 0f, 20f);
        obj.transform.localScale = Vector3.one * 92f;

        backgroundRenderer = obj.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = Sprite.Create(
            GenerateNebulaTexture(),
            new Rect(0, 0, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            TextureSize);
        backgroundRenderer.sortingOrder = -1000;
        backgroundRenderer.color = new Color(1f, 1f, 1f, 0.9f);
    }

    void CreateStarLayer()
    {
        GameObject obj = new GameObject("ProceduralStarLayer");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(0f, 0f, 19.9f);
        obj.transform.localScale = Vector3.one * 92f;

        starLayerRenderer = obj.AddComponent<SpriteRenderer>();
        starLayerRenderer.sprite = Sprite.Create(
            GenerateStarTexture(),
            new Rect(0, 0, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            TextureSize);
        starLayerRenderer.sortingOrder = -999;
        starLayerRenderer.color = new Color(1f, 1f, 1f, 0.62f);
    }

    void CreateTwinkleLayer()
    {
        GameObject obj = new GameObject("ProceduralTwinkleStarLayer");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(0f, 0f, 19.8f);
        obj.transform.localScale = Vector3.one * 92f;

        twinkleLayerRenderer = obj.AddComponent<SpriteRenderer>();
        twinkleLayerRenderer.sprite = Sprite.Create(
            GenerateTwinkleTexture(),
            new Rect(0, 0, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            TextureSize);
        twinkleLayerRenderer.sortingOrder = -998;
    }

    Texture2D GenerateNebulaTexture()
    {
        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 centerA = new Vector2(TextureSize * 0.32f, TextureSize * 0.58f);
        Vector2 centerB = new Vector2(TextureSize * 0.72f, TextureSize * 0.36f);

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                Vector2 p = new Vector2(x, y);
                float a = Mathf.Clamp01(1f - Vector2.Distance(p, centerA) / 260f);
                float b = Mathf.Clamp01(1f - Vector2.Distance(p, centerB) / 220f);
                float noise = Mathf.PerlinNoise(x * 0.018f, y * 0.018f);

                Color baseColor = new Color(0.018f, 0.022f, 0.048f, 0.72f);
                Color nebulaA = new Color(0.055f, 0.13f, 0.25f, 0.46f);
                Color nebulaB = new Color(0.14f, 0.045f, 0.13f, 0.36f);
                Color c = Color.Lerp(baseColor, nebulaA, a * noise * 0.62f);
                c = Color.Lerp(c, nebulaB, b * (1f - noise) * 0.45f);
                texture.SetPixel(x, y, c);
            }
        }

        BlurTexture(texture, 3, 0.92f);
        texture.Apply();
        return texture;
    }

    Texture2D GenerateStarTexture()
    {
        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        Random.State previousState = Random.state;
        Random.InitState(2317);
        for (int i = 0; i < 420; i++)
        {
            int x = Random.Range(0, TextureSize);
            int y = Random.Range(0, TextureSize);
            float brightness = Random.Range(0.35f, 0.85f);
            Color c = new Color(0.68f + brightness * 0.22f, 0.78f + brightness * 0.16f, 1f, Random.Range(0.22f, 0.52f));
            texture.SetPixel(x, y, c);

            if (brightness > 0.68f && x > 1 && x < TextureSize - 2 && y > 1 && y < TextureSize - 2)
            {
                Color glow = new Color(c.r, c.g, c.b, c.a * 0.28f);
                texture.SetPixel(x + 1, y, glow);
                texture.SetPixel(x - 1, y, glow);
                texture.SetPixel(x, y + 1, glow);
                texture.SetPixel(x, y - 1, glow);
            }
        }

        SoftBlurTexture(texture, 1, 0.92f, 0.55f);
        texture.Apply();
        Random.state = previousState;
        return texture;
    }

    Texture2D GenerateTwinkleTexture()
    {
        Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        Random.State previousState = Random.state;
        Random.InitState(9321);
        for (int i = 0; i < 56; i++)
        {
            int x = Random.Range(3, TextureSize - 3);
            int y = Random.Range(3, TextureSize - 3);
            float brightness = Random.Range(0.55f, 0.86f);
            Color core = new Color(0.82f, 0.9f, 1f, brightness * 0.62f);
            Color glow = new Color(0.42f, 0.66f, 1f, brightness * 0.24f);
            Color faint = new Color(0.32f, 0.48f, 1f, brightness * 0.08f);

            texture.SetPixel(x, y, core);
            texture.SetPixel(x + 1, y, glow);
            texture.SetPixel(x - 1, y, glow);
            texture.SetPixel(x, y + 1, glow);
            texture.SetPixel(x, y - 1, glow);

            texture.SetPixel(x + 2, y, faint);
            texture.SetPixel(x - 2, y, faint);
            texture.SetPixel(x, y + 2, faint);
            texture.SetPixel(x, y - 2, faint);
        }

        SoftBlurTexture(texture, 1, 0.9f, 0.6f);
        texture.Apply();
        Random.state = previousState;
        return texture;
    }

    void SoftBlurTexture(Texture2D texture, int radius, float alphaMultiplier, float originalWeight)
    {
        Color[] source = texture.GetPixels();
        Color[] blurred = new Color[source.Length];
        int size = texture.width;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color sum = Color.clear;
                int count = 0;

                for (int oy = -radius; oy <= radius; oy++)
                {
                    int py = Mathf.Clamp(y + oy, 0, size - 1);
                    for (int ox = -radius; ox <= radius; ox++)
                    {
                        int px = Mathf.Clamp(x + ox, 0, size - 1);
                        sum += source[py * size + px];
                        count++;
                    }
                }

                int index = y * size + x;
                Color averaged = sum / count;
                Color c = Color.Lerp(averaged, source[index], originalWeight);
                c.a *= alphaMultiplier;
                blurred[index] = c;
            }
        }

        texture.SetPixels(blurred);
    }

    void BlurTexture(Texture2D texture, int radius, float alphaMultiplier)
    {
        Color[] source = texture.GetPixels();
        Color[] blurred = new Color[source.Length];
        int size = texture.width;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color sum = Color.clear;
                int count = 0;

                for (int oy = -radius; oy <= radius; oy++)
                {
                    int py = Mathf.Clamp(y + oy, 0, size - 1);
                    for (int ox = -radius; ox <= radius; ox++)
                    {
                        int px = Mathf.Clamp(x + ox, 0, size - 1);
                        sum += source[py * size + px];
                        count++;
                    }
                }

                Color c = sum / count;
                c.a *= alphaMultiplier;
                blurred[y * size + x] = c;
            }
        }

        texture.SetPixels(blurred);
    }
}
