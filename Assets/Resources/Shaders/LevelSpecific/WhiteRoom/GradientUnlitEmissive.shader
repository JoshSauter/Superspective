Shader "Custom/GradientUnlitEmissive"
{
	Properties
	{
		_Power ("Amount powered (-1 - 1)", Float) = 0
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
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float t : TEXCOORD0;
			};
			
			#define GRADIENT_RESOLUTION 10	// 10 == MaxNumberOfKeysSetInGradient + 1 (for keyTime of 0) + 1 (for keyTime of 1)

			int _ColorGradientMode;			// 0 == Blend, 1 == Fixed
			float _AlphaGradientKeyTimes[GRADIENT_RESOLUTION];
			float _AlphaGradient[GRADIENT_RESOLUTION];
			float _ColorGradientKeyTimes[GRADIENT_RESOLUTION];
			fixed4 _ColorGradient[GRADIENT_RESOLUTION];

			int _EmissionColorGradientMode;			// 0 == Blend, 1 == Fixed
			float _EmissionAlphaGradientKeyTimes[GRADIENT_RESOLUTION];
			float _EmissionAlphaGradient[GRADIENT_RESOLUTION];
			float _EmissionColorGradientKeyTimes[GRADIENT_RESOLUTION];
			fixed4 _EmissionColorGradient[GRADIENT_RESOLUTION];

			float _Power;
			
			float4 GradientColor(float t) {
				float4 colorChosen = float4(0,0,0,0);
				float alphaValue = 0;
				t = saturate(t);
				for (int i = 1; i < GRADIENT_RESOLUTION; i++) {
					// inRange is equivalent to "if (t > _ColorGradientKeyTimes[i-1] && t <= _ColorGradientKeyTimes[i])"
					int colorInRange = (1-step(t, _ColorGradientKeyTimes[i-1])) * step(t, _ColorGradientKeyTimes[i]);
					int alphaInRange = (1-step(t, _AlphaGradientKeyTimes[i-1])) * step(t, _AlphaGradientKeyTimes[i]);
					float colorLerpValue = saturate((t - _ColorGradientKeyTimes[i-1]) / (_ColorGradientKeyTimes[i] - _ColorGradientKeyTimes[i-1]) + _ColorGradientMode);
					float alphaLerpValue = saturate((t - _AlphaGradientKeyTimes[i-1]) / (_AlphaGradientKeyTimes[i] - _AlphaGradientKeyTimes[i-1]) + _ColorGradientMode);

					colorChosen += colorInRange * lerp(_ColorGradient[i-1], _ColorGradient[i], colorLerpValue);
					alphaValue += alphaInRange * lerp(_AlphaGradient[i-1], _AlphaGradient[i], alphaLerpValue);
				}
				colorChosen.a = alphaValue;

				return colorChosen;
			}

			float4 EmissionGradientColor(float t) {
				float4 colorChosen = float4(0,0,0,0);
				float alphaValue = 0;
				t = saturate(t);
				for (int i = 1; i < GRADIENT_RESOLUTION; i++) {
					// inRange is equivalent to "if (t > _EmissionColorGradientKeyTimes[i-1] && t <= _EmissionColorGradientKeyTimes[i])"
					int colorInRange = (1-step(t, _EmissionColorGradientKeyTimes[i-1])) * step(t, _EmissionColorGradientKeyTimes[i]);
					int alphaInRange = (1-step(t, _EmissionAlphaGradientKeyTimes[i-1])) * step(t, _EmissionAlphaGradientKeyTimes[i]);
					float colorLerpValue = saturate((t - _EmissionColorGradientKeyTimes[i-1]) / (_EmissionColorGradientKeyTimes[i] - _EmissionColorGradientKeyTimes[i-1]) + _EmissionColorGradientMode);
					float alphaLerpValue = saturate((t - _EmissionAlphaGradientKeyTimes[i-1]) / (_EmissionAlphaGradientKeyTimes[i] - _EmissionAlphaGradientKeyTimes[i-1]) + _EmissionColorGradientMode);

					colorChosen += colorInRange * lerp(_EmissionColorGradient[i-1], _EmissionColorGradient[i], colorLerpValue);
					alphaValue += alphaInRange * lerp(_EmissionAlphaGradient[i-1], _EmissionAlphaGradient[i], alphaLerpValue);
				}
				colorChosen.a = alphaValue;

				return colorChosen;
			}

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				o.t = v.uv.y;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				// sample the texture
				fixed4 col = GradientColor(i.t);

				// apply fog
				float t = 2*i.t - 1;
				float p = 2*_Power - 1;
				if ((t > 0 && t < p) || (t < 0 && t > p)) {
 					col += EmissionGradientColor(i.t);
				}
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
		// Pass to render object as a shadow caster
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On ZTest LEqual Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"

			struct v2f {
				V2F_SHADOW_CASTER;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert( appdata_base v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag( v2f i ) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}
