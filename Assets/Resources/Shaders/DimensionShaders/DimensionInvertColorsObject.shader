Shader "Custom/DimensionShaders/DimensionInvertColorsObject"
{
	Properties
	{
		_MainTex("Albedo", 2D) = "white" {}
		_Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
		[HDR]
		_EmissionColor("Emissive Color", Color) = (0, 0, 0, 0)
		_Dimension("Dimension", Int) = 0
		_Channel("Channel", Int) = 0
		_Inverse("Inverted (true: 1, false: 0)", Int) = 0
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
			#include "DimensionShaderHelpers.cginc"

			sampler2D _MainTex;
            float4 _MainTex_ST;
			
			float4 _Color;
			
			int _Dimension;
			int _Channel;
			int _Inverse;

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
				ClipDimensionObject(i.vertex, _Dimension, _Channel, _Inverse);

				return _Color;
			}
			ENDCG
		}
	}
}
