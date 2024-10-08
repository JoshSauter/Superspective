﻿Shader "Custom/Projector/ProjectedSurfaceDepthBuffer"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Geometry-4" }
		LOD 100

		CGINCLUDE
		// make fog work
		#pragma multi_compile_fog
			
		#include "UnityCG.cginc"
		struct appdata {
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f {
			float2 uv : TEXCOORD0;
			UNITY_FOG_COORDS(1)
			float4 vertex : SV_POSITION;
		};

		sampler2D _MainTex;
		float4 _MainTex_ST;
			
		v2f vert (appdata v) {
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.uv, _MainTex);
			UNITY_TRANSFER_FOG(o,o.vertex);
			return o;
		}
		fixed4 fragBlack (v2f i) : SV_Target {
			return 0;
		}
		ENDCG
		Pass {
			ZWrite Off

			Stencil {
				Ref 1
				Comp Always
				Pass Replace
			}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragBlack
			ENDCG
		}
	}
}
