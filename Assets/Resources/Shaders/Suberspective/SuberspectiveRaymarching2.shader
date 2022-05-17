Shader "Suberspective/SuberspectiveRaymarching2" {
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
		Tags { "RenderType"="Transparent" }
        Pass {
            CGPROGRAM
            #include "SuberspectiveHelpers.cginc"
            #include "../Raymarching/RaymarchingUtils.cginc"
            
            #pragma vertex SuberspectiveVert
            #pragma fragment frag

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
            	
                //float sphere = sphereSDF(repeatRegular(pos, 96), (1 + 0.5 * _SinTime.z) * 56);
                //float outerSphere = sphereSDF(repeatRegular(pos * hash(pos.x + pos.y + pos.z), 96), (1 + 0.5 * _SinTime.z) * 56);
                float outerSphere = sphereSDF(repeatRegular(pos, 96), (1 + 0.5 * _SinTime.y) * 56);
                float innerSphere = sphereSDF(repeatRegular(pos, 96), (1 + 0.5 * _SinTime.y) * 40);
                float pathwayArea = boxSDF(pos + float3(0,6,0), float3(14,8,1000));
                float pathwayBlocks = boxSDF(repeat(pos - float3(0,7.5,0), float3(12,16,12)), float3(4,.5,4));
                float pathwayBlocksCutout = boxSDF(repeat(pos - float3(0,7.5,0), float3(12,16,12)), float3(3,1,3));
                float pathway = intersectionSDF(pathwayArea, diffSDF(pathwayBlocks, pathwayBlocksCutout));
                float sphere = unionSDF(pathwayArea, diffSDF(outerSphere, innerSphere));
                
                //float size = 6;
                //float3 repeatPos = repeatRegular(pos, size*2);
                //float3 baseSize = float3(size,size,size);
                float width = 0.5;
                float bigBoxSDF = boxSDF(repeatPos, baseSize);
                float xBoxSDF = boxSDF(repeatPos, baseSize + width * float3(1,-1,-1));
                float yBoxSDF = boxSDF(repeatPos, baseSize + width * float3(-1,1,-1));
                float zBoxSDF = boxSDF(repeatPos, baseSize + width * float3(-1,-1,1));
                
                float3 repeatOffsetPos = repeatRegular(pos - size * float3(1,1,1), size*2);
                size = 1.5;
                baseSize = float3(size,size,size);
                width = .5;
                float cornerBoxSDF = boxSDF(repeatOffsetPos, baseSize);
                float xCornerBoxSDF = boxSDF(repeatOffsetPos, baseSize + width * float3(1,-1,-1));
                float yCornerBoxSDF = boxSDF(repeatOffsetPos, baseSize + width * float3(-1,1,-1));
                float zCornerBoxSDF = boxSDF(repeatOffsetPos, baseSize + width * float3(-1,-1,1));
                
                float repeatedCube = diffSDF(diffSDF(diffSDF(bigBoxSDF, xBoxSDF), yBoxSDF), zBoxSDF);
                float offsetRepeatedCube = diffSDF(diffSDF(diffSDF(cornerBoxSDF, xCornerBoxSDF), yCornerBoxSDF), zCornerBoxSDF);
                float cubes = unionSDF(repeatedCube, cornerBoxSDF);
                
                return smoothIntersectionSDF(cubes, sphere, .5);
            }

            fixed4 frag (SuberspectiveV2F i) : SV_Target {
            	#ifdef DISSOLVE_OBJECT
            	SuberspectiveClipOnly(i);
            	#else
            	SuberspectiveClipOnly(i);
            	#endif
                float3 viewDirection = normalize(i.worldPos - _WorldSpaceCameraPos);
                float depth = 100;
                float end = 400 + depth;
                for (int x = 0; x < MAX_STEPS && depth < end; x++) {
                    float3 position = i.worldPos + depth * viewDirection;
                    float sdfValue = worldSDF(position);
                    if (sdfValue < .001) {
                        float col = 1.0 - (x / (float)MAX_STEPS);
                        //return col * normalize(noised(position/256));
                        //return col * fixed4(position.x,position.y,(-position.x - position.y) / 2.0,1);
                    	col = smoothstep(0.0,1.0,col);
                        return fixed4(col, col-.6, col-.6, 1);
                    }
                    
                    depth += sdfValue + .001;
                }
                
                return fixed4(0,0,0,0);
            }

            ENDCG
        }
    }
	CustomEditor "SuberspectiveGUI"
}
