using UnityEngine.Rendering;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using System;
namespace MPipeline
{
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
            data.arrayCollection = new RenderArray(true);
        }
        //Initialized In Every Scene
        public void InitScene()
        {
            if (isInitialized) return;
            isInitialized = true;
            PipelineFunctions.InitBaseBuffer(ref data.baseBuffer);
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
        public static List<PipelineEvent> preRenderEvents = new List<PipelineEvent>();
        private List<RenderTexture> temporaryTextures = new List<RenderTexture>(10);
        private PipelineEvent[] events;
        public PipelineResources resources;
        public GameObject eventParent;
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
            LoadAllEvents();
        }

        public void LoadAllEvents()
        {
            events = eventParent.GetComponentsInChildren<PipelineEvent>();
            foreach (var i in events)
            {
                i.InitEvent(resources);
            }
        }

        public void DisposeAllEvents()
        {
            foreach (var i in events)
            {
                i.DisposeEvent();
            }
            events = null;
            drawEvents.Clear();
            preRenderEvents.Clear();
        }

        private void OnDestroy()
        {
            if (singleton != this) return;
            singleton = null;
            DisposeScene();
            DisposeAllEvents();
        }

        public void BeforePipeline(Camera cam)
        {
            foreach (var i in preRenderEvents)
            {
                i.PreRenderFrame(cam);
            }
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
            data.resources = resources;
            PipelineFunctions.GetViewProjectMatrix(cam, out data.vp, out data.inverseVP);
            ref RenderArray arr = ref data.arrayCollection;
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
}