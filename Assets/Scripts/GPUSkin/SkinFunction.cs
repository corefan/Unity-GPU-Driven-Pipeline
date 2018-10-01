using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
public unsafe static class SkinFunction
{
    public const int BONEWEIGHTSIZE = 32;
    public const int MATRIXSIZE = 64;
    public const int THREADGROUPCOUNT = 64;
    public static void GetPointsFromSplitData(Mesh mesh, ref MeshSplitData data, out NativeArray<BoneWeight> boneWeights, out NativeArray<Point> points)
    {
        mesh.GetBoneWeights(data.weight);
        mesh.GetVertices(data.vertices);
        mesh.GetTangents(data.tangent);
        mesh.GetUVs(0, data.uv);
        mesh.GetNormals(data.normal);
        mesh.GetTriangles(data.triangle, 0);
        boneWeights = new NativeArray<BoneWeight>(data.weight.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        points = new NativeArray<Point>(data.triangle.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        for(int i = 0; i < data.weight.Count; ++i)
        {
            boneWeights[i] = data.weight[i];
        }
        ulong pointsPtr = (ulong)points.GetUnsafePtr();
        for (int i = 0; i < data.triangle.Count; ++i)
        {
            int index = data.triangle[i];
            Point* p = (Point*)(pointsPtr + (ulong)(i * Point.SIZE));
            p->normal = data.normal[index];
            p->tangent = data.tangent[index];
            p->texcoord = data.uv[index];
            p->vertex = data.vertices[index];
        }
    }
    /// <summary>
    /// Get Skin Mesh from weights array and points array
    /// </summary>
    /// <param name="weights"></param>
    /// <param name="points"></param>
    /// <returns></returns>
    public static SkinMesh PointsArrayToBuffer(NativeArray<BoneWeight> weights, NativeArray<Point> points)
    {
        SkinMesh retValue;
        retValue.verticesBuffer = new ComputeBuffer(points.Length, Point.SIZE);
        retValue.weightsBuffer = new ComputeBuffer(weights.Length, BONEWEIGHTSIZE);
        retValue.verticesBuffer.SetData(points);
        retValue.weightsBuffer.SetData(weights);
        return retValue;
    }
    /// <summary>
    /// Update Bone Position from Transform
    /// </summary>
    /// <param name="skr"></param>
    public static void UpdateBonePos(ref SkinRenderer skr)
    {
        ulong startPtr = (ulong)skr.bones.GetUnsafePtr();
        for(int i = 0; i < skr.bonesTrans.Length; ++i)
        {
            Matrix4x4* matPtr = (Matrix4x4*)(startPtr + (ulong)(MATRIXSIZE * i));
            *matPtr = skr.bonesTrans[i].localToWorldMatrix;
        }
        skr.boneBuffers.SetData(skr.bones);
    }
    /// <summary>
    /// Set buffers to Compute Shader and Dispatch
    /// </summary>
    /// <returns></returns> All Vertex Count
    public static int GpuSkin(ComputeShader skinShader, ComputeBuffer allVerticesBuffer, SkinRenderer[] allSkr, SkinMesh[] allMeshes)
    {
        Shader.SetGlobalBuffer(ShaderIDs.allVerticesBuffer, allVerticesBuffer);
        skinShader.SetBuffer(0, ShaderIDs.allVerticesBuffer, allVerticesBuffer);
        int offset = 0;
        for(int i = 0; i < allSkr.Length; ++i)
        {
            ref SkinRenderer s = ref allSkr[i];
            ref SkinMesh m = ref allMeshes[s.meshIndex];
            skinShader.SetBuffer(0, ShaderIDs.verticesBuffer, m.verticesBuffer);
            skinShader.SetBuffer(0, ShaderIDs.weightsBuffer, m.weightsBuffer);
            skinShader.SetBuffer(0, ShaderIDs.boneBuffers, s.boneBuffers);
            skinShader.SetInt(ShaderIDs._OffsetIndex, offset);
            offset += m.verticesBuffer.count;
            ComputeShaderUtility.Dispatch(skinShader, 0, m.verticesBuffer.count, THREADGROUPCOUNT);
        }
        return offset;
    }
    public static void InitSkinRendererIndex(ref SkinRenderer rend, ref SkinDataCollect collect, ref MeshSplitData splitData)
    {
        int ind;
        if(collect.meshToSkin.TryGetValue(rend.mesh, out ind))
        {
            rend.meshIndex = ind;
        }else
        {
            if(collect.meshLength >= collect.skinMeshes.Length)
            {
                SkinMesh[] newSkinMesh = new SkinMesh[collect.skinMeshes.Length * 2];
                for(int i = 0; i < collect.meshLength; ++i)
                {
                    newSkinMesh[i] = collect.skinMeshes[i];
                }
                collect.skinMeshes = newSkinMesh;
            }
            SkinMesh newMesh;
            NativeArray<BoneWeight> boneWeights;
            NativeArray<Point> points;
            GetPointsFromSplitData(rend.mesh, ref splitData, out boneWeights, out points);
            newMesh = PointsArrayToBuffer(boneWeights, points);
            boneWeights.Dispose();
            points.Dispose();
            rend.meshIndex = collect.meshLength;
            collect.meshLength++;
        }
    }
}
