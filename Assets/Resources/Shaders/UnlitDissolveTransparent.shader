Shader "Custom/UnlitDissolveTransparent"
{
	Properties
	{
        _Color ("Color at 0", Color) = (0,0,0,0)
        _Color2 ("Color at 1", Color) = (0,0,0,0)
        _MainTex("Dissolve Texture (RGB)", 2D) = "white" {}
        _DissolveValue("Dissolve Amount", Range(0.0, 1.0)) = 0
 
        _BurnSize("Burn Size", Range(0.0, 1.0)) = 0.15
        _BurnRamp("Burn Ramp (RGB)", 2D) = "white" {}
        _BurnColor("Burn Color", Color) = (1,1,1,1)
 
        _EmissionAmount("Emission amount", float) = 2.0
		[HDR]
		_EmissionColor("Emissive Color", Color) = (0, 0, 0, 0)
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 100
		Cull Back
		//Blend SrcAlpha OneMinusSrcAlpha
		Blend DstColor SrcColor

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			fixed4 _Color;
			fixed4 _Color2;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _BumpMap;
			sampler2D _BurnRamp;
			fixed4 _BurnColor;
			float _BurnSize;
			float _DissolveValue;
			float _EmissionAmount;
			float4 _EmissionColor;

			struct v2f
			{
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};
			
			v2f vert (appdata_full v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				half test = tex2D(_MainTex, i.texcoord.xy).rgb - _DissolveValue;
				if (_Color.a == 0) clip(-test);
				if (_Color2.a == 0) clip(test);

				fixed4 col = fixed4(0,0,0,0);
				if (test < 0) {
					col = _Color2;
					if (-test < _BurnSize && _DissolveValue < 1) {
						col += tex2D(_BurnRamp, float2(-test * (1 / _BurnSize), 0)) * _BurnColor * _EmissionAmount;
					}
				}
				else {
					col = _Color;
					if (test < _BurnSize && _DissolveValue < 1 && _DissolveValue > 0) {
						col += tex2D(_BurnRamp, float2(test * (1 / _BurnSize), 0)) * _BurnColor * _EmissionAmount;
					}
				}
				UNITY_APPLY_FOG(i.fogCoord, col);
				col += _EmissionColor;
				return col;
			}
			ENDCG
		}
	}

	Fallback "VertexLit"
}
