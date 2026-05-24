Shader "Custom/WaterRipple_Builtin"
{
    Properties
    {
        // 噪声贴图
        _NoiseTex ("Noise Tex", 2D) = "white" {}

        // 扰动强度
        _Strength ("Strength", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        // 抓取屏幕纹理
        GrabPass
        {
            "_GrabTexture"
        }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                // 裁剪空间坐标
                float4 vertex : SV_POSITION;

                // 普通UV
                float2 uv : TEXCOORD0;

                // 屏幕UV
                float4 grabPos : TEXCOORD1;
            };

            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;

            // GrabPass生成的屏幕纹理
            sampler2D _GrabTexture;

            float _Strength;

            v2f vert (appdata v)
            {
                v2f o;

                // 顶点转换
                o.vertex = UnityObjectToClipPos(v.vertex);

                // UV变换
                o.uv = TRANSFORM_TEX(v.uv, _NoiseTex);

                // 获取屏幕坐标
                o.grabPos = ComputeGrabScreenPos(o.vertex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 采样noise
                float noise = tex2D(_NoiseTex, i.uv).r;

                // SCREEN_UV
                float2 screenUV = i.grabPos.xy / i.grabPos.w;

                // UV扰动
                screenUV += noise * _Strength;

                // 采样屏幕纹理
                fixed4 col = tex2D(_GrabTexture, screenUV);

                return col;
            }

            ENDCG
        }
    }
}