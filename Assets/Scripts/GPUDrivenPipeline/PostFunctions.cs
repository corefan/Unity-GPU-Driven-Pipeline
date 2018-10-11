using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MPipeline
{
    public delegate void PostProcessAction(ref PipelineCommandData data, RenderTexture source, RenderTexture dest);
    public static class PostFunctions
    {
        public static void Blit(ref PipelineCommandData data, PostProcessAction renderFunc)
        {
            RenderTexture source = data.targets.renderTarget;
            RenderTexture dest = data.targets.backupTarget;
            renderFunc(ref data, source, dest);
            data.targets.renderTarget = dest;
            data.targets.backupTarget = source;
            data.targets.colorBuffer = dest.colorBuffer;
        }
    }
}