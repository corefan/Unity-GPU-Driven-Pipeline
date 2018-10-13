using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MPipeline
{
    [PipelineEvent(false, true)]
    public class MotionVectorEvent : PipelineEvent
    {
        protected override void Init(PipelineResources resources)
        {
          

        }

        protected override void Dispose()
        {
            
        }

        public override void FrameUpdate(ref PipelineCommandData data)
        {

        }
        public override void PreRenderFrame(Camera cam)
        {
            throw new System.NotImplementedException();
        }
    }
}