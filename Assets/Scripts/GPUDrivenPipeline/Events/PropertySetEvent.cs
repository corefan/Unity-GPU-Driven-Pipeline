using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MPipeline
{
    [PipelineEvent(false, true)]
    public class PropertySetEvent : PipelineEvent
    {
        private Dictionary<Camera, Matrix4x4> lastVPs = new Dictionary<Camera, Matrix4x4>();

        protected override void Dispose()
        {
            lastVPs.Clear();
        }

        public override void FrameUpdate(ref PipelineCommandData data)
        {
            //Calculate Last VP for motion vector and Temporal AA
            Matrix4x4 nonJitterVP = GL.GetGPUProjectionMatrix(data.cam.nonJitteredProjectionMatrix, false) * data.cam.worldToCameraMatrix;
            Matrix4x4 lastVp;
            if (!lastVPs.TryGetValue(data.cam, out lastVp))
                lastVp = nonJitterVP;
            Shader.SetGlobalMatrix(ShaderIDs._LastVp, lastVp);
            Shader.SetGlobalMatrix(ShaderIDs._NonJitterVP, nonJitterVP);
            lastVPs[data.cam] = nonJitterVP;
            Shader.SetGlobalMatrix(ShaderIDs._InvVP, data.inverseVP);
            Shader.SetGlobalVectorArray(ShaderIDs._FarClipCorner, data.arrayCollection.farFrustumCorner);
        }
    }
}