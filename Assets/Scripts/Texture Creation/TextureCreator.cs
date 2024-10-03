using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEngine;

public static class TextureCreator
{
    public static Texture2D generateTextureOfNoise(NativeArray<float4> noise)
    {
        //because each element in noise contains 4 noise values
        Texture2D texture2D = new Texture2D(noise.Length * 4, noise.Length * 4);
        
        for(int x = 0 ; x < texture2D.width; x+= 4)
        {
            for(int y = 0 ; y < texture2D.height; y+= 4)
            {
                //folding this 2d array in single-dimensioned array to access  noise properly
                int noiseIndex = (int)floor(x * 0.25f) * texture2D.height + (int) floor(y * 0.25f);
                float4 noiseVector = noise[noiseIndex];
                
                Color color1 = new Color(noiseVector.x, noiseVector.x, noiseVector.x);  // grayscale
                Color color2 = new Color(noiseVector.y, noiseVector.y, noiseVector.y);  // grayscale
                Color color3 = new Color(noiseVector.z, noiseVector.z, noiseVector.z);  // grayscale
                Color color4 = new Color(noiseVector.w, noiseVector.w, noiseVector.w);  // grayscale

                //all of this so the offset is not diagonal on the 2d array that is the texture but rather linear on the single-dimensioned array of that texture
                //this offset to account for the 4 pixels needed to be filled in each iteration

                int X = (int) (noiseIndex - floor(y * 0.25f) / texture2D.height * 0.25f);
                int Y = (int) ((noiseIndex - floor(x * 0.25f) * texture2D.height) * 0.25f);
                
                int XPlus1 =(int) ( (noiseIndex + 1) - floor(y * 0.25f) / texture2D.height * 0.25f);
                int YPlus1 = (int) (((noiseIndex + 1) - floor(x * 0.25f) * texture2D.height) * 0.25f);
                
                int XPlus2 = (int) ((noiseIndex + 2) - floor(y * 0.25f) / texture2D.height * 0.25f);
                int YPlus2 = (int) (((noiseIndex + 2) - floor(x * 0.25f) * texture2D.height) * 0.25f);
                
                int XPlus3 = (int) ((noiseIndex + 3) - floor(y * 0.25f) / texture2D.height * 0.25f);
                int YPlus3 = (int) (((noiseIndex + 3) - floor(x * 0.25f) * texture2D.height) * 0.25f);

                texture2D.SetPixel(X, Y, color1);
                texture2D.SetPixel(XPlus1, YPlus1, color2);
                texture2D.SetPixel(XPlus2, YPlus2, color3);
                texture2D.SetPixel(XPlus3, YPlus3, color4);
            }
        }

        return texture2D;
    }
}
