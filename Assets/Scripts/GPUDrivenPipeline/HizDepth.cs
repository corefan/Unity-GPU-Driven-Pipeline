﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace MPipeline
{
    public unsafe struct HizDepth
    {
        private RenderTexture backupMip;
        private Material getLodMat;
        private MaterialPropertyBlock block;
        public void InitHiZ(PipelineResources resources)
        {
            block = new MaterialPropertyBlock();
            const int depthRes = 256;
            backupMip = new RenderTexture(depthRes * 2, depthRes, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
            backupMip.useMipMap = true;
            backupMip.autoGenerateMips = false;
            backupMip.enableRandomWrite = true;
            backupMip.wrapMode = TextureWrapMode.Clamp;
            backupMip.filterMode = FilterMode.Point;
            getLodMat = new Material(resources.HizLodShader);
        }
        public void GetMipMap(RenderTexture depthMipTexture, CommandBuffer buffer)
        {
            for (int i = 1; i < 8; ++i)
            {
                block.SetTexture(ShaderIDs._MainTex, depthMipTexture);
                block.SetInt(ShaderIDs._PreviousLevel, i - 1);
                buffer.SetRenderTarget(backupMip, i);
                buffer.DrawMesh(GraphicsUtility.mesh, Matrix4x4.identity, getLodMat, 0, 0, block);
                block.SetTexture(ShaderIDs._MainTex, backupMip);
                block.SetInt(ShaderIDs._PreviousLevel, i);
                buffer.SetRenderTarget(depthMipTexture, i);
                buffer.DrawMesh(GraphicsUtility.mesh, Matrix4x4.identity, getLodMat, 0, 0, block);
            }
        }
        public void DisposeHiZ()
        { 
            backupMip.Release();
            Object.Destroy(backupMip);
        }
    }
}