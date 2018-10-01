using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class ShaderIDs
{
    public static int _Count = Shader.PropertyToID("_Count");
    public static int planes = Shader.PropertyToID("planes");
    public static int _ShadowCamDirection = Shader.PropertyToID("_ShadowCamDirection");
    public static int _DirShadowMap = Shader.PropertyToID("_DirShadowMap");
    public static int _InvVP = Shader.PropertyToID("_InvVP");
    public static int _LastVp = Shader.PropertyToID("_LastVp");
    public static int _ShadowMapVP = Shader.PropertyToID("_ShadowMapVP");
    public static int _ShadowMapVPs = Shader.PropertyToID("_ShadowMapVPs");
    public static int _ShadowCamPos = Shader.PropertyToID("_ShadowCamPos");
    public static int _ShadowCamPoses = Shader.PropertyToID("_ShadowCamPoses");
    public static int _ShadowDisableDistance = Shader.PropertyToID("_ShadowDisableDistance");
    public static int _LightFinalColor = Shader.PropertyToID("_LightFinalColor");
    public static int _LightPos = Shader.PropertyToID("_LightPos");
    public static int _MainTex = Shader.PropertyToID("_MainTex");
    public static int _SoftParam = Shader.PropertyToID("_SoftParam");
    public static int _OffsetIndex = Shader.PropertyToID("_OffsetIndex");
    public static int clusterBuffer = Shader.PropertyToID("clusterBuffer");
    public static int instanceCountBuffer = Shader.PropertyToID("instanceCountBuffer");
    public static int resultBuffer = Shader.PropertyToID("resultBuffer");
    public static int verticesBuffer = Shader.PropertyToID("verticesBuffer");
    public static int _NormalBiases = Shader.PropertyToID("_NormalBiases");
    public static int weightsBuffer = Shader.PropertyToID("weightsBuffer");
    public static int allVerticesBuffer = Shader.PropertyToID("allVerticesBuffer");
    public static int boneBuffers = Shader.PropertyToID("boneBuffers");
    public static int _FarClipCorner = Shader.PropertyToID("_FarClipCorner");
}
