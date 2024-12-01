// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Custom/CustomDepthNormalsTexture" {
Properties {
    _MainTex ("", 2D) = "white" {}
}

CGINCLUDE
#include "UnityCG.cginc"
#define UNITY_BUILT_IN_COLOR
#include "Suberspective/SuberspectiveHelpers.cginc"

struct SuberspectiveDepthNormalsV2F {
	float4 clipPos : SV_POSITION;
	float4 nz : TEXCOORD0;
	float4 screenPos : TEXCOORD1;
	float3 worldPos : TEXCOORD2;
	float2 uv : TEXCOORD3;
#ifdef DISSOLVE_OBJECT
	float2 dissolveUV : TEXCOORD6;
#endif
};

SuberspectiveDepthNormalsV2F SuberspectiveDepthNormalsVert(appdata_base v) {
	SuberspectiveDepthNormalsV2F o;
	o.clipPos = UnityObjectToClipPos(v.vertex);
	o.screenPos = ComputeScreenPos(o.clipPos);
	o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
#ifdef DISSOLVE_OBJECT
	o.dissolveUV = TRANSFORM_TEX(v.texcoord, _DissolveTex);
#endif
    
    o.nz.xyz = COMPUTE_VIEW_NORMAL;
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}

// Copied from SuberspectiveHelpers.cginc
// Some shaders only need to know which pixels are being rendered or not, this skips the color calculations
inline void SuberspectiveClipOnly(SuberspectiveDepthNormalsV2F i) {
#ifdef DIMENSION_OBJECT
	ClipDimensionObject(i.clipPos);
#endif
#ifdef DISSOLVE_OBJECT
	ClipDissolve(i.dissolveUV);
#endif
#ifdef POWER_TRAIL_OBJECT
    ClipPowerTrail(i.worldPos);
#endif
#ifdef SHUTTERED_OBJECT
	ClipShutteredAreas(i.worldPos, float4(1,1,1,1));
#endif
#ifdef PORTAL_COPY_OBJECT
	ClipPortalCopy(i.worldPos);
#endif
#ifdef RENDER_IN_ZONE_OBJECT
	ClipRenderZone(i.worldPos);
#endif
}

float4 SuberspectiveDepthNormalsFrag(SuberspectiveDepthNormalsV2F i) : SV_Target {
	if (i.nz.w > 1) i.nz.w = .999999;
    SuberspectiveClipOnly(i);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}

ENDCG

////////////////////////////////
// Suberspective uber shaders //
////////////////////////////////
SubShader {
    Tags { "RenderType"="Suberspective" }
    Pass {
Cull[__CullMode]
CGPROGRAM
#pragma vertex SuberspectiveDepthNormalsVert
#pragma fragment SuberspectiveDepthNormalsFrag
ENDCG
    }
}
SubShader {
	Tags { "RenderType"="CullEverything"}
	Pass {
CGPROGRAM
#pragma vertex SuberspectiveCullEverythingVert
#pragma fragment SuberspectiveDepthNormalsFrag

SuberspectiveDepthNormalsV2F SuberspectiveCullEverythingVert(appdata_base v) {
	SuberspectiveDepthNormalsV2F o;
	o.clipPos = UnityObjectToClipPos(v.vertex);
    o.screenPos = ComputeScreenPos(o.clipPos);
	o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
#ifdef DISSOLVE_OBJECT
	o.dissolveUV = TRANSFORM_TEX(v.texcoord, _DissolveTex);
#endif
    
	o.nz.xyz = float3(1,0,0);
	o.nz.w = .99999;
    return o;
}
ENDCG
	}
}
SubShader {
    Tags { "RenderType"="PortalMaterial" }
    Pass {

CGPROGRAM
#pragma vertex SuberspectiveDepthNormalsVert
#pragma fragment SuberspectivePortalDepthNormalsFrag

uniform sampler2D_float _DepthNormals;

float4 SuberspectivePortalDepthNormalsFrag(SuberspectiveDepthNormalsV2F i) : SV_Target {
	float2 uv = i.screenPos.xy / i.screenPos.w;
    SuberspectiveClipOnly(i);

	float4 col = tex2D(_DepthNormals, uv);
    if (length(col) == 0) {
     clip(-1);
	}
	return col;
}
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="Opaque" }
    Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

struct v2f {
    float4 pos : SV_POSITION;
    float4 nz : TEXCOORD0;
	float4 worldPos : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};
float rand(float2 co){
    return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
}
v2f vert( appdata_base v ) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
	o.worldPos = mul(unity_ObjectToWorld, v.vertex);
    o.nz.xyz = COMPUTE_VIEW_NORMAL;
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}

float4 frag(v2f i) : SV_Target {
	i.nz.w = clamp(i.nz.w, 0, .999);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
    }
}

SubShader {
    Tags { "RenderType"="TransparentCutout" }
    Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
fixed4 _Color;
struct v2f {
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 nz : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};
v2f vert( appdata_base v ) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
    o.nz.xyz = COMPUTE_VIEW_NORMAL;
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}
uniform fixed _Cutoff;
float4 frag(v2f i) : SV_Target {
    float4 texcol = tex2D( _MainTex, i.uv );
    clip( texcol.a*_Color.a - _Cutoff );
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
}
}

SubShader{
	Tags { "RenderType" = "TransparentWithBorder" }
	Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct v2f {
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	float4 nz : TEXCOORD1;
	float4 vertexPos : TEXCOORD2;
	UNITY_VERTEX_OUTPUT_STEREO
};
v2f vert(appdata_base v) {
	v2f o;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	o.pos = UnityObjectToClipPos(v.vertex);
	o.vertexPos = v.vertex;
	o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
	o.nz.xyz = COMPUTE_VIEW_NORMAL;
	o.nz.w = COMPUTE_DEPTH_01;
	return o;
}
uniform fixed _Cutoff;
float4 frag(v2f i) : SV_Target {
	float3 worldScale = float3(
        length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x)), // scale x axis
        length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y)), // scale y axis
        length(float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z))  // scale z axis
    );
	
	float thresholdBase = .50 - pow(i.nz.w, 2);
	float inset = 0.025;
	half isBorderX = abs(i.vertexPos.x) > thresholdBase - (inset / worldScale.x) ? 1 : 0;
	half isBorderY = abs(i.vertexPos.y) > thresholdBase - (inset / worldScale.y) ? 1 : 0;
	half isBorderZ = abs(i.vertexPos.z) > thresholdBase - (inset / worldScale.z) ? 1 : 0;
	clip(isBorderX + isBorderY + isBorderZ - 1.5);
	return EncodeDepthNormal(i.nz.w, i.nz.xyz);
}
ENDCG
}
}

SubShader {
    Tags { "RenderType"="TreeBark" }
    Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "UnityBuiltin3xTreeLibrary.cginc"
struct v2f {
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 nz : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};
v2f vert( appdata_full v ) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    TreeVertBark(v);

    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.texcoord.xy;
    o.nz.xyz = COMPUTE_VIEW_NORMAL;
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}
float4 frag( v2f i ) : SV_Target {
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
    }
}

SubShader {
    Tags { "RenderType"="TreeLeaf" }
    Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "UnityBuiltin3xTreeLibrary.cginc"
struct v2f {
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 nz : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};
v2f vert( appdata_full v ) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    TreeVertLeaf(v);

    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.texcoord.xy;
    o.nz.xyz = COMPUTE_VIEW_NORMAL;
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}
uniform fixed _Cutoff;
float4 frag( v2f i ) : SV_Target {
    half alpha = tex2D(_MainTex, i.uv).a;

    clip (alpha - _Cutoff);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
    }
}

SubShader {
    Tags { "RenderType"="TreeOpaque" "DisableBatching"="True" }
    Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "TerrainEngine.cginc"
struct v2f {
    float4 pos : SV_POSITION;
    float4 nz : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};
struct appdata {
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
v2f vert( appdata v ) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    TerrainAnimateTree(v.vertex, v.color.w);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.nz.xyz = COMPUTE_VIEW_NORMAL;
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}
float4 frag(v2f i) : SV_Target {
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
    }
}

SubShader {
    Tags { "RenderType"="TreeTransparentCutout" "DisableBatching"="True" }
    Pass {
        Cull Back
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "TerrainEngine.cginc"

struct v2f {
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 nz : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};
struct appdata {
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 color : COLOR;
    float4 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
v2f vert( appdata v ) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    TerrainAnimateTree(v.vertex, v.color.w);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.texcoord.xy;
    o.nz.xyz = COMPUTE_VIEW_NORMAL;
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}
uniform fixed _Cutoff;
float4 frag(v2f i) : SV_Target {
    half alpha = tex2D(_MainTex, i.uv).a;

    clip (alpha - _Cutoff);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
    }
    Pass {
        Cull Front
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "TerrainEngine.cginc"

struct v2f {
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 nz : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};
struct appdata {
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 color : COLOR;
    float4 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
v2f vert( appdata v ) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    TerrainAnimateTree(v.vertex, v.color.w);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.texcoord.xy;
    o.nz.xyz = -COMPUTE_VIEW_NORMAL;
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}
uniform fixed _Cutoff;
float4 frag(v2f i) : SV_Target {
    float4 texcol = tex2D( _MainTex, i.uv );
    clip( texcol.a - _Cutoff );
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
    }

}

SubShader {
    Tags { "RenderType"="TreeBillboard" }
    Pass {
        Cull Off
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "TerrainEngine.cginc"
struct v2f {
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 nz : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};
v2f vert (appdata_tree_billboard v) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    TerrainBillboardTree(v.vertex, v.texcoord1.xy, v.texcoord.y);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv.x = v.texcoord.x;
    o.uv.y = v.texcoord.y > 0;
    o.nz.xyz = float3(0,0,1);
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}
float4 frag(v2f i) : SV_Target {
    float4 texcol = tex2D( _MainTex, i.uv );
    clip( texcol.a - 0.001 );
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
    }
}

SubShader {
    Tags { "RenderType"="GrassBillboard" }
    Pass {
        Cull Off
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "TerrainEngine.cginc"

struct v2f {
    float4 pos : SV_POSITION;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;
    float4 nz : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert (appdata_full v) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    WavingGrassBillboardVert (v);
    o.color = v.color;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.texcoord.xy;
    o.nz.xyz = COMPUTE_VIEW_NORMAL;
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}
uniform fixed _Cutoff;
float4 frag(v2f i) : SV_Target {
    float4 texcol = tex2D( _MainTex, i.uv );
    fixed alpha = texcol.a * i.color.a;
    clip( alpha - _Cutoff );
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
    }
}

SubShader {
    Tags { "RenderType"="Grass" }
    Pass {
        Cull Off
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "TerrainEngine.cginc"
struct v2f {
    float4 pos : SV_POSITION;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;
    float4 nz : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert (appdata_full v) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    WavingGrassVert (v);
    o.color = v.color;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = v.texcoord;
    o.nz.xyz = COMPUTE_VIEW_NORMAL;
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}
uniform fixed _Cutoff;
float4 frag(v2f i) : SV_Target {
    float4 texcol = tex2D( _MainTex, i.uv );
    fixed alpha = texcol.a * i.color.a;
    clip( alpha - _Cutoff );
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
    }
}
Fallback Off
}
