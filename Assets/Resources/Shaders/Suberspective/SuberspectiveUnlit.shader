Shader "Suberspective/SuberspectiveUnlit" {
	CGINCLUDE
	#include "SuberspectiveHelpers.cginc"
	ENDCG
	
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
        [HideInInspector] _DissolveColorAt0("Color at 0", Color) = (0,0,0,0)
        _DissolveColorAt1("Color at 1", Color) = (1,1,1,0)
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
        Tags { "Queue"="Geometry" "RenderType"="Suberspective" }
        LOD 100
        
    	Cull[__CullMode]
    	BlendOp[__BlendOp]
    	Blend[__SrcBlend][__DstBlend]
    	ZWrite[__ZWrite]

        Pass {
            CGPROGRAM
            #pragma vertex SuberspectiveVert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                UNITY_FOG_COORDS(1)
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            	float2 emissionUV : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };
            
			float4 _Color;
            float _EmissionEnabled;
			float4 _EmissionColor;

            fixed4 frag (SuberspectiveV2F i) : SV_Target {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv_MainTex) * _Color;
            	// This is possibly set to false by PowerTrail
            	half emissionEnabled = 1;
            	SuberspectiveRender(i, col, emissionEnabled);
            	
            	fixed4 emissionSample = tex2D(_EmissionMap, i.uv_EmissionMap);
            	// Emission Sample Color -> Grayscale conversion:
            	float emissionBrightness = _EmissionEnabled * dot(emissionSample.rgb, float3(0.2126, 0.7152, 0.0722));
            	col += emissionEnabled * emissionBrightness * emissionSample.a * _EmissionColor;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
        
		// Pass to render object as a shadow caster
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On ZTest LEqual Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            	float3 normal : NORMAL;
            };
			
			struct v2f {
                float2 uv : TEXCOORD0;
				V2F_SHADOW_CASTER;
				UNITY_VERTEX_OUTPUT_STEREO
                float3 worldPos : TEXCOORD1;
			};

			v2f vert(appdata v ) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}

			float4 frag(v2f i) : SV_Target {
				// TODO: Figure this out if needed
				// SuberspectiveClipOnly(i.pos, i.uv, i.worldPos);
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
    }
	CustomEditor "SuberspectiveGUI"
}
