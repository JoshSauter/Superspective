#ifndef SUBERSPECTIVE_INCLUDED
#define SUBERSPECTIVE_INCLUDED
	
#include "SuberspectiveCommon.cginc"

struct pixel_t {
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
	float4	vertex			: SV_POSITION;
	fixed4	faceColor		: COLOR;
	fixed4	outlineColor	: COLOR1;
	float4	texcoord0		: TEXCOORD0;			// Texture UV, Mask UV
	half4	param			: TEXCOORD1;			// Scale(x), BiasIn(y), BiasOut(z), Bias(w)
	half4	mask			: TEXCOORD2;			// Position in clip space(xy), Softness(zw)
	#if (UNDERLAY_ON | UNDERLAY_INNER)
	float4	texcoord1		: TEXCOORD3;			// Texture UV, alpha, reserved
	half2	underlayParam	: TEXCOORD4;			// Scale(x), Bias(y)
	float3  worldPos		: TEXCOORD5;
	#ifdef DISSOLVE_OBJECT
	float2 uv_DissolveTex	: TEXCOORD6;
	#endif
	#else
	float3  worldPos		: TEXCOORD3;
	#ifdef DISSOLVE_OBJECT
	float2 uv_DissolveTex	: TEXCOORD4;
	#endif
	#endif
};

inline void SuberspectiveClipOnly(pixel_t i) {
	float2 uv_DimensionMask = float2(0,0);
	float2 uv_DissolveTex = float2(0,0);
#ifdef DIMENSION_OBJECT
	uv_DimensionMask = i.vertex;
#endif
#ifdef DISSOLVE_OBJECT
	uv_DissolveTex = i.uv_DissolveTex;
#endif
	SuberspectiveClipOnly(uv_DimensionMask, uv_DissolveTex, i.worldPos);
}

#endif