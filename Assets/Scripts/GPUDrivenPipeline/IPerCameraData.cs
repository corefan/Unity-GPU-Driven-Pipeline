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
        public abstract void DisposeProperty();
    }
}
