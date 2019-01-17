Shader "Unlit/LavaWave"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_WaveA ("Wave A (dir, steepness, wavelength)", Vector) = (1, 0, 0.5, 10)
		_WaveB ("Wave B ", Vector) = (1, 0, 0.5, 10)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _WaveA, _WaveB;
			
			float3 GerstnerWave(
				float4 wave, float3 p
			) {
				float steepness = wave.z;
				float wavelength = wave.w;
				float k = 2 * UNITY_PI / wavelength;
				float c = sqrt(9.8 / k);
				float2 d = normalize(wave.xy);
				float f = k * (dot(d, p.xz) - c * _Time.y);
				float a = steepness / k;

				return float3(
					d.x * (a * cos(f)),
					a * sin(f),
					d.y * (a * cos(f))
					);
			}


			v2f vert (appdata v)
			{
				v2f o;

				float3 gridPoint = v.vertex;
				float3 p = gridPoint;
				p += GerstnerWave(_WaveA, gridPoint);
				p += GerstnerWave(_WaveB, gridPoint);
				v.vertex = float4(p, 0);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
