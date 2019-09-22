// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/ScreenCutout"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// Tags { "RenderType"="Opaque" "Queue"="Geometry" }
		//Tags { "Queue"="Transparent" }
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "False" "RenderType" = "PortalSurface" }
		Lighting Off
		Cull Back
		ZWrite On
		ZTest Less
		
		Fog{ Mode Off }

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
				//float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD1;
				float2 uvs[1] : TEXCOORD2;
			};
			
			sampler2D _MainTex;

			float _ResolutionX;
			float _ResolutionY;
			sampler2D _CameraDepthNormalsTexture;
			uniform float4 _CameraDepthNormalsTexture_TexelSize;
			half4 _CameraDepthNormalsTexture_ST;
			
			#define DEPTH_THRESHOLD_BASELINE 0.001
			#define DEPTH_THRESHOLD_CONSTANT 0.16
			#define DEPTH_SENSITIVITY 0.07
			half DepthValuesAreSimilar(float a, float b) {
				// Further values have more tolerance for difference in depth values
				float depthThreshold = DEPTH_THRESHOLD_BASELINE + DEPTH_THRESHOLD_CONSTANT * min(a, b);
				float depthDiff = abs(a - b);
				
				// Multiplied by magic number 100 just to keep things from getting too close to underflow levels (DEPTH_THRESHOLD_CONSTANT is 100x higher to compensate)
				int isSameDepth = 100 * depthDiff * DEPTH_SENSITIVITY < depthThreshold;
				return isSameDepth ? 1.0 : 0.0;
			}

			v2f vert (appdata v) {
				const half2 displacements[3] = {
					half2( 0,  0),
					half2( 0,  1),
					half2(-1,  0)
				};
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);

				o.uvs[0] = o.screenPos;

				return o;
			}

			fixed4 frag (v2f i) : SV_Target {
				//return fixed4(0.2, 0.7, 0.4, 1);
				i.screenPos /= i.screenPos.w;
				float depthValue;
				float3 normalValue;
				float depthValues[3];
				float4 sample = tex2D(_CameraDepthNormalsTexture, float2(i.screenPos.r, i.screenPos.g));
				DecodeDepthNormal(sample, depthValue, normalValue);
				depthValues[0] = depthValue;
				sample = tex2D(_CameraDepthNormalsTexture, float2(i.screenPos.r, i.screenPos.g + (1 / _ResolutionY)));
				DecodeDepthNormal(sample, depthValue, normalValue);
				depthValues[1] = depthValue;
				sample = tex2D(_CameraDepthNormalsTexture, float2(i.screenPos.r - (1 / _ResolutionX), i.screenPos.g));
				DecodeDepthNormal(sample, depthValue, normalValue);
				depthValues[2] = depthValue;
				
				//return fixed4(i.screenPos.r, 0, 0, 1);
				//return fixed4(depthValues[1], depthValues[1], depthValues[1], 1);
				if ((DepthValuesAreSimilar(depthValues[0], depthValues[1]) * DepthValuesAreSimilar(depthValues[0], depthValues[2])) == 0) {
					//return fixed4(0, 0.5, 0.7, 1);
					clip(-1);
					//return fixed4(1,0,0,1);
				}
				fixed4 col = tex2D(_MainTex, float2(i.screenPos.x, i.screenPos.y));
				
				return col;
			}
			ENDCG
		}
	}
}
