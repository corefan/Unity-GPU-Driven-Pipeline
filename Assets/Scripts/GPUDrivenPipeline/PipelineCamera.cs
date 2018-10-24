using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MPipeline
{
    [RequireComponent(typeof(Camera))]
    public class PipelineCamera : MonoBehaviour
    {
        [System.NonSerialized]
        public Camera cam;
        [System.NonSerialized]
        public RenderTargets targets;
        private List<RenderTexture> temporaryTextures = new List<RenderTexture>(15);
        public Dictionary<PipelineEvent, IPerCameraData> postDatas = new Dictionary<PipelineEvent, IPerCameraData>();
        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.renderingPath = RenderingPath.Forward;
            cam.cullingMask = 0;
            cam.clearFlags = CameraClearFlags.Nothing;
            targets = RenderTargets.Init();
        }

        private void OnDisable()
        {
            foreach(var i in postDatas.Values)
            {
                i.DisposeProperty();
            }
            postDatas.Clear();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (RenderPipeline.singleton)
            {
                PipelineFunctions.InitRenderTarget(ref targets, cam, temporaryTextures);
                RenderPipeline.singleton.Render(this, destination);
                PipelineFunctions.ReleaseRenderTarget(temporaryTextures);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }
    }
}
