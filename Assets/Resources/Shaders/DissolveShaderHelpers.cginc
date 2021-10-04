sampler2D _DissolveTex;
float4 _DissolveTex_ST;
float _DissolveValue;

float _DissolveBurnSize;
sampler2D _DissolveBurnRamp;
float4 _DissolveBurnColor;
float _DissolveEmissionAmount;

// Color of the material when _DissolveValue is 0
float4 _DissolveColorAt0;
// Color of the material when _DissolveValue is 1
float4 _DissolveColorAt1;

float4 Dissolve(float2 texcoord) {
    half test = tex2D(_DissolveTex, texcoord.xy).rgb - _DissolveValue;
    if (_DissolveColorAt0.a == 0) clip(-test);
    if (_DissolveColorAt1.a == 0) clip(test);

    float4 col = float4(0,0,0,0);
    if (test < 0) {
        col = _DissolveColorAt1;
        if (-test < _DissolveBurnSize && _DissolveValue < 1) {
            col += tex2D(_DissolveBurnRamp, float2(-test * (1 / _DissolveBurnSize), 0)) * _DissolveBurnColor * _DissolveEmissionAmount;
        }
    }
    else {
        col = _DissolveColorAt0;
        if (test < _DissolveBurnSize && _DissolveValue < 1 && _DissolveValue > 0) {
            col += tex2D(_DissolveBurnRamp, float2(test * (1 / _DissolveBurnSize), 0)) * _DissolveBurnColor * _DissolveEmissionAmount;
        }
    }
    return col;
}