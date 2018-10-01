using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeometryEvent : PipelineEvent
{
    public Material proceduralMaterial;
    public override void FrameUpdate(ref PipelineCommandData data)
    {
        ref var baseBuffer = ref data.baseBuffer;
        ref var constEntity = ref data.constEntity;
        Graphics.SetRenderTarget(data.targets.geometryColorBuffer, data.targets.depthBuffer);
        Shader.SetGlobalMatrix(ShaderIDs._InvVP, data.inverseVP);
        PipelineFunctions.SetShaderBuffer(ref baseBuffer);
        PipelineFunctions.SetBaseBuffer(ref baseBuffer, constEntity.gpuFrustumShader, constEntity.arrayCollection.frustumPlanes);
        PipelineFunctions.RunCullDispatching(ref baseBuffer, constEntity.gpuFrustumShader);
        PipelineFunctions.RenderProceduralCommand(ref baseBuffer, proceduralMaterial);
    }
}