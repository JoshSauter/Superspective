// Used from a CommandBuffer to render darkness values of DeepblackObjects to a RenderTexture for later use in the DeepblackMask shader
Shader "Hidden/DarknessMask" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            uniform float _Darkness;
            uniform float _FalloffFactor;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag (v2f i) : SV_Target {
                float dist = distance(_WorldSpaceCameraPos, i.worldPos);
                float scale = saturate(1.0 - dist * _FalloffFactor);
                return _Darkness * scale;
            }
            ENDCG
        }
    }
}
