Shader "Custom/UI/CardFrame"
{
    Properties
    {
        _MainTex          ("Texture",              2D)     = "white" {}
        _CardColor        ("Card Color",           Color)  = (0.05, 0.08, 0.15, 1)
        _BorderColor      ("Border Color",         Color)  = (0.0, 0.9, 1.0, 1)
        _BorderWidth      ("Border Width (px)",    Float)  = 4
        _GlowWidth        ("Glow Width (px)",      Float)  = 10
        _GlowIntensity    ("Glow Intensity",       Range(0,1)) = 1
        _CornerRadius     ("Corner Radius (px)",   Float)  = 12
        _CardSize         ("Card Size (W H _ _)",  Vector) = (130, 182, 0, 0)
        // 1 = 显示深色背景（完整模式）  0 = 内部透明，只显示边框+发光（叠加模式）
        _FillAlpha        ("Fill Alpha",           Range(0,1)) = 0
        _ShadowTop        ("Shadow Extent",        Range(0,1)) = 0.38
        _ShadowAlpha      ("Shadow Max Alpha",     Range(0,1)) = 0.78

        // Unity UI 必要项
        [HideInInspector] _StencilComp     ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil         ("Stencil ID",         Float) = 0
        [HideInInspector] _StencilOp       ("Stencil Operation",  Float) = 0
        [HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask",  Float) = 255
        [HideInInspector] _ColorMask       ("Color Mask",         Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull      Off
        Lighting  Off
        ZWrite    Off
        ZTest     [unity_GUIZTestMode]
        Blend     SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4    _CardColor;
            fixed4    _BorderColor;
            float     _BorderWidth;
            float     _GlowWidth;
            float     _GlowIntensity;
            float     _CornerRadius;
            float4    _CardSize;
            float     _FillAlpha;
            float     _ShadowTop;
            float     _ShadowAlpha;
            float4    _ClipRect;

            // ── 圆角矩形 SDF ────────────────────────────────────
            // p        : 像素坐标（中心为原点）
            // halfSize : 矩形半尺寸（像素）
            // r        : 圆角半径（像素）
            // 返回值 < 0 = 内部，= 0 = 边缘，> 0 = 外部
            float RoundedRectSDF(float2 p, float2 halfSize, float r)
            {
                float2 q = abs(p) - halfSize + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.uv       = v.uv;
                o.color    = v.color;
                o.worldPos = v.vertex;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UV → 以卡牌中心为原点的像素坐标
                float2 p    = (i.uv - 0.5) * _CardSize.xy;
                float  dist = RoundedRectSDF(p, _CardSize.xy * 0.5, _CornerRadius);

                // 超出发光范围直接丢弃，避免矩形透明区域
                clip(_GlowWidth - dist);

                fixed4 col;

                if (dist > 0.0)
                {
                    // ── 圆角遮罩区域（方形卡图超出圆角的部分）
                    // 边缘处保留发光，越往角落越暗直至完全遮住卡图
                    float glowT = 1.0 - smoothstep(0.0, _GlowWidth, dist);
                    glowT = glowT * glowT;
                    float maskT = 1.0 - glowT; // 越深入角落 maskT 越大
                    col = fixed4(
                        lerp(_BorderColor.rgb, fixed3(0, 0, 0), maskT),
                        max(_BorderColor.a * glowT * _GlowIntensity, maskT)
                    );
                }
                else if (dist <= 0.0 && dist > -_BorderWidth)
                {
                    // ── 边框区域：内边做 1.5px 抗锯齿过渡
                    float t = smoothstep(-_BorderWidth, -_BorderWidth + 1.5, dist);
                    col = lerp(_CardColor, _BorderColor, t);
                    col.a = 1.0;
                }
                else
                {
                    // ── 卡牌内部
                    // 背景填充（FillAlpha=0 时透明）
                    fixed4 bg = fixed4(_CardColor.rgb, _CardColor.a * _FillAlpha);

                    // 底部深色阴影渐变（从底部向上淡出）
                    float shadowT = pow(1.0 - smoothstep(0.0, _ShadowTop, i.uv.y), 2.0) * _ShadowAlpha;
                    fixed4 shadow = fixed4(0.0, 0.0, 0.0, shadowT);

                    // 将阴影叠加在背景上（标准 alpha 合成）
                    float outA   = shadow.a + bg.a * (1.0 - shadow.a);
                    fixed3 outRGB = outA > 0.001
                        ? (shadow.rgb * shadow.a + bg.rgb * bg.a * (1.0 - shadow.a)) / outA
                        : fixed3(0, 0, 0);
                    col = fixed4(outRGB, outA);
                }

                // 受 CanvasGroup alpha 和 UI Mask 裁剪控制
                col.a *= i.color.a * UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                return col;
            }
            ENDCG
        }
    }
}
