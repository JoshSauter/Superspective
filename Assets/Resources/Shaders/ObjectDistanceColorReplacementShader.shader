Shader "Unlit/ObjectDistanceColorReplacementShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_MinDistance ("Min Distance", float) = 0
		_MaxDistance ("Max Distance", float) = 60

		_Gradient0 ("Gradient Color 0", Color) = (1, 1, 1, 1)
		_GradientTime0 ("Gradient Time 0", Range(0.0, 1.0)) = 0
		_Gradient1 ("Gradient Color 1", Color) = (1, 0.84705882352, 0.84705882352, 1)
		_GradientTime1 ("Gradient Time 1", Range(0.0, 1.0)) = 0.06764325
		_Gradient2 ("Gradient Color 2", Color) = (0.96078431372, 1, 0.73725490196, 1)
		_GradientTime2 ("Gradient Time 2", Range(0.0, 1.0)) = 0.1764706
		_Gradient3 ("Gradient Color 3", Color) = (0.2862745098, 1, 0.51372549019, 1)
		_GradientTime3 ("Gradient Time 3", Range(0.0, 1.0)) = 0.3941253
		_Gradient4 ("Gradient Color 4", Color) = (0.10980392156, 0.96470588235, 1, 1)
		_GradientTime4 ("Gradient Time 4", Range(0.0, 1.0)) = 0.5529412
		_Gradient5 ("Gradient Color 5", Color) = (0.03529411764, 0.52156862745, 1, 1)
		_GradientTime5 ("Gradient Time 5", Range(0.0, 1.0)) = 0.7088273
		_Gradient6 ("Gradient Color 6", Color) = (0.41176470588, 0, 0.72156862745, 1)
		_GradientTime6 ("Gradient Time 6", Range(0.0, 1.0)) = 0.8617685
		_Gradient7 ("Gradient Color 7", Color) = (0, 0, 0, 1)
		_GradientTime7 ("Gradient Time 7", Range(0.0, 1.0)) = 1
	}
	SubShader
	{
		Tags {
			"RenderType"="Opaque"
			"ReplacementTag"="Unlit"
		}
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			fixed4 _Gradient0;
			float _GradientTime0;
			fixed4 _Gradient1;
			float _GradientTime1;
			fixed4 _Gradient2;
			float _GradientTime2;
			fixed4 _Gradient3;
			float _GradientTime3;
			fixed4 _Gradient4;
			float _GradientTime4;
			fixed4 _Gradient5;
			float _GradientTime5;
			fixed4 _Gradient6;
			float _GradientTime6;
			fixed4 _Gradient7;
			float _GradientTime7;

			float _MinDistance;
			float _MaxDistance;

			float4 _Color;

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
				float depth : DEPTH;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			fixed4 getColor(v2f v) {
			// Clamp the distance
				float distanceToPlayer = v.depth;
				float time = (distanceToPlayer - 0) / (60 - 0);
				fixed4 lowerBoundCol = fixed4(0,0,0,1);
				fixed4 upperBoundCol = fixed4(0,0,0,1);
				float lowerBound = 0;
				float upperBound = 0;

				// I suck at shader scripting:
				if (time < _GradientTime1) {
					lowerBound = _GradientTime0;
					lowerBoundCol = _Gradient0;
					upperBound = _GradientTime1;
					upperBoundCol = _Gradient1;
				}
				else if (time < _GradientTime2) {
					lowerBound = _GradientTime1;
					lowerBoundCol = _Gradient1;
					upperBound = _GradientTime2;
					upperBoundCol = _Gradient2;
				}
				else if (time < _GradientTime3) {
					lowerBound = _GradientTime2;
					lowerBoundCol = _Gradient2;
					upperBound = _GradientTime3;
					upperBoundCol = _Gradient3;
				}
				else if (time < _GradientTime4) {
					lowerBound = _GradientTime3;
					lowerBoundCol = _Gradient3;
					upperBound = _GradientTime4;
					upperBoundCol = _Gradient4;
				}
				else if (time < _GradientTime5) {
					lowerBound = _GradientTime4;
					lowerBoundCol = _Gradient4;
					upperBound = _GradientTime5;
					upperBoundCol = _Gradient5;
				}
				else if (time < _GradientTime6) {
					lowerBound = _GradientTime5;
					lowerBoundCol = _Gradient5;
					upperBound = _GradientTime6;
					upperBoundCol = _Gradient6;
				}
				else {
					lowerBound = _GradientTime6;
					lowerBoundCol = _Gradient6;
					upperBound = _GradientTime7;
					upperBoundCol = _Gradient7;
				}

				float t = (time - lowerBound) / (upperBound - lowerBound);
				fixed4 newColor = lerp(lowerBoundCol, upperBoundCol, t);

				// sample the texture
				fixed4 col = tex2D(_MainTex, v.uv);
				// apply fog
				UNITY_APPLY_FOG(v.fogCoord, col);
				return lerp(col, newColor, cos(_Time[0])/2);
			}

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				o.depth = -mul(UNITY_MATRIX_MV, v.vertex).z;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float clampedDepth = clamp(0, 60, i.depth);
				float normalizedDepth = (clampedDepth - 0) / (60 - 0);
				float invert = 1 - (normalizedDepth * normalizedDepth);
				return float4(invert, invert, invert, 1) * _Color;
			}

			ENDCG
		}
	}
}
