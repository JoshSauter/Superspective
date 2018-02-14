Shader "Custom/Overlay/PencilSketchOverlay"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_SketchTex ("Sketch Texture", 2D) = "white" {}
		_Intensity ("Intensity", Range(0.0, 1.0)) = 0.3
	}
	SubShader
	{
		// No culling or depth
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
				float2 uv : TEXCOORD0;
				float2 sketch_uv : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			half4 _SketchTex_ST;
			sampler2D _SketchTex;
			float _Intensity;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.sketch_uv = UnityStereoScreenSpaceUVAdjust(v.uv, _SketchTex_ST);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 originalSample = tex2D(_MainTex, i.uv);
				fixed4 sketchSample = tex2D(_SketchTex, i.sketch_uv);
				return lerp(originalSample, sketchSample, _Intensity);
			}
			ENDCG
		}
	}
}
