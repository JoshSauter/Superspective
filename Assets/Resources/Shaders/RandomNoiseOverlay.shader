Shader "Hidden/RandomNoiseOverlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Intensity ("Intensity", Float) = 0
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

            uniform float _Intensity;

            float rand(float2 co){
                return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
            }

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
                // just invert the colors
                float noise = rand(_Time.x + float2(i.uv.y, i.uv.x)) * rand(_Time.x + i.uv);
                return col * (1-_Intensity) + noise * (_Intensity);
            }
            ENDCG
        }
    }
}
