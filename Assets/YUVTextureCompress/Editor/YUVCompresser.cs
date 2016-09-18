using UnityEngine;
using System.Collections;

public class YUVCompresser
{

	static public void Compress2A8 (Color[] src, int sampleRateY, int sampleRateU, int sampleRateV, int sampleRateA, out float[] compressedY, out float[] compressedU, out float[] compressedV, out float[] compressedA)
	{
		if (!Mathf.IsPowerOfTwo (src.Length))
		{
			compressedY = null;
			compressedU = null;
			compressedV = null;
			compressedA = null;

			return;
		}

		float[] srcY = new float[src.Length];
		float[] srcU = new float[src.Length];
		float[] srcV = new float[src.Length];
		float[] srcA = new float[src.Length];

		for (int i = 0; i < src.Length; ++i)
		{
			srcY [i] = src [i].r;
			srcU [i] = src [i].g;
			srcV [i] = src [i].b;
			srcA [i] = src [i].a;
		}

		compressedY = Compress2A8 (srcY, sampleRateY);
		compressedU = Compress2A8 (srcU, sampleRateU);
		compressedV = Compress2A8 (srcV, sampleRateV);
		compressedA = Compress2A8 (srcA, sampleRateA);
	}

	static public float[] Compress2A8 (float[] src, int ratio)
	{
		int srcSzie = (int)System.Math.Sqrt (src.Length);
		int size = srcSzie / ratio;

		float[] compressed = new float[size * size];

		int blockSize = ratio * ratio;
		float blockValue = 0;
		for (int i = 0; i < size * size; ++i)
		{
			int srcX = i % size * ratio;
			int srcY = i / size * ratio;

			for (int x = 0; x < ratio; ++x)
			{
				for (int y = 0; y < ratio; ++y)
				{
					try
					{
						blockValue += src [srcX + x + (srcY + y) * srcSzie];
					}
					catch
					{
						Debug.LogError ("srcX [" + srcX + "]srcY[" + srcY + "] x[" + x + "]y[" + y + "] index [" + (srcX + x + (srcY + y) * srcSzie) + "]");
					}
				}
			}

			compressed [i] = blockValue / (float)blockSize;
			blockValue = 0;
		}

		return compressed;
	}
}
