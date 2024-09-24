using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using static NoiseVisualization;
using UnityEditor.ShaderGraph.Internal;

public static partial class Noise
{
    
    public delegate JobHandle ScheduleDelegate (
		NativeArray<float3x4> positions, NativeArray<float4> noise,
		Settings settings, SpaceTRS domainTRS, int resolution, float time, JobHandle dependency
	);


    [Serializable]
	public struct Settings
	{
		public int seed;

		[Range(1, 6)]
		public int octaves;

		[Range(2, 4)]
		public int lacunarity;

		[Min(1)]
		public int frequency;
		[Range(0f, 1f)]
		public float persistence;


		public static Settings Default => new Settings{
			frequency = 4,
			octaves = 1
		};
	}


    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	public struct Job<N> : IJobFor where N : struct, INoise 
    {
        [ReadOnly]
		public NativeArray<float3x4> positions;

		[WriteOnly]
		public NativeArray<float4> noise;

		public Settings settings;

		public float3x4 domainTRS;

		public float time;

		public void Execute (int i) {
			
			float4x3 position = domainTRS.TransformVectors(transpose(positions[i]));
			position.c0 += sin(time);
			var hash = SmallXXHash4.Seed(settings.seed);
			int frequency = settings.frequency;
			

			float amplitude = 1f, amplitudeSum = 0f;
			float4 sum = 0f;

				for (int o = 0; o < settings.octaves; o++) {
					sum += amplitude *  default(N).GetNoise4(frequency * position, hash + o, frequency);
					amplitudeSum += amplitude;
					frequency *= settings.lacunarity;
					amplitude *= settings.persistence;
				}
				noise[i] = sum / amplitudeSum;
		}

        public static JobHandle ScheduleParallel (
			NativeArray<float3x4> positions, NativeArray<float4> noise,
			Settings settings, SpaceTRS domainTRS, int resolution	, float time, JobHandle dependency
		) => new Job<N> {
			positions = positions,
			noise = noise,
			settings = settings,
			time = time,
			domainTRS = domainTRS.Matrix,
		}.ScheduleParallel(positions.Length, resolution, dependency);
	
	
    }

	public interface INoise
    {
		float4 GetNoise4 (float4x3 positions, SmallXXHash4 hash, int frequency);
		
    }

	
    public struct Simplex2D<G> : INoise where G: struct, IGradient
    {
		public float4 GetNoise4 (float4x3 positions, SmallXXHash4 hash, int frequency) 
		{
			positions *= frequency * (1f / sqrt(3f));

            float4 skew = (positions.c0 + positions.c2) * ((sqrt(3f) - 1f) / 2f);
			float4 sx = positions.c0 + skew, sz = positions.c2 + skew;

			int4
				x0 = (int4)floor(sx), x1 = x0 + 1,
				z0 = (int4)floor(sz), z1 = z0 + 1;	
            
			bool4 xGz = sx - x0 > sz - z0;

			int4 xC = select(x0, x1, xGz), zC = select(z0, z1, xGz);

			SmallXXHash4 h0 = hash.Eat(x0), h1 = hash.Eat(x1),
			hC = SmallXXHash4.select(h0, h1, xGz);

			return default(G).EvaluateCombined(
				Kernel(h0.Eat(z0), x0, z0, positions) +
				Kernel(h1.Eat(z1), x1, z1, positions) + 
				Kernel(hC.Eat(zC), xC, zC, positions)
			);
		}

        static float4 Kernel (SmallXXHash4 hash, float4 lx, float4 lz, float4x3 positions) 
        {
            float4 unskew = (lx + lz) * ((3f - sqrt(3f)) / 6f);
			float4 x = positions.c0 - lx + unskew, z = positions.c2 - lz + unskew;
			float4 f = 0.5f - x * x - z * z;
			f = f * f * f * 8f;
			return max(f, 0f) * default(G).Evaluate(hash, x, z);
		}
    }





}