Shader "Custom/DepthOnly"
{
    SubShader
    {
        Tags {"Queue" = "Overlay" "RenderType" = "Opaque" }
 
        Lighting Off
 
        Pass
        {
            ZWrite Off
            ColorMask 0
        }
    }

	Fallback "VertexLit"
}
