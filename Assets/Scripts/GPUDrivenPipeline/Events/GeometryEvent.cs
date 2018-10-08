using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace MPipeline
{
    public unsafe class GeometryEvent : PipelineEvent
    {
        #region HIZDEPTH
        public HizDepth hizDepth;
        #endregion
        protected override void Awake()
        {
            base.Awake();
            hizDepth.InitHiZ();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            hizDepth.DisposeHiZ();
        }

        public Material proceduralMaterial;
        public override void FrameUpdate(ref PipelineCommandData data)
        {
            int kernel = hizDepth.DrawHizDepth(ref data);
            ref var baseBuffer = ref data.baseBuffer;
            ref var constEntity = ref data.constEntity;
            Graphics.SetRenderTarget(data.targets.geometryColorBuffer, data.targets.depthBuffer);
            Shader.SetGlobalMatrix(ShaderIDs._InvVP, data.inverseVP);
            PipelineFunctions.SetShaderBuffer(ref baseBuffer);
            PipelineFunctions.SetBaseBuffer(ref baseBuffer, constEntity.gpuFrustumShader, constEntity.arrayCollection.frustumPlanes, kernel);
            if (kernel > 0)
            {
                PipelineFunctions.SetHizOccBuffer(ref data, hizDepth.depthMipTexture, constEntity.gpuFrustumShader, kernel);
            }
            PipelineFunctions.RunCullDispatching(ref baseBuffer, constEntity.gpuFrustumShader, kernel);
            PipelineFunctions.RenderProceduralCommand(ref baseBuffer, proceduralMaterial);
        }
        public override void PreRenderFrame(Camera cam)
        {
            throw new System.NotImplementedException();
        }
    }
    [System.Serializable]
    public unsafe struct HizDepth
    {
        private ComputeShader cullingShader;
        private ComputeBuffer allCubeBuffer;
        private ComputeBuffer instanceCountBuffer;
        private ComputeBuffer resultBuffer;
        private ComputeBuffer verticesBuffer;
        private RenderTexture backupMip;
        private Material getLodMat;
        private Material mat;
        [System.NonSerialized]
        public RenderTexture depthMipTexture;
        public bool enableHiz;
        public Transform[] occluderTransforms;
        public Mesh cube;

        public void InitHiZ()
        {
            if (occluderTransforms.Length == 0)
            {
                enableHiz = false;
                return;
            }
            cullingShader = Resources.Load<ComputeShader>("OccluderCulling");
            allCubeBuffer = new ComputeBuffer(occluderTransforms.Length, Matrix3x4.SIZE);
            resultBuffer = new ComputeBuffer(occluderTransforms.Length, Matrix3x4.SIZE);
            instanceCountBuffer = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
            verticesBuffer = new ComputeBuffer(36, 16);
            const int depthRes = 256;
            depthMipTexture = new RenderTexture(depthRes, depthRes, 16, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            backupMip = new RenderTexture(depthRes, depthRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            backupMip.useMipMap = true;
            backupMip.autoGenerateMips = false;
            depthMipTexture.useMipMap = true;
            depthMipTexture.autoGenerateMips = false;
            depthMipTexture.filterMode = FilterMode.Point;
            depthMipTexture.wrapMode = TextureWrapMode.Clamp;
            backupMip.filterMode = FilterMode.Point;
            getLodMat = new Material(Shader.Find("Hidden/GetLOD"));
            mat = new Material(Shader.Find("Unlit/IndirectDepth"));
            int[] triangle = cube.triangles;
            Vector3[] vertices = cube.vertices;
            NativeArray<Vector4> vecs = new NativeArray<Vector4>(triangle.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < triangle.Length; ++i)
            {
                vecs[i] = vertices[triangle[i]];
            }
            NativeArray<Matrix3x4> allInfos = new NativeArray<Matrix3x4>(occluderTransforms.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            Matrix3x4* input = (Matrix3x4*)allInfos.GetUnsafePtr();
            for (int i = 0; i < allInfos.Length; ++i)
            {
                input[i] = new Matrix3x4(occluderTransforms[i].localToWorldMatrix);
            }
            allCubeBuffer.SetData(allInfos);
            verticesBuffer.SetData(vecs);
            vecs.Dispose();
            allInfos.Dispose();
        }
        private void ExecuteComputeShader(Vector4[] frustumPlanes)
        {
            cullingShader.SetBuffer(0, ShaderIDs.allCubeBuffer, allCubeBuffer);
            cullingShader.SetBuffer(0, ShaderIDs.instanceCountBuffer, instanceCountBuffer);
            cullingShader.SetBuffer(1, ShaderIDs.instanceCountBuffer, instanceCountBuffer);
            cullingShader.SetBuffer(0, ShaderIDs.resultBuffer, resultBuffer);
            cullingShader.SetVectorArray(ShaderIDs.planes, frustumPlanes);
            cullingShader.Dispatch(1, 1, 1, 1);
            ComputeShaderUtility.Dispatch(cullingShader, 0, allCubeBuffer.count, 64);
        }
        private void DrawOccluder(ref PipelineCommandData data)
        {
            mat.SetBuffer(ShaderIDs.verticesBuffer, verticesBuffer);
            mat.SetBuffer(ShaderIDs.resultBuffer, resultBuffer);
            Graphics.SetRenderTarget(depthMipTexture, 0);
            GL.Clear(true, true, Color.white);
            mat.SetPass(0);
            Graphics.DrawProceduralIndirect(MeshTopology.Triangles, instanceCountBuffer);
        }
        private void GetMipMap()
        {
            for (int i = 1; i < 8; ++i)
            {
                getLodMat.SetTexture(ShaderIDs._MainTex, depthMipTexture);
                getLodMat.SetInt(ShaderIDs._PreviousLevel, i - 1);
                Graphics.SetRenderTarget(backupMip, i);
                getLodMat.SetPass(0);
                Graphics.DrawMeshNow(GraphicsUtility.mesh, Matrix4x4.identity);
                getLodMat.SetTexture(ShaderIDs._MainTex, backupMip);
                getLodMat.SetInt(ShaderIDs._PreviousLevel, i);
                Graphics.SetRenderTarget(depthMipTexture, i);
                getLodMat.SetPass(0);
                Graphics.DrawMeshNow(GraphicsUtility.mesh, Matrix4x4.identity);
            }
        }
        public void DisposeHiZ()
        {
            if (occluderTransforms.Length == 0)
                return;
            Object.Destroy(getLodMat);
            allCubeBuffer.Dispose();
            resultBuffer.Dispose();
            instanceCountBuffer.Dispose();
            verticesBuffer.Dispose();
            Resources.UnloadAsset(cullingShader);
            depthMipTexture.Release();
            backupMip.Release();
            Object.Destroy(backupMip);
            Object.Destroy(depthMipTexture);
        }
        /// <summary>
        /// Draw and get compute shader kernel
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns> Compute Shader Kernel
        public int DrawHizDepth(ref PipelineCommandData data)
        {
            if (!enableHiz) return PipelineBaseBuffer.ComputeShaderKernels.ClusterCullKernel;
            ExecuteComputeShader(data.constEntity.arrayCollection.frustumPlanes);
            DrawOccluder(ref data);
            GetMipMap();
            return PipelineBaseBuffer.ComputeShaderKernels.ClusterCullOccKernel;
        }
    }
}