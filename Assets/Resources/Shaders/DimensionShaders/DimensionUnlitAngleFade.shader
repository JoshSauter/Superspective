Shader "Custom/DimensionShaders/DimensionUnlitAngleFade" {
	Properties {
		_Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Color2 ("Main Color 2", Color) = (0.0, 0.0, 0.0, 1.0)
		[HDR]
		_EmissionColor("Emissive Color", Color) = (0, 0, 0, 0)
		[HDR]
		_EmissionColor2("Emissive Color 2", Color) = (0, 0, 0, 0)
	}
	SubShader {
		Tags { "RenderType"="DimensionObject" }
		LOD 100

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "DimensionShaderHelpers.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD0;
			};

			float4 _Color, _Color2;
			float4 _EmissionColor, _EmissionColor2;
			float3 _ColorChangePoint;
			float3 _ColorChangeAxis;
			float _ColorChangeWidth;
			float3 _ZeroDegreesVector, _NinetyDegreesVector;
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			float inverseLerp(float a, float b, float value) {
				return (value - a) / (b - a);
			}

			float float3InverseLerp(float3 a, float3 b, float3 value) {
				float3 AB = b - a;
				float3 AV = value - a;
				return dot(AV, AB) / dot(AB, AB);
			}

			float3 proj(float3 p, float3 planePoint, float3 planeNormal) {
				float distance = dot((p - planePoint), planeNormal);
				return p - planeNormal * distance;
			}

			float getLerpValueOfPos(float3 p) {
				float3 projectedPos = proj(p, _ColorChangePoint, normalize(_ColorChangeAxis));
				float actualLerpPos = float3InverseLerp(normalize(_ZeroDegreesVector), normalize(_NinetyDegreesVector), normalize(projectedPos-_ColorChangePoint));

				float startValue = .5 - _ColorChangeWidth/2.0;
				float endValue = .5 + _ColorChangeWidth/2.0;

				return saturate(inverseLerp(startValue, endValue, actualLerpPos));
			}
			
			fixed4 frag (v2f i) : SV_Target {
				ClipDimensionObject(i.vertex);
				float t = getLerpValueOfPos(i.worldPos);

				// sample the texture
				fixed4 col = lerp(_Color, _Color2, t);
				// apply fog
				col += lerp(_EmissionColor, _EmissionColor2, t);
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
