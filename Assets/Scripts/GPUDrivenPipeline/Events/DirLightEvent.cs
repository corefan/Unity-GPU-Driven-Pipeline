using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirLightEvent : PipelineEvent
{
    private Material shadMaskMaterial;
    protected override void Awake()
    {
        base.Awake();
        shadMaskMaterial = new Material(Shader.Find("Hidden/ShadowMask"));
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Destroy(shadMaskMaterial);
    }

    public override void FrameUpdate(ref PipelineCommandData data)
    {
        if (SunLight.current == null) return;
        int pass;
        if (SunLight.current.enableShadow)
        {
            PipelineFunctions.DrawShadow(data.cam, ref data.constEntity, ref data.baseBuffer, ref SunLight.current.settings, ref SunLight.shadMap);
            PipelineFunctions.UpdateShadowMaskState(shadMaskMaterial, ref SunLight.shadMap, ref data.constEntity.arrayCollection.cascadeShadowMapVP, ref data.constEntity.arrayCollection.shadowCameraPos);
            pass = 0;
        }
        else
        {
            pass = 1;
        }
        Graphics.SetRenderTarget(data.targets.colorBuffer, data.targets.depthBuffer);
        Shader.SetGlobalVector(ShaderIDs._LightPos, -SunLight.current.transform.forward);
        shadMaskMaterial.SetPass(pass);
        Graphics.DrawMeshNow(GraphicsUtility.mesh, Matrix4x4.identity);
    }
}
