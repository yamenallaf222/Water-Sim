using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public static class Shapes
{
    public delegate JobHandle ScheduleDelegate(
    NativeArray<float3x4> positions, NativeArray<float3x4> normals, int resolution, float4x4 trs, JobHandle dependency        
    );

    public static float4x2 IndexTo4UV (int i, float resolution, float invResolution) 
    {
        float4x2 uv;
        float4 i4 = 4f * i + float4(0f, 1f, 2f, 3f);
        uv.c1 = floor(invResolution * i4 + 0.00001f);
        uv.c0 = invResolution * (i4 - resolution * uv.c1 + 0.5f);
        uv.c1 = invResolution * (uv.c1 + 0.5f);
        return uv;
    }

    public struct Point4
    {
        public float4x3 positions, normals;
    }


    public interface IShape
    {
		public Point4 GetPoint4 (int i, float resolution, float invResolution);
    }


    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<S> : IJobFor where S : struct, IShape
    {

        [WriteOnly]
        NativeArray<float3x4> positions, normals;

        public float3x4 positionTRS, normalTRS;

        public float resolution, invResolution;



        public void Execute(int i)
        {
            float4x2 uv;

            float4 i4 = 4f * i + float4(0f, 1f, 2f, 3f);
            uv.c1 = floor(invResolution * i4 + 0.00001f);
            uv.c0 = invResolution * (i4 - resolution * uv.c1 + 0.5f) - 0.5f;
            uv.c1 = invResolution * (uv.c1 + 0.5f) - 0.5f;

            Point4 p = default(S).GetPoint4(i, resolution, invResolution);


            positions[i] = transpose(positionTRS.TransformVectors(p.positions));


            float3x4 n = transpose(normalTRS.TransformVectors(p.normals, 0f));

            normals[i] = float3x4(normalize(n.c0), normalize(n.c1), normalize(n.c2), normalize(n.c3));

        }




        public static JobHandle ScheduleParallel(
            NativeArray<float3x4> positions, NativeArray<float3x4> normals, int resolution, float4x4 trs, JobHandle dependency
        )
        {

            float4x4 tim = transpose(inverse(trs));

            return new Job<S> 
            {
                positions = positions,
                resolution = resolution,
                normals = normals,
                positionTRS = trs.Get3x4(),
                normalTRS = transpose(inverse(trs)).Get3x4(),
                invResolution = 1f / resolution

            }.ScheduleParallel(positions.Length, resolution, dependency);
        }

    }

    public struct Plane : IShape
    {

        public Point4 GetPoint4 (int i, float resolution, float invResolution) {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution);
            return new Point4 {
                positions = float4x3(uv.c0 - 0.5f, 0f, uv.c1 - 0.5f),
                normals = float4x3(0f, 1f, 0f)
            };
        }
    }
}
