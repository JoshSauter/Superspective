﻿Shader "Custom/ObjectObscureShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_DiscardTex ("Discard Texture", 2D) = "black" {}
		_Cutoff ("Cutoff", Range(0.0, 1.0)) = 0
		_Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_ResolutionX ("Render Texture X-Resolution", float) = 512
		_ResolutionY ("Render Texture Y-Resolution", float) = 512
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _DiscardTex;
			float _Cutoff;
			float4 _Color;
			float4 _MainTex_ST;

			float _ResolutionX;
			float _ResolutionY;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv) * _Color;
				float2 viewportVertex = float2(i.vertex.x / _ResolutionX, i.vertex.y / _ResolutionY);
				clip(col.a - tex2D(_DiscardTex, viewportVertex).r + tex2D(_DiscardTex, viewportVertex).g - _Cutoff);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}