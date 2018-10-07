#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Concurrent;
using System.Linq;
using Unity.Jobs;
using System;
namespace MPipeline
{

    [ExecuteInEditMode]
    public unsafe class ClusterGenerator : MonoBehaviour
    {
        private Mesh testMesh;
        public Material testMat;
        public bool clear = true;

        private void OnDisable()
        {
            testMesh = GetComponent<MeshFilter>().sharedMesh;
            List<Vector4Int> value = ClusterFunctions.AddTrianglesToDictionary(testMesh.triangles);
            Bounds bd = testMesh.bounds;
            GetFragmentJob gt = new GetFragmentJob(bd.center, bd.extents, testMesh.vertices, value);
            (gt.Schedule(value.Count, 16)).Complete();
            var voxel = GetFragmentJob.voxelFragments;
            const int VoxelCount = ClusterFunctions.VoxelCount;
            Fragment first = new Fragment();
            Vector3Int inputPoint = new Vector3Int();
            for (int i = 0; i < VoxelCount; ++i) {
                for (int j = 0; j < VoxelCount; ++j)
                    for (int k = 0; k < VoxelCount; ++k)
                    {
                        var lst = voxel[i, j, k];
                        if(lst.Count > 0)
                        {
                            first = lst[0];
                            inputPoint = new Vector3Int(i, j, k);
                            goto BREAK;
                        }
                    }
            }
            BREAK:
            NativeArray<Fragment> resultCluster;
            LOOP:
            bool stillLooping = ClusterFunctions.GetFragFromVoxel(voxel, inputPoint, out resultCluster, out first);
            resultCluster.Dispose();
            if (stillLooping)
                goto LOOP;
            gt.Dispose();
        }
    }
    #region STRUCT
    public struct Vector4Int
    {
        public int x;
        public int y;
        public int z;
        public int w;
        public Vector4Int(int x, int y, int z, int w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }
    public struct Fragment
    {
        public Vector4Int indices;
        public Vector3 position;
        public Vector3Int voxel;
    }

    #endregion
    public unsafe static class ClusterFunctions
    {
        public const int ClusterCount = PipelineBaseBuffer.CLUSTERCLIPCOUNT / 4;
        public const int VoxelCount = 10;
        private static bool CheckCluster(Dictionary<Vector2Int, int> targetDict, Vector3Int triangle, out Vector4Int cluster)
        {
            cluster = new Vector4Int();
            NativeArray<Vector2Int> waitingTarget = new NativeArray<Vector2Int>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> currentTarget = new NativeArray<int>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            waitingTarget[0] = new Vector2Int(triangle.y, triangle.x);
            waitingTarget[1] = new Vector2Int(triangle.z, triangle.y);
            waitingTarget[2] = new Vector2Int(triangle.x, triangle.z);
            currentTarget[0] = triangle.z;
            currentTarget[1] = triangle.x;
            currentTarget[2] = triangle.y;
            for (int i = 0; i < 3; ++i)
            {
                int value;
                if (targetDict.TryGetValue(waitingTarget[i], out value))
                {
                    targetDict.Remove(waitingTarget[i]);
                    targetDict.Remove(new Vector2Int(waitingTarget[i].y, value));
                    targetDict.Remove(new Vector2Int(value, waitingTarget[i].x));
                    cluster = new Vector4Int(currentTarget[i], waitingTarget[i].y, waitingTarget[i].x, value);
                    return true;
                }
            }
            targetDict[new Vector2Int(triangle.x, triangle.y)] = triangle.z;
            targetDict[new Vector2Int(triangle.z, triangle.x)] = triangle.y;
            targetDict[new Vector2Int(triangle.y, triangle.z)] = triangle.x;
            waitingTarget.Dispose();
            currentTarget.Dispose();
            return false;
        }
        public static List<Vector4Int> AddTrianglesToDictionary(int[] triangleArray)
        {
            List<Vector4Int> allClusters = new List<Vector4Int>();
            Dictionary<Vector2Int, int> dict = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < triangleArray.Length; i += 3)
            {
                Vector4Int cluster;
                if (CheckCluster(dict, new Vector3Int(triangleArray[i], triangleArray[i + 1], triangleArray[i + 2]), out cluster))
                {
                    allClusters.Add(cluster);
                }
            }
            foreach (var i in dict.Keys)
            {
                int v = dict[i];
                allClusters.Add(new Vector4Int(i.x, i.y, v, v));
            }
            return allClusters;
        }

        public static Vector3Int GetVoxel(Vector3 left, Vector3 right)
        {
            Vector3 result;
            result.x = left.x / right.x;
            result.y = left.y / right.y;
            result.z = left.z / right.z;
            result *= VoxelCount;
            result.x = Mathf.Min(9.9f, result.x);
            result.y = Mathf.Min(9.9f, result.y);
            result.z = Mathf.Min(9.9f, result.z);
            return new Vector3Int((int)result.x, (int)result.y, (int)result.z);
        }

        public static void SetPointToVoxel(Vector4Int points, Vector3[] vertices, List<Fragment>[,,] voxel, Vector3 leftPoint, Vector3 extent)
        {
            Vector3 position = vertices[points.x]
                               + vertices[points.y]
                               + vertices[points.z]
                               + vertices[points.w];
            position /= 4;
            Vector3 distToLeft = position - leftPoint;
            Vector3Int voxelIndex = GetVoxel(distToLeft, extent);
            Fragment frag;
            frag.indices = points;
            frag.voxel = voxelIndex;
            frag.position = position;
            List<Fragment> lists = voxel[voxelIndex.x, voxelIndex.y, voxelIndex.z];
            lock (lists)
            {
                lists.Add(frag);
            }
        }
        private static void AddVoxelToList(List<Fragment>[,,] voxelArray, List<Fragment> targetArray, bool[,,] alreadyCalculated, Vector3Int coord)
        {
            if (coord.x >= VoxelCount || coord.x < 0 || coord.y >= VoxelCount || coord.y < 0 || coord.z >= VoxelCount || coord.z < 0 || alreadyCalculated[coord.x, coord.y, coord.z] || targetArray.Count >= (ClusterCount + 1))
                return;
            alreadyCalculated[coord.x, coord.y, coord.z] = true;
            List<Fragment> voxelValue = voxelArray[coord.x, coord.y, coord.z];
            targetArray.AddRange(voxelValue);
            voxelValue.Clear();
            AddVoxelToList(voxelArray, targetArray, alreadyCalculated, coord + new Vector3Int(-1, 0, 0));
            AddVoxelToList(voxelArray, targetArray, alreadyCalculated, coord + new Vector3Int(1, 0, 0));
            AddVoxelToList(voxelArray, targetArray, alreadyCalculated, coord + new Vector3Int(0, -1, 0));
            AddVoxelToList(voxelArray, targetArray, alreadyCalculated, coord + new Vector3Int(0, 1, 0));
            AddVoxelToList(voxelArray, targetArray, alreadyCalculated, coord + new Vector3Int(0, 0, -1));
            AddVoxelToList(voxelArray, targetArray, alreadyCalculated, coord + new Vector3Int(0, 0, 1));
        }

        public static bool GetFragFromVoxel(List<Fragment>[,,] voxelArray, Vector3Int inputPoint, out NativeArray<Fragment> resultCluster, out Fragment next)
        {
            bool[,,] alreadyCalculated = new bool[VoxelCount, VoxelCount, VoxelCount];
            List<Fragment> fragments = new List<Fragment>(ClusterCount + 1);
            AddVoxelToList(voxelArray, fragments, alreadyCalculated, inputPoint);
            if (fragments.Count > (ClusterCount + 1))
            {
                for (int i = ClusterCount; i < fragments.Count; ++i)
                {
                    Fragment f = fragments[i];
                    voxelArray[f.voxel.x, f.voxel.y, f.voxel.z].Add(f);
                }
            }
            if (ClusterCount < fragments.Count)
            {
                resultCluster = new NativeArray<Fragment>(ClusterCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < ClusterCount; ++i)
                {
                    resultCluster[i] = fragments[i];
                }
                next = fragments[ClusterCount];
                return true;
            }else
            {
                resultCluster = new NativeArray<Fragment>(fragments.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < fragments.Count; ++i)
                {
                    resultCluster[i] = fragments[i];
                }
                next = new Fragment();
                return false;
            }
        }
    }


    public unsafe struct GetFragmentJob : IJobParallelFor
    {
        public static Vector3[] vertices;
        public static List<Vector4Int> fragments;
        public static List<Fragment>[,,] voxelFragments;
        public static Vector3 leftPoint;
        public static Vector3 distance;
        public GetFragmentJob(Vector3 position, Vector3 extent, Vector3[] vert, List<Vector4Int> frag)
        {
            vertices = vert;
            fragments = frag;
            leftPoint = position - extent;
            distance = extent * 2;
            const int VoxelCount = ClusterFunctions.VoxelCount;
            voxelFragments = new List<Fragment>[VoxelCount, VoxelCount, VoxelCount];
            for (int i = 0; i < VoxelCount; ++i)
                for (int j = 0; j < VoxelCount; ++j)
                    for (int k = 0; k < VoxelCount; ++k)
                    {
                        voxelFragments[i, j, k] = new List<Fragment>();
                    }
        }
        public void Dispose()
        {
            vertices = null;
            fragments = null;
            voxelFragments = null;
            GC.Collect();
        }

        public void Execute(int index)
        {
            ClusterFunctions.SetPointToVoxel(fragments[index], vertices, voxelFragments, leftPoint, distance);
        }
    }
}
#endif