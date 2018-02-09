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
				half4 overlayColor;

//				overlayColor.r = test.r;
//				overlayColor.g = test.g;
//				overlayColor.b = test.b;

				overlayColor.rgb = normalValues;
				overlayColor.a = 1;
				return overlayColor;
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}