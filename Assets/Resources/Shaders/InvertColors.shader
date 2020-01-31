Shader "Hidden/InvertColors"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "ReplacementTag"="Unlit"}
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			
			float _ResolutionX;
			float _ResolutionY;

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
				float2 viewportVertex = float2(i.vertex.x / _ResolutionX, i.vertex.y / _ResolutionY) - float2(0.5, 0.5);
				//return fixed4(length(viewportVertex), length(viewportVertex), length(viewportVertex), 1);
				//return fixed4(viewportVertex.x, viewportVertex.y, 0, 1);
				fixed4 col = tex2D(_MainTex, i.uv);
				// just invert the colors
				col = lerp(1 - col, col, length(viewportVertex));
				return col;
			}
			ENDCG
		}
	}
}
