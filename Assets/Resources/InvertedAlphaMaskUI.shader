Shader "UI/InvertedAlphaMaskUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _MaskTex ("Mask Texture", 2D) = "white" {}
        _MaskColor ("Mask Color", Color) = (1,1,1,1)
        _MaskRect ("Mask Rect", Vector) = (0,0,1,1)
        _MaskUVRect ("Mask UV Rect", Vector) = (0,0,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 localPosition : TEXCOORD1;
                float2 maskLocalPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;

            sampler2D _MaskTex;
            fixed4 _MaskColor;
            float4 _MaskRect;
            float4 _MaskUVRect;
            float4x4 _MaskWorldToLocal;

            float4 _ClipRect;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 worldPosition = mul(unity_ObjectToWorld, input.vertex);

                output.vertex = UnityObjectToClipPos(input.vertex);
                output.localPosition = input.vertex;
                output.maskLocalPosition = mul(_MaskWorldToLocal, worldPosition).xy;
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;

                float2 maskSize = max(_MaskRect.zw - _MaskRect.xy, float2(0.0001, 0.0001));
                float2 maskNormalized = (input.maskLocalPosition - _MaskRect.xy) / maskSize;
                float maskInside =
                    step(0.0, maskNormalized.x) *
                    step(0.0, maskNormalized.y) *
                    step(maskNormalized.x, 1.0) *
                    step(maskNormalized.y, 1.0);

                float2 maskUv = lerp(_MaskUVRect.xy, _MaskUVRect.zw, saturate(maskNormalized));
                fixed maskAlpha = tex2D(_MaskTex, maskUv).a * _MaskColor.a * maskInside;

                color.a *= saturate(1.0 - maskAlpha);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.localPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
