// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/JG/Depth"
{
	Properties{
	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		Pass
		{
			Fog{ Mode Off }
			CGPROGRAM

			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			//uniform sampler2D _NoiseTex;
			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 depth : TEXCOORD0;
				//float2 scrPos : TEXCOORD3;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.depth = o.pos.zw;
				//o.scrPos = ComputeScreenPos(o.pos);
				return o;
			}

			half4 frag(v2f i) : COLOR
			{
				float dpth = Linear01Depth(i.depth.x / i.depth.y);
				//float noise = tex2D(_NoiseTex, i.scrPos);
				return dpth; //- (noise*dpth)*0.05;
			}
			ENDCG
		}
	}
}