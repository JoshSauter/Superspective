Shader "Hidden/8BitColors"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

				float maxVal = max(col.r, max(col.g, col.b));
				float3 color_resolution = float3(16.0, 16.0, 8.0);
				float3 color_bands = floor(col.rgb * color_resolution) / (color_resolution - 1.0);
				col = fixed4(min(color_bands, 1.0), col.a) * 1.05;
                return col;
            }
            ENDCG
        }
    }
}
