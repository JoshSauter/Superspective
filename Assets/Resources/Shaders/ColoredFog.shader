Shader "Hidden/ColoredFog"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_FogColorRamp ("Fog color ramp", 2D) = "white" {}
		_FogStartDistance ("Fog start distance", float) = 0
		_FogEndDistance ("Fog end distance", float) = 1
		_FogDensity ("Fog density", float) = 1
		_FogExponent ("Fog exponentiality", int) = 2
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 interpolatedRay : TEXCOORD1;
			};
		
			sampler2D _MainTex;
			sampler2D_float _CameraDepthTexture;
			float4 _CameraDepthTexture_ST;
			sampler2D _FogColorRamp;
			float _FogStartDistance;
			float _FogEndDistance;
			float _FogDensity;
			int _FogExponent;

			// for fast world space reconstruction
			uniform float4x4 _FrustumCornersWS;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				int frustumIndex = v.uv.x + (2 * o.uv.y);
				o.interpolatedRay = _FrustumCornersWS[frustumIndex];
				o.interpolatedRay.w = frustumIndex;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 originalColor = tex2D(_MainTex, i.uv);
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);
				float depthValueMul = saturate((depth - _FogStartDistance) / (_FogEndDistance - _FogStartDistance));
				fixed4 fogColor = tex2D(_FogColorRamp, (float2(depthValueMul, 0)));
				//return fixed4 (_FogStartDistance, _FogEndDistance, 0, 1);
				//return fogColor;
				float fogginess = saturate(fogColor.a * _FogDensity * pow(depthValueMul, _FogExponent));
				return lerp(originalColor, fogColor, fogginess);
			}
			ENDCG
		}
	}
}
