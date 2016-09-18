using UnityEngine;
using System.Collections;
using UnityEditor;

/// <summary>
/// RGB to YUV editor.
/// NOTE: YUV can has a Gamma fix on Y chanel,makes it a Y'UV
/// NOTE: OK,yuv is really complex……YCbCr basicly uses the same function,but total different theary.the color space is quite different
/// </summary>
public class RGB2YUVEditor : EditorWindow
{

	#region window

	static RGB2YUVEditor sWindow;

	[MenuItem ("SteinFramewrok/RGB2YUV")]
	static void Open ()
	{
		if (sWindow == null)
			sWindow = EditorWindow.CreateInstance<RGB2YUVEditor> ();
		sWindow.Show ();
	}

	#endregion

	/// <summary>
	/// Convert standard.
	/// </summary>
	public enum ConvertStandard
	{
		YUVSDTV,
		YUVHDTV,
		YCbCr601,
		YCbCr709,
		YCbCr2020,
	}

	/// <summary>
	/// in debug mode ,editor will generate the converted texture,and reverted texture
	/// the reverted texture is for checking correction of the revert matrix .
	/// </summary>
	bool mDebug = false;

	string mDestPath;

	/// <summary>
	/// The source texture.
	/// </summary>
	Texture2D srcTexture;

	Texture2D convertedTexture;

	int sampleRateY = 1;
	int sampleRateU = 2;
	int sampleRateV = 2;
	int sampleRateA = 2;

	Texture2D textureA;
	Texture2D textureY;
	Texture2D textureU;
	Texture2D textureV;

	Color[] convertedColor;

	//basicly this is the best,though the differece from YCbCr709 is quite small
	ConvertStandard mCurStandard = ConvertStandard.YCbCr2020;

	Matrix4x4 mConvertMatrix;
	/// <summary>
	/// for debug texture
	/// </summary>
	Matrix4x4 mInvertMatrix;


	/// <summary>
	/// SDTV  with BT.601
	/// https://en.wikipedia.org/wiki/YUV
	/// </summary>
	void InitYUVConvertMatrixSDTV ()
	{
		mConvertMatrix = CalcuConvertMatrix (0.299f, 0.114f);
		mInvertMatrix = CalcuInvertMatrix (0.299f, 0.114f);
	}

	/// <summary>
	/// HDTV  with BT.709
	/// https://en.wikipedia.org/wiki/YUV
	/// </summary>
	void InitYUVConvertMatrixHDTV ()
	{
		mConvertMatrix = CalcuConvertMatrix (0.2126f, 0.0722f);		
		mInvertMatrix = CalcuInvertMatrix (0.2126f, 0.0722f);	

	}

	/// <summary>
	/// ITU_R BT.601
	/// https://en.wikipedia.org/wiki/YCbCr
	/// </summary>
	void InitYCbCrConvertMatrix601 ()
	{
		mConvertMatrix = CalcuConvertMatrix (0.299f, 0.114f, 0.5f, 0.5f);

		mInvertMatrix = CalcuInvertMatrix (0.299f, 0.114f, 0.5f, 0.5f);
	}

	/// <summary>
	/// ITU_R BT.709
	/// https://en.wikipedia.org/wiki/YCbCr
	/// </summary>
	void InitYCbCrConvertMatrix709 ()
	{
		mConvertMatrix = CalcuConvertMatrix (0.2126f, 0.0722f, 0.5f, 0.5f);
		mInvertMatrix = CalcuInvertMatrix (0.2126f, 0.0722f, 0.5f, 0.5f);
	}

	/// <summary>
	/// ITU_R BT.2020
	/// https://en.wikipedia.org/wiki/YCbCr
	/// </summary>
	void InitYCbCrConvertMatrix2020 ()
	{
		mConvertMatrix = CalcuConvertMatrix (0.2627f, 0.0593f, 0.5f, 0.5f);
		mInvertMatrix = CalcuInvertMatrix (0.2627f, 0.0593f, 0.5f, 0.5f);
	}

	Matrix4x4 CalcuConvertMatrix (float wr, float wb, float umax = 0.436f, float vmax = 0.615f)
	{
		Matrix4x4 convertMatrix = new Matrix4x4 ();

		float wg = 1 - wr - wb;

		convertMatrix.m00 = wr;
		convertMatrix.m01 = wg;
		convertMatrix.m02 = wb;
		convertMatrix.m03 = 0;

		float scaleU = umax / (1 - wb);

		convertMatrix.m10 = -scaleU * wr;
		convertMatrix.m11 = -scaleU * wg;
		convertMatrix.m12 = scaleU * (1 - wb);
		convertMatrix.m13 = 0.5f;

		float scaluV = vmax / (1 - wr);

		convertMatrix.m20 = scaluV * (1 - wr);
		convertMatrix.m21 = -scaluV * wg;
		convertMatrix.m22 = -scaluV * wb;
		convertMatrix.m23 = 0.5f;

		convertMatrix.m30 = 0;
		convertMatrix.m31 = 0;
		convertMatrix.m32 = 0;
		convertMatrix.m33 = 1;

		return convertMatrix;
	}

	Matrix4x4 CalcuInvertMatrix (float wr, float wb, float umax = 0.436f, float vmax = 0.615f)
	{
		Matrix4x4 convertMatrix = new Matrix4x4 ();
		float wg = 1 - wr - wb;

		convertMatrix.m00 = 1;
		convertMatrix.m01 = 0;
		convertMatrix.m02 = (1 - wr) / vmax;
		convertMatrix.m03 = -0.5f * convertMatrix.m02;


		convertMatrix.m10 = 1;
		convertMatrix.m11 = -wb * (1 - wb) / (umax * wg);
		convertMatrix.m12 = -wr * (1 - wr) / (vmax * wg);
		convertMatrix.m13 = 0.5f * wb * (1 - wb) / (umax * wg) + 0.5f * wr * (1 - wr) / (vmax * wg);


		convertMatrix.m20 = 1;
		convertMatrix.m21 = (1 - wb) / umax;
		convertMatrix.m22 = 0;
		convertMatrix.m23 = -0.5f * (1 - wb) / umax;

		convertMatrix.m30 = 0;
		convertMatrix.m31 = 0;
		convertMatrix.m32 = 0;
		convertMatrix.m33 = 1;

		return convertMatrix;
	}

	void RGBToYUV (Color rgb, out Color convertedColor)
	{
		float a = rgb.a;
		rgb.a = 1;
		convertedColor = mConvertMatrix * rgb;

		convertedColor.a = a;
	}

	void YUVToRGB (Color yuv, out Color invertedColor)
	{
		float a = yuv.a;
		yuv.a = 1;
		invertedColor = mInvertMatrix * yuv;
		invertedColor.a = a;
	}

	void OnGUI ()
	{
		mDebug = EditorGUILayout.Toggle ("DebugMode:", mDebug);

		EditorGUILayout.BeginHorizontal ();
		mDestPath = EditorGUILayout.TextField ("Convert Dest Path:", mDestPath);
		if (GUILayout.Button ("select path"))
		{
			mDestPath = EditorUtility.OpenFolderPanel ("Convert Dest Path", string.Empty, string.Empty);
			if (mDestPath.StartsWith (Application.dataPath))
			{
				mDestPath = mDestPath.Remove (0, Application.dataPath.Length + 1 - "Assets/".Length);
				mDestPath += "/";
			}
			else
			{
				EditorUtility.DisplayDialog ("Invalid Path", "Select a folder under Assets", "Got It!");
			}
		}
		EditorGUILayout.EndHorizontal ();

		mCurStandard = (ConvertStandard)EditorGUILayout.EnumPopup ("Convert Standard:", mCurStandard);

		srcTexture = EditorGUILayout.ObjectField ("source texutre:", srcTexture, typeof(Texture2D), false) as Texture2D;

		sampleRateY = EditorGUILayout.IntField ("ChannelY compress rate:", sampleRateY);
		sampleRateU = EditorGUILayout.IntField ("ChannelU compress rate:", sampleRateU);
		sampleRateV = EditorGUILayout.IntField ("ChannelV compress rate:", sampleRateV);
		sampleRateA = EditorGUILayout.IntField ("ChannelA compress rate:", sampleRateA);

		if (GUILayout.Button ("Convert"))
		{
			if (srcTexture == null)
				return;
			//init convert matrix with select standard
			switch (mCurStandard)
			{
				case ConvertStandard.YUVSDTV:
					InitYUVConvertMatrixSDTV ();
					break;
				case ConvertStandard.YUVHDTV:
					InitYUVConvertMatrixHDTV ();
					break;
				case ConvertStandard.YCbCr601:
					InitYCbCrConvertMatrix601 ();
					break;
				case ConvertStandard.YCbCr709:
					InitYCbCrConvertMatrix709 ();
					break;
				case ConvertStandard.YCbCr2020:
					InitYCbCrConvertMatrix2020 ();
					break;
			}
			//get source color array
			Color[] rgbPixels = srcTexture.GetPixels ();

			//init result array
			Color[] YUVColors = new Color[rgbPixels.Length];
			Color[] InvertColors = new Color[rgbPixels.Length];

			Color tmpColor = new Color ();
			//do convert
			for (int i = 0; i < rgbPixels.Length; ++i)
			{
				RGBToYUV (rgbPixels [i], out tmpColor);
				YUVColors [i] = tmpColor;

				YUVToRGB (YUVColors [i], out tmpColor);
				InvertColors [i] = tmpColor;
			}
			//generate converted texture
			convertedTexture = new Texture2D (srcTexture.width, srcTexture.height);
			convertedTexture.SetPixels (YUVColors);

			if (mDebug)
			{
				CreateTexture (convertedTexture, mDestPath + srcTexture.name + "_" + mCurStandard.ToString () + ".png");

				Texture2D invertTexture = new Texture2D (srcTexture.width, srcTexture.height);
				invertTexture.SetPixels (InvertColors);

				CreateTexture (invertTexture, mDestPath + srcTexture.name + "_" + mCurStandard.ToString () + "_Inverted.png");
			}

			float[] compressedY;
			float[] compressedU;
			float[] compressedV;
			float[] compressedA;

			YUVCompresser.Compress2A8 (convertedTexture.GetPixels (), sampleRateY, sampleRateU, sampleRateV, sampleRateA, out compressedY, out compressedU, out compressedV, out compressedA);

			textureY = BuildA8Texture (srcTexture.width / sampleRateY, srcTexture.height / sampleRateY, compressedY);
			textureU = BuildA8Texture (srcTexture.width / sampleRateU, srcTexture.height / sampleRateU, compressedU);
			textureV = BuildA8Texture (srcTexture.width / sampleRateV, srcTexture.height / sampleRateV, compressedV);
			textureA = BuildA8Texture (srcTexture.width / sampleRateA, srcTexture.height / sampleRateA, compressedA);

			CreateTexture (textureY, mDestPath + srcTexture.name + "_" + mCurStandard.ToString () + "_y.png");
			CreateTexture (textureU, mDestPath + srcTexture.name + "_" + mCurStandard.ToString () + "_u.png");
			CreateTexture (textureV, mDestPath + srcTexture.name + "_" + mCurStandard.ToString () + "_v.png");
			CreateTexture (textureA, mDestPath + srcTexture.name + "_" + mCurStandard.ToString () + "_a.png");
		}

		//invert matrix display
		for (int r = 0; r < 4; r++)
		{
			EditorGUILayout.BeginHorizontal ();
			Vector4 vectorR = mInvertMatrix.GetRow (r);
			EditorGUILayout.LabelField (vectorR.x.ToString ());
			EditorGUILayout.LabelField (vectorR.y.ToString ());
			EditorGUILayout.LabelField (vectorR.z.ToString ());
			EditorGUILayout.LabelField (vectorR.w.ToString ());
			EditorGUILayout.EndHorizontal ();
		}
			
	}

	Texture2D BuildA8Texture (int w, int h, float[] alpha)
	{
		Color[] alphaColor = new Color[alpha.Length];
		for (int i = 0; i < alpha.Length; ++i)
		{
			alphaColor [i] = new Color (0, 0, 0, alpha [i]);
		}
		Texture2D texture = new Texture2D (w, h);
		texture.SetPixels (alphaColor);
		return texture;
	}

	void CreateTexture (Texture2D texture, string assetPath)
	{
		string path = AssetDatabase.GenerateUniqueAssetPath (assetPath);

		using (var outstream = new System.IO.StreamWriter (path))
		{
			using (var outfile = new System.IO.BinaryWriter (outstream.BaseStream))
			{
				outfile.Write (texture.EncodeToPNG ());
			}
		}
		AssetDatabase.Refresh ();
	}
}
