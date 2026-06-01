Shader "Custom/SpriteSkew"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _RollAngle("Roll Angle", Range(-1,1)) = 0 // -1=左倾, 0=正, 1=右倾
        _Perspective("Perspective", Range(0,2)) = 0.6 // 透视强度
    }
        SubShader
        {
            Tags {"Queue" = "Transparent" "RenderType" = "Transparent"}
            LOD 100

            Pass
            {
                ZWrite Off
                Blend SrcAlpha OneMinusSrcAlpha
                Cull Off

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                    float4 color : COLOR;
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    float2 texcoord : TEXCOORD0;
                    fixed4 color : COLOR;
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                fixed4 _Color;
                float _RollAngle;
                float _Perspective;

                v2f vert(appdata_t v)
                {
                    v2f o;

                    // ==========================
                    // === Sprite中心化坐标 ======
                    // ==========================
                    float x = v.vertex.x;
                    float y = v.vertex.y;

                    // 归一化到[-0.5, 0.5]，sprite是中心为0的正方形（通常是 -0.5~+0.5 ）
                    // 如果不是，建议你手动修正，使 x,y 取值为-0.5~+0.5
                    // 这样 roll 变换更标准

                    // ==========================
                    // === Roll变换部分 =========
                    // ==========================

                    float roll = _RollAngle * 1.2; // -1到1映射为-1.2到1.2弧度, 约-69°~+69°
                    float perspective = _Perspective; // 0.6~1.0

                    // 伪3D roll投影
                    // 假设y轴是飞机机身前后，x轴是机翼左右
                    // 横滚就是绕y轴（Unity 2D里就是x变形，y深度压缩）
                    float x2 = x * cos(roll);
                    float z = -x * sin(roll);
                    float y2 = y + z * perspective;

                    v.vertex.x = x2;
                    v.vertex.y = y2;

                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.color = v.color * _Color;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    return tex2D(_MainTex, i.texcoord) * i.color;
                }
                ENDCG
            }
        }

}
