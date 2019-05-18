Shader "Custom/DepthOnly"
{
    SubShader
    {
        Tags {"Queue" = "Geometry-1" "RenderType" = "Opaque" }
 
        Lighting Off
 
        Pass
        {
            ZWrite Off
            ColorMask 0
        }
    }

	Fallback "VertexLit"
}
