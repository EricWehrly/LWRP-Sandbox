// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Noise/Diffuse3D" {

	Properties
	{
		_Color("Color", Color) = (1,1,1,1)

		_Factor1("Factor 1", float) = 1
		_Factor2("Factor 2", float) = 1
		_Factor3("Factor 3", float) = 1
	}
		SubShader
	{ Tags { "RenderType" = "Opaque" } LOD 200

	Pass
	{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 3.0
		#pragma glsl
		// #include "ImprovedPerlinNoise3D.cginc"
		#pragma multi_compile_fog

		#include "UnityCG.cginc"
           
			float _Factor1;
			float _Factor2;
			float _Factor3;

		struct appdata
		{
			float4 vertex : POSITION;
		};

		struct v2f
		{
			float3 noiseUV : TEXCOORD0;
			UNITY_FOG_COORDS(1)
			float4 vertex : SV_POSITION;
		};

		float noise(half2 uv)
		{
			return frac(sin(dot(uv, float2(_Factor1, _Factor2))) * _Factor3);
		}

		fixed4 _Color;
		v2f vert(appdata v)
		{

			v2f o;
			UNITY_INITIALIZE_OUTPUT(v2f,o);
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.noiseUV = mul(unity_ObjectToWorld, v.vertex).xyz;
			UNITY_TRANSFER_FOG(o,o.vertex);
			return o;
		}

		fixed4 frag(v2f i) : SV_Target
		{

			//uncomment this for fractal noise
			//float n = fBm(i.noiseUV, 4);

			//uncomment this for turbulent noise
			float n = noise(i.noiseUV);

		//uncomment this for ridged multi fractal
		//float n = ridgedmf(i.noiseUV, 4, 1.0);
		fixed4 col = _Color;
		col.rgb *= n;
		UNITY_APPLY_FOG(i.fogCoord, col);
		return col;
	}
	ENDCG
}
	}
		FallBack "Diffuse"
}
