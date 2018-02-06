// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Custom/CustomDepthNormalsTexture" {
Properties {
    _MainTex ("", 2D) = "white" {}
}

CGINCLUDE
#include "UnityCG.cginc"

sampler2D _DiscardTex1;
sampler2D _DiscardTex2;
float _ResolutionX;
float _ResolutionY;

struct ObscuredObjectFragIn {
    float4 pos : SV_POSITION;
    float4 nz : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

float4 SampleDiscardPixel1(ObscuredObjectFragIn i) {
	float2 viewportVertex = float2(i.pos.x / _ResolutionX, i.pos.y / _ResolutionY);
	return tex2D(_DiscardTex1, viewportVertex);
}
float4 SampleDiscardPixel2(ObscuredObjectFragIn i) {
	float2 viewportVertex = float2(i.pos.x / _ResolutionX, i.pos.y / _ResolutionY);
	return tex2D(_DiscardTex2, viewportVertex);
}

ObscuredObjectFragIn ObscuredObjectVert( appdata_base v ) {
    ObscuredObjectFragIn o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.nz.xyz = COMPUTE_VIEW_NORMAL;
    o.nz.w = COMPUTE_DEPTH_01;
    return o;
}
// DISCARD TEXTURE 1
fixed4 GminusR1_frag(ObscuredObjectFragIn i) : SV_Target {
	float4 samplePixel = SampleDiscardPixel1(i);
	clip(samplePixel.g-samplePixel.r);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
fixed4 GminusB1_frag(ObscuredObjectFragIn i) : SV_Target {
	float4 samplePixel = SampleDiscardPixel1(i);
	clip(samplePixel.g-samplePixel.b);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
fixed4 RminusG1_frag(ObscuredObjectFragIn i) : SV_Target {
	float4 samplePixel = SampleDiscardPixel1(i);
	clip(samplePixel.r-samplePixel.g);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
fixed4 RminusB1_frag(ObscuredObjectFragIn i) : SV_Target {
	float4 samplePixel = SampleDiscardPixel1(i);
	clip(samplePixel.r-samplePixel.b);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
fixed4 BminusR1_frag(ObscuredObjectFragIn i) : SV_Target {
	float4 samplePixel = SampleDiscardPixel1(i);
	clip(samplePixel.b-samplePixel.r);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
fixed4 BminusG1_frag(ObscuredObjectFragIn i) : SV_Target {
	float4 samplePixel = SampleDiscardPixel1(i);
	clip(samplePixel.b-samplePixel.g);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
// DISCARD TEXTURE 2
fixed4 GminusR2_frag(ObscuredObjectFragIn i) : SV_Target {
	float4 samplePixel = SampleDiscardPixel2(i);
	clip(samplePixel.g-samplePixel.r);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
fixed4 GminusB2_frag(ObscuredObjectFragIn i) : SV_Target {
	float4 samplePixel = SampleDiscardPixel2(i);
	clip(samplePixel.g-samplePixel.b);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
fixed4 RminusG2_frag(ObscuredObjectFragIn i) : SV_Target {
	float4 samplePixel = SampleDiscardPixel2(i);
	clip(samplePixel.r-samplePixel.g);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
fixed4 RminusB2_frag(ObscuredObjectFragIn i) : SV_Target {
	float4 samplePixel = SampleDiscardPixel2(i);
	clip(samplePixel.r-samplePixel.b);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
fixed4 BminusR2_frag(ObscuredObjectFragIn i) : SV_Target {
	float4 samplePixel = SampleDiscardPixel2(i);
	clip(samplePixel.b-samplePixel.r);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}
fixed4 BminusG2_frag(ObscuredObjectFragIn i) : SV_Target {
	float4 samplePixel = SampleDiscardPixel2(i);
	clip(samplePixel.b-samplePixel.g);
    return EncodeDepthNormal (i.nz.w, i.nz.xyz);
}

ENDCG


// DISCARD TEXTURE 1 SUBSHADERS
SubShader {
    Tags { "RenderType"="GreenMinusRed1" }
    Pass {
CGPROGRAM
#pragma vertex ObscuredObjectVert
#pragma fragment GminusR1_frag
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="GreenMinusBlue1" }
    Pass {
CGPROGRAM
#pragma vertex ObscuredObjectVert
#pragma fragment GminusB1_frag
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="RedMinusGreen1" }
    Pass {
CGPROGRAM
#pragma vertex ObscuredObjectVert
#pragma fragment RminusG1_frag
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="RedMinusBlue1" }
    Pass {
CGPROGRAM
#pragma vertex ObscuredObjectVert
#pragma fragment RminusB1_frag
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="BlueMinusRed1" }
    Pass {
CGPROGRAM
#pragma vertex ObscuredObjectVert
#pragma fragment BminusR1_frag
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="BlueMinusGreen1" }
    Pass {
CGPROGRAM
#pragma vertex ObscuredObjectVert
#pragma fragment BminusG1_frag
ENDCG
    }
}


// DISCARD TEXTURE 2 SUBSHADERS
SubShader {
    Tags { "RenderType"="GreenMinusRed2" }
    Pass {
CGPROGRAM
#pragma vertex ObscuredObjectVert
#pragma fragment GminusR2_frag
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="GreenMinusBlue2" }
    Pass {
CGPROGRAM
#pragma vertex ObscuredObjectVert
#pragma fragment GminusB2_frag
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="RedMinusGreen2" }
    Pass {
CGPROGRAM
#pragma vertex ObscuredObjectVert
#pragma fragment RminusG2_frag
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="RedMinusBlue2" }
    Pass {
CGPROGRAM
#pragma vertex ObscuredObjectVert
#pragma fragment RminusB2_frag
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="BlueMinusRed2" }
    Pass {
CGPROGRAM
#pragma vertex ObscuredObjectVert
#pragma fragment BminusR2_frag
ENDCG
    }
}
SubShader {
    Tags { "RenderType"="BlueMinusGreen2" }
    Pass {
CGPROGRAM
#pragma vertex ObscuredObjectVert
#pragma fragment BminusG2_frag
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
