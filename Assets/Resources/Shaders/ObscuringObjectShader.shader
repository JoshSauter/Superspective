Shader "Unlit/ObscuringObjectShader"
{
	Properties
	{
		_MainTex ("Cutoff Texture", 2D) = "white" {}
		_Cutoff ("Cutoff value", Range(0.0, 1.0)) = 0.5
		_Color ("Color", Color) = (0.0, 0.0, 0.0, 1.0)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			ZTest Always
			Blend One One
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
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
			
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Cutoff;
			float4 _Color;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 cutoffSample = tex2D(_MainTex, i.uv);
				clip(cutoffSample - _Cutoff);
				
				fixed4 col = _Color;

				return col;
			}
			ENDCG
		}
	}
}
