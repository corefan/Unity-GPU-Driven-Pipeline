using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using System;

public unsafe static class PipelineFunctions
{
    const int CASCADELEVELCOUNT = 4;
    const int Vector2IntSize = 8;
    const int CASCADECLIPSIZE = (CASCADELEVELCOUNT + 1) * sizeof(float);
    /// <summary>
    /// Get Frustum Planes
    /// </summary>
    /// <param name="invVp"></param> View Projection Inverse Matrix
    /// <param name="cullingPlanes"></param> Culling Planes results
    public static void GetCullingPlanes(ref Matrix4x4 invVp, Vector4[] cullingPlanes, Vector4[] farClipPlane, Vector4[] nearClipPlane)
    {
        Vector3 nearLeftButtom = invVp.MultiplyPoint(new Vector3(-1, -1, 1));
        Vector3 nearLeftTop = invVp.MultiplyPoint(new Vector3(-1, 1, 1));
        Vector3 nearRightButtom = invVp.MultiplyPoint(new Vector3(1, -1, 1));
        Vector3 nearRightTop = invVp.MultiplyPoint(new Vector3(1, 1, 1));
        Vector3 farLeftButtom = invVp.MultiplyPoint(new Vector3(-1, -1, 0));
        Vector3 farLeftTop = invVp.MultiplyPoint(new Vector3(-1, 1, 0));
        Vector3 farRightButtom = invVp.MultiplyPoint(new Vector3(1, -1, 0));
        Vector3 farRightTop = invVp.MultiplyPoint(new Vector3(1, 1, 0));
        nearClipPlane[0] = nearLeftButtom;
        nearClipPlane[1] = nearRightButtom;
        nearClipPlane[2] = nearLeftTop;
        nearClipPlane[3] = nearRightTop;
        farClipPlane[0] = farLeftButtom;
        farClipPlane[1] = farRightButtom;
        farClipPlane[2] = farLeftTop;
        farClipPlane[3] = farRightTop;
        Plane plane;
        //Near
        plane = new Plane(nearRightTop, nearRightButtom, nearLeftButtom);
        cullingPlanes[0] = plane.normal;
        cullingPlanes[0].w = plane.distance;
        //Up
        plane = new Plane(farLeftTop, farRightTop, nearRightTop);
        cullingPlanes[1] = plane.normal;
        cullingPlanes[1].w = plane.distance;
        //Down
        plane = new Plane(nearRightButtom, farRightButtom, farLeftButtom);
        cullingPlanes[2] = plane.normal;
        cullingPlanes[2].w = plane.distance;
        //Left
        plane = new Plane(farLeftButtom, farLeftTop, nearLeftTop);
        cullingPlanes[3] = plane.normal;
        cullingPlanes[3].w = plane.distance;
        //Right
        plane = new Plane(farRightButtom, nearRightButtom, nearRightTop);
        cullingPlanes[4] = plane.normal;
        cullingPlanes[4].w = plane.distance;
        //Far
        plane = new Plane(farLeftButtom, farRightButtom, farRightTop);
        cullingPlanes[5] = plane.normal;
        cullingPlanes[5].w = plane.distance;

    }
    public static void GetCullingPlanes(ref Matrix4x4 invVp, Vector4[] cullingPlanes)
    {
        Vector3 nearLeftButtom = invVp.MultiplyPoint(new Vector3(-1, -1, 1));
        Vector3 nearLeftTop = invVp.MultiplyPoint(new Vector3(-1, 1, 1));
        Vector3 nearRightButtom = invVp.MultiplyPoint(new Vector3(1, -1, 1));
        Vector3 nearRightTop = invVp.MultiplyPoint(new Vector3(1, 1, 1));
        Vector3 farLeftButtom = invVp.MultiplyPoint(new Vector3(-1, -1, 0));
        Vector3 farLeftTop = invVp.MultiplyPoint(new Vector3(-1, 1, 0));
        Vector3 farRightButtom = invVp.MultiplyPoint(new Vector3(1, -1, 0));
        Vector3 farRightTop = invVp.MultiplyPoint(new Vector3(1, 1, 0));
        Plane plane;
        //Near
        plane = new Plane(nearRightTop, nearRightButtom, nearLeftButtom);
        cullingPlanes[0] = plane.normal;
        cullingPlanes[0].w = plane.distance;
        //Up
        plane = new Plane(farLeftTop, farRightTop, nearRightTop);
        cullingPlanes[1] = plane.normal;
        cullingPlanes[1].w = plane.distance;
        //Down
        plane = new Plane(nearRightButtom, farRightButtom, farLeftButtom);
        cullingPlanes[2] = plane.normal;
        cullingPlanes[2].w = plane.distance;
        //Left
        plane = new Plane(farLeftButtom, farLeftTop, nearLeftTop);
        cullingPlanes[3] = plane.normal;
        cullingPlanes[3].w = plane.distance;
        //Right
        plane = new Plane(farRightButtom, nearRightButtom, nearRightTop);
        cullingPlanes[4] = plane.normal;
        cullingPlanes[4].w = plane.distance;
        //Far
        plane = new Plane(farLeftButtom, farRightButtom, farRightTop);
        cullingPlanes[5] = plane.normal;
        cullingPlanes[5].w = plane.distance;

    }
    /// <summary>
    /// Initialize pipeline buffers
    /// </summary>
    /// <param name="baseBuffer"></param> pipeline base buffer
    public static void InitBaseBuffer(ref PipelineBaseBuffer baseBuffer, string infoPath, string pointPath)
    {
        TextAsset pointText = Resources.Load<TextAsset>(pointPath);
        TextAsset infoText = Resources.Load<TextAsset>(infoPath);
        byte[] pointBytes = pointText.bytes;
        byte[] infoBytes = infoText.bytes;
        Point* points = null;
        ObjectInfo* infos = null;
        int pointLength = 0;
        int infoLength = 0;
        fixed (void* ptr = &pointBytes[0])
        {
            points = (Point*)ptr;
            pointLength = pointBytes.Length / Point.SIZE;
        }
        fixed (void* ptr = &infoBytes[0])
        {
            infos = (ObjectInfo*)ptr;
            infoLength = infoBytes.Length / ObjectInfo.SIZE;
        }
        NativeArray<ObjectInfo> allInfos = new NativeArray<ObjectInfo>(infoLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        NativeArray<Point> allPoints = new NativeArray<Point>(pointLength, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        void* destination = allPoints.GetUnsafePtr();
        UnsafeUtility.MemCpy(destination, points, pointBytes.Length);
        destination = allInfos.GetUnsafePtr();
        UnsafeUtility.MemCpy(destination, infos, infoBytes.Length);
        baseBuffer.clusterBuffer = new ComputeBuffer(allInfos.Length, ObjectInfo.SIZE);
        baseBuffer.clusterBuffer.SetData(allInfos);
        baseBuffer.resultBuffer = new ComputeBuffer(allInfos.Length, PipelineBaseBuffer.UINTSIZE);
        baseBuffer.instanceCountBuffer = new ComputeBuffer(1, PipelineBaseBuffer.INDIRECTSIZE, ComputeBufferType.IndirectArguments);
        baseBuffer.verticesBuffer = new ComputeBuffer(allPoints.Length, Point.SIZE);
        baseBuffer.verticesBuffer.SetData(allPoints);
        baseBuffer.clusterCount = allInfos.Length;
        baseBuffer.clusterOffset = 0;
        allInfos.Dispose();
        allPoints.Dispose();
        Resources.UnloadAsset(pointText);
        Resources.UnloadAsset(infoText);
    }
    /// <summary>
    /// Get Frustum Corners
    /// </summary>
    /// <param name="distance"></param> target distance range
    /// <param name="shadMap"></param> shadowmap component
    /// <param name="mask"></param> shadowmask component
    public static void GetfrustumCorners(Vector2 distance, ref ShadowMapComponent shadMap, Camera targetCamera)
    {
        //bottom left
        shadMap.frustumCorners[0] = targetCamera.ViewportToWorldPoint(new Vector3(0, 0, distance.x));
        // bottom right
        shadMap.frustumCorners[1] = targetCamera.ViewportToWorldPoint(new Vector3(1, 0, distance.x));
        // top left
        shadMap.frustumCorners[2] = targetCamera.ViewportToWorldPoint(new Vector3(0, 1, distance.x));
        // top right
        shadMap.frustumCorners[3] = targetCamera.ViewportToWorldPoint(new Vector3(1, 1, distance.x));
        //bottom left
        shadMap.frustumCorners[4] = targetCamera.ViewportToWorldPoint(new Vector3(0, 0, distance.y));
        // bottom right
        shadMap.frustumCorners[5] = targetCamera.ViewportToWorldPoint(new Vector3(1, 0, distance.y));
        // top left
        shadMap.frustumCorners[6] = targetCamera.ViewportToWorldPoint(new Vector3(0, 1, distance.y));
        // top right
        shadMap.frustumCorners[7] = targetCamera.ViewportToWorldPoint(new Vector3(1, 1, distance.y));
    }
    /// <summary>
    /// Set Shadowcamera Position
    /// </summary>
    /// <param name="shadMap"></param> Shadowmap component
    /// <param name="settings"></param> Shadowmap Settings
    public static void SetShadowCameraPositionCloseFit(ref ShadowMapComponent shadMap, ref ShadowmapSettings settings)
    {
        Camera shadowCam = shadMap.shadowCam;
        NativeArray<AspectInfo> shadowFrustumPlanes = shadMap.shadowFrustumPlanes;
        AspectInfo info = shadowFrustumPlanes[0];
        info.planeNormal = shadowCam.transform.right;
        shadowFrustumPlanes[0] = info;
        info = shadowFrustumPlanes[1];
        info.planeNormal = shadowCam.transform.up;
        shadowFrustumPlanes[1] = info;
        info = shadowFrustumPlanes[2];
        info.planeNormal = shadowCam.transform.forward;
        shadowFrustumPlanes[2] = info;
        for (int i = 0; i < 3; ++i)
        {
            info = shadowFrustumPlanes[i];
            float least = float.MaxValue;
            float maximum = float.MinValue;
            Vector3 lessPoint = Vector3.zero;
            Vector3 morePoint = Vector3.zero;
            for (int x = 0; x < 8; ++x)
            {
                float dotValue = Vector3.Dot(info.planeNormal, shadMap.frustumCorners[x]);
                if (dotValue < least)
                {
                    least = dotValue;
                    lessPoint = shadMap.frustumCorners[x];
                }
                if (dotValue > maximum)
                {
                    maximum = dotValue;
                    morePoint = shadMap.frustumCorners[x];
                }
            }
            info.size = (maximum - least) / 2f;
            info.inPlanePoint = lessPoint + info.planeNormal * info.size;
            shadowFrustumPlanes[i] = info;
        }
        AspectInfo temp = shadowFrustumPlanes[2];
        temp.size = settings.farestDistance;    //Farest Cascade Distance
        shadowFrustumPlanes[2] = temp;
        Transform tr = shadowCam.transform;
        for (int i = 0; i < 3; ++i)
        {
            info = shadowFrustumPlanes[i];
            float dist = Vector3.Dot(info.inPlanePoint, info.planeNormal) - Vector3.Dot(tr.position, info.planeNormal);
            tr.position += dist * info.planeNormal;
        }
        shadowCam.orthographicSize = shadowFrustumPlanes[1].size;
        shadowCam.aspect = shadowFrustumPlanes[0].size / shadowFrustumPlanes[1].size;
        shadowCam.nearClipPlane = 0;
        shadowCam.farClipPlane = shadowFrustumPlanes[2].size * 2;
        tr.position -= shadowFrustumPlanes[2].size * shadowFrustumPlanes[2].planeNormal;
    }
    public static void SetShadowCameraPositionStaticFit(ref StaticFit fit, int pass, Matrix4x4[] vpMatrices, out Matrix4x4 invShadowVP)
    {
        fit.shadowCam.aspect = 1;
        float range = 0;
        Vector3 averagePos = Vector3.zero;
        foreach (var i in fit.frustumCorners)
        {
            averagePos += i;
        }
        averagePos /= fit.frustumCorners.Length;
        foreach (var i in fit.frustumCorners)
        {
            float dist = Vector3.Distance(averagePos, i);
            if (range < dist)
            {
                range = dist;
            }
        }
        fit.shadowCam.orthographicSize = range;
        float farClipPlane = fit.mainCamTrans.farClipPlane;
        Vector3 targetPosition = averagePos - fit.shadowCam.transform.forward * farClipPlane * 0.5f;
        fit.shadowCam.nearClipPlane = 0;
        fit.shadowCam.farClipPlane = farClipPlane;
        ref Matrix4x4 shadowVP = ref vpMatrices[pass];
        invShadowVP = shadowVP.inverse;
        Vector3 ndcPos = shadowVP.MultiplyPoint(targetPosition);
        Vector2 uv = new Vector2(ndcPos.x, ndcPos.y) * 0.5f + new Vector2(0.5f, 0.5f);
        uv.x = (int)(uv.x * fit.resolution + 0.5);
        uv.y = (int)(uv.y * fit.resolution + 0.5);
        uv /= fit.resolution;
        uv = uv * 2f - Vector2.one;
        ndcPos = new Vector3(uv.x, uv.y, ndcPos.z);
        targetPosition = invShadowVP.MultiplyPoint(ndcPos);
        fit.shadowCam.transform.position = targetPosition;
        shadowVP = GL.GetGPUProjectionMatrix(fit.shadowCam.projectionMatrix, false) * fit.shadowCam.worldToCameraMatrix;
        invShadowVP = shadowVP.inverse;
    }
    /// <summary>
    /// Initialize Per frame shadowmap buffers for Shadowmap shader
    /// </summary>
    public static void UpdateShadowMapState(ref ShadowMapComponent comp, ref ShadowmapSettings settings)
    {
        Camera shadowCam = comp.shadowCam;
        Shader.SetGlobalVector(ShaderIDs._NormalBiases, settings.normalBias);
        Shader.SetGlobalVector(ShaderIDs._ShadowDisableDistance, new Vector4(settings.firstLevelDistance, settings.secondLevelDistance, settings.thirdLevelDistance, settings.farestDistance));
        Shader.SetGlobalVector(ShaderIDs._LightFinalColor, comp.light.color * comp.light.intensity);
        Shader.SetGlobalVector(ShaderIDs._SoftParam, settings.cascadeSoftValue / settings.resolution);
    }
    /// <summary>
    /// Initialize per cascade shadowmap buffers
    /// </summary>
    public static void UpdateCascadeState(ref ShadowMapComponent comp, float bias, int pass)
    {
        Camera shadowCam = comp.shadowCam;
        Vector4 shadowcamDir = shadowCam.transform.forward;
        shadowcamDir.w = bias;
        Graphics.SetRenderTarget(comp.shadowmapTexture, 0, CubemapFace.Unknown, depthSlice: pass);
        GL.Clear(true, true, Color.white);
        Shader.SetGlobalVector(ShaderIDs._ShadowCamDirection, shadowcamDir);
        Matrix4x4 rtVp = GL.GetGPUProjectionMatrix(shadowCam.projectionMatrix, true) * shadowCam.worldToCameraMatrix;
        comp.shadowDepthMaterial.SetMatrix(ShaderIDs._ShadowMapVP, rtVp);
        Shader.SetGlobalVector(ShaderIDs._ShadowCamPos, shadowCam.transform.position);
        comp.shadowDepthMaterial.SetPass(0);
    }
    /// <summary>
    /// Initialize shadowmask per frame buffers
    /// </summary>
    public static void UpdateShadowMaskState(Material shadowMaskMaterial, ref ShadowMapComponent shadMap, ref Matrix4x4[] cascadeShadowMapVP, ref Vector4[] shadowCameraPos)
    {
        Shader.SetGlobalMatrixArray(ShaderIDs._ShadowMapVPs, cascadeShadowMapVP);
        Shader.SetGlobalVectorArray(ShaderIDs._ShadowCamPoses, shadowCameraPos);
        Shader.SetGlobalTexture(ShaderIDs._DirShadowMap, shadMap.shadowmapTexture);

    }
    public static void Dispose(ref PipelineBaseBuffer baseBuffer)
    {
        baseBuffer.verticesBuffer.Dispose();
        baseBuffer.clusterBuffer.Dispose();
        baseBuffer.instanceCountBuffer.Dispose();
        baseBuffer.resultBuffer.Dispose();
    }
    /// <summary>
    /// Set Basement buffers
    /// </summary>
    public static void SetBaseBuffer(ref PipelineBaseBuffer baseBuffer, ComputeShader gpuFrustumShader, Vector4[] frustumCullingPlanes)
    {
        var compute = gpuFrustumShader;
        compute.SetVectorArray(ShaderIDs.planes, frustumCullingPlanes);
        compute.SetBuffer(0, ShaderIDs.clusterBuffer, baseBuffer.clusterBuffer);
        compute.SetBuffer(0, ShaderIDs.instanceCountBuffer, baseBuffer.instanceCountBuffer);
        compute.SetBuffer(1, ShaderIDs.instanceCountBuffer, baseBuffer.instanceCountBuffer);
        compute.SetBuffer(0, ShaderIDs.resultBuffer, baseBuffer.resultBuffer);
    }
    public static void SetShaderBuffer(ref PipelineBaseBuffer basebuffer)
    {
        Shader.SetGlobalBuffer(ShaderIDs.verticesBuffer, basebuffer.verticesBuffer);
        Shader.SetGlobalBuffer(ShaderIDs.resultBuffer, basebuffer.resultBuffer);
    }
    public static void SetShaderBuffer(ref PipelineBaseBuffer basebuffer, CommandBuffer geometry)
    {
        geometry.SetGlobalBuffer(ShaderIDs.verticesBuffer, basebuffer.verticesBuffer);
        geometry.SetGlobalBuffer(ShaderIDs.resultBuffer, basebuffer.resultBuffer);
    }
    public static void RunCullDispatching(ref PipelineBaseBuffer baseBuffer, ComputeShader computeShader)
    {
        computeShader.Dispatch(1, 1, 1, 1);
        ComputeShaderUtility.Dispatch(computeShader, 0, baseBuffer.clusterCount, 64);
    }
    public static void RenderProceduralCommand(ref PipelineBaseBuffer buffer, Material material)
    {
        material.SetPass(0);
        Graphics.DrawProceduralIndirect(MeshTopology.Triangles, buffer.instanceCountBuffer);
    }
    public static void GetViewProjectMatrix(Camera currentCam, out Matrix4x4 vp, out Matrix4x4 invVP)
    {
        vp = GL.GetGPUProjectionMatrix(currentCam.projectionMatrix, false) * currentCam.worldToCameraMatrix;
        invVP = vp.inverse;
    }
    public static void DrawShadow(Camera currentCam, ref PipelineConstEntity constEntity, ref PipelineBaseBuffer baseBuffer, ref ShadowmapSettings settings, ref ShadowMapComponent shadMap)
    {
        var arr = constEntity.arrayCollection;
        var gpuFrustumShader = constEntity.gpuFrustumShader;
        StaticFit staticFit;
        staticFit.resolution = settings.resolution / 2;
        staticFit.mainCamTrans = currentCam;
        staticFit.shadowCam = shadMap.shadowCam;
        staticFit.frustumCorners = shadMap.frustumCorners;
        UpdateShadowMapState(ref shadMap, ref settings);
        float* clipDistances = (float*)UnsafeUtility.Malloc(CASCADECLIPSIZE, 16, Allocator.Temp);
        clipDistances[0] = staticFit.shadowCam.nearClipPlane;
        clipDistances[1] = settings.firstLevelDistance;
        clipDistances[2] = settings.secondLevelDistance;
        clipDistances[3] = settings.thirdLevelDistance;
        clipDistances[4] = settings.farestDistance;
        for (int pass = 0; pass < CASCADELEVELCOUNT; ++pass)
        {
            Vector2 farClipDistance = new Vector2(clipDistances[pass], clipDistances[pass + 1]);
            GetfrustumCorners(farClipDistance, ref shadMap, currentCam);
            // PipelineFunctions.SetShadowCameraPositionCloseFit(ref shadMap, ref settings);
            Matrix4x4 invpVPMatrix;
            SetShadowCameraPositionStaticFit(ref staticFit, pass, arr.cascadeShadowMapVP, out invpVPMatrix);
            arr.shadowCameraPos[pass] = shadMap.shadowCam.transform.position;
            GetCullingPlanes(ref invpVPMatrix, arr.shadowFrustumPlanes);
            SetBaseBuffer(ref baseBuffer, constEntity.gpuFrustumShader, constEntity.arrayCollection.shadowFrustumPlanes);
            RunCullDispatching(ref baseBuffer, gpuFrustumShader);
            float* biasList = (float*)UnsafeUtility.AddressOf(ref settings.bias);
            UpdateCascadeState(ref shadMap, biasList[pass] / currentCam.farClipPlane, pass);
            Graphics.DrawProceduralIndirect(MeshTopology.Triangles, baseBuffer.instanceCountBuffer);
        }
        UnsafeUtility.Free(clipDistances, Allocator.Temp);
    }
    public static void InitRenderTarget(ref RenderTargets tar, Camera tarcam, List<RenderTexture> collectRT)
    {
        collectRT.Clear();
        tar.gbufferTextures[0] = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.ARGBHalf, collectRT);
        tar.gbufferTextures[1] = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.ARGBHalf, collectRT);
        tar.gbufferTextures[2] = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.ARGBFloat, collectRT);
        tar.gbufferTextures[3] = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 24, RenderTextureFormat.ARGBHalf, collectRT);
        for (int i = 0; i < tar.gbufferTextures.Length; ++i)
        {
            tar.geometryColorBuffer[i] = tar.gbufferTextures[i].colorBuffer;
            Shader.SetGlobalTexture(tar.gbufferIndex[i], tar.gbufferTextures[i]);
        }
        tar.renderTarget = tar.gbufferTextures[3];
        tar.backupTarget = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.ARGBHalf, collectRT);
        tar.motionVectorTexture = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.RGFloat, collectRT);
        tar.colorBuffer = tar.renderTarget.colorBuffer;
        tar.depthBuffer = tar.renderTarget.depthBuffer;
        foreach(var i in collectRT)
        {
            i.filterMode = FilterMode.Point;
        }
    }

    public static RenderTexture GetTemporary(RenderTextureDescriptor descriptor, List<RenderTexture> collectList)
    {
        RenderTexture rt = RenderTexture.GetTemporary(descriptor);
        collectList.Add(rt);
        return rt;
    }

    public static RenderTexture GetTemporary(int width, int height, int depth, RenderTextureFormat format, List<RenderTexture> collectList)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height, depth, format, RenderTextureReadWrite.Linear);
        collectList.Add(rt);
        return rt;
    }

    public static void ReleaseRenderTarget(List<RenderTexture> tar)
    {
        foreach (var i in tar)
        {
            RenderTexture.ReleaseTemporary(i);
        }
    }
    public static int PrepareDirShadow(Camera currentCam, ref PipelineConstEntity constEntity, ref PipelineBaseBuffer baseBuffer, SunLight sun, ref ShadowMapComponent shadMap)
    {
        int pass;
        if (sun.enableShadow)
        {
            DrawShadow(currentCam, ref constEntity, ref baseBuffer, ref sun.settings, ref shadMap);
            pass = 0;
        }
        else
        {
            pass = 1;
        }
        return pass;
    }
    public static void DrawDirLight(CommandBuffer afterLightCommand, Material shadMaskMat, int pass)
    {
        afterLightCommand.SetGlobalVector(ShaderIDs._LightPos, -SunLight.current.transform.forward);
        afterLightCommand.DrawMesh(GraphicsUtility.mesh, Matrix4x4.identity, shadMaskMat, 0, pass);
    }
    public static void InsertTo<T>(this List<T> targetArray, T value, Func<T, T, int> compareResult)
    {
        Vector2Int range = new Vector2Int(0, targetArray.Count);
        while (true)
        {
            if (targetArray.Count == 0)
            {
                targetArray.Add(value);
                return;
            }
            else if (Math.Abs(range.x - range.y) == 1)
            {
                int compareX = compareResult(targetArray[range.x], value);
                if (compareX < 0)
                {
                    targetArray.Insert(range.x, value);
                    return;
                }
                else if (compareX > 0)
                {
                    if (range.y < targetArray.Count && compareResult(targetArray[range.y], value) == 0)
                    {
                        return;
                    }
                    else
                    {
                        targetArray.Insert(range.y, value);
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                int currentIndex = (int)((range.x + range.y) / 2f);
                int compare = compareResult(targetArray[currentIndex], value);
                if (compare == 0)
                {
                    return;
                }
                else
                {
                    if (compare < 0)
                    {
                        range.y = currentIndex;
                    }
                    else if (compare > 0)
                    {
                        range.x = currentIndex;
                    }
                }
            }
        }
    }
}