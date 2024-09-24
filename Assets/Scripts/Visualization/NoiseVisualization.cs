using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using static Noise;
using static Unity.Mathematics.math;

public class NoiseVisualization : MonoBehaviour
{
    public readonly struct SmallXXHash4
    {
	    const uint primeB = 0b10000101111010111100101001110111;
	    const uint primeC = 0b11000010101100101010111000111101;
	    const uint primeD = 0b00100111110101001110101100101111;
	    const uint primeE = 0b00010110010101100110011110110001;


        readonly uint4 accumulator;

        public SmallXXHash4 (uint4 accumulator) {
            this.accumulator = accumulator;
        }

        public static SmallXXHash4 operator + (SmallXXHash4 h, int v) => h.accumulator + (uint) v;

        public static implicit operator SmallXXHash4 (uint4 accumulator) =>
            new SmallXXHash4(accumulator);

        public static SmallXXHash4 Seed (int4 seed) => (uint4)seed + primeE;
        
        static uint4 RotateLeft (uint4 data, int steps) =>
            (data << steps) | (data >> 32 - steps);


        public uint4 BytesA => (uint4)this & 255;
        
        public uint4 BytesB => ((uint4)this >> 8) & 255;

        public uint4 BytesC => ((uint4)this >> 16) & 255;

        public uint4 BytesD => (uint4)this >> 24;


        public float4 Floats01A => (float4) BytesA * (1f / 255f);
        
        public float4 Floats01B => (float4)BytesB * (1f / 255f);

        public float4 Floats01C => (float4)BytesC * (1f / 255f);

        public float4 Floats01D => (float4)BytesD * (1f / 255f);


                public SmallXXHash4 Eat (int4 data) =>
            RotateLeft(accumulator + (uint4)data * primeC, 17) * primeD;

        public static implicit operator uint4 (SmallXXHash4 hash) {
            uint4 avalanche = hash.accumulator;

            avalanche ^= avalanche >> 15;
            avalanche *= primeB;
            avalanche ^= avalanche >> 13;
            avalanche *= primeC;
            avalanche ^= avalanche >> 16;

            return avalanche;
        }

        public static SmallXXHash4 select (SmallXXHash4 a, SmallXXHash4 b, bool4 c) => math.select(a.accumulator, b.accumulator, c);

        public uint4 GetBits(int count, int shift)
        {
            return ((uint4)this >> shift) & (uint)( (1 << count) - 1);
        }

        public float4 GetBitsAsFloats01(int count, int shift)
        {
            return (float4)GetBits(count, shift) * (1f / (( 1 << count) - 1));
        }
    }


    
    static int
        normalsId = Shader.PropertyToID("_Normals"),
        positionsId = Shader.PropertyToID("_Positions"),
        configId = Shader.PropertyToID("_Config"),
        noiseId = Shader.PropertyToID("_Noise");



    [SerializeField]
    Mesh instanceMesh;

    [SerializeField]
    Material material;


    [SerializeField, Range(1, 512)]
    int resolution = 16;

    [SerializeField, Range(- 0.5f, 0.5f)]
    float displacement = 0.1f;


    [SerializeField, Range(0.1f, 10f)]
	float instanceScale = 2f;

    [SerializeField]
    Settings noiseSettings = Settings.Default;

    NativeArray<float3x4> positions, normals;
    NativeArray<float4> noise;

    


    [SerializeField]
    SpaceTRS domain = new SpaceTRS{scale = 2f};

    ComputeBuffer noiseBuffer;
    ComputeBuffer positionsBuffer, normalsBuffer;


    Bounds bounds;


    MaterialPropertyBlock propertyBlock;

    
    // bool isDirty;
    


    static Shapes.ScheduleDelegate shapeJob = Shapes.Job<Shapes.Plane>.ScheduleParallel;
    static ScheduleDelegate noiseJob = Job<Simplex2D<Simplex>>.ScheduleParallel;


     private void OnEnable() {
        
        // isDirty = true;

        int length = resolution * resolution;

        length = length / 4 + (length & 1);

        positions = new NativeArray<float3x4>(length, Allocator.Persistent);

        normals = new NativeArray<float3x4>(length, Allocator.Persistent);


        positionsBuffer = new ComputeBuffer(length * 4, 3 * 4);

        normalsBuffer = new ComputeBuffer(length * 4, 3 * 4);

        propertyBlock ??= new MaterialPropertyBlock();

        noise = new NativeArray<float4>(length, Allocator.Persistent);

        noiseBuffer = new ComputeBuffer(length * 4, 4);

        propertyBlock.SetBuffer(noiseId, noiseBuffer);

        propertyBlock.SetBuffer(positionsId, positionsBuffer);

        propertyBlock.SetBuffer(normalsId, normalsBuffer);

        propertyBlock.SetVector(configId, new Vector4(resolution, instanceScale/ resolution, displacement));

        }

    private void OnDisable() {
        
        positions.Dispose();
        normals.Dispose();
        positionsBuffer.Release();
        normalsBuffer.Release();

        positionsBuffer = null;
        normalsBuffer = null;

        noise.Dispose();
        noiseBuffer.Release();
        noiseBuffer = null;

    }


    private void OnValidate() {
        
        if(positionsBuffer != null && enabled)
        {
            OnDisable();
            OnEnable();
        }

    }


    private void Update() {
        
        // if(isDirty || transform.hasChanged)
        // {
            // isDirty = false;

            // transform.hasChanged = false;

            noiseJob(positions, noise, noiseSettings, domain, resolution, Time.time, shapeJob(positions, normals, resolution,transform.localToWorldMatrix, default)).Complete();

            noiseBuffer.SetData(noise.Reinterpret<uint>( 4 * 4));     

            positionsBuffer.SetData(positions);
            normalsBuffer.SetData(normals);


            bounds = new Bounds(transform.position, float3(2f * cmax(abs(transform.lossyScale)) + displacement));


        // }


        Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, bounds, resolution * resolution, propertyBlock);
    }

}
