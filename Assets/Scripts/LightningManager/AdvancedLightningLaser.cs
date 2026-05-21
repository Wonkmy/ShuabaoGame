using System.Collections.Generic;
using UnityEngine;

public class AdvancedLightning : MonoBehaviour
{
    [Header("材质")]
    public Material lightningMaterial;

    [Header("线段")]
    public int segmentCount = 40;

    [Header("扰动")]
    public float noiseStrength = 0.55f;
    public float noiseSpeed = 18f;

    [Header("宽度")]
    public float coreWidth = 0.015f;
    public float mainWidth = 0.045f;
    public float glowWidth = 0.16f;

    [Header("生命周期")]
    public float duration = 0.15f;

    [Header("分叉")]
    public int branchCount = 4;
    public float branchLength = 0.7f;

    private LineRenderer coreLine;
    private LineRenderer mainLine;
    private LineRenderer glowLine;

    private readonly List<LineRenderer> branchLines =
        new List<LineRenderer>();

    private Vector3[] mainPoints;

    public Vector3 startPos { get; set; }
    public Vector3 endPos { get; set; }

    private float timer;
    private float noiseSeed;

    // =========================
    // 对外接口
    // =========================

    public void Init(
        Vector3 start,
        Vector3 end
    )
    {
        startPos = start;
        endPos = end;

        noiseSeed = Random.Range(0f, 999f);

        GenerateLightning();
    }

    void Awake()
    {
        CreateLines();
    }

    void Update()
    {
        timer += Time.deltaTime;

        GenerateLightning();

        UpdateShaderFlow();
    }
    public void UpdatePosition(Vector3 start, Vector3 end)
    {
        startPos = start;
        endPos = end;
    }
    void CreateLines()
    {
        coreLine = CreateLine(
            "CoreLine",
            coreWidth,
            Color.white
        );

        mainLine = CreateLine(
            "MainLine",
            mainWidth,
            new Color(0f, 1f, 0f, 0.9f)
        );

        glowLine = CreateLine(
            "GlowLine",
            glowWidth,
            new Color(0f, 0.85f, 1f, 0.25f)
        );

        for (int i = 0; i < branchCount; i++)
        {
            LineRenderer branch =
                CreateLine(
                    "Branch_" + i,
                    mainWidth * 0.5f,
                    new Color(0f, 0.7f, 1f, 0.55f)
                );

            branchLines.Add(branch);
        }

        mainPoints = new Vector3[segmentCount];
    }

    void GenerateLightning()
    {
        Vector3 dir = endPos - startPos;

        Vector3 dirNormal = dir.normalized;

        Vector3 side =
            Vector3.Cross(
                dirNormal,
                Camera.main.transform.forward
            ).normalized;

        for (int i = 0; i < segmentCount; i++)
        {
            float t =
                i / (float)(segmentCount - 1);

            Vector3 pos =
                Vector3.Lerp(startPos, endPos, t);

            if (i != 0 && i != segmentCount - 1)
            {
                float fade =
                    Mathf.Sin(t * Mathf.PI);

                // Perlin整体趋势
                float perlin =
                    Mathf.PerlinNoise(
                        noiseSeed + t * 2f,
                        Time.time * noiseSpeed
                    );

                perlin =
                    (perlin - 0.5f) * 2f;

                // 高频抖动
                float jitter =
                    Random.Range(-1f, 1f) * 0.45f;

                float offset =
                    (perlin + jitter)
                    * noiseStrength
                    * fade;

                pos += side * offset;

                // 上下扰动
                pos +=
                    Camera.main.transform.up
                    * Random.Range(
                        -noiseStrength,
                        noiseStrength
                    )
                    * 0.2f
                    * fade;
            }

            mainPoints[i] = pos;
        }

        SetLine(coreLine, mainPoints);
        SetLine(mainLine, mainPoints);
        SetLine(glowLine, mainPoints);

        GenerateBranches();
    }

    void GenerateBranches()
    {
        for (int i = 0; i < branchLines.Count; i++)
        {
            LineRenderer branch =
                branchLines[i];

            bool show =
                Random.value > 0.35f;

            branch.enabled = show;

            if (!show)
                continue;

            int startIndex =
                Random.Range(
                    2,
                    segmentCount - 3
                );

            Vector3 branchStart =
                mainPoints[startIndex];

            Vector3 dir =
                (endPos - startPos).normalized;

            Vector3 side =
                Vector3.Cross(
                    dir,
                    Camera.main.transform.forward
                ).normalized;

            float sideDir =
                Random.value > 0.5f ? 1 : -1;

            Vector3 branchEnd =
                branchStart
                + side
                * sideDir
                * Random.Range(
                    branchLength * 0.4f,
                    branchLength
                );

            int count = 5;

            Vector3[] points =
                new Vector3[count];

            for (int p = 0; p < count; p++)
            {
                float t =
                    p / (float)(count - 1);

                Vector3 pos =
                    Vector3.Lerp(
                        branchStart,
                        branchEnd,
                        t
                    );

                if (p != 0 && p != count - 1)
                {
                    float fade =
                        Mathf.Sin(t * Mathf.PI);

                    pos += side
                        * Random.Range(-0.15f, 0.15f)
                        * fade;
                }

                points[p] = pos;
            }

            SetLine(branch, points);
        }
    }

    LineRenderer CreateLine(
        string lineName,
        float width,
        Color color
    )
    {
        GameObject obj =
            new GameObject(lineName);

        obj.transform.SetParent(transform);

        LineRenderer line =
            obj.AddComponent<LineRenderer>();

        line.useWorldSpace = true;

        line.alignment =
            LineAlignment.View;

        line.textureMode =
            LineTextureMode.Stretch;

        line.numCapVertices = 4;
        line.numCornerVertices = 4;

        line.startWidth = width;
        line.endWidth = width;

        line.material =
            new Material(lightningMaterial);

        line.material.color = color;

        return line;
    }

    void SetLine(
        LineRenderer line,
        Vector3[] points
    )
    {
        line.positionCount = points.Length;
        line.SetPositions(points);
    }

    void UpdateShaderFlow()
    {
        float flowTime = Time.time;

        coreLine.material.SetFloat(
            "_FlowTime",
            flowTime
        );

        mainLine.material.SetFloat(
            "_FlowTime",
            flowTime
        );

        glowLine.material.SetFloat(
            "_FlowTime",
            flowTime
        );

        foreach (var branch in branchLines)
        {
            branch.material.SetFloat(
                "_FlowTime",
                flowTime
            );
        }
    }
}