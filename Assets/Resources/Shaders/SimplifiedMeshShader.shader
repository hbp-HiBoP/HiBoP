
Shader "Custom/SimplifiedMeshShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
	SubShader{
		Tags{ "RenderType" = "Opaque" }

		CGPROGRAM

		#pragma surface surf CustomLight

		half4 LightingCustomLight(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
		{
			half3 h = normalize(lightDir + viewDir);
			half diff = max(0, dot(s.Normal, h) * 0.7 + 0.3);
			float nh = max(0, dot(s.Normal, viewDir));
			float spec = pow(nh, 70.0);

			half4 c;
			c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * atten;
			c.a = s.Alpha;
			return c;
		}

		struct Input
		{
			float2 uv_MainTex;
		};

		sampler2D _MainTex;

		void surf(Input IN, inout SurfaceOutput o)
		{
			o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
		}

		ENDCG
	}

	Fallback "Diffuse"
}