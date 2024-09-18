Shader "Custom/ParticleNoiseShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Lighting Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _MainTex_ST;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                return o;
            }

            float rand(float3 co) {
				float r1 = frac(sin(dot(co, float3(127.1, 311.7, 74.7))) * 43758.5453);
				float r2 = frac(sin(dot(co, float3(269.5, 183.3, 54.2))) * 43758.5453);
				return frac(r1 + r2);
			}

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the noise texture
                float noiseValue = rand(i.worldPos);

                // Sample the main texture (Default-Particle texture)
                fixed4 col = tex2D(_MainTex, i.texcoord);

                return col * noiseValue;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
