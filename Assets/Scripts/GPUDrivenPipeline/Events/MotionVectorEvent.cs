using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public override void FrameUpdate(ref PipelineCommandData data)
    {
        motionVecMat.SetMatrix(ShaderIDs._LastVp, lastVp);
        motionVecMat.SetMatrix(ShaderIDs._InvVP, data.inverseVP);
        Graphics.SetRenderTarget(data.targets.motionVectorTexture.colorBuffer, data.targets.depthBuffer);
        GL.Clear(false, true, Color.black);
        motionVecMat.SetPass(0);
        Graphics.DrawMeshNow(GraphicsUtility.mesh, Matrix4x4.identity);
        lastVp = data.vp;
    }
}
