using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
public struct SkinRenderer
{
    public Mesh mesh;
    public ComputeBuffer boneBuffers;
    public NativeArray<Matrix4x4> bones;
    public Transform[] bonesTrans;
    public int meshIndex;
}

public struct SkinMesh
{
    public ComputeBuffer verticesBuffer;
    public ComputeBuffer weightsBuffer;
}

public struct SkinDataCollect
{
    public SkinMesh[] skinMeshes;
    public int meshLength;
    public Dictionary<Mesh, int> meshToSkin;
    public ComputeBuffer allVerticesBuffer;
}

public struct MeshSplitData
{
    public List<Vector3> vertices;
    public List<Vector2> uv;
    public List<Vector3> normal;
    public List<Vector4> tangent;
    public List<BoneWeight> weight;
    public List<int> triangle;
}