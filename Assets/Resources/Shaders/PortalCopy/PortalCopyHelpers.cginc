float3 _PortalPos;
float3 _PortalNormal;

void ClipPortalCopy(float3 worldPos, float disabled = 0.0) {
    float clipTest = -dot(worldPos - _PortalPos, _PortalNormal);// + 0.0001;
    if (clipTest == 0) clipTest = 1;
    clipTest = saturate(clipTest);
    clip(disabled+clipTest);
}