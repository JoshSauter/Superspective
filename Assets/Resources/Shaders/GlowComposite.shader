// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/GlowComposite"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
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
				float4 vertex : SV_POSITION;
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float2 cornerSamples[4] : TEXCOORD2;
			};
			
			sampler2D _MainTex;
			half4 _MainTex_ST;
			float2 _MainTex_TexelSize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv0 = v.uv;
				o.uv1 = v.uv;

				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					o.uv1.y = 1 - o.uv1.y;
				#endif

				const half2 uvDisplacements[4] = {
					half2(-1, -1),
					half2( 1, -1),
					half2( 1,  1),
					half2(-1,  1)
				};

				for (int i = 0; i < 4; i++) {
					float2 sampleUV = v.uv + _MainTex_TexelSize * 2 * uvDisplacements[i];
					o.cornerSamples[i] = UnityStereoScreenSpaceUVAdjust(sampleUV, _MainTex_ST);
				}

				return o;
			}
			
			sampler2D _GlowPrePassTex;
			sampler2D _GlowBlurredTex;
			sampler2D _TempTex0;
			float _Intensity;

			float luminance(fixed4 c) {
				return c.r * .3 + c.g * .59 + c.b * .11;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv0);
				float maxLuminance = luminance(col);
				fixed4 maxColor = col;
				fixed4 avgColor = col;

				fixed4 glowCol = max(0, tex2D(_GlowBlurredTex, i.uv1) - tex2D(_GlowPrePassTex, i.uv1));
				float maxGlowLuminance = luminance(glowCol);
				fixed4 maxGlow = glowCol;
				for (int j = 0; j < 4; j++) {
					fixed4 sample = tex2D(_MainTex, i.cornerSamples[j]);
					avgColor += sample;
					float sampleLum = luminance(sample);
					if (sampleLum > maxLuminance) {
						maxLuminance = sampleLum;
						maxColor = sample;
					}

					
					fixed4 glowSample = max(0, tex2D(_GlowBlurredTex, i.cornerSamples[j]) - tex2D(_GlowPrePassTex, i.cornerSamples[j]));
					float glowSampleLum = luminance(glowSample);
					if (glowSampleLum > maxGlowLuminance) {
						maxGlowLuminance = glowSampleLum;
						maxGlow = glowSample;
					}
				}
				avgColor /= 5.0;
				float avgLuminance = luminance(avgColor);
				//fixed4 glow = maxGlow;
				//float glowLuminance = maxGlowLuminance;
				fixed4 glow = max(0, tex2D(_GlowBlurredTex, i.uv1) - tex2D(_GlowPrePassTex, i.uv1));
				float glowLuminance = luminance(glow);

				fixed4 darkGlow =  -glowLuminance * fixed4(1,1,1,1) * 10 *_Intensity;
				fixed4 brightGlow = glowLuminance * fixed4(1,1,1,1) * 10 *_Intensity;

				//return maxGlow;
				//return darkGlow;
				//return glowLuminance * fixed4(1,1,1,1);
				//return glow;
				//return col + glow * _Intensity;
				//fixed4 finalColor = lerp(col, maxColor, length(2*i.uv0-1) - 0.5);
				fixed4 glowColor = step(.5, maxLuminance) * darkGlow + (1-step(.5, maxLuminance)) * brightGlow;

				//return lerp(col, glowColor, glowLuminance);
				return lerp(col, col + glowColor, glowLuminance);
				//return col + length(glow) * fixed4(1,1,1,1) * -_Intensity;
				//return glow * _Intensity;
				//return col - length(glow) * fixed4(1,1,1,1) * -_Intensity;
			}
			ENDCG
		}
	}
}
