// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/DepthNormalOverlay" {
	Properties {
		_MainTex ("", 2D) = "white" {}
	}

	// Common Shader code
	CGINCLUDE
	#include "UnityCG.cginc"

	sampler2D _CameraDepthNormalsTexture;

	struct v2f {
		float4 pos : SV_POSITION;
		float4 scrPos : TEXCOORD1;
	};

	// Common vertex Shader
	v2f vert (appdata_base v){
		v2f o;
		o.pos = UnityObjectToClipPos (v.vertex);
		o.scrPos = ComputeScreenPos(o.pos);
		return o;
	}
ENDCG

	SubShader {
		Tags { "RenderType"="Opaque" }

		// Depth Render Pass
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			// Fragment Shader
			half4 frag (v2f i) : COLOR {
				float depthValue;
				float3 normalValues;
				DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.scrPos.xy), depthValue, normalValues);
				half4 overlayColor;

				overlayColor.r = depthValue;
				overlayColor.g = depthValue;
				overlayColor.b = depthValue;

				overlayColor.a = 1;
				return overlayColor;
			}
			ENDCG
		}
		// Normal Render Pass
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			// Fragment Shader
			half4 frag (v2f i) : COLOR {
				float depthValue;
				float3 normalValues;
				DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.scrPos.xy), depthValue, normalValues);
				half4 overlayColor;

				overlayColor.rgb = normalValues;
				overlayColor.a = 1;

				return overlayColor;
			}
			ENDCG
		}
		// FresnelNormal Render Pass
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			// Fragment Shader
			half4 frag (v2f i) : COLOR {
				float depthValue;
				float3 normalValues;
				DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.scrPos.xy), depthValue, normalValues);
				half4 overlayColor;

				// get the perspective projection
                float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
                // conver the uvs into view space by "undoing" projection
                float3 viewDir = -normalize(float3((i.scrPos * 2 - 1) / p11_22, -1));
 
                float fresnel = 1.0 - dot(viewDir.xyz, normalValues);
 
                return float4(fresnel, fresnel, fresnel, 1);
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}