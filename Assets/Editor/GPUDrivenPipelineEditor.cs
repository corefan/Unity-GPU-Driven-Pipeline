using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(RenderPipeline))]
public class GPUDrivenPipelineEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
       // GpuDrivenPipeline target = serializedObject.targetObject as GpuDrivenPipeline;
    }

    public static void GetTransforms(List<Transform> targetTrans, Transform parent)
    {
        if (parent.GetComponent<MeshRenderer>() && parent.GetComponent<MeshFilter>() && parent.gameObject.isStatic)
        {
            targetTrans.Add(parent);
        }
        for (int i = 0; i < parent.childCount; ++i)
        {
            GetTransforms(targetTrans, parent.GetChild(i));
        }
    }
}
