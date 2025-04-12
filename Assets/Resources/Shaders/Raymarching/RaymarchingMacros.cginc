// Raymarching implementation
/// <summary>
/// Users can specify their own world SDF function by defining the SDF macro.
/// Usage: #define SDF(p) worldSdf(p) -- where worldSdf is the user's SDF function.
/// </summary>
#ifndef SDF
#define SDF(p) FallbackSDF(p) // Fallback SDF if none is defined
#endif

float FallbackSDF(float3 p) {
    return length(p) - 1.0; // Default sphere SDF
}

float CalculateSoftShadow(float3 pos, float3 lightDir, int maxShadowSteps, float shadowSoftness) {
    float shadow = 1.0;
    float minDistance = 1.0;
    float t = 0.01;

    for (int i = 0; i < maxShadowSteps; i++) {
        float3 p = pos + t * lightDir;
        float sdfValue = SDF(p);

        // Track the minimum distance to adjust shadow softness
        minDistance = min(minDistance, sdfValue * shadowSoftness / t);
        
        if (sdfValue < 0.001) {
            shadow = 0.0;
            break;
        }
        
        shadow = min(shadow, 10.0 * sdfValue / t); // Soft shadow falloff
        t += sdfValue * shadowSoftness;

        // Exit if the distance becomes large enough
        if (t > shadowSoftness) break;
    }
    return saturate(shadow * minDistance); // Clamp the shadow factor between 0 and 1
}

/// <summary>
/// Raymarches the scene to find the intersection point of the ray with the scene.
/// Returns a fixed4 color with the following components:
/// x: The smoothed raymarching value of the object at the intersection point
/// y: The ratio of steps taken to the maximum steps
/// z: The minimum distance to the object
/// w: The shadow factor at the intersection point
/// </summary>
fixed4 Raymarch(int maxSteps, float3 worldPos, float startDepth, float endDepth) {
    float3 viewDirection = normalize(worldPos - _WorldSpaceCameraPos);
    float depth = startDepth;
    float end = endDepth + depth;
    
    float col = 0;
    float minDistance = 99999;
    float shadow = 0;
    int stepsTaken = 0;
    for (; stepsTaken < maxSteps && depth < end; stepsTaken++) {
        float3 position = worldPos + depth * viewDirection;
        float sdfValue = SDF(position);
        if (sdfValue < minDistance) {
            minDistance = sdfValue;
        }
        
        if (sdfValue < .001) {
            col = 1 - stepsTaken / (float)maxSteps;
            //return col * normalize(noised(position/256));
            //return col * fixed4(position.x,position.y,(-position.x - position.y) / 2.0,1);
            // TODO: Maybe pass in the light direction/shadow softness as parameters
            float shadowSoftness = 0.5;
            int maxShadowSteps = maxSteps / 2;
            float3 lightDir = normalize(float3(-53.12, 122.594, -137.799));
            shadow = CalculateSoftShadow(position, lightDir, maxShadowSteps, shadowSoftness);
            col = smoothstep(0.0,1.0,col);
            break;
        }
        else {
          depth += sdfValue + .001;
        }
    }
                
    return fixed4(col, stepsTaken / (float)maxSteps, minDistance, shadow);
}
