#include "../Suberspective/SuberspectiveUniforms.cginc"

// p is the test position, pointA is the center of the sphere at one end of the capsule, pointB is the center of the sphere at the other end
float SdfCapsule(float3 p, float3 pointA, float3 pointB, float radius) {
    float3 pa = p - pointA;
    float3 ba = pointB - pointA;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h) - radius;
}

float SdfCylinder(float3 p, float3 pointA, float3 pointB, float radius){
    float3 ba = pointB - pointA;
    pointB += normalize(ba) * _CapsuleRadius / 2.0;
    ba = pointB - pointA; // Needed to calculate once to get the direction, again to capture the adjusted endpoint
    float3 pa = p - pointA;
    float baba = dot(ba, ba);
    float paba = dot(pa, ba);
    float x = length(pa*baba-ba*paba) - radius*baba;
    float y = abs(paba-baba*0.5)-baba*0.5;
    float x2 = x*x;
    float y2 = y*y*baba;
    float d = (max(x, y) < 0.0) ? -min(x2, y2) : (((x > 0.0) ? x2 : 0.0) + ((y > 0.0) ? y2 : 0.0));
    return sign(d)*sqrt(abs(d))/baba;
}

float SdfSegment(float3 p, float3 pointA, float3 pointB, float radius){
    return _UseCylinder == 1 ? SdfCylinder(p, pointA, pointB, radius) : SdfCapsule(p, pointA, pointB, radius);
}

float WorldSdf(float3 p) {
    // Any positive starting value will suffice since we don't care about the distance from point to the SDF, just the sign of the result
    float minValue = 100.0;

    for (int i = 0; i < MAX_NODES; i++) {
        float lerpValue = _InterpolationValues[i];
        if (lerpValue <= 0) continue;

        float3 startCapsulePos = _NodePositions[_StartPositionIDs[i]];
        float3 endCapsulePos = lerp(startCapsulePos, _NodePositions[_EndPositionIDs[i]], lerpValue);

        minValue = min(minValue, SdfSegment(p, startCapsulePos, endCapsulePos, _CapsuleRadius));
    }

    return minValue;
}

bool EmissionEnabled(float3 worldPos) {
    float emissionEnabled = step(WorldSdf(worldPos), 0);
    if (_ReverseVisibility > 0) {
        emissionEnabled = 1 - emissionEnabled;     
    }
    
    return emissionEnabled;
}

void ClipPowerTrail(float3 worldPos) {
    clip(EmissionEnabled(worldPos) - _HiddenPowerTrail);
}