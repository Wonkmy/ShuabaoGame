using UnityEngine;

public class CameraEffect : MonoBehaviour
{
    public Material mat;

    [Range(0, 1)]
    public float intensity;

    [Range(0, 1)]
    public float darkIntensity;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        mat.SetFloat("_Intensity", intensity);
        mat.SetFloat("_DarkIntensity", darkIntensity);
        Graphics.Blit(src, dest, mat);
    }
}