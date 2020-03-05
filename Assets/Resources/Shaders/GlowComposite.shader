// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/GlowComposite"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_GlowIntensity ("Glow Intensity", Range(0,10)) = 1.0
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

			float _GlowIntensity;

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

			float luminance(fixed4 c) {
				return c.r * .3 + c.g * .59 + c.b * .11;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv0);
				fixed4 maxColor = col;
				fixed4 avgColor = col;

				for (int j = 0; j < 4; j++) {
					fixed4 sample = tex2D(_MainTex, i.cornerSamples[j]);
					avgColor += sample;
				}
				avgColor /= 5.0;
				float avgLuminance = luminance(avgColor);
				//fixed4 glow = maxGlow;
				//float glowLuminance = maxGlowLuminance;
				//fixed4 blur = tex2D(_GlowBlurredTex, i.uv1);
				//fixed4 prepass = tex2D(_GlowPrePassTex, i.uv1);
				fixed4 glow = max(0, tex2D(_GlowBlurredTex, i.uv1) - tex2D(_GlowPrePassTex, i.uv1));

				return lerp(col, glow, min(1, _GlowIntensity*glow.a));
			}
			ENDCG
		}
	}
}
