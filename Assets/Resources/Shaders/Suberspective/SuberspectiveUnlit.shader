Shader "Suberspective/SuberspectiveUnlit" {
	CGINCLUDE
		#pragma multi_compile __ DIMENSION_OBJECT
		#pragma multi_compile __ DISSOLVE_OBJECT
	
		#include "UnityCG.cginc"
#ifdef DIMENSION_OBJECT
		#include "../DimensionShaders/DimensionShaderHelpers.cginc"
#endif   
#ifdef DISSOLVE_OBJECT
		#include "../DissolveShaderHelpers.cginc"
#endif
	ENDCG
	
    Properties {
		_Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
		[HDR]
		_EmissionColor("Emissive Color", Color) = (0, 0, 0, 0)
    	// DimensionObject
		_Inverse("Inverted (true: 1, false: 0)", Int) = 0
    	// DissolveObject
        _DissolveColorAt0 ("Color at 0", Color) = (0,0,0,0)
        _DissolveColorAt1 ("Color at 1", Color) = (0,0,0,0)
        _DissolveTex("Dissolve Texture", 2D) = "white" {}
        _DissolveValue("Dissolve Amount", Range(0.0, 1.0)) = 0
 
        _DissolveBurnSize("Burn Size", Range(0.0, 1.0)) = 0.15
        _DissolveBurnRamp("Burn Ramp (RGB)", 2D) = "white" {}
        _DissolveBurnColor("Burn Color", Color) = (1,1,1,1)
 
        _EmissionAmount("Emission amount", float) = 2.0
    }
    SubShader {
        Tags { "Queue"="Geometry" "RenderType"="Suberspective" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
			float4 _Color;
			float4 _EmissionColor;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
#ifdef DIMENSION_OBJECT
                ClipDimensionObject(i.vertex);
#endif
#ifdef DISSOLVE_OBJECT
				col *= Dissolve(i.texcoord.xy);
#endif
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

			struct v2f {
				V2F_SHADOW_CASTER;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert( appdata_base v ) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag( v2f i ) : SV_Target {
#ifdef DIMENSION_OBJECT
				ClipDimensionObject(i.pos.xy);
#endif
#ifdef DISSOLVE_OBJECT
				Dissolve(i.texcoord.xy);
#endif
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
    }
}
