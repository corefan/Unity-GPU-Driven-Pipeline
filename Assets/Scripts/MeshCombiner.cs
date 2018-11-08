using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
namespace MPipeline
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public unsafe class MeshCombiner : MonoBehaviour
    {
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
                pt.objIndex = (uint)materialIndex;
            }
            int[] triangleArray = targetMesh.triangles;
            for (int i = 0; i < triangleArray.Length; ++i)
            {
                triangleArray[i] += originLength;
            }
            triangles.AddRange(triangleArray);
        }
        public CombinedModel ProcessCluster(MeshRenderer[] allRenderers)
        {
            MeshFilter[] allFilters = new MeshFilter[allRenderers.Length];
            int sumVertexLength = 0;
            int sumTriangleLength = 0;
            for (int i = 0; i < allFilters.Length; ++i)
            {
                allFilters[i] = allRenderers[i].GetComponent<MeshFilter>();
                sumVertexLength += allFilters[i].sharedMesh.vertexCount;
            }
            sumTriangleLength = (int)(sumVertexLength * 1.5);
            NativeList<Point> points = new NativeList<Point>(sumVertexLength, sumVertexLength, Allocator.Temp);
            NativeList<int> triangles = new NativeList<int>(sumTriangleLength, sumTriangleLength, Allocator.Temp);
            List<Material> allMat = new List<Material>();
            for (int i = 0; i < allFilters.Length; ++i)
            {
                Mesh mesh = allFilters[i].sharedMesh;
                Material mat = allRenderers[i].sharedMaterial;
                int index;
                if ((index = allMat.IndexOf(mat)) < 0)
                {
                    index = allMat.Count;
                    allMat.Add(mat);
                }
                GetPoints(points, triangles, mesh, index);
            }
            Vector3 less = points[0].vertex;
            Vector3 more = points[0].vertex;
            for (int i = 1; i < points.Length; ++i)
            {
                Vector3 current = points[i].vertex;
                if (less.x > current.x) less.x = current.x;
                if (more.x < current.x) more.x = current.x;
                if (less.y > current.y) less.y = current.y;
                if (more.y < current.y) more.y = current.y;
                if (less.z > current.z) less.z = current.z;
                if (more.z < current.z) more.z = current.z;
            }
            Vector3 center = (less + more) / 2;
            Vector3 extent = more - center;
            Bounds b = new Bounds(center, extent);
            CombinedModel md;
            md.bound = b;
            md.allPoints = points;
            md.triangles = triangles;
            md.containedMaterial = allMat;
            return md;
        }
        public struct CombinedModel
        {
            public NativeList<Point> allPoints;
            public NativeList<int> triangles;
            public List<Material> containedMaterial;
            public Bounds bound;
        }
    }
    
}