// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Raymarching"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
	
		Tags { "RenderType"="Opaque" }
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
                //float sphere = sphereSDF(repeatRegular(pos, 96), (1 + 0.5 * _SinTime.z) * 56);
                //float outerSphere = sphereSDF(repeatRegular(pos * hash(pos.x + pos.y + pos.z), 96), (1 + 0.5 * _SinTime.z) * 56);
                float outerSphere = sphereSDF(repeatRegular(pos, 96), (1 + 0.5 * _SinTime.y) * 56);
                float innerSphere = sphereSDF(repeatRegular(pos, 96), (1 + 0.5 * _SinTime.y) * 40);
                float pathwayArea = boxSDF(pos + float3(0,6,0), float3(14,8,1000));
                float pathwayBlocks = boxSDF(repeat(pos - float3(0,7.5,0), float3(12,16,12)), float3(4,.5,4));
                float pathwayBlocksCutout = boxSDF(repeat(pos - float3(0,7.5,0), float3(12,16,12)), float3(3,1,3));
                float pathway = intersectionSDF(pathwayArea, diffSDF(pathwayBlocks, pathwayBlocksCutout));
                float sphere = unionSDF(pathwayArea, diffSDF(outerSphere, innerSphere));
                
                float size = 6;
                float3 repeatPos = repeatRegular(pos, size*2);
                float3 baseSize = float3(size,size,size);
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

            fixed4 frag (v2f i) : SV_Target {
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
                        return 1-fixed4(col, col, col, 1);
                    }
                    
                    depth += sdfValue + .001;
                }
                
                return 1-fixed4(0,0,0,0);
            }
			/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

			float _ResolutionX;
			float _ResolutionY;

			float2 rotate(in float2 v, in float a) {
				return float2(cos(a)*v.x + sin(a)*v.y, -sin(a)*v.x + cos(a)*v.y);
			}

			float torus(in float3 p, in float2 t) {
				float2 q = abs(float2(max(abs(p.x), abs(p.z))-t.x, p.y));
				return max(q.x, q.y)-t.y;
			}

			// These are all equally interesting, but I could only pick one :(
			float trap(in float3 p) {
				//return abs(max(abs(p.z)-0.1, abs(p.x)-0.1))-0.01;
				//return length(max(abs(p.xy) - 0.05, 0.0));
				//return length(p)-0.5;
				//return length(max(abs(p) - 0.35, 0.0));
				//return abs(length(p.xz)-0.2)-0.01;
				//return abs(min(torus(float3(p.x, fmod(p.y,0.4)-0.2, p.z), float2(0.666, 0.03)), max(abs(p.z)-0.023, abs(p.x)-0.023)))-0.0023;
				return abs(min(torus(p, float2(0.3, 0.05)), max(abs(p.z)-0.05, abs(p.x)-0.05)))-0.005;
				//return min(length(p.xz), min(length(p.yz), length(p.xy))) - 0.05;
			}

			float map(in float3 p) {
				float cutout = dot(abs(p.yz),float2(0.5, 0.5))-0.035;
				float road = max(abs(p.y-0.025), abs(p.z)-0.035);
	
				float3 z = abs(1.0-fmod(p,2.0));
				z.xz = rotate(z.xz, -_Time.z*-0.035);
				z.yz = rotate(z.yz, _Time.z*-0.025);

				float d = 999.0;
				float s = 1.0;
				for (float i = 0.0; i < 3.0; i++) {
					z.xz = rotate(z.xz, radians(i*10.0));
					z.zy = rotate(z.yz, radians((i+1.0)*20.0*1.1234));
					z = abs(1.0-fmod(z+i/3.0,2.0));
		
					z = z*2.0 - 0.3;
					s *= 0.5;
					d = min(d, trap(z) * s);
				}
				return max(d, -cutout);
			}

			float3 hsv(in float h, in float s, in float v) {
				return lerp(float3(1.0, 1.0, 1.0), clamp((abs(frac(h + float3(3, 3, 3) / 3.0) * 6.0 - 3.0) - 1.0), 0.0 , 1.0), s) * v;
			}

			float3 intersect(in float3 rayOrigin, in float3 rayDir) {
				float total_dist = 0.0;
				float3 p = rayOrigin;
				float d = 1.0;
				float iter = 0.0;
				float mind = 0.; // Move road from side to side slowly
	
				for (int i = 0; i < MAX_STEPS; i++)
				{		
					if (d < 0.001) continue;
		
					d = map(p);
					// This rotation causes the occasional distortion - like you would see from heat waves
					p += d*float3(rayDir.x, rotate(rayDir.yz, sin(mind)));
					mind = min(mind, d);
					mind *= sin(_Time.z*.23);
					total_dist += d;
					iter++;
				}

				float3 color = float3(0.0, 0.0, 0.0);
				if (d < 0.001) {
					float x = iter/((float)MAX_STEPS);
					float y = (d-0.01)/0.01/((float)MAX_STEPS);
					float z = (0.01-d)/0.01/((float)MAX_STEPS);
					if (max(abs(p.y-0.025), abs(p.z)-0.035)<0.002) { // Road
						float w = smoothstep(fmod(p.x*50.0, 4.0), 2.0, 2.01);
						w -= 1.0-smoothstep(fmod(p.x*50.0+2.0, 4.0), 2.0, 1.99);
						w = frac(w+0.0001);
						float a = frac(smoothstep(abs(p.z), 0.0025, 0.0026));
						color = float3((1.0-x-y*2.)*lerp(float3(0.8, 0.8, 0), float3(0.1, 0.1, 0.1), 1.0-(1.0-w)*(1.0-a)));
					} else {
						float q = 1.0-x-y*2.+z;
						color = hsv(q*0.2+0.85, 1.0-q*0.2, q);
					}
				} else
					color = hsv(d, 1.0, 1.0)*mind*45.0; // Background
				return color;
			}


			fixed4 frag2(v2f i) : SV_Target {
                float3 viewDirection = normalize(i.worldPos - _WorldSpaceCameraPos);
                float depth = 0;
                float end = 400 + depth;

				return float4(intersect(i.worldPos, viewDirection), 0);
                //for (int x = 0; x < MAX_STEPS && depth < end; x++) {
                //    float3 position = i.worldPos + depth * viewDirection;
                //    float sdfValue = worldSDF(position);
                //    if (sdfValue < .001) {
                //        float col = 1.0 - (x / (float)MAX_STEPS);
                //        //return col * normalize(noised(position/256));
                //        //return col * fixed4(position.x,position.y,(-position.x - position.y) / 2.0,1);
                //        return fixed4(col, col, col, 1);
                //    }
                //    
                //    depth += sdfValue + .001;
                //}
                
                return fixed4(0,0,0,0);
            }

            ENDCG
        }
    }
}
