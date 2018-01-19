// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Site"
{
	Properties
	{
		_Color("Color",Color) = (1.0,1.0,1.0,1.0)
		_SelectedColor("Selected Color", Color) = (0.0, 0.0, 0.0, 1.0)
		_SelectedWidth("Selected Width", Range(0.0, 10.0)) = 0.5
	}
	SubShader
	{
		Tags { "Queue"="Transparent" }
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			CGPROGRAM
			
			// Pragmas
			#pragma vertex vert
			#pragma fragment frag
			
			// User Defined Variables
			uniform float4 _Color;
			
			// Base Input Structs
			struct vertexInput
			{
				float4 vertex : POSITION;
			};
			struct vertexOutput
			{
				float4 pos : SV_POSITION;
			};
			
			// Vertex Function
			vertexOutput vert(vertexInput v)
			{
				vertexOutput o;
				o.pos = UnityObjectToClipPos(v.vertex);
				return o;			
			}
			
			// Fragment Function
			float4 frag(vertexOutput i) : COLOR
			{
				return _Color;
			}
			
			ENDCG
		}
		/*
		Pass
		{
			Cull Off
			ZWrite On
			ColorMask RGB
			CGPROGRAM
			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag

			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				fixed4 color : COLOR;
			};
			
			uniform float _SelectedWidth;
			uniform float4 _SelectedColor;

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				float3 norm   = normalize(mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal));
				float2 offset = TransformViewToProjection(norm.xy);
				o.pos.xy += offset * o.pos.z * _SelectedWidth;
				o.color = _SelectedColor;
				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{
				return i.color;
			}
			ENDCG
		}
		*/
	}
}