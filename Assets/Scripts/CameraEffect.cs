using UnityEngine;

public class CameraEffect : MonoBehaviour
{
    public Material mat;

    [Range(0, 1)]
    public float intensity;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        mat.SetFloat("_Intensity", intensity);

        Graphics.Blit(src, dest, mat);
    }
}