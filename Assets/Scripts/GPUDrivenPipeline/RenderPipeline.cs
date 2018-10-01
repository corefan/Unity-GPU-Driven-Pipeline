﻿using UnityEngine.Rendering;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using System;

public unsafe class RenderPipeline : MonoBehaviour
{
    #region STATIC_AREA
    public static RenderPipeline singleton;
    public static PipelineCommandData data;
    private static bool isConstInitialize = false;
    private static bool isInitialized = false;
    public static void InitConst()
    {
        if (isConstInitialize)
            return;
        isConstInitialize = true;
        data.constEntity.arrayCollection = new RenderArray(true);
        data.constEntity.gpuFrustumShader = Resources.Load<ComputeShader>("GpuFrustumCulling");
    }
    //Initialized In Every Scene
    public void InitScene()
    {
        if (isInitialized) return;
        isInitialized = true;
        PipelineFunctions.InitBaseBuffer(ref data.baseBuffer, "MapInfos", "MapPoints");
    }
    public void DisposeScene()
    {
        if (!isInitialized) return;
        isInitialized = false;
        PipelineFunctions.Dispose(ref data.baseBuffer);
    }
    #endregion
    // private Camera currentCam;
    public static List<PipelineEvent> drawEvents = new List<PipelineEvent>();
    private List<RenderTexture> temporaryTextures = new List<RenderTexture>(10);
    private void Awake()
    {
        if (singleton)
        {
            Debug.LogError("Render Pipeline should be Singleton!");
            Destroy(this);
            return;
        }
        singleton = this;
        data.targets = RenderTargets.Init();
        InitConst();
        InitScene();
        GC.Collect();
    }
    private void OnDestroy()
    {
        if (singleton != this) return;
        singleton = null;
        DisposeScene();
    }

    public void Render(Camera cam, RenderTexture dest)
    {
        if (!isInitialized) return;
        PipelineFunctions.InitRenderTarget(ref data.targets, cam, temporaryTextures);
        //Clear Frame
        Graphics.SetRenderTarget(data.targets.geometryColorBuffer, data.targets.depthBuffer);
        GL.Clear(true, true, Color.black);
        //Set Global Data
        data.cam = cam;
        PipelineFunctions.GetViewProjectMatrix(cam, out data.vp, out data.inverseVP);
        ref RenderArray arr = ref data.constEntity.arrayCollection;
        PipelineFunctions.GetCullingPlanes(ref data.inverseVP, arr.frustumPlanes, arr.farFrustumCorner, arr.nearFrustumCorner);
        //Start Calling Events
        foreach (var i in drawEvents)
        {
            i.FrameUpdate(ref data);
        }
        //Finalize Frame
        Graphics.Blit(data.targets.renderTarget, dest);
        PipelineFunctions.ReleaseRenderTarget(temporaryTextures);
    }
}
