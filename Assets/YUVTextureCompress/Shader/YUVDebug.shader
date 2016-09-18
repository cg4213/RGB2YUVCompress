//
// with correct param ,this shader can calcu the right convert matrix, with A LOT CALCULATION!!!
//DO NOT USE IN RUNTIME
//
Shader "Unlit/YUVDebug"
{
	Properties
	{
		_MainTexY ("TextureY", 2D) = "white" {}
		_MainTexU ("TextureU", 2D) = "white" {}
		_MainTexV ("TextureV", 2D) = "white" {}
		_MainTexA ("TextureA", 2D) = "white" {}
		_Wr ("Wr",float) = 0.0
		_Wb ("Wb",float) = 0.0
		_Umax("Umax",float) = 0.0
		_Vmax("Vmax",float) = 0.0
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
			float _Wr;
			float _Wb;
			float _Wg;// = 1-_Wr-_Wb;
			float _Umax;
			float _Vmax;

			int _matrixReady = 0;

			float4x4 convertMatrix;
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
					_Wg = 1-_Wr-_Wb;

				    convertMatrix = float4x4(
																		1.0,	0.0,												(1-_Wr)/_Vmax,						-0.5*(1-_Wr)/_Vmax,
																	 	1.0,	-_Wb*(1.0-_Wb)/(_Umax*_Wg),		-_Wr*(1.0-_Wr)/(_Vmax*_Wg),	0.5*_Wb*(1.0-_Wb)/(_Umax*_Wg)+0.5*_Wr*(1.0-_Wr)/(_Vmax*_Wg),
																	 	1.0,	(1-_Wb)/_Umax,							0,												-0.5*(1-_Wb)/_Umax,
																	 	0.0,	0.0,												0,												1
																	 	);

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
//				fixed4 col = hsv;
				return col;
				 
			}
			ENDCG
		}
	}
}