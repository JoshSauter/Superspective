float3 _MinRenderZone;
float3 _MaxRenderZone;

void ClipRenderZone(float3 worldPos) {
    float3 zeroSizeBox = step(_MaxRenderZone, _MinRenderZone);
    const float ignore = max(max(zeroSizeBox.x, zeroSizeBox.y), zeroSizeBox.z);
    if (ignore) return;

    float3 s = step(_MinRenderZone, worldPos) - step(_MaxRenderZone, worldPos);
    const float insideBox = s.x * s.y * s.z;
    clip(insideBox-1);
}
