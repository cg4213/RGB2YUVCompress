Shader "Unlit/YCbCr2020"
{
	Properties
	{
		_MainTexY ("TextureY", 2D) = "white" {}
		_MainTexU ("TextureU", 2D) = "white" {}
		_MainTexV ("TextureV", 2D) = "white" {}
		_MainTexA ("TextureA", 2D) = "white" {}
		}
	SubShader
	{
		LOD 100
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		Pass
		{
			Cull Off
			Lighting Off
			ZWrite Off
			Fog { Mode Off }
			Offset -1, -1
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
				fixed4 color : COLOR;
			};

			sampler2D _MainTexY;
			sampler2D _MainTexU;
			sampler2D _MainTexV;
			sampler2D _MainTexA;

			int _matrixReady = 0;
			float4x4 convertMatrix;
			//does not work……
//			const float4x4 convertMatrix = float4x4(
//																		1,		0,													1.4746,							-0.7373,
//																	 	1,		-0.1645531,									-0.5713531,					0.3679531,
//																	 	1,		1.8814,											0,									-0.9407,
//																	 	0,		0,													0,									1
//																	 	);

			v2f o;

			v2f vert (appdata v)
			{
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.texcoord = v.texcoord;
				o.color = v.color;
				return o;
			}



			float4 hsv_to_rgb(float4 HSV)
			{
				if(_matrixReady ==0)
				{			 
					convertMatrix = float4x4(
																		1,		0,													1.4746,							-0.7373,
																	 	1,		-0.1645531,									-0.5713531,					0.3679531,
																	 	1,		1.8814,											0,									-0.9407,
																	 	0,		0,													0,									1
																	 	);
					_matrixReady = 1;
				}

				float a = HSV.a;
				HSV.a =1;
				float4 RGB = mul(convertMatrix, HSV);
				RGB.a = a;
				return (RGB);
			}

			fixed4 frag (v2f IN) : Color
			{
				// sample the texture
				float4 hsv;
				 hsv.r =	 tex2D(_MainTexY, IN.texcoord).a;
				 hsv.g = tex2D(_MainTexU, IN.texcoord).a;
				 hsv.b = tex2D(_MainTexV, IN.texcoord).a;
				 hsv.a= tex2D(_MainTexA, IN.texcoord).a;

				fixed4 col = hsv_to_rgb(hsv);
				return col;
				 
			}
			ENDCG
		}
	}
}