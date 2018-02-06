// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/DepthNormalOverlay" {
	Properties {
		_MainTex ("", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }

		// Depth Render Pass
		Pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _DiscardTex;
			sampler2D _CameraDepthNormalsTexture;

			struct v2f {
				float4 pos : SV_POSITION;
				float4 scrPos:TEXCOORD1;
			};

			//Vertex Shader
			v2f vert (appdata_base v){
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.scrPos=ComputeScreenPos(o.pos);
				return o;
			}

			//Fragment Shader
			half4 frag (v2f i) : COLOR {
				float depthValue;
				float3 normalValues;
				DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.scrPos.xy), depthValue, normalValues);
				depthValue = depthValue;
				half4 depth;

				depth.r = depthValue;
				depth.g = depthValue;
				depth.b = depthValue;

				// depth.rgb = normalValues.xyz;
				depth.a = 1;
				return depth;
			}
			ENDCG
		}
		// Normal Render Pass
		Pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _DiscardTex;
			sampler2D _CameraDepthNormalsTexture;

			struct v2f {
				float4 pos : SV_POSITION;
				float4 scrPos:TEXCOORD1;
			};

			//Vertex Shader
			v2f vert (appdata_base v){
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.scrPos=ComputeScreenPos(o.pos);
				return o;
			}

			//Fragment Shader
			half4 frag (v2f i) : COLOR {
				float depthValue;
				float3 normalValues;
				DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.scrPos.xy), depthValue, normalValues);
				half4 depth;

				// depth.r = depthValue;
				// depth.g = depthValue;
				// depth.b = depthValue;

				depth.rgb = normalValues.xyz;
				depth.a = 1;
				return depth;
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}