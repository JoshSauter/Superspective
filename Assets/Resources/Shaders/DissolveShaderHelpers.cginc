sampler2D _DissolveTex;
float4 _DissolveTex_ST;
float _DissolveValue;

float _DissolveBurnSize;
sampler2D _DissolveBurnRamp;
float4 _DissolveBurnColor;
float _DissolveEmissionAmount;

// Color of the material when _DissolveValue is 0, is automatically set to _Color
float4 _DissolveColorAt0;
// Color of the material when _DissolveValue is 1
float4 _DissolveColorAt1;

// Returns the dissolve test value
half ClipDissolve(float2 uv, float disabled = 0.0) {
    half test = tex2D(_DissolveTex, uv.xy).rgb - _DissolveValue - 0.01;
    if (_DissolveValue == 0) {
        test = 0;
    }
    if (_DissolveColorAt0.a == 0) clip(disabled-test);
    if (_DissolveColorAt1.a == 0) clip(disabled+test);

    return test;
}

// Returns the color after applying the Dissolve shader to the existingCol
float4 Dissolve(float2 uv, float4 existingCol, float disabled = 0) {
    half test = ClipDissolve(uv, disabled);

    float4 col = float4(0,0,0,0);
    if (test < 0) {
        col = _DissolveColorAt1;
    }
    else {
        col = _DissolveColorAt0;
    }
    if (abs(test) < _DissolveBurnSize && _DissolveValue < 1 && _DissolveValue > 0) {
        // Since this is multiplied by the color, brighten the emission for darker colors
        float multiplier = 10.0 / saturate((0.01 + max(max(existingCol.r, existingCol.g), existingCol.b)));
        float4 burn = tex2D(_DissolveBurnRamp, float2(1-(abs(test) / _DissolveBurnSize), 0)) * _DissolveBurnColor * multiplier * _DissolveEmissionAmount;
        burn.a = 0; // Don't add to the alpha
        col += burn;
    }
    return col;
}
