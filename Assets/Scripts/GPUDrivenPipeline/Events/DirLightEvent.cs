using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace MPipeline
{
    [PipelineEvent(false, true)]
    public unsafe class DirLightEvent : PipelineEvent
    {
        
        private Material shadMaskMaterial;
        private RenderTexture volumetricTex;
        public ComputeShader shader;
        private static int[] _Count = new int[2];
        private Matrix4x4[] cascadeShadowMapVP = new Matrix4x4[4];
        private Vector4[] shadowFrustumVP = new Vector4[6];
        private MaterialPropertyBlock lightBlock;

        protected override void Init(PipelineResources resources)
        {
            lightBlock = new MaterialPropertyBlock();
            shadMaskMaterial = new Material(resources.shadowMaskShader);
            for (int i = 0; i < cascadeShadowMapVP.Length; ++i)
            {
                cascadeShadowMapVP[i] = Matrix4x4.identity;
            }
        }


        protected override void Dispose()
        {
            Destroy(shadMaskMaterial);
            if (volumetricTex) volumetricTex.Release();
            Destroy(volumetricTex);
            volumetricTex = null;
        }

        public override void FrameUpdate(PipelineCamera cam, ref PipelineCommandData data, CommandBuffer buffer)
        {
            if (SunLight.current == null) return;
            int pass;
            lightBlock.Clear();
            if (SunLight.current.enableShadow)
            {
                PipelineFunctions.UpdateShadowMapState(ref SunLight.shadMap, lightBlock, ref SunLight.current.settings, buffer);
                PipelineFunctions.DrawShadow(cam.cam, data.resources.gpuFrustumCulling, buffer, ref data.baseBuffer, ref SunLight.current.settings, ref SunLight.shadMap, cascadeShadowMapVP, shadowFrustumVP);
                PipelineFunctions.UpdateShadowMaskState(lightBlock, ref SunLight.shadMap, cascadeShadowMapVP);
                pass = 0;
            }
            else
            {
                pass = 1;
            }
            buffer.SetRenderTarget(cam.targets.renderTargetIdentifier, cam.targets.depthIdentifier);
            buffer.SetGlobalVector(ShaderIDs._LightPos, -SunLight.shadMap.shadCam.forward);
            buffer.DrawMesh(GraphicsUtility.mesh, Matrix4x4.identity, shadMaskMaterial, 0, pass, lightBlock);
        }

        private void CheckTex(Camera cam)
        {
            Vector2Int size = new Vector2Int(cam.pixelWidth / 8, cam.pixelHeight / 8);
            if (volumetricTex == null)
            {
                goto GETNEW;
            }
            else if (volumetricTex.width != size.x || volumetricTex.height != size.y)
            {
                goto RELEASELAST;
            }
            else
            {
                return;
            }
            RELEASELAST:
            volumetricTex.Release();
            Destroy(volumetricTex);
            GETNEW:
            volumetricTex = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            volumetricTex.enableRandomWrite = true;
        }
    }
}