using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
namespace MPipeline
{
    public unsafe struct CullResult
    {
        #region STATIC
        private static void GetCullingPlanes(ref Matrix4x4 invVp, Plane* frustumPlanes)
        {
            Vector3 nearLeftButtom = invVp.MultiplyPoint(new Vector3(-1, -1, 1));
            Vector3 nearLeftTop = invVp.MultiplyPoint(new Vector3(-1, 1, 1));
            Vector3 nearRightButtom = invVp.MultiplyPoint(new Vector3(1, -1, 1));
            Vector3 nearRightTop = invVp.MultiplyPoint(new Vector3(1, 1, 1));
            Vector3 farLeftButtom = invVp.MultiplyPoint(new Vector3(-1, -1, 0));
            Vector3 farLeftTop = invVp.MultiplyPoint(new Vector3(-1, 1, 0));
            Vector3 farRightButtom = invVp.MultiplyPoint(new Vector3(1, -1, 0));
            Vector3 farRightTop = invVp.MultiplyPoint(new Vector3(1, 1, 0));
            //Near
            frustumPlanes[0] = new Plane(nearRightTop, nearRightButtom, nearLeftButtom);
            //Up
            frustumPlanes[1] = new Plane(farLeftTop, farRightTop, nearRightTop);
            //Down
            frustumPlanes[2] = new Plane(nearRightButtom, farRightButtom, farLeftButtom);
            //Left
            frustumPlanes[3] = new Plane(farLeftButtom, farLeftTop, nearLeftTop);
            //Right
            frustumPlanes[4] = new Plane(farRightButtom, nearRightButtom, nearRightTop);
            //Far
            frustumPlanes[5] = new Plane(farLeftButtom, farRightButtom, farRightTop);
        }
        #endregion
        public NativeArray<DrawingPolicy> policy;
        public int policyLength;
        public NativeArray<BinarySort> sorts;
        public NativeArray<Plane> frustumPlane;
        public JobHandle objectCullingJob;
        public JobHandle sortJob;
        private static bool isRunning = false;
        private bool StartCulling(ref PipelineCommandData data)
        {
            if (!isRunning) return false;
            isRunning = true;
            ObjectCullJob job = new ObjectCullJob();
            ObjectCullJob.policies = policy;
            ObjectCullJob.sorts = sorts;
            ObjectCullJob.frustumPlanes = (Plane*)frustumPlane.GetUnsafePtr();
            ObjectCullJob.cameraPos = data.cam.transform.position;
            ObjectCullJob.cameraFarClipDistance = data.cam.farClipPlane;
            GetCullingPlanes(ref data.inverseVP, ObjectCullJob.frustumPlanes);
            SortJob sort = new SortJob();
            SortJob.sorts = sorts;
            objectCullingJob = job.Schedule(policyLength, 32);
            sortJob = sort.Schedule(sorts.Length, 1, objectCullingJob);
            JobHandle.ScheduleBatchedJobs();
            return true;
        }
    }

    public unsafe struct ObjectCullJob : IJobParallelFor
    {
        #region STATIC
        private static bool PlaneTest(ref Matrix4x4 ObjectToWorld, ref Vector3 extent, out Vector3 position, Plane* frustumPlanes)
        {
            Vector3 right = new Vector3(ObjectToWorld.m00, ObjectToWorld.m10, ObjectToWorld.m20);
            Vector3 up = new Vector3(ObjectToWorld.m01, ObjectToWorld.m11, ObjectToWorld.m21);
            Vector3 forward = new Vector3(ObjectToWorld.m02, ObjectToWorld.m12, ObjectToWorld.m22);
            position = new Vector3(ObjectToWorld.m03, ObjectToWorld.m13, ObjectToWorld.m23);
            for (int i = 0; i < 6; ++i)
            {
                Plane plane = frustumPlanes[i];
                float r = Vector3.Dot(position, plane.normal);
                Vector3 absNormal = new Vector3(Mathf.Abs(Vector3.Dot(plane.normal, right)), Mathf.Abs(Vector3.Dot(plane.normal, up)), Mathf.Abs(Vector3.Dot(plane.normal, forward)));
                float f = Vector3.Dot(absNormal, extent);
                if ((r - f) >= -plane.distance)
                    return false;
            }
            return true;
        }
        private static Mutex[] mutexs = new Mutex[20];
        public static NativeArray<DrawingPolicy> policies;
        public static NativeArray<BinarySort> sorts;
        [NativeDisableUnsafePtrRestriction]
        public static Plane* frustumPlanes;
        public static Vector3 cameraPos;
        public static float cameraFarClipDistance;
        #endregion
        public void Execute(int i)
        {
            DrawingPolicy* obj = (DrawingPolicy*)policies.GetUnsafePtr() + i;
            Vector3 position;
            if (PlaneTest(ref obj->localToWorldMatrix, ref obj->extent, out position, frustumPlanes))
            {
                float distance = Vector3.Distance(position, cameraPos);
                //Use Sqrt here because usually close objects' account is more than the far objects based on regular LOD design.
                float layer = Mathf.Sqrt(distance / cameraFarClipDistance);
                int layerCount = sorts.Length;
                int layerValue = (int)Mathf.Clamp(Mathf.Lerp(0, layerCount, layer), 0, layerCount - 1);
                sorts[layerValue].Add(distance, ref *obj, mutexs[layerValue]);
            }
        }
    }
    public unsafe struct SortJob : IJobParallelFor
    {
        public static NativeArray<BinarySort> sorts;
        public void Execute(int i)
        {
            sorts[i].Sort();
        }
    }
}
