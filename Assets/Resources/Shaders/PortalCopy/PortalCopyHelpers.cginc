float3 _PortalPos;
float3 _PortalNormal;
float _FudgeDistance;

void ClipPortalCopy(float3 worldPos) {
    float3 adjustedCenter = _PortalPos + _PortalNormal * _FudgeDistance;
    float clipTest = dot(adjustedCenter - worldPos, _PortalNormal);
    if (clipTest == 0) clipTest = 1;
    clip(clipTest);
}
