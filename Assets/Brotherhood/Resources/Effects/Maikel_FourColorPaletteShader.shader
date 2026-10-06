Shader "Maikel/FourColorPaletteShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        _OriginA ("Origin A", Color) = (1,1,1,1)
        _OriginB ("Origin B", Color) = (1,1,1,1)
        _OriginC ("Origin C", Color) = (1,1,1,1)
        _OriginD ("Origin D", Color) = (1,1,1,1)
        _OriginE ("Origin E", Color) = (1,1,1,1)
        _OriginF ("Origin F", Color) = (1,1,1,1)
        _SwapA ("Swap A", Color) = (1,1,1,1)
        _SwapB ("Swap B", Color) = (1,1,1,1)
        _SwapC ("Swap C", Color) = (1,1,1,1)
        _SwapD ("Swap D", Color) = (1,1,1,1)
        _SwapE ("Swap E", Color) = (1,1,1,1)
        _SwapF ("Swap F", Color) = (1,1,1,1)
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
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
                fixed4 color : COLOR;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _OriginA, _OriginB, _OriginC, _OriginD, _OriginE, _OriginF;
            fixed4 _SwapA, _SwapB, _SwapC, _SwapD, _SwapE, _SwapF;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 pixel = tex2D(_MainTex, input.uv);
                if(pixel.a <= 0.001) discard;
                // The exporter left a dummy shader body. The original material
                // retains the six exact origin/swap colours used by Verdiales.
                float nearest = 1e10;
                fixed3 replacement = pixel.rgb;
                #define MATCH(A, B) { float3 diff = pixel.rgb - A.rgb; float distance = dot(diff,diff); if(distance < nearest) { nearest = distance; replacement = B.rgb; } }
                MATCH(_OriginA, _SwapA)
                MATCH(_OriginB, _SwapB)
                MATCH(_OriginC, _SwapC)
                MATCH(_OriginD, _SwapD)
                MATCH(_OriginE, _SwapE)
                MATCH(_OriginF, _SwapF)
                #undef MATCH
                return fixed4(replacement, pixel.a) * input.color;
            }
            ENDCG
        }
    }
}
