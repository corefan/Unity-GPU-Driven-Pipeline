using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MPipeline
{
    public class MotionVectorEvent : PipelineEvent
    {
        private Material motionVecMat;
        private Matrix4x4 lastVp;
        protected override void Awake()
        {
            base.Awake();
            lastVp = Matrix4x4.identity;
            motionVecMat = new Material(Shader.Find("Hidden/MotionVector"));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Destroy(motionVecMat);
        }

        public override void FrameUpdate(ref PipelineCommandData data)
        {
            Matrix4x4 vp = GL.GetGPUProjectionMatrix(data.cam.nonJitteredProjectionMatrix, false) * data.cam.worldToCameraMatrix;
            motionVecMat.SetMatrix(ShaderIDs._LastVp, lastVp);
            motionVecMat.SetMatrix(ShaderIDs._InvVP, vp.inverse);
            Graphics.SetRenderTarget(data.targets.motionVectorTexture.colorBuffer, data.targets.depthBuffer);
            GL.Clear(false, true, Color.black);
            motionVecMat.SetPass(0);
            Graphics.DrawMeshNow(GraphicsUtility.mesh, Matrix4x4.identity);
            lastVp = vp;
        }
        public override void PreRenderFrame(Camera cam)
        {
            throw new System.NotImplementedException();
        }
    }
}