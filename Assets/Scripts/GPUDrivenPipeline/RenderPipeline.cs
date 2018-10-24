using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;
namespace MPipeline
{
    public unsafe class RenderPipeline : MonoBehaviour
    {
        #region STATIC_AREA
        public static RenderPipeline singleton;
        public static PipelineCommandData data;
        private static bool isInitialized = false;
        //Initialized In Every Scene
        public void InitScene()
        {
            if (isInitialized) return;
            isInitialized = true;
            data.arrayCollection = new RenderArray(true);
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
        private List<PipelineEvent> allEvents;
        public GameObject eventParent;
        public PipelineResources resources;
        private void Awake()
        {
            if (singleton)
            {
                Debug.LogError("Render Pipeline should be Singleton!");
                Destroy(this);
                return;
            }
            singleton = this;
            InitScene();
            allEvents = new List<PipelineEvent>(eventParent.GetComponentsInChildren<PipelineEvent>());
            foreach (var i in allEvents)
                i.InitEvent(resources);
        }
        /// <summary>
        /// Add and remove Events Manually
        /// Probably cause unnecessary error, try to avoid calling this methods
        /// </summary>
        /// <param name="evt"></param>
        public void AddEventManually(PipelineEvent evt)
        {
            allEvents.Add(evt);
            evt.InitEvent(resources);
        }

        public void RemoveEventManually(PipelineEvent evt)
        {
            allEvents.Remove(evt);
            evt.DisposeEvent();
        }

        private void OnDestroy()
        {
            if (singleton != this) return;
            singleton = null;
            DisposeScene();
            foreach (var i in allEvents)
                i.DisposeEvent();
            allEvents = null;
        }

        public void Render(PipelineCamera pipelineCam, RenderTexture dest)
        {
            if (!isInitialized) return;
            //Set Global Data
            Camera cam = pipelineCam.cam;
            data.resources = resources;
            PipelineFunctions.GetViewProjectMatrix(cam, out data.vp, out data.inverseVP);
            ref RenderArray arr = ref data.arrayCollection;
            PipelineFunctions.GetCullingPlanes(ref data.inverseVP, arr.frustumPlanes, arr.farFrustumCorner, arr.nearFrustumCorner);
            //Start Calling Events
            foreach (var i in preRenderEvents)
            {
                i.PreRenderFrame(pipelineCam, ref data);
            }
            //Run job system together
            JobHandle.ScheduleBatchedJobs();
            //Start Prepare Render Targets
            
            foreach (var i in drawEvents)
            {
                i.FrameUpdate(pipelineCam, ref data);
            }
            //Finalize Frame
            Graphics.Blit(pipelineCam.targets.renderTarget, dest);

        }
    }
}