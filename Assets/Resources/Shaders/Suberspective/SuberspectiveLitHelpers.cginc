#ifndef SUBERSPECTIVE_INCLUDED
#define SUBERSPECTIVE_INCLUDED
	
#include "SuberspectiveCommon.cginc"

sampler2D _MainTex;
float4 _MainTex_ST;
sampler2D _EmissionMap;
float4 _EmissionMap_ST;

struct SuberspectiveV2F {
    float4 clipPos : SV_POSITION;
    float3 worldPos : TEXCOORD0;
    float4 screenPos : TEXCOORD1;
	float3 normalDir : NORMAL;
    float2 uv_MainTex : TEXCOORD3;
    float2 uv_EmissionMap : TEXCOORD4;
    float linearDepth : SV_Depth;
#ifdef DISSOLVE_OBJECT
    float2 uv_DissolveTex : TEXCOORD5;
#endif
};

SuberspectiveV2F SuberspectiveVert(appdata_full v) {
    SuberspectiveV2F o;
    o.clipPos = UnityObjectToClipPos(v.vertex);
    o.screenPos = ComputeScreenPos(o.clipPos);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	o.normalDir = UnityObjectToWorldNormal(v.normal);
    o.uv_MainTex = TRANSFORM_TEX(v.texcoord, _MainTex);
    o.uv_EmissionMap = TRANSFORM_TEX(v.texcoord, _EmissionMap);
    o.linearDepth = COMPUTE_DEPTH_01;
#ifdef DISSOLVE_OBJECT
    o.uv_DissolveTex = TRANSFORM_TEX(v.texcoord, _DissolveTex);
#endif
    return o;
}

// Some shaders only need to know which pixels are being rendered or not, this skips the color calculations
inline void SuberspectiveClipOnly(SuberspectiveV2F i) {
    float2 uv_DimensionMask = float2(0,0);
    float2 uv_DissolveTex = float2(0,0);
#ifdef DIMENSION_OBJECT
    uv_DimensionMask = i.clipPos;
#endif
#ifdef DISSOLVE_OBJECT
    uv_DissolveTex = i.uv_DissolveTex;
#endif
    SuberspectiveClipOnly(uv_DimensionMask, uv_DissolveTex, i.worldPos);
}

inline void SuberspectiveRender(SuberspectiveV2F i, inout float4 color, inout half emissionEnabled) {
    float2 uv_DimensionMask = float2(0,0);
    float2 uv_DissolveTex = float2(0,0);
#ifdef DIMENSION_OBJECT
    uv_DimensionMask = i.clipPos;
#endif
#ifdef DISSOLVE_OBJECT
    uv_DissolveTex = i.uv_DissolveTex;
#endif

    SuberspectiveRender(uv_DimensionMask, uv_DissolveTex, i.worldPos, color, emissionEnabled);
}

#endif
