Shader "Custom/InvertColorsObjectDissolve"
{
	Properties
	{
        _MainTex("Dissolve Texture (RGB)", 2D) = "white" {}
        _DissolveAmount("Dissolve Amount", Range(0.0, 1.0)) = 0
 
        _BurnSize("Burn Size", Range(0.0, 1.0)) = 0.15
        _BurnRamp("Burn Ramp (RGB)", 2D) = "white" {}
        _BurnColor("Burn Color", Color) = (1,1,1,1)
 
        _EmissionAmount("Emission amount", float) = 2.0
	}
	SubShader
	{
		BlendOp Sub
		Blend One One
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _BumpMap;
			sampler2D _BurnRamp;
			fixed4 _BurnColor;
			float _BurnSize;
			float _DissolveAmount;
			float _EmissionAmount;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
			};
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				half test = tex2D(_MainTex, i.uv.xy).rgb - _DissolveAmount;
				clip(test);
				return fixed4(1,1,1,1);
			}
			ENDCG
		}
	}

	Fallback "VertexLit"
}
