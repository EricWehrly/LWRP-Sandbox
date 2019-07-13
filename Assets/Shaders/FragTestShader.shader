// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/TerrainTest" {

	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		testTexture("Texture", 2D) = "white"{}
		testScale("Scale", Float) = 1
	}
		SubShader
	{ 
		Tags { "RenderType" = "Opaque" }
		LOD 200

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

			const static int maxLayerCount = 8;
			const static float epsilon = 1E-4;

			int layerCount;
			float3 baseColors[maxLayerCount];
			float baseStartHeights[maxLayerCount];
			float baseBlends[maxLayerCount];
			float baseColorStrength[maxLayerCount];
			float baseTextureScales[maxLayerCount];

			float minHeight;
			float maxHeight;

			sampler2D testTexture;
			float testScale;

			UNITY_DECLARE_TEX2DARRAY(baseTextures);

			struct appdata {
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD0;
				half3 worldNormal : TEXCOORD1;
			};

			float inverseLerp(float a, float b, float value) {
				return saturate((value - a) / (b - a));
			}

			float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
				float3 scaledWorldPos = worldPos / scale;
				float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
				float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
				float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
				return xProjection + yProjection + zProjection;
			}

			v2f vert(float4 vertex : POSITION, float3 normal : NORMAL)
			{
				v2f o;
				o.worldPos = mul(unity_ObjectToWorld, vertex);
				o.vertex = UnityObjectToClipPos(vertex);
				o.worldNormal = UnityObjectToWorldNormal(normal);
				return o;
			}

			fixed4 _Color;
			fixed4 frag(v2f IN) : SV_Target
			{
				// _Color.r = 0;
				// _Color.g = 0;

				float heightPercent = inverseLerp(minHeight,maxHeight, IN.worldPos.y);
				float3 blendAxes = abs(IN.worldNormal);
				blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

				fixed3 col = _Color;
				for (int i = 0; i < layerCount; i++) {
					float lerpA = -baseBlends[i] / 2.0 - epsilon;
					float lerpB = baseBlends[i] / 2.0;
					float lerpC = heightPercent - baseStartHeights[i];
					float drawStrength = inverseLerp(lerpA, lerpB, lerpC);

					float3 baseColor = baseColors[i] * baseColorStrength[i];
					float3 textureColor = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1 - baseColorStrength[i]);
					// col = textureColor;

					// o.Albedo = o.Albedo * (1 - drawStrength) + (baseColor + textureColor) * drawStrength;
					col = col * (1 - drawStrength) + (baseColor + textureColor) * drawStrength;
				}
				_Color.rgb = col;

				// _Color = tex2D(testTexture, IN.worldPos.yz / testScale);
				// UNITY_APPLY_FOG(IN.fogCoord, _Color);
				return _Color;
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
