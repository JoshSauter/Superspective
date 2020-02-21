Shader "Custom/DimensionShaders/DimensionDissolve"
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
		_Dimension("Dimension", Int) = 0
		_Channel("Channel", Int) = 0
	}
	SubShader
	{
		Tags { "Queue"="Geometry" "RenderType"="DimensionDissolveDoubleSided" }
		LOD 100
		Cull Off

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
					if (test < _BurnSize && _DissolveValue < 1) {
						col += tex2D(_BurnRamp, float2(test * (1 / _BurnSize), 0)) * _BurnColor * _EmissionAmount;
					}
				}
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
		// Pass to render object as a shadow caster
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On ZTest LEqual Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"
			#include "DimensionShaderHelpers.cginc"

			int _Dimension;
			int _Channel;

			struct v2f {
				V2F_SHADOW_CASTER;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert( appdata_base v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag( v2f i ) : SV_Target {
				ClipDimensionObject(i.pos.xy, _Dimension, _Channel);
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}
