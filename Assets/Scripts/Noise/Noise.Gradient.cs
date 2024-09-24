using Unity.Mathematics;
using static Unity.Mathematics.math;
using static NoiseVisualization;
public static partial class Noise
{
    public interface IGradient
    {
        public float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y); 
        float4 EvaluateCombined(float4 value);
    }

    public static class BaseGradients
    {
        static float4x2 SquareVectors(SmallXXHash4 hash)
        {
            float4x2 v;
            v.c0 = hash.Floats01A * 2f - 1f;
            v.c1 = 0.5f - abs(v.c0);
            v.c0 -= floor(v.c0 + 0.5f);

            return v;
        }
        public static float4 Circle(SmallXXHash4 hash, float4 x, float4 y)
        {
            float4x2 v = SquareVectors(hash);
            return (v.c0 * x + v.c1 * y) * rsqrt(v.c0 * v.c0 + v.c1 * v.c1);
        }  
    }

    public struct Simplex : IGradient
    {
        public float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y) => BaseGradients.Circle(hash, x, y) * (5.832f / sqrt(2f));
        
        public float4 EvaluateCombined(float4 value) => value;        

    }



}
