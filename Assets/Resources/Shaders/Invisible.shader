Shader "Unlit/Invisible" {
    SubShader {
        Tags { "RenderType"="Transparent" "PortalTag"="True" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v) {
                v2f o;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
				clip(-1);
                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}
