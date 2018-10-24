using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace MPipeline
{
    [PipelineEvent(false, true)]
    public unsafe class GeometryEvent : PipelineEvent
    {
        HizDepth hizDepth;
        Material linearMat;
        public bool doubleCheck = true;
        protected override void Init(PipelineResources resources)
        {
            hizDepth = new HizDepth();
            hizDepth.InitHiZ(resources);
            linearMat = new Material(resources.linearDepthShader);
            PipelineFunctions.InitOcclusionBuffer(ref hizDepth.buffers);
            Application.targetFrameRate = int.MaxValue;
        }

        protected override void Dispose()
        {
            PipelineFunctions.DisposeOcclusionBuffer(ref hizDepth.buffers);
            hizDepth.DisposeHiZ();
        }
        public System.Func<IPerCameraData> getOcclusionData = () => new HizOcclusionData();
        public Material proceduralMaterial;
        public override void FrameUpdate(PipelineCamera cam, ref PipelineCommandData data)
        {
            HizOcclusionData hizData = IPerCameraData.GetProperty(this, cam, getOcclusionData) as HizOcclusionData;
            ref var baseBuffer = ref data.baseBuffer;
            var gpuFrustumShader = data.resources.gpuFrustumCulling;
            Graphics.SetRenderTarget(cam.targets.geometryColorBuffer, cam.targets.depthBuffer);
            GL.Clear(true, true, Color.black);
            if (doubleCheck)
            {
                PipelineFunctions.UpdateOcclusionBuffer(
                    ref data.baseBuffer, gpuFrustumShader,
                    ref hizDepth.buffers, hizData,
                    data.arrayCollection.frustumPlanes,
                    cam.cam.orthographic, baseBuffer.resultBuffer.count);
                PipelineFunctions.DrawLastFrameCullResult(ref data.baseBuffer, ref hizDepth.buffers, proceduralMaterial);
                hizData.lastFrameCameraUp = cam.transform.up;
                PipelineFunctions.ClearOcclusionData(ref data.baseBuffer, ref hizDepth.buffers, gpuFrustumShader);
                Graphics.Blit(cam.targets.depthTexture, hizData.historyDepth, linearMat, 0);
                hizDepth.GetMipMap(hizData.historyDepth);
                PipelineFunctions.OcclusionRecheck(ref data.baseBuffer, ref hizDepth.buffers, gpuFrustumShader, hizData);
                Graphics.SetRenderTarget(cam.targets.geometryColorBuffer, cam.targets.depthBuffer);
                PipelineFunctions.DrawRecheckCullResult(ref hizDepth.buffers, proceduralMaterial);
            }
            else
            {
                gpuFrustumShader.SetVector(ShaderIDs._CameraUpVector, hizData.lastFrameCameraUp);
                gpuFrustumShader.SetBuffer(5, ShaderIDs.clusterBuffer, baseBuffer.clusterBuffer);
                gpuFrustumShader.SetTexture(5, ShaderIDs._HizDepthTex, hizData.historyDepth);
                gpuFrustumShader.SetVectorArray(ShaderIDs.planes, data.arrayCollection.frustumPlanes);
                gpuFrustumShader.SetBuffer(5, ShaderIDs.resultBuffer, baseBuffer.resultBuffer);
                gpuFrustumShader.SetBuffer(5, ShaderIDs.instanceCountBuffer, baseBuffer.instanceCountBuffer);
                gpuFrustumShader.SetBuffer(PipelineBaseBuffer.ComputeShaderKernels.ClearClusterKernel, ShaderIDs.instanceCountBuffer, baseBuffer.instanceCountBuffer);
                ComputeShaderUtility.Dispatch(gpuFrustumShader, 5, baseBuffer.clusterCount, 64);
                hizData.lastFrameCameraUp = cam.transform.up;
                proceduralMaterial.SetBuffer(ShaderIDs.resultBuffer, baseBuffer.resultBuffer);
                proceduralMaterial.SetBuffer(ShaderIDs.verticesBuffer, baseBuffer.verticesBuffer);
                PipelineFunctions.RenderProceduralCommand(ref baseBuffer, proceduralMaterial);
                gpuFrustumShader.Dispatch(PipelineBaseBuffer.ComputeShaderKernels.ClearClusterKernel, 1, 1, 1);
            }
            Graphics.Blit(cam.targets.depthTexture, hizData.historyDepth, linearMat, 0);
            hizDepth.GetMipMap(hizData.historyDepth);
        }
    }
    public class HizOcclusionData : IPerCameraData
    {
        public Vector3 lastFrameCameraUp = Vector3.up;
        public RenderTexture historyDepth;
        public HizOcclusionData()
        {
            historyDepth = new RenderTexture(512, 256, 0, RenderTextureFormat.RHalf);
            historyDepth.useMipMap = true;
            historyDepth.autoGenerateMips = false;
            historyDepth.filterMode = FilterMode.Point;
            historyDepth.wrapMode = TextureWrapMode.Clamp;
        }
        public override void DisposeProperty()
        {
            historyDepth.Release();
            Object.Destroy(historyDepth);
        }
    }
}