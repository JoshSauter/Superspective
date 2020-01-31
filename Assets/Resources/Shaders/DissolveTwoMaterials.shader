Shader "Custom/DissolveTwoMaterials" {
    Properties {
        _Color ("Color at 0", Color) = (0,0,0,0)
        _Color2 ("Color at 1", Color) = (0,0,0,0)
        _MainTex("Dissolve Texture (RGB)", 2D) = "white" {}
        _DissolveValue("Dissolve Amount", Range(0.0, 1.0)) = 0
 
        _BurnSize("Burn Size", Range(0.0, 1.0)) = 0.15
        _BurnRamp("Burn Ramp (RGB)", 2D) = "white" {}
        _BurnColor("Burn Color", Color) = (1,1,1,1)
 
        _EmissionAmount("Emission amount", float) = 2.0
    }
    SubShader {
        Tags { "RenderType"="DissolveDoubleSided" }
        LOD 200
        Cull Off
        CGPROGRAM
        #pragma surface surf Lambert addshadow
        #pragma target 3.0
 
        fixed4 _Color;
        fixed4 _Color2;
        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _BurnRamp;
        fixed4 _BurnColor;
        float _BurnSize;
        float _DissolveValue;
        float _EmissionAmount;
 
        struct Input {
            float2 uv_MainTex;
        };
 
 
        void surf (Input IN, inout SurfaceOutput o) {
            half test = tex2D(_MainTex, IN.uv_MainTex).rgb - _DissolveValue;
			if (_Color.a == 0) clip(-test);
			if (_Color2.a == 0) clip(test);

			if (test < 0) {
				if (-test < _BurnSize && _DissolveValue < 1) {
					o.Emission = tex2D(_BurnRamp, float2(-test * (1 / _BurnSize), 0)) * _BurnColor * _EmissionAmount;
				}
				
				o.Albedo = _Color2.rgb;
				o.Alpha = _Color2.a;
			}
			else {
				if (test < _BurnSize && _DissolveValue < 1) {
					o.Emission = tex2D(_BurnRamp, float2(test * (1 / _BurnSize), 0)) * _BurnColor * _EmissionAmount;
				}

				o.Albedo = _Color.rgb;
				o.Alpha = _Color.a;
			}
        }
        ENDCG
    }
    FallBack "Diffuse"
}