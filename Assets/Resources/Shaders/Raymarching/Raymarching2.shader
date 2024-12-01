// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Raymarching2"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
		Tags { "RenderType"="Transparent" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "RaymarchingUtils.cginc"

            struct v2f
            {
                float4 clipPos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert (appdata_full v) {
                v2f o;
                o.clipPos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            #define MAX_STEPS 50
            
            float worldSDF(in float3 pos) {
            	float sinTime = (_SinTime.y + 1) / 2.0;
            	float cosTime = (_CosTime.y + 1) / 2.0;
            	pos.x *= .1 + cosTime;
            	pos.z *= .1 + sinTime;

            	float baseSize = 6;
                float size = (.25+sinTime) * baseSize;
                float3 repeatPos = repeatRegular(pos  +float3(sinTime,sinTime,sinTime), baseSize*4);

            	//return sphereSDF(repeatPos, 2*baseSize);
            	
				float box = emptyCubeSDF(repeatPos, size, size-.5);
            	
            	float biggerBox = emptyCubeSDF(repeatPos, (.5+sinTime) * size*2, (.5+sinTime) * size/1.1);

            	float smallerBox = emptyCubeSDF(repeatPos, size/2, size/4);

            	float spheres = sphereSDF(repeatPos, 2*baseSize);
            	
            	return smoothIntersectionSDF(spheres, unionSDF(smallerBox, unionSDF(box, biggerBox)), .5);
            }

            #define SDF(p) worldSDF(p)
            #include "RaymarchingMacros.cginc"

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = Raymarch(MAX_STEPS, i.worldPos, 100, 400);
                return fixed4(col.r, col.g * .6, col.b * .6, col.a);
            }

            ENDCG
        }
    }
}
