Shader "Custom/LowHpEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        // 低血量红边强度
        _Intensity ("Intensity", Range(0,1)) = 0

        // Boss来袭屏幕变暗强度
        _DarkIntensity ("DarkIntensity", Range(0,1)) = 0
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;

            float _Intensity;
            float _DarkIntensity;

            fixed4 frag(v2f_img i) : SV_Target
            {
                // 原画面
                fixed4 col = tex2D(_MainTex, i.uv);

                // =========================
                // Boss来袭屏幕整体变暗
                // =========================

                col.rgb *= (1.0 - _DarkIntensity);

                // =========================
                // 低血量红边
                // =========================

                float2 center = i.uv - 0.5;

                float dist = length(center);

                // 边缘区域
                float edge = smoothstep(0.35, 0.75, dist);

                // 呼吸效果
                float pulse = sin(_Time.y * 5) * 0.5 + 0.5;

                float redStrength = edge * pulse * _Intensity;

                // 红边叠加
                col.rgb = lerp(col.rgb,col.rgb + float3(0.8, 0, 0),redStrength);

                return col;
            }

            ENDCG
        }
    }
}