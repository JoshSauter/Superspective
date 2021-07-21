Shader "Hidden/PortalMask"
{
	    SubShader
    {
		Tags { "RenderType" = "Opaque" "Queue" = "Transparent" "PortalTag" = "Standard" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			
			// Depth + Normal texture information, gathered from Unity's built-in global shader variables
			sampler2D _CameraDepthNormalsTexture;
			half4 _CameraDepthNormalsTexture_ST;

            struct appdata {
                float4 vertex : POSITION;
            };

			struct v2f {
				float4 pos : SV_POSITION;
				float4 nz : TEXCOORD0;
				float4 worldPos : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

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

			// Only write the object's depth to the DepthNormalsTexture, leave the normal at whatever value it was
			fixed4 frag(v2f i) : SV_Target {
				fixed4 sample = tex2D(_CameraDepthNormalsTexture, i.pos);
				float sampleDepthValue;
				float3 sampleNormalValue;
				DecodeDepthNormal(sample, sampleDepthValue, sampleNormalValue);
				
				//clip(.999 - sampleDepthValue);
				i.nz.w = clamp(i.nz.w, 0, .999);
				return fixed4(i.nz.w, i.nz.w, i.nz.w, 1);
				return EncodeDepthNormal (i.nz.w, i.nz.xyz);
			}
            ENDCG
        }
    }
		    SubShader
    {
		Tags { "RenderType" = "Opaque" "Queue" = "Transparent" "PortalTag" = "HideDepthNormal" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			
			// Depth + Normal texture information, gathered from Unity's built-in global shader variables
			sampler2D _CameraDepthNormalsTexture;
			half4 _CameraDepthNormalsTexture_ST;

            struct appdata {
                float4 vertex : POSITION;
            };

			struct v2f {
				float4 pos : SV_POSITION;
				float4 nz : TEXCOORD0;
				float4 worldPos : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

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

			// Only write the object's depth to the DepthNormalsTexture, leave the normal at whatever value it was
			fixed4 frag(v2f i) : SV_Target {
				fixed4 sample = tex2D(_CameraDepthNormalsTexture, i.pos);
				float sampleDepthValue;
				float3 sampleNormalValue;
				DecodeDepthNormal(sample, sampleDepthValue, sampleNormalValue);
				
				//clip(.999 - sampleDepthValue);
				return fixed4(1,1,1,1);
			}
            ENDCG
        }
    }
    SubShader
    {
		Tags { "RenderType" = "Opaque" "Queue" = "Transparent" "PortalTag" = "Dimension" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			#include "DimensionShaders/DimensionShaderHelpers.cginc"
			
			// Depth + Normal texture information, gathered from Unity's built-in global shader variables
			sampler2D _CameraDepthNormalsTexture;
			half4 _CameraDepthNormalsTexture_ST;

            struct appdata {
                float4 vertex : POSITION;
            };

			struct v2f {
				float4 pos : SV_POSITION;
				float4 nz : TEXCOORD0;
				float4 worldPos : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

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

			// Only write the object's depth to the DepthNormalsTexture, leave the normal at whatever value it was
			fixed4 frag(v2f i) : SV_Target {
				ClipDimensionObject(i.pos);
				fixed4 sample = tex2D(_CameraDepthNormalsTexture, i.pos);
				float sampleDepthValue;
				float3 sampleNormalValue;
				DecodeDepthNormal(sample, sampleDepthValue, sampleNormalValue);

				i.nz.w = clamp(i.nz.w, 0, .999);
				return fixed4(i.nz.w, i.nz.w, i.nz.w, 1);
				return EncodeDepthNormal (i.nz.w, i.nz.xyz);
			}
            ENDCG
        }
    }
	Fallback "Custom/CustomDepthNormalsTexture"
}