using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using System;
using System.Text;
using MPipeline;

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
        //Far
        plane = new Plane(farLeftButtom, farRightButtom, farRightTop);
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
        //Near
        plane = new Plane(nearRightTop, nearRightButtom, nearLeftButtom);
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
        //Far
        plane = new Plane(farLeftButtom, farRightButtom, farRightTop);
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
        //Near
        plane = new Plane(nearRightTop, nearRightButtom, nearLeftButtom);
        cullingPlanes[5] = plane.normal;
        cullingPlanes[5].w = plane.distance;

    }
    /// <summary>
    /// Initialize pipeline buffers
    /// </summary>
    /// <param name="baseBuffer"></param> pipeline base buffer
    public static void InitBaseBuffer(ref PipelineBaseBuffer baseBuffer)
    {
        TextAsset[] allFileFlags = Resources.LoadAll<TextAsset>("MapSigns");
        int clusterCount = 0;
        foreach (var i in allFileFlags)
        {
            clusterCount += int.Parse(i.text);
        }
        StringBuilder sb = new StringBuilder(50, 150);
        NativeArray<ClusterMeshData> allInfos = new NativeArray<ClusterMeshData>(clusterCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        NativeArray<Point> allPoints = new NativeArray<Point>(clusterCount * PipelineBaseBuffer.CLUSTERCLIPCOUNT, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        clusterCount = 0;
        int pointCount = 0;
        foreach (var i in allFileFlags)
        {
            sb.Clear();
            sb.Append("MapInfos/");
            sb.Append(i.name);
            TextAsset clusterFile = Resources.Load<TextAsset>(sb.ToString());
            sb.Clear();
            sb.Append("MapPoints/");
            sb.Append(i.name);
            TextAsset pointFile = Resources.Load<TextAsset>(sb.ToString());
            byte[] clusterArray = clusterFile.bytes;
            byte[] pointArray = pointFile.bytes;
            fixed (void* source = &clusterArray[0])
            {
                byte* dest = (byte*)allInfos.GetUnsafePtr() + clusterCount;
                UnsafeUtility.MemCpy(dest, source, clusterArray.Length);
            }
            clusterCount += clusterArray.Length;
            fixed (void* source = &pointArray[0])
            {
                byte* dest = (byte*)allPoints.GetUnsafePtr() + pointCount;
                UnsafeUtility.MemCpy(dest, source, pointArray.Length);
            }
            pointCount += pointArray.Length;
        }

        baseBuffer.clusterBuffer = new ComputeBuffer(allInfos.Length, ClusterMeshData.SIZE);
        baseBuffer.clusterBuffer.SetData(allInfos);
        baseBuffer.resultBuffer = new ComputeBuffer(allInfos.Length, PipelineBaseBuffer.UINTSIZE);
        baseBuffer.instanceCountBuffer = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
        NativeArray<uint> instanceCountBufferValue = new NativeArray<uint>(5, Allocator.Temp);
        instanceCountBufferValue[0] = PipelineBaseBuffer.CLUSTERVERTEXCOUNT;
        baseBuffer.instanceCountBuffer.SetData(instanceCountBufferValue);
        instanceCountBufferValue.Dispose();
        baseBuffer.verticesBuffer = new ComputeBuffer(allPoints.Length, Point.SIZE);
        baseBuffer.verticesBuffer.SetData(allPoints);
        baseBuffer.clusterCount = allInfos.Length;
        allInfos.Dispose();
        allPoints.Dispose();
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

    public static bool FrustumCulling(ref Matrix4x4 ObjectToWorld, Vector3 extent, Vector4* frustumPlanes)
    {
        Vector3 right = new Vector3(ObjectToWorld.m00, ObjectToWorld.m10, ObjectToWorld.m20);
        Vector3 up = new Vector3(ObjectToWorld.m01, ObjectToWorld.m11, ObjectToWorld.m21);
        Vector3 forward = new Vector3(ObjectToWorld.m02, ObjectToWorld.m12, ObjectToWorld.m22);
        Vector3 position = new Vector3(ObjectToWorld.m03, ObjectToWorld.m13, ObjectToWorld.m23);
        for (int i = 0; i < 6; ++i)
        {
            ref Vector4 plane = ref frustumPlanes[i];
            Vector3 normal = new Vector3(plane.x, plane.y, plane.z);
            float distance = plane.w;
            float r = Vector3.Dot(position, normal);
            Vector3 absNormal = new Vector3(Mathf.Abs(Vector3.Dot(normal, right)), Mathf.Abs(Vector3.Dot(normal, up)), Mathf.Abs(Vector3.Dot(normal, forward)));
            float f = Vector3.Dot(absNormal, extent);
            if ((r - f) >= -distance)
                return false;
        }
        return true;
    }

    public static void SetShadowCameraPositionStaticFit(ref StaticFit fit, ref OrthoCam shadCam, int pass, Matrix4x4[] vpMatrices, out Matrix4x4 invShadowVP)
    {
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
        shadCam.size = range;
        float farClipPlane = fit.mainCamTrans.farClipPlane;
        Vector3 targetPosition = averagePos - shadCam.forward * farClipPlane * 0.5f;
        shadCam.nearClipPlane = 0;
        shadCam.farClipPlane = farClipPlane;
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
        shadCam.position = targetPosition;
        shadCam.UpdateProjectionMatrix();
        shadCam.UpdateTRSMatrix();
        shadowVP = GL.GetGPUProjectionMatrix(shadCam.projectionMatrix, false) * shadCam.worldToCameraMatrix;
        invShadowVP = shadowVP.inverse;
    }
    /// <summary>
    /// Initialize Per frame shadowmap buffers for Shadowmap shader
    /// </summary>
    public static void UpdateShadowMapState(ref ShadowMapComponent comp, ref ShadowmapSettings settings)
    {
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
        Vector4 shadowcamDir = comp.shadCam.forward;
        shadowcamDir.w = bias;
        Graphics.SetRenderTarget(comp.shadowmapTexture, 0, CubemapFace.Unknown, depthSlice: pass);
        GL.Clear(true, true, Color.white);
        Shader.SetGlobalVector(ShaderIDs._ShadowCamDirection, shadowcamDir);
        Matrix4x4 rtVp = GL.GetGPUProjectionMatrix(comp.shadCam.projectionMatrix, true) * comp.shadCam.worldToCameraMatrix;
        comp.shadowDepthMaterial.SetMatrix(ShaderIDs._ShadowMapVP, rtVp);
        Shader.SetGlobalVector(ShaderIDs._ShadowCamPos, comp.shadCam.position);
        comp.shadowDepthMaterial.SetPass(0);
    }
    /// <summary>
    /// Initialize shadowmask per frame buffers
    /// </summary>
    public static void UpdateShadowMaskState(Material shadowMaskMaterial, ref ShadowMapComponent shadMap, Matrix4x4[] cascadeShadowMapVP)
    {
        Shader.SetGlobalMatrixArray(ShaderIDs._ShadowMapVPs, cascadeShadowMapVP);
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
    public static void SetBaseBuffer(ref PipelineBaseBuffer baseBuffer, ComputeShader gpuFrustumShader, Vector4[] frustumCullingPlanes, int kernel)
    {
        var compute = gpuFrustumShader;
        compute.SetVectorArray(ShaderIDs.planes, frustumCullingPlanes);
        compute.SetBuffer(kernel, ShaderIDs.clusterBuffer, baseBuffer.clusterBuffer);
        compute.SetBuffer(kernel, ShaderIDs.instanceCountBuffer, baseBuffer.instanceCountBuffer);
        compute.SetBuffer(1, ShaderIDs.instanceCountBuffer, baseBuffer.instanceCountBuffer);
        compute.SetBuffer(kernel, ShaderIDs.resultBuffer, baseBuffer.resultBuffer);
    }

    public static void SetShaderBuffer(ref PipelineBaseBuffer basebuffer, Material mat)
    {
        mat.SetBuffer(ShaderIDs.verticesBuffer, basebuffer.verticesBuffer);
        mat.SetBuffer(ShaderIDs.resultBuffer, basebuffer.resultBuffer);
    }

    public static void DrawLastFrameCullResult(
        ref PipelineBaseBuffer baseBuffer,
        ref OcclusionBuffers occBuffer,
        Material indirectMaterial)
    {
        indirectMaterial.SetBuffer(ShaderIDs.resultBuffer, baseBuffer.resultBuffer);
        indirectMaterial.SetBuffer(ShaderIDs.verticesBuffer, baseBuffer.verticesBuffer);
        indirectMaterial.SetPass(0);
        Graphics.DrawProceduralIndirect(MeshTopology.Triangles, baseBuffer.instanceCountBuffer);
    }

    public static void DrawRecheckCullResult(
        ref OcclusionBuffers occBuffer,
        Material indirectMaterial)
    {
        indirectMaterial.SetPass(0);
        Graphics.DrawProceduralIndirect(MeshTopology.Triangles, occBuffer.reCheckCount);
    }

    public static void RunCullDispatching(ref PipelineBaseBuffer baseBuffer, ComputeShader computeShader, int kernel, bool isOrtho)
    {
        computeShader.SetInt(ShaderIDs._CullingPlaneCount, isOrtho ? 6 : 5);
        ComputeShaderUtility.Dispatch(computeShader, kernel, baseBuffer.clusterCount, 64);
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
    public static void DrawShadow(Camera currentCam,
        ComputeShader gpuFrustumShader,
        ref RenderArray arrayCollection,
        ref PipelineBaseBuffer baseBuffer,
        ref ShadowmapSettings settings,
        ref ShadowMapComponent shadMap,
        Matrix4x4[] cascadeShadowMapVP,
        Vector4[] shadowFrustumPlanes)
    {
        var arr = arrayCollection;
        StaticFit staticFit;
        staticFit.resolution = settings.resolution;
        staticFit.mainCamTrans = currentCam;
        staticFit.frustumCorners = shadMap.frustumCorners;
        UpdateShadowMapState(ref shadMap, ref settings);
        float* clipDistances = (float*)UnsafeUtility.Malloc(CASCADECLIPSIZE, 16, Allocator.Temp);
        clipDistances[0] = shadMap.shadCam.nearClipPlane;
        clipDistances[1] = settings.firstLevelDistance;
        clipDistances[2] = settings.secondLevelDistance;
        clipDistances[3] = settings.thirdLevelDistance;
        clipDistances[4] = settings.farestDistance;
        SetShaderBuffer(ref baseBuffer, shadMap.shadowDepthMaterial);
        for (int pass = 0; pass < CASCADELEVELCOUNT; ++pass)
        {
            Vector2 farClipDistance = new Vector2(clipDistances[pass], clipDistances[pass + 1]);
            GetfrustumCorners(farClipDistance, ref shadMap, currentCam);
            // PipelineFunctions.SetShadowCameraPositionCloseFit(ref shadMap, ref settings);
            Matrix4x4 invpVPMatrix;
            SetShadowCameraPositionStaticFit(ref staticFit, ref shadMap.shadCam, pass, cascadeShadowMapVP, out invpVPMatrix);
            GetCullingPlanes(ref invpVPMatrix, shadowFrustumPlanes);
            SetBaseBuffer(ref baseBuffer, gpuFrustumShader, shadowFrustumPlanes, 0);
            RunCullDispatching(ref baseBuffer, gpuFrustumShader, 0, true);
            float* biasList = (float*)UnsafeUtility.AddressOf(ref settings.bias);
            UpdateCascadeState(ref shadMap, biasList[pass] / currentCam.farClipPlane, pass);
            Graphics.DrawProceduralIndirect(MeshTopology.Triangles, baseBuffer.instanceCountBuffer);
            gpuFrustumShader.Dispatch(1, 1, 1, 1);
        }
        UnsafeUtility.Free(clipDistances, Allocator.Temp);
    }
    public static void InitRenderTarget(ref RenderTargets tar, Camera tarcam, List<RenderTexture> collectRT)
    {
        tar.gbufferTextures[0] = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.ARGB32, FilterMode.Bilinear, collectRT);
        tar.gbufferTextures[1] = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.ARGB32, FilterMode.Bilinear, collectRT);
        tar.gbufferTextures[2] = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, collectRT);
        tar.gbufferTextures[3] = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 24, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, collectRT);
        tar.gbufferTextures[4] = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.RGHalf, FilterMode.Point, collectRT);
        tar.gbufferTextures[5] = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.RFloat, FilterMode.Point, collectRT);
        for (int i = 0; i < tar.gbufferTextures.Length; ++i)
        {
            tar.gbufferTextures[i].filterMode = FilterMode.Bilinear;
            tar.geometryColorBuffer[i] = tar.gbufferTextures[i].colorBuffer;
            Shader.SetGlobalTexture(tar.gbufferIndex[i], tar.gbufferTextures[i]);
        }
        tar.renderTarget = tar.gbufferTextures[3];
        tar.backupTarget = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.ARGBHalf, FilterMode.Bilinear, collectRT);
        tar.backupTarget.filterMode = FilterMode.Bilinear;
        tar.colorBuffer = tar.renderTarget.colorBuffer;
        tar.depthBuffer = tar.renderTarget.depthBuffer;
    }

    public static RenderTexture GetTemporary(RenderTextureDescriptor descriptor, List<RenderTexture> collectList)
    {
        RenderTexture rt = RenderTexture.GetTemporary(descriptor);
        collectList.Add(rt);
        return rt;
    }


    public static RenderTexture GetTemporary(int width, int height, int depth, RenderTextureFormat format, FilterMode filterMode, List<RenderTexture> collectList)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height, depth, format, RenderTextureReadWrite.Linear);
        rt.filterMode = filterMode;
        collectList.Add(rt);
        return rt;
    }

    public static RenderTexture GetTemporary(int width, int height, int depth, RenderTextureFormat format, RenderTextureReadWrite readWrite, FilterMode filterMode, List<RenderTexture> collectList)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height, depth, format, readWrite);
        rt.filterMode = filterMode;
        collectList.Add(rt);
        return rt;
    }

    public static void ReleaseRenderTarget(List<RenderTexture> tar)
    {
        foreach (var i in tar)
        {
            RenderTexture.ReleaseTemporary(i);
        }
        tar.Clear();
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
    public static void InitOcclusionBuffer(ref OcclusionBuffers buffers)
    {
        buffers.dispatchBuffer = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
        NativeArray<uint> occludedCountList = new NativeArray<uint>(5, Allocator.Temp, NativeArrayOptions.ClearMemory);
        occludedCountList[0] = 0;
        occludedCountList[1] = 1;
        occludedCountList[2] = 1;
        occludedCountList[3] = 0;
        occludedCountList[4] = 0;
        buffers.dispatchBuffer.SetData(occludedCountList);
        buffers.reCheckCount = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
        occludedCountList[0] = PipelineBaseBuffer.CLUSTERVERTEXCOUNT;
        occludedCountList[1] = 0;
        occludedCountList[2] = 0;
        buffers.reCheckCount.SetData(occludedCountList);
        occludedCountList.Dispose();
    }
    public static void DisposeOcclusionBuffer(ref OcclusionBuffers buffers)
    {
        buffers.dispatchBuffer.Dispose();
        buffers.reCheckCount.Dispose();
        if(buffers.reCheckResult != null)
            buffers.reCheckResult.Dispose();
    }
    public static void UpdateOcclusionBuffer(
        ref PipelineBaseBuffer basebuffer
        , ComputeShader coreShader
        , ref OcclusionBuffers buffers
        , HizOcclusionData occlusionData
        , Vector4[] frustumCullingPlanes
        , bool isOrtho, int clusterCount)
    {
        if (buffers.reCheckResult != null && buffers.reCheckResult.count != clusterCount)
        {
            buffers.reCheckResult.Dispose();
            buffers.reCheckResult = null;
        }
        if (buffers.reCheckResult == null)
        {
            buffers.reCheckResult = new ComputeBuffer(clusterCount, 4);
        }
        coreShader.SetInt(ShaderIDs._CullingPlaneCount, isOrtho ? 6 : 5);
        coreShader.SetVectorArray(ShaderIDs.planes, frustumCullingPlanes);
        coreShader.SetVector(ShaderIDs._CameraUpVector, occlusionData.lastFrameCameraUp);
        coreShader.SetBuffer(OcclusionBuffers.FrustumFilter, ShaderIDs.clusterBuffer, basebuffer.clusterBuffer);
        coreShader.SetTexture(OcclusionBuffers.FrustumFilter, ShaderIDs._HizDepthTex, occlusionData.historyDepth);
        coreShader.SetBuffer(OcclusionBuffers.FrustumFilter, ShaderIDs.dispatchBuffer, buffers.dispatchBuffer);
        coreShader.SetBuffer(OcclusionBuffers.FrustumFilter, ShaderIDs.resultBuffer, basebuffer.resultBuffer);
        coreShader.SetBuffer(OcclusionBuffers.FrustumFilter, ShaderIDs.instanceCountBuffer, basebuffer.instanceCountBuffer);
        coreShader.SetBuffer(OcclusionBuffers.FrustumFilter, ShaderIDs.reCheckResult, buffers.reCheckResult);
        ComputeShaderUtility.Dispatch(coreShader, OcclusionBuffers.FrustumFilter, basebuffer.clusterCount, 64);
    }
    public static void ClearOcclusionData(
        ref PipelineBaseBuffer baseBuffer
        , ref OcclusionBuffers buffers
        , ComputeShader coreShader)
    {
        coreShader.SetBuffer(OcclusionBuffers.ClearOcclusionData, ShaderIDs.dispatchBuffer, buffers.dispatchBuffer);
        coreShader.SetBuffer(OcclusionBuffers.ClearOcclusionData, ShaderIDs.instanceCountBuffer, baseBuffer.instanceCountBuffer);
        coreShader.SetBuffer(OcclusionBuffers.ClearOcclusionData, ShaderIDs.reCheckCount, buffers.reCheckCount);
        coreShader.Dispatch(OcclusionBuffers.ClearOcclusionData, 1, 1, 1);
    }
    public static void OcclusionRecheck(
        ref PipelineBaseBuffer baseBuffer
        , ref OcclusionBuffers buffers
        , ComputeShader coreShader
        , HizOcclusionData hizData)
    {
        coreShader.SetVector(ShaderIDs._CameraUpVector, hizData.lastFrameCameraUp);
        coreShader.SetBuffer(OcclusionBuffers.OcclusionRecheck, ShaderIDs.dispatchBuffer, buffers.dispatchBuffer);
        coreShader.SetBuffer(OcclusionBuffers.OcclusionRecheck, ShaderIDs.reCheckResult, buffers.reCheckResult);
        coreShader.SetBuffer(OcclusionBuffers.OcclusionRecheck, ShaderIDs.clusterBuffer, baseBuffer.clusterBuffer);
        coreShader.SetTexture(OcclusionBuffers.OcclusionRecheck, ShaderIDs._HizDepthTex, hizData.historyDepth);
        coreShader.SetBuffer(OcclusionBuffers.OcclusionRecheck, ShaderIDs.reCheckCount, buffers.reCheckCount);
        coreShader.SetBuffer(OcclusionBuffers.OcclusionRecheck, ShaderIDs.resultBuffer, baseBuffer.resultBuffer);
        coreShader.DispatchIndirect(OcclusionBuffers.OcclusionRecheck, buffers.dispatchBuffer);
    }
}