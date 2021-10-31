#define MAX_NODES 512
uniform float3 _NodePositions[MAX_NODES];
uniform int _StartPositionIDs[MAX_NODES];
uniform int _EndPositionIDs[MAX_NODES];
uniform float _InterpolationValues[MAX_NODES];
uniform float _CapsuleRadius;
uniform int _ReverseVisibility;

// p is the test position, pointA is the center of the sphere at one end of the capsule, pointB is the center of the sphere at the other end
float SdfCapsule(float3 p, float3 pointA, float3 pointB, float radius) {
    float3 pa = p - pointA;
    float3 ba = pointB - pointA;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h) - radius;
}

float WorldSdf(float3 p) {
    // Any positive starting value will suffice since we don't care about the distance from point to the SDF, just the sign of the result
    float minValue = 100.0;

    for (int i = 0; i < MAX_NODES; i++) {
        float lerpValue = _InterpolationValues[i];
        if (lerpValue <= 0) continue;

        float3 startCapsulePos = _NodePositions[_StartPositionIDs[i]];
        float3 endCapsulePos = lerp(startCapsulePos, _NodePositions[_EndPositionIDs[i]], lerpValue);

        minValue = min(minValue, SdfCapsule(p, startCapsulePos, endCapsulePos, _CapsuleRadius));
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
