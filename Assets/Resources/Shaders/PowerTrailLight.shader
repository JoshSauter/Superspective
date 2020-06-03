Shader "Custom/PowerTrailLight" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		[HDR]
		_EmissionColor("Emissive Color", Color) = (0, 0, 0, 0)
        //_CapsuleRadius ("Capsule Radius", Range(0,1)) = 0
    }
    SubShader {
		Tags { "RenderType"="Opaque" }
	    CGPROGRAM
	    #pragma surface surf Standard fullforwardshadows
		
        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
		
        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
	    fixed4 _EmissionColor;

        // Power trail info
        #define MAX_NODES 512
        uniform float3 _NodePositions[MAX_NODES];
        uniform int _StartPositionIDs[MAX_NODES];
        uniform int _EndPositionIDs[MAX_NODES];
        uniform float _InterpolationValues[MAX_NODES];
        uniform float _CapsuleRadius;

        // p is the test position, pointA is the center of the sphere at one end of the capsule, pointB is the center of the sphere at the other end
        float sdfCapsule(float3 p, float3 pointA, float3 pointB, float radius) {
            float3 pa = p - pointA;
            float3 ba = pointB - pointA;
            float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
            return length(pa - ba * h) - radius;
	    }

        float worldSdf(float3 p) {
            // Any positive starting value will suffice since we don't care about the distance from point to the SDF, just the sign of the result
            float minValue = 100.0;

            for (int i = 0; i < MAX_NODES; i++) {
                float lerpValue = _InterpolationValues[i];
                if (lerpValue <= 0) continue;

                float3 startCapsulePos = _NodePositions[_StartPositionIDs[i]];
                float3 endCapsulePos = lerp(startCapsulePos, _NodePositions[_EndPositionIDs[i]], lerpValue);

                minValue = min(minValue, sdfCapsule(p, startCapsulePos, endCapsulePos, _CapsuleRadius));
			}

            return minValue;
		}

        void surf (Input IN, inout SurfaceOutputStandard o) {
            // DEBUG
            //float test = worldSdf(IN.worldPos);
            // ENDDEBUG
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            // Only turn on emission if worldSDF <= 0
		    o.Emission = _EmissionColor * step(worldSdf(IN.worldPos), 0);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
