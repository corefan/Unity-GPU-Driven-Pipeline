using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using System;
using System.Text;

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
        baseBuffer.instanceCountBuffer = new ComputeBuffer(1, PipelineBaseBuffer.INDIRECTSIZE, ComputeBufferType.IndirectArguments);
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

    public static void SetHizOccBuffer(ref PipelineCommandData data, RenderTexture hizDepth, ComputeShader shader, int kernel)
    {
        shader.SetTexture(kernel, ShaderIDs._HizDepthTex, hizDepth);
        shader.SetVector(ShaderIDs._CameraUpVector, data.cam.transform.up);
        shader.SetMatrix(ShaderIDs._VP, data.vp);
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
    public static void RunCullDispatching(ref PipelineBaseBuffer baseBuffer, ComputeShader computeShader, int kernel, bool isOrtho)
    {
        computeShader.SetInt(ShaderIDs._CullingPlaneCount, isOrtho ? 6 : 5);
        computeShader.Dispatch(1, 1, 1, 1);
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
        }
        UnsafeUtility.Free(clipDistances, Allocator.Temp);
    }
    public static void InitRenderTarget(ref RenderTargets tar, Camera tarcam, List<RenderTexture> collectRT)
    {
        tar.gbufferTextures[0] = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.ARGBHalf, collectRT);
        tar.gbufferTextures[1] = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.ARGBHalf, collectRT);
        tar.gbufferTextures[2] = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.ARGBFloat, collectRT);
        tar.gbufferTextures[3] = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 24, RenderTextureFormat.ARGBHalf, collectRT);
        tar.gbufferTextures[4] = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.RGFloat, collectRT);
        for (int i = 0; i < tar.gbufferTextures.Length; ++i)
        {
            tar.gbufferTextures[i].filterMode = FilterMode.Point;
            tar.geometryColorBuffer[i] = tar.gbufferTextures[i].colorBuffer;
            Shader.SetGlobalTexture(tar.gbufferIndex[i], tar.gbufferTextures[i]);
        }
        tar.renderTarget = tar.gbufferTextures[3];
        tar.backupTarget = GetTemporary(tarcam.pixelWidth, tarcam.pixelHeight, 0, RenderTextureFormat.ARGBHalf, collectRT);
        tar.colorBuffer = tar.renderTarget.colorBuffer;
        tar.depthBuffer = tar.renderTarget.depthBuffer;
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
}