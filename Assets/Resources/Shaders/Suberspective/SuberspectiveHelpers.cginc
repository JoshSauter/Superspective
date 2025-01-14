﻿#ifndef SUBERSPECTIVE_INCLUDED
#define SUBERSPECTIVE_INCLUDED
	
#include "SuberspectiveCommon.cginc"
#include "SuberspectiveUniforms.cginc"

struct SuberspectiveV2F {
	float4 clipPos : SV_POSITION;
	float3 worldPos : TEXCOORD0;
	float4 screenPos : TEXCOORD1;
	float2 uv_MainTex : TEXCOORD2;
	float2 uv_EmissionMap : TEXCOORD3;
	float4 color : COLOR; // vertex color
#ifdef DISSOLVE_OBJECT
	float2 uv_DissolveTex : TEXCOORD4;
#endif
};

SuberspectiveV2F SuberspectiveVert(appdata_full v) {
	SuberspectiveV2F o;
	float3 vertex = v.vertex;
	o.clipPos = UnityObjectToClipPos(vertex);
	o.screenPos = ComputeScreenPos(o.clipPos);
	o.worldPos = mul(unity_ObjectToWorld, float4(vertex.xyz, 1.0)).xyz;
	o.uv_MainTex = TRANSFORM_TEX(v.texcoord, _MainTex);
    o.uv_EmissionMap = TRANSFORM_TEX(v.texcoord, _EmissionMap);
	o.color = v.color;
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

#ifndef DEPTH_NORMALS_TEXTURE
#define DEPTH_NORMALS_TEXTURE
sampler2D_float _CameraDepthNormalsTexture;
#endif

inline float4 DepthColor(float2 screenPos) {
	float depthValue;
	float3 normalValues;
	DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, screenPos), depthValue, normalValues);
	half4 overlayColor;

	overlayColor.r = depthValue;
	overlayColor.g = depthValue;
	overlayColor.b = depthValue;

	overlayColor.a = 1;
	return overlayColor;
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

	if (length(i.color) > 0) {
		color.rgb *= i.color.rgb;
		color.a *= i.color.a;
		color = saturate(color);
	}

	SuberspectiveRender(uv_DimensionMask, uv_DissolveTex, i.worldPos, color, emissionEnabled);
	//color = DepthColor(i.screenPos.xy / i.screenPos.w);
}

#endif
