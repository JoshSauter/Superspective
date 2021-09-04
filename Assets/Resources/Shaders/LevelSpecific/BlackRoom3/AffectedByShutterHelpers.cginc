#define NUM_SHUTTERS 32

#include "UnityCG.cginc"

fixed4 _Color;
int _Inverse;
sampler2D _Noise;
float _Shutters[NUM_SHUTTERS];
float3 _Shutter_Center;
float _Shutter_Height;

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
    return tex2D(_Noise, uv).r;
}
            
fixed4 ClipShutteredAreas(float3 pos) {
    float3 diff = pos - _Shutter_Center;
    float angle = atan2(diff.x, diff.z) + UNITY_PI;
    const float anglePerShutter = UNITY_PI * 2 / NUM_SHUTTERS;
    int index = angle / anglePerShutter;
    int arrayIndex = ArrayIndex(index);
    int arrayIndexNext = ArrayIndex((index+1)%NUM_SHUTTERS);
    
    float shutterLerpHeight = lerp(_Shutters[arrayIndex], _Shutters[arrayIndexNext], (angle / anglePerShutter) - index);
    float shutterY = (1 - shutterLerpHeight) * _Shutter_Height;
    float random = 1+rand(diff);
    shutterLerpHeight *= random;
    if (_Inverse) shutterLerpHeight = 1 - shutterLerpHeight;
    int posIsShutteredInY = 0.5*length(diff) > shutterY*random;
    int result = _Inverse * (1-posIsShutteredInY) + (1-_Inverse) * posIsShutteredInY;
    clip(-result);
    return lerp(_Color, 1-_Color, saturate(shutterLerpHeight));
}