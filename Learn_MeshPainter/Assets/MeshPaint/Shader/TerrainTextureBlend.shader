Shader "meshPainter/TerrainTextureBlend" {
	Properties { 
		_Splat0("Layer 1(RGBA)", 2D) = "white" {}
		_Splat1("Layer 2(RGBA)", 2D) = "white" {}
		_Splat2("Layer 3(RGBA)", 2D) = "white" {}
		_Splat3("Layer 4(RGBA)", 2D) = "white" {}
		_Tiling3("_Tiling4 x/y", Vector) = (1,1,0,0)
		_Control("Control (RGBA)", 2D) = "white" {}
		_Weight("Blend Weight" , Range(0.001,1)) = 0.2
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue" = "Geometry"}

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf BlinnPhong
		#pragma target 3.0

		struct Input {
			float2 uv_Control : TEXCOORD0;
			float2 uv_Splat0 : TEXCOORD1;
			float2 uv_Splat1 : TEXCOORD2;
			float2 uv_Splat2 : TEXCOORD3;
			float2 uv_Splat3 : TEXCOORD4;
		};
		
		sampler2D _Control;
		sampler2D _Splat0, _Splat1, _Splat2, _Splat3;
		float _Weight;
		float4 _Tiling3;

		inline half4 Blend(half high1, half high2, half high3, half high4, half4 control)
		{
			half4 blend;

			blend.r = high1 * control.r;
			blend.g = high2 * control.g;
			blend.b = high3 * control.b;
			blend.a = high4 * control.a;

			half ma = max(blend.r, max(blend.g, max(blend.b, blend.a)));
			blend = max(blend - ma + _Weight, 0) * control;
			return blend / (blend.r + blend.g + blend.b + blend.a);
		}

		void surf(Input IN, inout SurfaceOutput o)
		{
			half4 splat_control = tex2D(_Control, IN.uv_Control).rgba;

			half4 lay1 = tex2D(_Splat0, IN.uv_Splat0);
			half4 lay2 = tex2D(_Splat1, IN.uv_Splat1);
			half4 lay3 = tex2D(_Splat2, IN.uv_Splat2);
			half4 lay4 = tex2D(_Splat3, IN.uv_Control * _Tiling3.xy);

			half4 blend = Blend(lay1.a, lay2.a, lay3.a, lay4.a, splat_control);
			o.Alpha = 0.0;
			o.Albedo.rgb = blend.r * lay1 + blend.g * lay2 + blend.b * lay3 + blend.a * lay4;//混合
		}
		ENDCG
	}
	FallBack "Diffuse"
}
