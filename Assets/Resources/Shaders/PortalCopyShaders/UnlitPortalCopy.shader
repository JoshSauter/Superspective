Shader "Custom/UnlitPortalCopy" {
	Properties {
		_Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
		[HDR]
		_EmissionColor("Emissive Color", Color) = (0, 0, 0, 0)
		_PortalPos("Portal Position", Vector) = (0, 0, 0)
		_PortalNormal("Portal Normal", Vector) = (0, 0, 1)
	}
	SubShader {
		Tags { "RenderType"="PortalCopy" }
		LOD 100

		//ZWrite Off
		Cull Back

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD0;
			};

			float4 _Color;
			float4 _EmissionColor;

			float3 _PortalPos;
			float3 _PortalNormal;
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				float clipTest = -dot(i.worldPos - _PortalPos, _PortalNormal);
				if (clipTest == 0) clipTest = -1;
				clip(clipTest);

				// sample the texture
				fixed4 col = _Color;
				// apply fog
				col += _EmissionColor;
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

			float4 frag( v2f i ) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}
