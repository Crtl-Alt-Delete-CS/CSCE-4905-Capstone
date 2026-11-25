Shader "BioVisionAR/OrganelleOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.85, 0.1, 1)
        _OutlinePixels ("Outline Width (pixels)", Float) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+1"
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Resources/Shaders/CrossSectionCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlinePixels;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 positionVS = TransformWorldToView(positionWS);
                float3 normalVS = mul((float3x3)GetWorldToViewMatrix(), normalWS);

                float2 direction = normalVS.xy;
                direction = dot(direction, direction) > 1e-6 ? normalize(direction) : float2(0, 0);

                float4 clip = TransformWViewToHClip(positionVS);
                clip.xy += direction * (_OutlinePixels * 2.0) * clip.w / _ScreenParams.xy;
                output.positionCS = clip;
                output.positionWS = positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                ApplyCrossSectionClip(input.positionWS);
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
