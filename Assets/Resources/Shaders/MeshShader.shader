Shader "Custom/Brain"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB)", 2D) = "white" {}
		_AoTex("AO (RGB)", 2D) = "white" {}
		_ColorTex("Color map (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Atlas("Atlas", int) = 0
	}

	SubShader
	{
		CGPROGRAM
			#pragma surface surf Standard
			#pragma target 3.0

			sampler2D _MainTex;
			sampler2D _AoTex;
			sampler2D _ColorTex;

			int _Atlas;
			half _Glossiness;
			half _Metallic;
			fixed4 _Color;

			uniform int _StrongCuts;
			uniform int _CutCount;
			uniform float3 _CutPoints[20];
			uniform float3 _CutNormals[20];

			struct Input
			{
				float4 pos : SV_POSITION;
				float2 uv_MainTex : TEXCOORD0;
				float2 uv2_AoTex :   TEXCOORD1;
				float2 uv3_ColorTex :   TEXCOORD2;
				float4 vertex_col : COLOR;
				float3 worldPos;
			};

			float is_clipped(Input IN)
			{
				float3 localPos = IN.worldPos - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
				float clipping = 1;
				if (!_StrongCuts)
				{
					for (int i = 0; i < _CutCount && i < 20; ++i)
					{
						int value = sign(dot(_CutNormals[i], _CutPoints[i] - localPos));
						if (value < 0)
						{
							clipping = -1;
						}
						else
						{
							clipping = 1;
							break;
						}
					}
				}
				else
				{
					for (int i = 0; i < _CutCount && i < 20; ++i)
					{
						int value = sign(dot(_CutNormals[i], _CutPoints[i] - localPos));
						if (value < 0)
						{
							clipping = -1;
							break;
						}
					}
				}
				return clipping;
			}

			void display_atlas(Input IN, inout SurfaceOutputStandard o)
			{
				if (IN.vertex_col.a > 0)
				{
					o.Albedo = IN.vertex_col.rgb * _Color;
				}
				else
				{
					o.Albedo = tex2D(_MainTex, IN.uv_MainTex);
				}
				o.Alpha = 1.f;
			}

			void display_ieeg(Input IN, inout SurfaceOutputStandard o)
			{
				fixed4 col;
				fixed4 ao = tex2D(_AoTex, IN.uv2_AoTex);
				float color = ao.r * 2.5; // boost alpha (because of low tri mesh density compated to cuts textures)
				if (color > 1) color = 1;
				col = (1 - color) * tex2D(_MainTex, IN.uv_MainTex) + (color)* tex2D(_ColorTex, IN.uv3_ColorTex);
				col *= _Color;
				o.Albedo = col.rgb;
				o.Alpha = 1.f;
			}

			void surf(Input IN, inout SurfaceOutputStandard o)
			{
				clip(is_clipped(IN));
							
				if (_Atlas)
				{
					display_atlas(IN, o);
				}
				else
				{
					display_ieeg(IN, o);
				}

				o.Metallic =  _Metallic;
				o.Smoothness = _Glossiness;
			}

		ENDCG
	}

		FallBack "Diffuse"
}
