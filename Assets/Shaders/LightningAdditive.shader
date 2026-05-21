Shader "Custom/LightningAdditive"
{
    Properties
    {
        _Color ("Color", Color) = (1,0,0,1)

        _FlowSpeed ("Flow Speed", Float) = 8

        _FlowTime ("Flow Time", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha One

        ZWrite Off
        Cull Off

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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;

            float _FlowSpeed;

            float _FlowTime;

            v2f vert(appdata v)
            {
                v2f o;

                o.pos =
                    UnityObjectToClipPos(v.vertex);

                o.uv = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float flow =
                    sin(
                        (i.uv.x * 35.0)
                        - (_FlowTime * _FlowSpeed)
                    );

                flow =
                    flow * 0.5 + 0.5;

                float pulse =
                    0.4 + flow * 1.2;

                fixed4 col =
                    _Color;

                col.rgb *= pulse;

                col.a =
                    pulse * _Color.a;

                return col;
            }

            ENDCG
        }
    }
}