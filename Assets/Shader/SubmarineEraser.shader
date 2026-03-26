Shader "Custom/SubmarineEraser"
{
    Properties
    {
        _DirtyTex ("Dirty Texture", 2D) = "white" {}
        _MaskTex ("Clean Mask", 2D) = "black" {}
        _MossTint ("Moss Tint", Color) = (0.42, 0.52, 0.26, 1)
        _MossStrength ("Moss Strength", Range(0, 1)) = 0.45
        _ScanColor ("Scan Color", Color) = (0, 1, 0, 1)
        _IsScanning ("Is Scanning", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "SubmarineEraserPass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_DirtyTex); SAMPLER(sampler_DirtyTex);
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _DirtyTex_ST;
                float4 _MossTint;
                float _MossStrength;
                float4 _ScanColor;
                float _IsScanning;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _DirtyTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 dirtyColor = SAMPLE_TEXTURE2D(_DirtyTex, sampler_DirtyTex, IN.uv);
                float mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, IN.uv).r;
                half mossMask = saturate((dirtyColor.g * 1.25 + dirtyColor.r * 0.35) - dirtyColor.b * 0.2);

                // Original dirt texture stays visible, but picks up a thin mossy olive tint.
                dirtyColor.rgb = lerp(dirtyColor.rgb, dirtyColor.rgb * _MossTint.rgb, mossMask * _MossStrength);

                if (_IsScanning > 0.5 && mask < 0.9)
                {
                    dirtyColor.rgb += _ScanColor.rgb * 0.6;
                }

                dirtyColor.a *= (1.0 - mask);
                return dirtyColor;
            }
            ENDHLSL
        }
    }
}
