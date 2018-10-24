using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MPipeline
{
    [PipelineEvent(false, true)]
    public class PropertySetEvent : PipelineEvent
    {
        private System.Func<LastVPData> getLastVP = () => new LastVPData();
        public override void FrameUpdate(PipelineCamera cam, ref PipelineCommandData data)
        {
            LastVPData lastData = IPerCameraData.GetProperty(this, cam, getLastVP) as LastVPData;
            if(lastData == null)
            {
                lastData = new LastVPData();
                cam.postDatas.Add(this, lastData);
            }
            //Calculate Last VP for motion vector and Temporal AA
            Matrix4x4 nonJitterVP = GL.GetGPUProjectionMatrix(cam.cam.nonJitteredProjectionMatrix, false) * cam.cam.worldToCameraMatrix;
            ref Matrix4x4 lastVp = ref lastData.lastVP;
            Shader.SetGlobalMatrix(ShaderIDs._LastVp, lastVp);
            Shader.SetGlobalMatrix(ShaderIDs._NonJitterVP, nonJitterVP);
            Shader.SetGlobalMatrix(ShaderIDs._InvVP, data.inverseVP);
            Shader.SetGlobalMatrix(ShaderIDs._VP, data.vp);
            Shader.SetGlobalMatrix(ShaderIDs._InvLastVP, lastVp.inverse);
            Shader.SetGlobalVectorArray(ShaderIDs._FarClipCorner, data.arrayCollection.farFrustumCorner);
            lastVp = nonJitterVP;
        }
    }

    public class LastVPData : IPerCameraData
    {
        public Matrix4x4 lastVP = Matrix4x4.identity;
        public override void DisposeProperty()
        {
        }
    }
}