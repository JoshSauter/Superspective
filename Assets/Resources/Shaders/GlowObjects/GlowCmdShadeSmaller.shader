Shader "Hidden/GlowCmdShaderSmaller"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex - v.normal * .005);//* sign(v.vertex));
				return o;
			}
			
			fixed4 _GlowColor;

			fixed4 frag (v2f i) : SV_Target {
				return _GlowColor;
			}
			ENDCG
		}
	}
}
