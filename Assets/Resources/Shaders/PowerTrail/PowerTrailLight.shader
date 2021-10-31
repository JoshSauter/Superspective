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
	    #pragma surface surf NoLighting fullforwardshadows
		
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

	    #include "PowerTrailHelpers.cginc"

        void surf (Input IN, inout SurfaceOutput o) {
            // DEBUG
            //float test = worldSdf(IN.worldPos);
            // ENDDEBUG
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            // Only turn on emission if worldSDF <= 0
            float emissionEnabled = EmissionEnabled(IN.worldPos);
		    o.Emission = _EmissionColor * emissionEnabled;
        }

        fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten) {
            return fixed4(s.Albedo, s.Alpha);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
