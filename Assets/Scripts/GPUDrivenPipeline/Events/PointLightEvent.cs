using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
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
            cullJob.resultIndices = new NativeList<int>(MPointLight.allPointLights.Count, Allocator.Temp);
            cullJobHandler = cullJob.Schedule(MPointLight.allPointLights.Count, 32);
        }

        public override void FrameUpdate(PipelineCamera cam, ref PipelineCommandData data)
        {
            Graphics.SetRenderTarget(cam.targets.colorBuffer, cam.targets.depthBuffer);
            cullJobHandler.Complete();
            UnsafeUtility.ReleaseGCObject(gcHandler);
            pointLightMaterial.SetBuffer(ShaderIDs.verticesBuffer, sphereBuffer);
            foreach (var i in cullJob.resultIndices)
            {
                MPointLight light = MPointLight.allPointLights[i];
                pointLightMaterial.SetFloat(ShaderIDs._LightRange, 1f / light.range);
                pointLightMaterial.SetVector(ShaderIDs._LightColor, light.color);
                pointLightMaterial.SetVector(ShaderIDs._LightPos, light.position);
                pointLightMaterial.SetFloat(ShaderIDs._LightIntensity, light.intensity);
                pointLightMaterial.SetPass(0);
                Graphics.DrawProceduralIndirect(MeshTopology.Triangles, sphereIndirectBuffer);
            }
            cullJob.resultIndices.Dispose();
        }

        protected override void Dispose()
        {
            Destroy(pointLightMaterial);
            sphereBuffer.Dispose();
            sphereIndirectBuffer.Dispose();
        }
    }

    public unsafe struct MPointLightEvent : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction]
        public Vector4* planes;
        public NativeList<int> resultIndices;
        public void Execute(int index)
        {
            MPointLight cube = MPointLight.allPointLights[index];
            if (PipelineFunctions.FrustumCulling(cube.position, cube.range, planes))
            {
#if UNITY_EDITOR        //For debugging
                if (!resultIndices.ConcurrentAdd(index))
                {
                    Debug.LogError("Point Light culling Out of range");
                }
#else
                resultIndices.ConcurrentAdd(index);
#endif
            }
        }
    }
}
