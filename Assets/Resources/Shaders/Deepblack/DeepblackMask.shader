Shader "Hidden/DeepblackMask" {
    Properties {
        _MainTex ("Texture", 2D) = "black" {} // The screen texture
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
            sampler2D _DarknessMask;

            v2f vert(appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                fixed4 color = tex2D(_MainTex, i.uv);
                
                // Detect pure black pixels
                if (max(color.r, max(color.g, color.b)) == 0) {
                    fixed4 darknessSample = tex2D(_DarknessMask, i.uv);
                    float darkness = 10 * max(darknessSample.r, max(darknessSample.g, darknessSample.b));
                    return float4(darkness, darkness, darkness, 1.0);
                }
                return float4(0, 0, 0, 1); // Everything else is black
            }
            ENDCG
        }
    }
}
