Shader "Hidden/Overlay"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Intensity ("Intensity", Range(0.0, 1.0)) = 1.0
		_TopRight ("Top Right", Color) = (1, 0.4, 0.7, 0.1)
		_BotLeft ("Bottom Left", Color) = (0.2, .7, 0.7, 0.1)
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

			fixed4 _TopRight;
			fixed4 _BotLeft;
			float _Intensity;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				float t = (i.uv.x * i.uv.y);
				float u = (1-i.uv.y) * (1-i.uv.x);
				float2 intersectionPoint = float2((1 + i.uv.x - i.uv.y)/2, (1 + i.uv.y - i.uv.x)/2);
				float w = distance(intersectionPoint, i.uv);
				float z = distance(float2(0.5, 0.5), i.uv);
				col += ((_TopRight * t*t) + (_BotLeft * u*u)) * sqrt(w) * pow(z, 5) * _Intensity;
				return col;
			}
			ENDCG
		}
	}
}
