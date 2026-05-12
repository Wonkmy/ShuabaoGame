Shader "Custom/LowHpEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0,1)) = 0
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

            fixed4 frag(v2f_img i) : SV_Target
            {
                // 原本游戏画面
                fixed4 col = tex2D(_MainTex, i.uv);

                float2 center = i.uv - 0.5;
                float dist = length(center);

                // 只影响屏幕边缘
                float edge = smoothstep(0.35, 0.75, dist);

                // 呼吸闪烁
                float pulse = sin(_Time.y * 5) * 0.5 + 0.5;

                float redStrength = edge * pulse * _Intensity;

                // 在原画面基础上叠红，不覆盖原画面
                col.rgb = lerp(col.rgb, col.rgb + float3(0.8, 0, 0), redStrength);

                return col;
            }

            ENDCG
        }
    }
}