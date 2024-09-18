// Newer version of the RandomNoise shader
Shader "Suberspective/SuberspectiveStatic" {
    Properties {
	    // These are my internal cached values for the inspector enum
    	[HideInInspector] __SuberspectiveBlendMode("InternalBlendModeCache", Int) = 0
        [HideInInspector] __CullMode("__cull_mode", Float) = 2.0
        [HideInInspector] __BlendOp("__blend_op", Float) = 0.0
        [HideInInspector] __SrcBlend("__src", Float) = 1.0
		[HideInInspector] __DstBlend("__dst", Float) = 0.0
		[HideInInspector] __ZWrite("__zw", Float) = 1.0
    	
    	_MainTex("Main Texture", 2D) = "white" {}
		_Color("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
    	_EmissionEnabled("Emission enabled", Float) = 1.0
    	_EmissionMap("Emission Map", 2D) = "white" {}
		[HDR]
		_EmissionColor("Emissive Color", Color) = (0, 0, 0, 0)
    	// DimensionObject
		_Inverse("Inverted (true: 1, false: 0)", Int) = 0
    	// Hardcoded 8 here because no way to read NUM_CHANNELS from DimensionShaderHelpers.cginc
    	_Channel("Channel", Range(0, 8)) = 0
    	// DissolveObject
        _DissolveTex("Dissolve Texture", 2D) = "white" {}
        _DissolveValue("Dissolve Amount", Range(0.0, 1.0)) = 0
        _DissolveBurnSize("Burn Size", Range(0.0, 1.0)) = 0.15
        _DissolveBurnRamp("Burn Ramp (RGB)", 2D) = "white" {}
        _DissolveBurnColor("Burn Color", Color) = (.3,.6,1,1)
    	_DissolveEmissionAmount("Dissolve Emission Amount", float) = 2.0
    	// PowerTrail
    	_CapsuleRadius("SDF Capsule Radius", float) = .25
    	_ReverseVisibility("Invert PowerTrail", Int) = 0
    	// AffectedByShutter
        _ShutterNoise ("Noise Texture", 2D) = "black" {}
        _ShutterInverse ("Inverse", Int) = 0
        // PortalCopy
		_PortalPos("Portal Position", Vector) = (0, 0, 0)
		_PortalNormal("Portal Normal", Vector) = (0, 0, 1)
		_FudgeDistance("Fudge Position", Float) = 0.0
    	// RenderInZone
    	_MinRenderZone("Min Render Zone", Vector) = (0,0,0)
    	_MaxRenderZone("Max Render Zone", Vector) = (0,0,0)
    }
    SubShader {
	    Tags { "RenderType"="Suberspective" }
	    
	    Cull[__CullMode]
    	BlendOp[__BlendOp]
    	Blend[__SrcBlend][__DstBlend]
    	ZWrite[__ZWrite]
	    
        Pass {
            CGPROGRAM
            #pragma vertex SuberspectiveVert
            #pragma fragment frag

            #include "SuberspectiveHelpers.cginc"

			sampler2D _BumpMap;
			float _EmissionAmount;
            uniform float4 _Color;

			float rand(float3 co) {
				float r1 = frac(sin(dot(co, float3(127.1, 311.7, 74.7))) * 43758.5453);
				float r2 = frac(sin(dot(co, float3(269.5, 183.3, 54.2))) * 43758.5453);
				return frac(r1 + r2);
			}

            fixed4 frag (SuberspectiveV2F i) : SV_Target {
            	fixed4 col = _Color * tex2D(_MainTex, i.uv_MainTex);
            	col *= i.color;

            	float3 noiseSampleUv = i.worldPos;
            	
                float noise = rand(_Time.x + noiseSampleUv) * rand(_Time.x + noiseSampleUv);
            	float modulatedNoise = lerp(1.0, noise, col.a);

            	col.rgb *= modulatedNoise;
            	
            	return fixed4(col.rgb, col.a);
            }

            ENDCG
        }
    }
	CustomEditor "SuberspectiveGUI"
}
