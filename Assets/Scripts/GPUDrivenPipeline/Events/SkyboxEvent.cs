using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MPipeline
{
    [PipelineEvent(false, true)]
    public class SkyboxEvent : PipelineEvent
    {
        public Material skyboxMaterial;
        protected override void Dispose()
        {
            
        }

        protected override void Init(PipelineResources resources)
        {
            
        }
        public override void FrameUpdate(ref PipelineCommandData data)
        {
            Graphics.SetRenderTarget(data.targets.colorBuffer, data.targets.depthBuffer);
            skyboxMaterial.SetVectorArray(ShaderIDs._FarClipCorner, data.arrayCollection.farFrustumCorner);
            skyboxMaterial.SetPass(0);
            Graphics.DrawMeshNow(GraphicsUtility.mesh, Matrix4x4.identity);
        }
        public override void PreRenderFrame(Camera cam)
        {
            throw new System.NotImplementedException();
        }
    }
}
