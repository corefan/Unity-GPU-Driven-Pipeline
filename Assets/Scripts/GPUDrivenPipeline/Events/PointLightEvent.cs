using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
namespace MPipeline
{
    [PipelineEvent(true, true)]
    public unsafe class PointLightEvent : PipelineEvent
    {
        private ulong gcHandler;
        private MPointLightEvent cullJob;
        private JobHandle cullJobHandler;
        private Material pointLightMaterial;
        private ComputeBuffer sphereBuffer;
        private ComputeBuffer sphereIndirectBuffer;
        private NativeArray<int> indicesArray;
        private int shadowCount = 0;
        private int unShadowCount = 0;
        protected override void Init(PipelineResources resources)
        {
            pointLightMaterial = new Material(resources.pointLightShader);
            Vector3[] vertices = resources.sphereMesh.vertices;
            int[] triangle = resources.sphereMesh.triangles;
            NativeArray<Vector3> allVertices = new NativeArray<Vector3>(triangle.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < allVertices.Length; ++i)
            {
                allVertices[i] = vertices[triangle[i]];
            }
            sphereBuffer = new ComputeBuffer(allVertices.Length, sizeof(Vector3));
            sphereBuffer.SetData(allVertices);
            allVertices.Dispose();
            sphereIndirectBuffer = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
            NativeArray<uint> indirect = new NativeArray<uint>(5, Allocator.Temp, NativeArrayOptions.ClearMemory);
            indirect[0] = (uint)sphereBuffer.count;
            indirect[1] = 1;
            sphereIndirectBuffer.SetData(indirect);
        }

        public override void PreRenderFrame(PipelineCamera cam, ref PipelineCommandData data)
        {
            cullJob.planes = (Vector4*)UnsafeUtility.PinGCArrayAndGetDataAddress(data.arrayCollection.frustumPlanes, out gcHandler);
            indicesArray = new NativeArray<int>(MPointLight.allPointLights.Count, Allocator.Temp);
            cullJob.indices = (int*)indicesArray.GetUnsafePtr();
            cullJob.shadowCount = (int*) UnsafeUtility.AddressOf(ref shadowCount);
            cullJob.unShadowCount = (int*)UnsafeUtility.AddressOf(ref unShadowCount);
            cullJob.length = indicesArray.Length - 1;
            shadowCount = 0;
            unShadowCount = 0;
            cullJobHandler = cullJob.Schedule(MPointLight.allPointLights.Count, 32);
        }

        public override void FrameUpdate(PipelineCamera cam, ref PipelineCommandData data)
        {
            Graphics.SetRenderTarget(cam.targets.colorBuffer, cam.targets.depthBuffer);
            cullJobHandler.Complete();
            UnsafeUtility.ReleaseGCObject(gcHandler);
            pointLightMaterial.SetBuffer(ShaderIDs.verticesBuffer, sphereBuffer);
            //Un Shadow Point light
            for(int c = 0; c < unShadowCount; c++) {
                var i = cullJob.indices[cullJob.length - c];
                MPointLight light = MPointLight.allPointLights[i];
                pointLightMaterial.SetFloat(ShaderIDs._LightRange, 1f / light.range);
                pointLightMaterial.SetVector(ShaderIDs._LightColor, light.color);
                pointLightMaterial.SetVector(ShaderIDs._LightPos, light.position);
                pointLightMaterial.SetFloat(ShaderIDs._LightIntensity, light.intensity);
                pointLightMaterial.SetPass(0);
                Graphics.DrawProceduralIndirect(MeshTopology.Triangles, sphereIndirectBuffer);
            }
            //TODO
            //Shadow Point Light
            indicesArray.Dispose();
        }

        protected override void Dispose()
        {
            Destroy(pointLightMaterial);
            sphereBuffer.Dispose();
            sphereIndirectBuffer.Dispose();
        }
    }

    public unsafe struct PointLightDatas
    {
        
    }

    public unsafe struct MPointLightEvent : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction]
        public Vector4* planes;
        [NativeDisableUnsafePtrRestriction]
        public int* indices;
        [NativeDisableUnsafePtrRestriction]
        public int* shadowCount;
        [NativeDisableUnsafePtrRestriction]
        public int* unShadowCount;
        public int length;
        public void Execute(int index)
        {
            MPointLight cube = MPointLight.allPointLights[index];
            if (PipelineFunctions.FrustumCulling(cube.position, cube.range, planes))
            {
                if(cube.useShadow)
                {
                    int last = Interlocked.Increment(ref *shadowCount) - 1;
                    indices[last] = index;
                }else
                {
                    int last = Interlocked.Increment(ref *unShadowCount) - 1;
                    indices[length - last] = index;
                }
            }
        }
    }

}
