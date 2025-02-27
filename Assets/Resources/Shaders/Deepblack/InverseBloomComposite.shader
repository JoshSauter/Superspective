Shader "Hidden/InverseBloomComposite" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _DarkeningTex ("Darkening Mask", 2D) = "black" {}
        _DarkeningIntensity ("Darkening Intensity", Float) = 1.0
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _BloomedDarknessMask;
            float _DarkeningIntensity;

            v2f vert(appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 color = tex2D(_MainTex, i.uv);
                fixed4 darknessSample = saturate(tex2D(_BloomedDarknessMask, i.uv));
                float darkness = _DarkeningIntensity * max(darknessSample.r, max(darknessSample.g, darknessSample.b));

                return color * (1.0 - darkness);
            }
            ENDCG
        }
    }
}
