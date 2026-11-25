#ifndef BIOVISION_CROSS_SECTION_COMMON_INCLUDED
#define BIOVISION_CROSS_SECTION_COMMON_INCLUDED

float _CrossSectionEnabled;
float4 _CrossSectionPlane;
float _CrossSectionEdgeWidth;

inline float GetCrossSectionSignedDistance(float3 positionWS)
{
    return dot(positionWS, _CrossSectionPlane.xyz) + _CrossSectionPlane.w;
}

inline void ApplyCrossSectionClip(float3 positionWS)
{
    if (_CrossSectionEnabled > 0.5)
    {
        clip(-GetCrossSectionSignedDistance(positionWS));
    }
}

inline void ApplyCrossSectionForwardShading(float3 positionWS, bool isFrontFace, inout half3 rgb)
{
    if (_CrossSectionEnabled > 0.5)
    {
        if (!isFrontFace)
            rgb *= 0.72;

        float edgeWidth = max(_CrossSectionEdgeWidth, 0.002);
        float dist = -GetCrossSectionSignedDistance(positionWS);
        float edge = 1.0 - smoothstep(0.0, edgeWidth, dist);
        rgb = lerp(rgb, half3(0.0, 0.83, 0.67), edge * 0.45);
    }
}

#endif
