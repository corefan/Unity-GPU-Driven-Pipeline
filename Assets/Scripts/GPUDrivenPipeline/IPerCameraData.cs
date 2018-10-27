using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace MPipeline
{
    public abstract class IPerCameraData
    {
        public static IPerCameraData GetProperty(PipelineEvent targetEvent, PipelineCamera camera, Func<IPerCameraData> initFunc)
        {
            IPerCameraData data;
            if (!camera.postDatas.TryGetValue(targetEvent, out data))
            {
                data = initFunc();
                camera.postDatas.Add(targetEvent, data);
            }
            return data;
        }
        public static IPerCameraData GetProperty(PipelineEvent targetEvent, PipelineCamera camera, Func<PipelineCamera, PipelineEvent, IPerCameraData> initFunc)
        {
            IPerCameraData data;
            if (!camera.postDatas.TryGetValue(targetEvent, out data))
            {
                data = initFunc(camera, targetEvent);
                camera.postDatas.Add(targetEvent, data);
            }
            return data;
        }

        public static IPerCameraData GetProperty(PipelineEvent targetEvent, PipelineCamera camera, Func<PipelineCamera, IPerCameraData> initFunc)
        {
            IPerCameraData data;
            if (!camera.postDatas.TryGetValue(targetEvent, out data))
            {
                data = initFunc(camera);
                camera.postDatas.Add(targetEvent, data);
            }
            return data;
        }

        public static IPerCameraData GetProperty(PipelineEvent targetEvent, PipelineCamera camera, PipelineResources resource, Func<PipelineCamera, PipelineEvent, PipelineResources, IPerCameraData> initFunc)
        {
            IPerCameraData data;
            if (!camera.postDatas.TryGetValue(targetEvent, out data))
            {
                data = initFunc(camera, targetEvent, resource);
                camera.postDatas.Add(targetEvent, data);
            }
            return data;
        }

        public static IPerCameraData GetProperty(PipelineEvent targetEvent, PipelineCamera camera, PipelineResources resource, Func<PipelineEvent, PipelineResources, IPerCameraData> initFunc)
        {
            IPerCameraData data;
            if (!camera.postDatas.TryGetValue(targetEvent, out data))
            {
                data = initFunc(targetEvent, resource);
                camera.postDatas.Add(targetEvent, data);
            }
            return data;
        }

        public static IPerCameraData GetProperty(PipelineEvent targetEvent, PipelineCamera camera, PipelineResources resource, Func<PipelineCamera, PipelineResources, IPerCameraData> initFunc)
        {
            IPerCameraData data;
            if (!camera.postDatas.TryGetValue(targetEvent, out data))
            {
                data = initFunc(camera, resource);
                camera.postDatas.Add(targetEvent, data);
            }
            return data;
        }
        public static IPerCameraData GetProperty(PipelineEvent targetEvent, PipelineCamera camera, PipelineResources resource, Func<PipelineResources, IPerCameraData> initFunc)
        {
            IPerCameraData data;
            if (!camera.postDatas.TryGetValue(targetEvent, out data))
            {
                data = initFunc(resource);
                camera.postDatas.Add(targetEvent, data);
            }
            return data;
        }
        public abstract void DisposeProperty();
    }
}
