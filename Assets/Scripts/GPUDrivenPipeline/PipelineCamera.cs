using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MPipeline
{
    [RequireComponent(typeof(Camera))]
    public class PipelineCamera : MonoBehaviour
    {
        private Camera cam;
        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.renderingPath = RenderingPath.Forward;
            cam.cullingMask = 0;
            cam.clearFlags = CameraClearFlags.Nothing;
        }

        private void OnPreCull()
        {
            if (RenderPipeline.singleton)
            {
                RenderPipeline.singleton.BeforePipeline(cam);
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (RenderPipeline.singleton)
            {
                RenderPipeline.singleton.Render(cam, destination);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }
    }
}
