Shader "Custom/BlackRoom3/AffectedByShutter" {
    Properties {
		_Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
		[HDR]
		_EmissionColor("Emissive Color", Color) = (0, 0, 0, 0)
        _Noise ("NoiseTexture", 2D) = "black" {}
        _Inverse ("Inverse", Int) = 0
    }
    SubShader {
        Tags { "RenderType"="AffectedByShutter" }
        LOD 100

        Pass {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "AffectedByShutterHelpers.cginc"
            fixed4 _Color;
            fixed4 _EmissionColor;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD0;
            };
            
            v2f vert (appdata v) {
                v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = ClipShutteredAreas(i.worldPos, _Color);
                col += _EmissionColor;
                return col;
            }
            ENDCG
        }
    }
}
