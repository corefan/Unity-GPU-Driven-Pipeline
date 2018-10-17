using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
namespace MPipeline
{
    [PipelineEvent(true, true)]
    public class ReflectionEvent : PipelineEvent
    {
        private Material reflectMaterial;
        protected override void Init(PipelineResources resources)
        {
            reflectMaterial = new Material(resources.reflectionShader);
        }

        protected override void Dispose()
        {
            Destroy(reflectMaterial);
        }

        public override void FrameUpdate(ref PipelineCommandData data)
        {
            Graphics.SetRenderTarget(data.targets.colorBuffer, data.targets.depthBuffer);
            
        }

        public override void PreRenderFrame(Camera cam)
        {
            throw new System.NotImplementedException();
        }
    }
}