Shader "Custom/ObscuredObjectShaders/Outline_RedMinusGreen1"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BiasTowardsVisible ("Bias Towards Visible", Range(-1.0, 1.0)) = 0
		_Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_Outline ("Outline width", Range (0.0, 1)) = .005
	}

CGINCLUDE
#include "UnityCG.cginc"
	
sampler2D _MainTex;
sampler2D _DiscardTex1;
float _BiasTowardsVisible;
float4 _Color;
float4 _MainTex_ST;

float _ResolutionX;
float _ResolutionY;
			
uniform float _Outline;
uniform float4 _OutlineColor;

struct appdata
{
	// Obscure Shader data
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
	// Outline data
	float3 normal : NORMAL;
};

struct v2f
{
	// Obscure Shader data
	float2 uv : TEXCOORD0;
	UNITY_FOG_COORDS(1)
	float4 vertex : POSITION1;
	// Outline data
	// pos is vertex scaled by normal direction
	float4 pos : POSITION;
	float4 color : COLOR;
};

v2f vert (appdata v)
{
	v2f o;
	// Obscure Shader data
	o.vertex = UnityObjectToClipPos(v.vertex);
	o.uv = TRANSFORM_TEX(v.uv, _MainTex);
	UNITY_TRANSFER_FOG(o,o.vertex);

	// Outline data
	float3 norm   = mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal);
	float2 offset = TransformViewToProjection(norm.xy);
	o.pos = UnityObjectToClipPos(v.vertex);
	o.pos.xy += offset * o.pos.z * _Outline;
	o.color = _OutlineColor;
	return o;
}
ENDCG
	SubShader
	{
		Tags { "Queue"="Geometry" "RenderType"="GreenMinusRed1" }
		LOD 100

		Pass {
			Name "BASE"
			Cull Back
			Blend Zero One
 
			// uncomment this to hide inner details:
			//Offset -8, -8
 
			SetTexture [_OutlineColor] {
				ConstantColor (0,0,0,0)
				Combine constant
			}
		}

		Pass {
			Name "OUTLINE"
			Tags { "LightMode" = "Always" }
			Cull Front
 
			// you can choose what kind of blending mode you want for the outline
			//Blend SrcAlpha OneMinusSrcAlpha // Normal
			//Blend One One // Additive
			Blend One OneMinusDstColor // Soft Additive
			//Blend DstColor Zero // Multiplicative
			//Blend DstColor SrcColor // 2x Multiplicative
 
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
 
			half4 frag(v2f i) : COLOR {
				float4 col = i.color;
				float2 viewportVertex = float2(i.vertex.x / _ResolutionX, i.vertex.y / _ResolutionY);
				float4 samplePixel = tex2D(_DiscardTex1, viewportVertex);
				col.a = _BiasTowardsVisible + samplePixel.r - samplePixel.g;
				clip(col.a);
				return i.color;
			}
			ENDCG
		}
	}
	
	Fallback "VertexLit"
}
