using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEngine;
using System;

public static class TextureCreator
{
    public static Texture2D generateTextureOfNoise(NativeArray<float4> noise)
{
    int sideLength = Mathf.CeilToInt(Mathf.Sqrt(noise.Length)); // Assuming the noise array is square-sized

    Texture2D texture2D = new Texture2D(sideLength * 2, sideLength * 2, TextureFormat.RGBA32, true);
    
    int noiseIndex = 0;

    for (int x = 0; x < texture2D.width; x += 2)
    {
        for (int y = 0; y < texture2D.height; y += 2)
        {
            if (noiseIndex >= noise.Length)
                return texture2D; // Exit and return if noiseIndex is out of bounds

            float4 noiseVector = noise[noiseIndex++];
            
            // Normalize or clamp values to [0,1]
            Color color1 = new Color(Mathf.Clamp01(noiseVector.x), Mathf.Clamp01(noiseVector.x), Mathf.Clamp01(noiseVector.x));
            Color color2 = new Color(Mathf.Clamp01(noiseVector.y), Mathf.Clamp01(noiseVector.y), Mathf.Clamp01(noiseVector.y));
            Color color3 = new Color(Mathf.Clamp01(noiseVector.z), Mathf.Clamp01(noiseVector.z), Mathf.Clamp01(noiseVector.z));
            Color color4 = new Color(Mathf.Clamp01(noiseVector.w), Mathf.Clamp01(noiseVector.w), Mathf.Clamp01(noiseVector.w));

            texture2D.SetPixel(x, y, color1);
            texture2D.SetPixel(x, y + 1, color2);
            texture2D.SetPixel(x + 1, y, color3);
            texture2D.SetPixel(x + 1, y + 1, color4);
        }
    }

    texture2D.Apply(); // Apply changes to the GPU
    return texture2D;
}

}
