


Shader "Custom/InvisibleMeshShader"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		//_MainTex("Base (RGB)", 2D) = "white" {}
		//_AoTex("AO (RGB)", 2D) = "white" {}
		//_ColorTex("Color map (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent" }


		CGPROGRAM

		// Physically based Standard lighting model, and enable shadows on all light types
		//#pragma surface surf Standard fullforwardshadows alpha:blend decal:add 
#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

	struct Input
	{
		float4 pos : SV_POSITION;
	};

	half _Glossiness;
	half _Metallic;
	fixed4 _Color;

	void surf(Input IN, inout SurfaceOutputStandard o)
	{		
		o.Albedo = _Color.rgb;
		o.Alpha = 1.f;
		o.Metallic = _Metallic;
		o.Smoothness = _Glossiness;
	}

	ENDCG
	}

		FallBack "Diffuse"
}
