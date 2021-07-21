// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Custom/CustomDepthNormalsTexture" {
Properties {
    _MainTex ("", 2D) = "white" {}
}

CGINCLUDE
#include "UnityCG.cginc"
ENDCG

SubShader {
	Tags { "RenderType"="HideDepthNormal"}
	Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
struct v2f {
	float4 pos : SV_POSITION;
	float4 nz : TEXCOORD0;
};
v2f vert(appdata_base v) {
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.nz.xyz = float3(1,0,0);
	o.nz.w = .99999;
	return o;
}
fixed4 frag(v2f i) : SV_Target {
	return EncodeDepthNormal(i.nz.w, i.nz.xyz);
}
ENDCG
	}
}
SubShader {
    Tags { "RenderType"="DimensionDissolveDoubleSided" }
    Pass {

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "DimensionShaders/DimensionShaderHelpers.cginc"

fixed4 _Color;
fixed4 _Color2;
sampler2D _MainTex;
float4 _MainTex_ST;
sampler2D _BumpMap;
sampler2D _BurnRamp;
fixed4 _BurnColor;
float _BurnSize;
float _DissolveValue;

struct v2f {
    float4 pos : SV_POSITION;
	float2 texcoord : TEXCOORD0;
    float4 nz : TEXCOORD1;
	float4 worldPos : TEXCOORD2;
    UNITY_VERTEX_OUTPUT_STEREO
};
float rand(float2 co){
    return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
}
v2f vert( appdata_full v ) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
	o.worldPos = mul(unity_ObjectToWorld, v.vertex);
	o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
    o.nz.xyz = COMPUTE_VIEW_NORMAL;
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}

fixed4 frag(v2f i) : SV_Target {
	if (i.nz.w > 1) i.nz.w = 1;
	ClipDimensionObject(i.pos.xy);

    half test = tex2D(_MainTex, i.texcoord.xy).rgb - _DissolveValue;
	if (_Color.a == 0) clip(-test);
	if (_Color2.a == 0) clip(test);

	if (test < 0) {
		if (_Color2.a == 0) clip(-1);
	}
	else {
		if (_Color.a == 0) clip(-1);
	}

    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="DissolveDoubleSided" }
    Pass {

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

fixed4 _Color;
fixed4 _Color2;
sampler2D _MainTex;
float4 _MainTex_ST;
sampler2D _BumpMap;
sampler2D _BurnRamp;
fixed4 _BurnColor;
float _BurnSize;
float _DissolveValue;

struct v2f {
    float4 pos : SV_POSITION;
	float2 texcoord : TEXCOORD0;
    float4 nz : TEXCOORD1;
	float4 worldPos : TEXCOORD2;
    UNITY_VERTEX_OUTPUT_STEREO
};
float rand(float2 co){
    return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
}
v2f vert( appdata_full v ) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
	o.worldPos = mul(unity_ObjectToWorld, v.vertex);
	o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
    o.nz.xyz = COMPUTE_VIEW_NORMAL;
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}

fixed4 frag(v2f i) : SV_Target {
	if (i.nz.w > 1) i.nz.w = 1;

    half test = tex2D(_MainTex, i.texcoord.xy).rgb - _DissolveValue;
	if (_Color.a == 0) clip(-test);
	if (_Color2.a == 0) clip(test);

	if (test < 0) {
		if (_Color2.a == 0) clip(-1);
	}
	else {
		if (_Color.a == 0) clip(-1);
	}

    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="PortalMaterial" }
    Pass {

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

uniform sampler2D _DepthNormals;

struct appdata {
	float4 vertex : POSITION;
};

struct v2f {
	float4 vertex : SV_POSITION;
	float4 screenPos : TEXCOORD0;
};

v2f vert(appdata v) {
	v2f o;
	o.vertex = UnityObjectToClipPos(v.vertex);
	o.screenPos = ComputeScreenPos(o.vertex);
	return o;
}

fixed4 frag(v2f i) : SV_Target {
	float2 uv = i.screenPos.xy / i.screenPos.w;

	fixed4 col = tex2D(_DepthNormals, uv);
    if (length(col) == 0) {
     clip(-1);
	}
	return col;
}
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="DimensionPortalMaterial" }
    Pass {

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "DimensionShaders/DimensionShaderHelpers.cginc"

uniform sampler2D _DepthNormals;

struct appdata {
	float4 vertex : POSITION;
};

struct v2f {
	float4 vertex : SV_POSITION;
	float4 screenPos : TEXCOORD0;
};

v2f vert(appdata v) {
	v2f o;
	o.vertex = UnityObjectToClipPos(v.vertex);
	o.screenPos = ComputeScreenPos(o.vertex);
	return o;
}

fixed4 frag(v2f i) : SV_Target {
    ClipDimensionObjectFromScreenSpaceCoords(i.screenPos);
	float2 uv = i.screenPos.xy / i.screenPos.w;

	fixed4 col = tex2D(_DepthNormals, uv);
    if (length(col) == 0) {
     clip(-1);
	}
	return col;
}
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="PortalCopy" }
    Pass {

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

float3 _PortalPos;
float3 _PortalNormal;

struct v2f {
    float4 pos : SV_POSITION;
    float4 nz : TEXCOORD0;
	float4 worldPos : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};
float rand(float2 co){
    return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
}
v2f vert( appdata_full v ) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
	o.worldPos = mul(unity_ObjectToWorld, v.vertex);
    o.nz.xyz = COMPUTE_VIEW_NORMAL;
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}

fixed4 frag(v2f i) : SV_Target {
	if (i.nz.w > 1) i.nz.w = 1;
	
	float clipTest = -dot(i.worldPos - _PortalPos - _PortalNormal * 0.02, _PortalNormal);
	if (clipTest == 0) clipTest = -1;
	clip(clipTest);

    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="Dissolve" }
    Pass {

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_ST;
sampler2D _BumpMap;
sampler2D _BurnRamp;
fixed4 _BurnColor;
float _BurnSize;
float _DissolveValue;

struct v2f {
    float4 pos : SV_POSITION;
	float2 texcoord : TEXCOORD0;
    float4 nz : TEXCOORD1;
	float4 worldPos : TEXCOORD2;
    UNITY_VERTEX_OUTPUT_STEREO
};
float rand(float2 co){
    return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
}
v2f vert( appdata_full v ) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
	o.worldPos = mul(unity_ObjectToWorld, v.vertex);
	o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
    o.nz.xyz = COMPUTE_VIEW_NORMAL;
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}

fixed4 frag(v2f i) : SV_Target {
	if (i.nz.w > 1) i.nz.w = 1;

	half test = tex2D(_MainTex, i.texcoord.xy).rgb - _DissolveValue;
	clip(test);

    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
    }
}
// New partially visible objects
SubShader {
    Tags { "RenderType"="DimensionObject" }
    Pass {

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "DimensionShaders/DimensionShaderHelpers.cginc"

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

fixed4 frag(v2f i) : SV_Target {
	if (i.nz.w > 1) i.nz.w = 1;
	ClipDimensionObject(i.pos.xy);

    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
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

fixed4 frag(v2f i) : SV_Target {
	i.nz.w = clamp(i.nz.w, 0, .999);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
    }
}

SubShader {
    Tags { "RenderType"="PortalSurface" }
    Pass {
ColorMask BA
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

fixed4 frag(v2f i) : SV_Target {
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
struct v2f {
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 nz : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};
uniform float4 _MainTex_ST;
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
uniform sampler2D _MainTex;
uniform fixed _Cutoff;
uniform fixed4 _Color;
fixed4 frag(v2f i) : SV_Target {
    fixed4 texcol = tex2D( _MainTex, i.uv );
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
uniform float4 _MainTex_ST;
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
uniform sampler2D _MainTex;
uniform fixed _Cutoff;
uniform fixed4 _Color;
fixed4 frag(v2f i) : SV_Target {
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
fixed4 frag( v2f i ) : SV_Target {
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
uniform sampler2D _MainTex;
uniform fixed _Cutoff;
fixed4 frag( v2f i ) : SV_Target {
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
    fixed4 color : COLOR;
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
fixed4 frag(v2f i) : SV_Target {
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
    fixed4 color : COLOR;
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
uniform sampler2D _MainTex;
uniform fixed _Cutoff;
fixed4 frag(v2f i) : SV_Target {
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
    fixed4 color : COLOR;
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
uniform sampler2D _MainTex;
uniform fixed _Cutoff;
fixed4 frag(v2f i) : SV_Target {
    fixed4 texcol = tex2D( _MainTex, i.uv );
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
uniform sampler2D _MainTex;
fixed4 frag(v2f i) : SV_Target {
    fixed4 texcol = tex2D( _MainTex, i.uv );
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
    fixed4 color : COLOR;
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
uniform sampler2D _MainTex;
uniform fixed _Cutoff;
fixed4 frag(v2f i) : SV_Target {
    fixed4 texcol = tex2D( _MainTex, i.uv );
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
    fixed4 color : COLOR;
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
uniform sampler2D _MainTex;
uniform fixed _Cutoff;
fixed4 frag(v2f i) : SV_Target {
    fixed4 texcol = tex2D( _MainTex, i.uv );
    fixed alpha = texcol.a * i.color.a;
    clip( alpha - _Cutoff );
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
ENDCG
    }
}
Fallback Off
}
