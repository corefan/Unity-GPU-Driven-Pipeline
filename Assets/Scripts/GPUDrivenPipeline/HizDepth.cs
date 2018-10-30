using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MPipeline
{
    public unsafe struct HizDepth
    {
        private RenderTexture backupMip;
        private Material getLodMat;
        public void InitHiZ(PipelineResources resources)
        {
            const int depthRes = 256;
            backupMip = new RenderTexture(depthRes * 2, depthRes, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
            backupMip.useMipMap = true;
            backupMip.autoGenerateMips = false;
            backupMip.enableRandomWrite = true;
            backupMip.wrapMode = TextureWrapMode.Clamp;
            backupMip.filterMode = FilterMode.Point;
            getLodMat = new Material(resources.HizLodShader);
        }
        public void GetMipMap(RenderTexture depthMipTexture)
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
            backupMip.Release();
            Object.Destroy(backupMip);
        }
    }
}