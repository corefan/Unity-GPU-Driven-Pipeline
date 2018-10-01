using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessEvent : PipelineEvent
{
    public List<PostProcessingBase> processingBase;
    public override void FrameUpdate(ref PipelineCommandData data)
    {
        foreach(var i in processingBase)
        {
            RenderTexture source = data.targets.renderTarget;
            RenderTexture dest = data.targets.backupTarget;
            i.Render(ref data, source, dest);
            data.targets.renderTarget = dest;
            data.targets.backupTarget = source;
            data.targets.colorBuffer = dest.colorBuffer;
        }
    }
}
