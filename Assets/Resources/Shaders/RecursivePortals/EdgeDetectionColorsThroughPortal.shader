Shader "Hidden/EdgeDetectionColorsThroughPortal" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
		Tags { "RenderType" = "Opaque" "Queue" = "Transparent" "PortalTag" = "Standard" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            
			#include "../Suberspective/SuberspectiveHelpers.cginc"
            
            #define GRADIENT_RESOLUTION 10	// 10 == MaxNumberOfKeysSetInGradient + 1 (for keyTime of 0) + 1 (for keyTime of 1)

            //sampler2D _CameraDepthNormalsTexture;
            
            // Edge Colors
			int _ColorMode;				// 0 == Simple color, 1 == Gradient from inspector, 2 == Color ramp (gradient) texture
			fixed4 _EdgeColor;
			// Edge gradient
			float3 _FrustumCorners[4];	// Used to convert depth-based gradient to distance-based gradient which doesn't change as camera looks around
			int _GradientMode;			// 0 == Blend, 1 == Fixed
			float _GradientAlphaKeyTimes[GRADIENT_RESOLUTION];
			float _AlphaGradient[GRADIENT_RESOLUTION];
			float _GradientKeyTimes[GRADIENT_RESOLUTION];
			fixed4 _EdgeColorGradient[GRADIENT_RESOLUTION];
			// Edge gradient from texture
			sampler2D _GradientTexture;

            struct v2f {
                float2 uv : TEXCOORD0;
				float4 clipPos : SV_POSITION;
            	float3 ray : TEXCOORD1;
            	float4 screenPos : TEXCOORD2;
                UNITY_FOG_COORDS(1)
            };

            float4 GradientColor(float distance) {
				float4 colorChosen = float4(0,0,0,0);
				float alphaValue = 0;
				distance = saturate(distance);
				for (int i = 1; i < GRADIENT_RESOLUTION; i++) {
					// inRange is equivalent to "if (distance > _GradientKeyTimes[i-1] && distance <= _GradientKeyTimes[i])"
					int colorInRange = (1-step(distance, _GradientKeyTimes[i-1])) * step(distance, _GradientKeyTimes[i]);
					int alphaInRange = (1-step(distance, _GradientAlphaKeyTimes[i-1])) * step(distance, _GradientAlphaKeyTimes[i]);
					float colorLerpValue = saturate((distance - _GradientKeyTimes[i-1]) / (_GradientKeyTimes[i] - _GradientKeyTimes[i-1]) + _GradientMode);
					float alphaLerpValue = saturate((distance - _GradientAlphaKeyTimes[i-1]) / (_GradientAlphaKeyTimes[i] - _GradientAlphaKeyTimes[i-1]) + _GradientMode);

					colorChosen += colorInRange * lerp(_EdgeColorGradient[i-1], _EdgeColorGradient[i], colorLerpValue);
					alphaValue += alphaInRange * lerp(_AlphaGradient[i-1], _AlphaGradient[i], alphaLerpValue);
				}
				colorChosen.a = alphaValue;
            	
				return colorChosen;
			}

			float4 GradientFromTexture(float distance) {
				return tex2D(_GradientTexture, distance);
			}

			fixed4 FinalColor(float depth, float3 ray) {
				fixed4 edgeColor = fixed4(0,0,0,0);
            	// distance doesn't work with Portals for some reason, use depth instead for approximation
				float distance = depth;//saturate((length(ray * depth) - _ProjectionParams.y) / _ProjectionParams.z);
				if (_ColorMode == 0) edgeColor = _EdgeColor;
				if (_ColorMode == 1) edgeColor = GradientColor(distance);
				if (_ColorMode == 2) edgeColor = GradientFromTexture(distance);

				return edgeColor;
			}
            
            v2f vert (appdata_full v) {
                v2f o;
                o.clipPos = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.clipPos);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
				float2 center = MultiplyUV(UNITY_MATRIX_TEXTURE0, v.texcoord);
				o.ray = _FrustumCorners[center.x + 2 * center.y];
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
            	float2 uv = i.screenPos.xy / i.screenPos.w;
				float4 depthNormalSample = tex2D(_CameraDepthNormalsTexture, uv);
				float depthValue;
				float3 normalValue;
				DecodeDepthNormal(depthNormalSample, depthValue, normalValue);

            	//return fixed4(depthValue, depthValue, depthValue, 1);
                fixed4 col = FinalColor(depthValue, i.ray);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}