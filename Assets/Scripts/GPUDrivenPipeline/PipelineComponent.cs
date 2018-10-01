using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

public struct PipelineBaseBuffer
{
    public ComputeBuffer clusterBuffer;    //ObjectInfo
    public ComputeBuffer instanceCountBuffer; //uint4
    public ComputeBuffer resultBuffer; //uint
    public ComputeBuffer verticesBuffer;    //Point
    public int clusterCount;
    public int clusterOffset;
    public const int INDIRECTSIZE = 20;
    public const int UINTSIZE = 4;
    public const int CLUSTERCLIPCOUNT = 64;
    public const int CLUSTERVERTEXCOUNT = CLUSTERCLIPCOUNT * 6 / 4;
}

public struct AspectInfo
{
    public Vector3 inPlanePoint;
    public Vector3 planeNormal;
    public float size;
}
[System.Serializable]
public struct ShadowmapSettings
{
    public int resolution;
    public float firstLevelDistance;
    public float secondLevelDistance;
    public float thirdLevelDistance;
    public float farestDistance;
    public Vector4 bias;
    public Vector4 normalBias;
    public Vector4 cascadeSoftValue;
}

public struct ShadowMapComponent
{
    public Camera shadowCam;
    public Material shadowDepthMaterial;
    public RenderTexture shadowmapTexture;
    public NativeArray<Vector3> frustumCorners;
    public NativeArray<AspectInfo> shadowFrustumPlanes;
    public Light light;
}
[System.Serializable]
public struct Point
{
    public Vector3 vertex;
    public Vector4 tangent;
    public Vector3 normal;
    public Vector2 texcoord;
    public const int SIZE = 48;
}
[System.Serializable]
public struct ObjectInfo
{
    public Vector3 extent;
    public Vector3 position;
    public const int SIZE = 24;
}
public struct PerObjectData
{
    public Vector3 extent;
    public uint instanceOffset;
    public const int SIZE = 16;
}

public struct StaticFit
{
    public int resolution;
    public Camera shadowCam;
    public Camera mainCamTrans;
    public NativeArray<Vector3> frustumCorners;
}

public struct PipelineConstEntity
{
    public ComputeShader gpuFrustumShader;
    public RenderArray arrayCollection;
}

public struct RenderArray
{
    public Vector4[] farFrustumCorner;
    public Vector4[] nearFrustumCorner;
    public Vector4[] frustumPlanes;
    public Vector4[] shadowFrustumPlanes;
    public Matrix4x4[] cascadeShadowMapVP;
    public Vector4[] shadowCameraPos;
    public RenderArray(bool init)
    {
        if (init)
        {
            frustumPlanes = new Vector4[6];
            farFrustumCorner = new Vector4[6];
            nearFrustumCorner = new Vector4[6];
            shadowFrustumPlanes = new Vector4[6];
            cascadeShadowMapVP = new Matrix4x4[4];
            shadowCameraPos = new Vector4[4];
            for (int i = 0; i < cascadeShadowMapVP.Length; ++i)
            {
                cascadeShadowMapVP[i] = Matrix4x4.identity;
            }
        }
        else
        {
            farFrustumCorner = null;
            nearFrustumCorner = null;
            frustumPlanes = null;
            shadowFrustumPlanes = null;
            cascadeShadowMapVP = null;
            shadowCameraPos = null;
        }
    }
}


public struct RenderTargets
{
    public RenderTexture renderTarget;
    public RenderTexture backupTarget;
    public RenderTexture[] gbufferTextures;
    public RenderBuffer[] geometryColorBuffer;
    public RenderBuffer depthBuffer;
    public RenderBuffer colorBuffer;
    public RenderTexture motionVectorTexture;
    public int[] gbufferIndex;
    public static RenderTargets Init()
    {
        RenderTargets rt;
        rt.gbufferIndex = new int[]
        {
                Shader.PropertyToID("_CameraGBufferTexture0"),
                Shader.PropertyToID("_CameraGBufferTexture1"),
                Shader.PropertyToID("_CameraGBufferTexture2"),
                Shader.PropertyToID("_CameraGBufferTexture3"),
        };
        rt.colorBuffer = default;
        rt.gbufferTextures = new RenderTexture[4];
        rt.geometryColorBuffer = new RenderBuffer[4];
        rt.depthBuffer = default;
        rt.renderTarget = null;
        rt.backupTarget = null;
        rt.motionVectorTexture = null;
        return rt;
    }
}

public struct PipelineCommandData
{
    public RenderTargets targets;
    public Matrix4x4 vp;
    public Matrix4x4 inverseVP;
    public Camera cam;
    public PipelineBaseBuffer baseBuffer;
    public PipelineConstEntity constEntity;
}