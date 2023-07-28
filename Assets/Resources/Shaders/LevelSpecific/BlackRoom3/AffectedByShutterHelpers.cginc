#define NUM_SHUTTERS 32

int _ShutterInverse;
sampler2D _ShutterNoise;
float _Shutters[NUM_SHUTTERS];
float3 _ShutterCenter;
float _ShutterHeight;

int ArrayIndex(int index) {
    index = (2 * index + 1);
    if (index > NUM_SHUTTERS) {
        index = 2*NUM_SHUTTERS - index - 1;
    }
    return index;
}
            
float rand(float3 myVector)  {
    int boxSize = 10000;
    
    float2 uv = float2(myVector.x+myVector.z, myVector.y)/100;
    float2 gridUV = round(uv);
    return tex2D(_ShutterNoise, uv).r;
}

// Copied from UnityCG.cginc
#define PI 3.14159265359f

float4 ClipShutteredAreas(float3 pos, float4 col, float disabled = 0.0) {
    float3 diff = pos - _ShutterCenter;
    float angle = atan2(diff.x, diff.z) + PI;
    const float anglePerShutter = PI * 2 / NUM_SHUTTERS;
    uint index = angle / anglePerShutter;
    int arrayIndex = ArrayIndex(index);
    int arrayIndexNext = ArrayIndex((index+1)%NUM_SHUTTERS);
    
    float shutterLerpHeight = lerp(_Shutters[arrayIndex], _Shutters[arrayIndexNext], (angle / anglePerShutter) - index);
    float shutterY = (1 - shutterLerpHeight) * _ShutterHeight;
    float random = 1+rand(diff);
    shutterLerpHeight *= random;
    if (_ShutterInverse) shutterLerpHeight = 1 - shutterLerpHeight;
    int posIsShutteredInY = 0.5*length(diff) > shutterY*random;
    int result = _ShutterInverse * (1-posIsShutteredInY) + (1-_ShutterInverse) * posIsShutteredInY;
    clip(disabled-result);
    return lerp(col, 1-col, saturate(shutterLerpHeight));
}