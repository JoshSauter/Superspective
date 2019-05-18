Shader "Custom/DimensionShaders/DimensionWall"
{
    Properties
    {
        _Dimension ("Dimension", Int) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background-1000" }
        LOD 100

		Pass
        {
			ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
			#include "DimensionShaderHelpers.cginc"

			int _Dimension;

			struct appdata {
				float4 vertex : POSITION;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
			};
			

			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				return DimensionValueToColor(_Dimension);
			}
            ENDCG
        }
    }
}
