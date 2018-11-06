using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public unsafe class MeshCombiner : MonoBehaviour
{
    public List<Material> allMaterials;
    public List<Point[]> allPoints;
    public void GetPoints(NativeList<Point> points, NativeList<int> triangles, Mesh targetMesh, int materialIndex)
    {
        int originLength = points.Length;
        Vector3[] vertices = targetMesh.vertices;
        Vector2[] uv = targetMesh.uv;
        Vector3[] normal = targetMesh.normals;
        Vector4[] tangents = targetMesh.tangents;
        Action<Vector2[], NativeList<Point>, int> SetUV;
        if (uv.Length == vertices.Length)
        {
            SetUV = (vec, pt, i) => pt[i].texcoord = vec[i];
        }
        else
        {
            SetUV = (vec, pt, i) => pt[i].texcoord = Vector3.zero;
        }
        Action<Vector3[], NativeList<Point>, int> SetNormal;
        if (normal.Length == vertices.Length)
        {
            SetNormal = (vec, pt, i) => pt[i].normal = transform.localToWorldMatrix.MultiplyVector(vec[i]);
        }
        else
        {
            SetNormal = (vec, pt, i) => pt[i].normal = Vector3.zero;
        }
        Action<Vector4[], NativeList<Point>, int> SetTangent;
        if (tangents.Length == vertices.Length)
        {
            SetTangent = (vec, pt, i) =>
            {
                Vector3 worldTangent = vec[i];
                worldTangent = transform.localToWorldMatrix.MultiplyVector(worldTangent);
                pt[i].tangent = worldTangent;
                pt[i].tangent.w = vec[i].w;
            };
        }
        else
        {
            SetTangent = (vec, pt, i) => pt[i].tangent = Vector4.one;
        }
        points.AddRange(vertices.Length);
        for (int i = originLength; i < vertices.Length + originLength; ++i)
        {
            ref Point pt = ref points[i];
            pt.vertex = vertices[i];
            SetNormal(normal, points, i);
            SetTangent(tangents, points, i);
            SetUV(uv, points, i);
            pt.texcoord.z = materialIndex + 0.01f;
        }
        int[] triangleArray = targetMesh.triangles;
        for(int i = 0; i < triangleArray.Length; ++i)
        {
            triangleArray[i] += originLength;
        }
        triangles.AddRange(triangleArray);
    }
}