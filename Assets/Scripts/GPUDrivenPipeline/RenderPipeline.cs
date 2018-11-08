﻿using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine.Rendering;
namespace MPipeline
{
    public unsafe class RenderPipeline : MonoBehaviour
    {
        #region STATIC_AREA
        public enum CameraRenderingPath
        {
            Unlit, Forward, GPUDeferred
        }
        public static RenderPipeline singleton;
        public static PipelineCommandData data;
        public static Dictionary<CameraRenderingPath, DrawEvent> allDrawEvents = new Dictionary<CameraRenderingPath, DrawEvent>();
        private CommandBuffer buffer;
        //Initialized In Every Scene
        public void InitScene()
        {
            data.arrayCollection = new RenderArray(true);
            PipelineFunctions.InitBaseBuffer(ref data.baseBuffer);
        }
        public void DisposeScene()
        {
            PipelineFunctions.Dispose(ref data.baseBuffer);
        }
        #endregion
        private List<PipelineEvent> allEvents;
        public PipelineResources resources;
        private void Awake()
        {
            if (singleton)
            {
                Debug.LogError("Render Pipeline should be Singleton!");
                Destroy(gameObject);
                return;
            }
            buffer = new CommandBuffer();
            DontDestroyOnLoad(this);
            singleton = this;
            InitScene();
            allEvents = new List<PipelineEvent>(GetComponentsInChildren<PipelineEvent>());
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
            buffer.Dispose();
        }

        public void Render(CameraRenderingPath path, PipelineCamera pipelineCam, RenderTexture dest)
        {
            //Set Global Data
            Camera cam = pipelineCam.cam;
            data.resources = resources;
            PipelineFunctions.GetViewProjectMatrix(cam, out data.vp, out data.inverseVP);
            ref RenderArray arr = ref data.arrayCollection;
            PipelineFunctions.GetCullingPlanes(ref data.inverseVP, arr.frustumPlanes, arr.farFrustumCorner, arr.nearFrustumCorner);
            //Start Calling Events
            DrawEvent evt;           
            if (allDrawEvents.TryGetValue(path, out evt))
            {
                buffer.Clear();
                //Pre Calculate Events
                foreach (var i in evt.preRenderEvents)
                {
                    i.PreRenderFrame(pipelineCam, ref data, buffer);
                }
                //Run job system together
                JobHandle.ScheduleBatchedJobs();
                //Start Prepare Render Targets
                //Frame Update Events
                foreach (var i in evt.drawEvents)
                {
                    i.FrameUpdate(pipelineCam, ref data, buffer);
                }
                Graphics.ExecuteCommandBuffer(buffer);
            }
            //Finalize Frame
            Graphics.Blit(pipelineCam.targets.renderTarget, dest);

        }
    }
    public struct DrawEvent
    {
        public List<PipelineEvent> drawEvents;
        public List<PipelineEvent> preRenderEvents;
        public DrawEvent(int capacity)
        {
            drawEvents = new List<PipelineEvent>(capacity);
            preRenderEvents = new List<PipelineEvent>(capacity);
        }
    }
}