#ifndef SUBERSPECTIVE_INCLUDED
#define SUBERSPECTIVE_INCLUDED


#include "SuberspectiveCommon.cginc"

struct v2f {
	float4 vertex   : SV_POSITION;
	fixed4 color    : COLOR;
	float2 texcoord : TEXCOORD0;
	float3 worldPos : TEXCOORD1;
	UNITY_VERTEX_OUTPUT_STEREO
};

inline void SuberspectiveClipOnly(v2f i) {
	float2 uv_DimensionMask = float2(0,0);
	float2 uv_DissolveTex = float2(0,0);
	#ifdef DIMENSION_OBJECT
	uv_DimensionMask = i.vertex;
	#endif
	#ifdef DISSOLVE_OBJECT
	//uv_DissolveTex = i.uv_DissolveTex;
	#endif
	SuberspectiveClipOnly(uv_DimensionMask, uv_DissolveTex, i.worldPos);
}

#endif