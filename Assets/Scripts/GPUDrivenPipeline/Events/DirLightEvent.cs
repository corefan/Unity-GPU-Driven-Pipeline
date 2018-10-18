using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        protected override void Init(PipelineResources resources)
        {
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

        public override void FrameUpdate(ref PipelineCommandData data)
        {
            if (SunLight.current == null) return;
            int pass;
            if (SunLight.current.enableShadow)
            {
                PipelineFunctions.DrawShadow(data.cam, data.resources.gpuFrustumCulling, ref data.arrayCollection, ref data.baseBuffer, ref SunLight.current.settings, ref SunLight.shadMap, cascadeShadowMapVP, shadowFrustumVP);
                PipelineFunctions.UpdateShadowMaskState(shadMaskMaterial, ref SunLight.shadMap, cascadeShadowMapVP);
                /*   CheckTex(data.cam);
                   Graphics.SetRenderTarget(volumetricTex);
                   GL.Clear(false, true, Color.black);
                   shadMaskMaterial.SetPass(2);
                   Graphics.DrawMeshNow(GraphicsUtility.mesh, Matrix4x4.identity);
                   Graphics.Blit(volumetricTex, data.targets.renderTarget);*/
                pass = 0;
            }
            else
            {
                pass = 1;
            }
            Graphics.SetRenderTarget(data.targets.colorBuffer, data.targets.depthBuffer);
            Shader.SetGlobalVector(ShaderIDs._LightPos, -SunLight.shadMap.shadCam.forward);
            shadMaskMaterial.SetPass(pass);
            Graphics.DrawMeshNow(GraphicsUtility.mesh, Matrix4x4.identity);

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