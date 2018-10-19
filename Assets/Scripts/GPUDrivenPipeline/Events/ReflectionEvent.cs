using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
namespace MPipeline
{
    [PipelineEvent(true, true)]
    public unsafe class ReflectionEvent : PipelineEvent
    {
        private static readonly int _ReflectionProbe = Shader.PropertyToID("_ReflectionProbe");
        private Material reflectMaterial;
        private ulong gcHandler;
        private ReflectionCullJob cullJob;
        private JobHandle cullJobHandler;

        protected override void Init(PipelineResources resources)
        {
            reflectMaterial = new Material(resources.reflectionShader);
        }

        protected override void Dispose()
        {
            Destroy(reflectMaterial);
        }

        public override void PreRenderFrame(ref PipelineCommandData data)
        {
            cullJob.planes = (Vector4*)UnsafeUtility.PinGCArrayAndGetDataAddress(data.arrayCollection.frustumPlanes, out gcHandler);
            cullJob.resultIndices = new NativeList<int>(ReflectionCube.allCubes.Count, Allocator.Temp);
            cullJobHandler = cullJob.Schedule(ReflectionCube.allCubes.Count, 32);
        }

        public override void FrameUpdate(ref PipelineCommandData data)
        {
            Graphics.SetRenderTarget(data.targets.colorBuffer, data.targets.depthBuffer);
            cullJobHandler.Complete();
            UnsafeUtility.ReleaseGCObject(gcHandler);
            //Use Result Indices
            foreach(int i in cullJob.resultIndices)
            {
                ReflectionCube cube = ReflectionCube.allCubes[i];
                reflectMaterial.SetTexture(_ReflectionProbe, cube.reflectionCube);
                reflectMaterial.SetPass(0);
                Graphics.DrawMeshNow(GraphicsUtility.cubeMesh, cube.localToWorld);
            }
            //TODO
            cullJob.resultIndices.Dispose();
        }
    }

    public unsafe struct ReflectionCullJob : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction]
        public Vector4* planes;
        public NativeList<int> resultIndices;
        public void Execute(int index)
        {
            ReflectionCube cube = ReflectionCube.allCubes[index];
            if (PipelineFunctions.FrustumCulling(ref cube.localToWorld, new Vector3(0.5f, 0.5f, 0.5f), planes))
            {
#if UNITY_EDITOR        //For debugging
                if (!resultIndices.ConcurrentAdd(index))
                {
                    Debug.LogError("Reflection culling Out of range");
                }
#else
                resultIndices.ConcurrentAdd(index);
#endif
            }
        }
    }
}