﻿using System.Collections;
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
        private Material cubeDepthMaterial;
        private ComputeBuffer sphereBuffer;
        private ComputeBuffer sphereIndirectBuffer;
        private NativeArray<int> indicesArray;
        private CubeCullingBuffer cubeBuffer;
        private int shadowCount = 0;
        private int unShadowCount = 0;
        protected override void Init(PipelineResources resources)
        {
            cubeBuffer = new CubeCullingBuffer();
            CubeFunction.Init(ref cubeBuffer);
            pointLightMaterial = new Material(resources.pointLightShader);
            cubeDepthMaterial = new Material(resources.cubeDepthShader);
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
            indirect.Dispose();
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

            cullJobHandler.Complete();
            UnsafeUtility.ReleaseGCObject(gcHandler);
            pointLightMaterial.SetBuffer(ShaderIDs.verticesBuffer, sphereBuffer);
            //Un Shadow Point light
            Graphics.SetRenderTarget(cam.targets.colorBuffer, cam.targets.depthBuffer);
            for (int c = 0; c < unShadowCount; c++) {
                var i = cullJob.indices[cullJob.length - c];
                MPointLight light = MPointLight.allPointLights[i];
                pointLightMaterial.SetVector(ShaderIDs._LightColor, light.color);
                pointLightMaterial.SetVector(ShaderIDs._LightPos, new Vector4(light.position.x, light.position.y, light.position.z, light.range));
                pointLightMaterial.SetFloat(ShaderIDs._LightIntensity, light.intensity);
                pointLightMaterial.SetPass(0);
                Graphics.DrawProceduralIndirect(MeshTopology.Triangles, sphereIndirectBuffer);
            }
            //TODO
            if (shadowCount > 0)
            {
                NativeArray<Vector4> positions = new NativeArray<Vector4>(shadowCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < shadowCount; i++)
                {
                    MPointLight light = MPointLight.allPointLights[cullJob.indices[i]];
                    positions[i] = new Vector4(light.position.x, light.position.y, light.position.z, light.range);
                }
                CubeFunction.UpdateLength(ref cubeBuffer, shadowCount);
                var cullShader = data.resources.pointLightFrustumCulling;
                CubeFunction.SetBuffer(ref cubeBuffer, ref data.baseBuffer, cullShader);
                CubeFunction.PrepareDispatch(ref cubeBuffer, cullShader, positions);
                for (int i = 0; i < shadowCount; i++)
                {
                    MPointLight light = MPointLight.allPointLights[cullJob.indices[i]];
                    CubeFunction.DrawShadow(light, ref cubeBuffer, ref data.baseBuffer, cullShader, i, cubeDepthMaterial);
                    Graphics.SetRenderTarget(cam.targets.colorBuffer, cam.targets.depthBuffer);
                    pointLightMaterial.SetVector(ShaderIDs._LightColor, light.color);
                    pointLightMaterial.SetVector(ShaderIDs._LightPos, positions[i]);
                    pointLightMaterial.SetFloat(ShaderIDs._LightIntensity, light.intensity);
                    pointLightMaterial.SetTexture(ShaderIDs._CubeShadowMap, light.shadowmapTexture);
                    pointLightMaterial.SetPass(1);
                    Graphics.DrawProceduralIndirect(MeshTopology.Triangles, sphereIndirectBuffer);
                }
                positions.Dispose();
            }
            //Shadow Point Light
            indicesArray.Dispose();
        }

        protected override void Dispose()
        {
            Destroy(pointLightMaterial);
            Destroy(cubeDepthMaterial);
            sphereBuffer.Dispose();
            sphereIndirectBuffer.Dispose();
            CubeFunction.Dispose(ref cubeBuffer);
        }
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
