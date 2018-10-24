using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MPipeline
{
    [PipelineEvent(false, true)]
    public class SkyboxEvent : PipelineEvent
    {
        public Material skyboxMaterial;
        public override void FrameUpdate(PipelineCamera camera, ref PipelineCommandData data)
        {
            Graphics.SetRenderTarget(camera.targets.colorBuffer, camera.targets.depthBuffer);
            skyboxMaterial.SetPass(0);
            Graphics.DrawMeshNow(GraphicsUtility.mesh, Matrix4x4.identity);
        }
    }
}
