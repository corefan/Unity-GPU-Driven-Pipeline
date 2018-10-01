#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using System.IO;
[ExecuteInEditMode]
public unsafe class ClusterGenerator : MonoBehaviour
{
    private Mesh testMesh;
    public Material mat;
    public bool clear = true;
    public RenderPipeline pipelineComponent;
    public void GetAllTriangles(int[] indices, out Triangle* triangles, out int length)
    {
        triangles = (Triangle*)UnsafeUtility.AddressOf(ref indices[0]);
        length = indices.Length / 3;
    }
    public bool SimilarTriangle(Triangle* first, Triangle* second, out Cluster cluster, Vector3[] vertices)
    {
        int* firstPtr = (int*)first;
        int* secondPtr = (int*)second;
        Vector2Int tempInt = Vector2Int.zero;
        int* sameValue = (int*)&tempInt;
        int count = 0;
        for (int i = 0; i < 3; ++i)
        {
            for (int j = 0; j < 3; ++j)
            {
                if (firstPtr[i] == secondPtr[j])
                {
                    if (count >= 2) goto CONTINUELOGIC;
                    sameValue[count] = firstPtr[i];
                    count++;
                }
            }
        }
        CONTINUELOGIC:
        if (count < 2)
        {
            cluster = new Cluster(0, 0, 0, 0);
            return false;
        }
        else
        {
            int secondLastOne;
            for (secondLastOne = 0; secondLastOne < 3; ++secondLastOne)
            {
                if (secondPtr[secondLastOne] != sameValue[0] && secondPtr[secondLastOne] != sameValue[1])
                {
                    break;
                }
            }
            int firstLastOne;
            for (firstLastOne = 0; firstLastOne < 3; ++firstLastOne)
            {
                if (firstPtr[firstLastOne] != sameValue[0] && firstPtr[firstLastOne] != sameValue[1])
                {
                    break;
                }
            }
            cluster = new Cluster(firstPtr[firstLastOne], sameValue[0], sameValue[1], secondPtr[secondLastOne]);

            Vector3 originNormal = Vector3.Cross(vertices[first->y] - vertices[first->x], vertices[first->z] - vertices[first->y]).normalized;
            Vector3 currentNormal = Vector3.Cross(vertices[cluster.y] - vertices[cluster.x], vertices[cluster.z] - vertices[cluster.y]).normalized;
            if (Vector3.Dot(originNormal, currentNormal) < 0)
            {
                int i = cluster.y;
                cluster.y = cluster.z;
                cluster.z = i;
            }
            return true;
        }
    }
    public Cluster DegenerateCluster(Triangle* triangle)
    {
        return new Cluster(triangle->x, triangle->y, triangle->z, triangle->z);
    }
    public UnsafeList<Cluster> GetAllCluster(int[] indices, Vector3[] vertices)
    {
        Triangle* triangles;
        int length;
        GetAllTriangles(indices, out triangles, out length);
        UnsafeList<Cluster> clusters = new UnsafeList<Cluster>(length);
        bool* clustedFlags = (bool*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<bool>() * length, 16, Allocator.Temp);
        for (int i = 0; i < length; ++i)
        {
            clustedFlags[i] = false;
        }
        for (int i = 0; i < length; ++i)
        {
            for (int j = i + 1; j < length; ++j)
            {
                if (clustedFlags[j] || clustedFlags[i]) continue;
                Cluster currentCluster;
                if (SimilarTriangle(triangles + i, triangles + j, out currentCluster, vertices))
                {
                    clustedFlags[j] = true;
                    clustedFlags[i] = true;
                    clusters.Add(ref currentCluster);
                }
            }
        }

        for (int i = 0; i < length; ++i)
        {
            if (!clustedFlags[i])
            {
                clusters.Add(DegenerateCluster(triangles + i));
            }
        }
        UnsafeUtility.Free(clustedFlags, Allocator.Temp);
        return clusters;
    }
    public List<List<ClusterInfo>> GetClusterDistances(UnsafeList<Cluster> clusters, Vector3[] verts)
    {
        Vector3* positions = (Vector3*)UnsafeUtility.Malloc(sizeof(Vector3) * clusters.length, 16, Allocator.Temp);
        List<List<ClusterInfo>> distances = new List<List<ClusterInfo>>();
        ClusterDistancesDatas.positions = positions;
        ClusterDistancesDatas.distances = distances;
        ClusterDistancesDatas.verts = verts;
        ClusterDistancesDatas.clusters = clusters;
        for (int i = 0; i < clusters.length; ++i)
        {
            ClusterDistancesDatas.distances.Add(new List<ClusterInfo>(ClusterDistancesDatas.clusters.length));
            Cluster* c = (Cluster*)ClusterDistancesDatas.clusters[i];
            ClusterDistancesDatas.positions[i] = ClusterDistancesDatas.verts[c->x] + ClusterDistancesDatas.verts[c->y] + ClusterDistancesDatas.verts[c->z] + ClusterDistancesDatas.verts[c->w];
            ClusterDistancesDatas.positions[i] /= 4f;
        }
        JobHandle getSortDistanceJob = (new GetSortedDistance()).Schedule(clusters.length, 1);
        getSortDistanceJob.Complete();
        ClusterDistancesDatas.positions = null;
        ClusterDistancesDatas.distances = null;
        ClusterDistancesDatas.verts = null;
        UnsafeUtility.Free(positions, Allocator.Temp);
        return distances;
    }
    public List<Cluster[]> GetClusters(List<List<ClusterInfo>> clusterDistances, UnsafeList<Cluster> clusters)
    {
        int groupCount = clusters.length / 16;
        if (clusters.length % 16 > 0)
        {
            groupCount++;
        }
        List<Cluster[]> results = new List<Cluster[]>(groupCount);
        bool* isClusted = (bool*)UnsafeUtility.Malloc(clusters.length, 16, Allocator.Temp);
        UnsafeUtility.MemClear(isClusted, clusters.length);
        for (int i = 0; i < groupCount; ++i)
        {
            int clusterInfoIndex = Mathf.Min(i * 16, clusters.length - 1);
            List<ClusterInfo> currentClusterInfos = clusterDistances[clusterInfoIndex];
            Cluster[] currentCluster = new Cluster[16];
            results.Add(currentCluster);
            int count = 0;
            foreach (var v in currentClusterInfos)
            {
                if (count >= 16)
                {
                    break;
                }
                if (!isClusted[v.cluster])
                {
                    currentCluster[count] = *(Cluster*)clusters[v.cluster];
                    count++;
                    isClusted[v.cluster] = true;
                }
            }
            Cluster degenerate = new Cluster(0, 0, 0, 0);
            for (; count < 16; ++count)
            {
                currentCluster[count] = degenerate;
            }
        }
        UnsafeUtility.Free(isClusted, Allocator.Temp);
        return results;
    }
    private void OnDisable()
    {
        testMesh = GetComponent<MeshFilter>().sharedMesh;
        Vector3[] vert = testMesh.vertices;
        Vector3[] nor = testMesh.normals;
        Vector4[] tangents = testMesh.tangents;
        Vector2[] uv0 = testMesh.uv;
        int[] tris = testMesh.triangles;
        UnsafeList<Cluster> v = GetAllCluster(tris, vert);
        var clusterDistance = GetClusterDistances(v, vert);
        List<Cluster[]> clusterGroups = GetClusters(clusterDistance, v);
        uint infoByteSize = (uint)(clusterGroups.Count * ObjectInfo.SIZE);
        ObjectInfo* infos = (ObjectInfo*)UnsafeUtility.Malloc(infoByteSize, 16, Allocator.Temp);
        uint pointsByteSize = (uint)(clusterGroups.Count * 64 * Point.SIZE);
        Point* points = (Point*)UnsafeUtility.Malloc(pointsByteSize, 16, Allocator.Temp);
        for (int i = 0, pointsCount = 0; i < clusterGroups.Count; ++i)
        {
            Cluster* group = (Cluster*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<Cluster>() * clusterGroups[i].Length, 16, Allocator.Temp);
            for (int a = 0; a < clusterGroups[i].Length; ++a)
            {
                group[a] = clusterGroups[i][a];
            }
            int* decodedGroup = (int*)group;
            ObjectInfo info;
            info.position = Vector3.zero;
            info.extent = Vector3.zero;
            Vector3* allPositions = (Vector3*)UnsafeUtility.Malloc(sizeof(Vector3) * 64, 16, Allocator.Temp);
            for (int a = 0; a < 64; ++a)
            {

                int vertID = decodedGroup[a];

                Point p;
                p.vertex = vert[vertID];
                p.vertex = transform.localToWorldMatrix.MultiplyPoint(p.vertex);
                allPositions[a] = p.vertex;
                info.position += p.vertex;
                p.normal = nor[vertID];
                p.normal = transform.localToWorldMatrix.MultiplyVector(p.normal);
                p.tangent = tangents[vertID];
                Vector3 tangentXYZ = new Vector3(p.tangent.x, p.tangent.y, p.tangent.z);
                tangentXYZ = transform.localToWorldMatrix.MultiplyVector(tangentXYZ);
                p.tangent.x = tangentXYZ.x;
                p.tangent.y = tangentXYZ.y;
                p.tangent.z = tangentXYZ.z;
                p.texcoord = uv0[vertID];
                points[pointsCount] = p;
                pointsCount++;

            }
            info.position /= 64f;
            for (int a = 0; a < 64; ++a)
            {
                Vector3 point = allPositions[a];
                Vector3 dir = point - info.position;
                dir = dir.Abs();
                info.extent = dir.Max(info.extent);
            }
            infos[i] = info;
            byte[] pointsBytes = new byte[pointsByteSize];
            byte[] infosBytes = new byte[infoByteSize];
            fixed(byte* pointStartPtr = &pointsBytes[0])
            {
                UnsafeUtility.MemCpy(pointStartPtr, points, pointsByteSize);
            }
            fixed(byte* infoStartPtr = &infosBytes[0])
            {
                UnsafeUtility.MemCpy(infoStartPtr, infos, infoByteSize);
            }
            ByteArrayToFile("Assets/Resources/MapPoints.bytes", pointsBytes);
            ByteArrayToFile("Assets/Resources/MapInfos.bytes", infosBytes);
            UnsafeUtility.Free(allPositions, Allocator.Temp);
            UnsafeUtility.Free(group, Allocator.Temp);
            UnsafeUtility.Free(points, Allocator.Temp);
            UnsafeUtility.Free(infos, Allocator.Temp);
        }
    }
    public bool ByteArrayToFile(string fileName, byte[] byteArray)
    {
        try
        {
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                fs.Write(byteArray, 0, byteArray.Length);
                return true;
            }
        }
        catch
        {
            Debug.Log("Exception caught in process");
            return false;
        }
    }
}

public static class Vector3Utility
{
    public static Vector3 Abs(ref this Vector3 vec)
    {
        vec.x = Mathf.Abs(vec.x);
        vec.y = Mathf.Abs(vec.y);
        vec.z = Mathf.Abs(vec.z);
        return vec;
    }

    public static Vector3 Max(ref this Vector3 left, Vector3 right)
    {
        Vector3 value;
        value.x = Mathf.Max(left.x, right.x);
        value.y = Mathf.Max(left.y, right.y);
        value.z = Mathf.Max(left.z, right.z);
        return value;
    }
}
public struct Triangle
{
    public int x;
    public int y;
    public int z;
    public Triangle(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}
public struct ClusterInfo
{
    public int cluster;
    public float distance;
}
public struct Cluster
{
    public int x;
    public int y;
    public int z;
    public int w;
    public Cluster(int x, int y, int z, int w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }
}
public static unsafe class ClusterDistancesDatas
{
    public static Vector3* positions;
    public static List<List<ClusterInfo>> distances;
    public static Vector3[] verts;
    public static UnsafeList<Cluster> clusters;
}

public unsafe struct GetSortedDistance : IJobParallelFor
{//Cluster Length
    public static System.Comparison<ClusterInfo> smallToLargeFunction = (x, y) =>
    {
        if (x.distance < y.distance) return -1;
        if (x.distance > y.distance) return 1;
        return 0;
    };
    public void Execute(int i)
    {
        for (int j = 0; j < ClusterDistancesDatas.clusters.length; ++j)
        {
            ClusterInfo info;
            info.distance = Vector3.Distance(ClusterDistancesDatas.positions[i], ClusterDistancesDatas.positions[j]);
            info.cluster = j;
            ClusterDistancesDatas.distances[i].Add(info);
        }
        ClusterDistancesDatas.distances[i].Sort(smallToLargeFunction);
    }
}

#endif