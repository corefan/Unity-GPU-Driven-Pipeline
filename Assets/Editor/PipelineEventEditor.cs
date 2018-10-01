using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(PipelineEvent), true)]
public class PipelineEventEditor : Editor
{
    
    public override void OnInspectorGUI()
    {
        PipelineEvent target = serializedObject.targetObject as PipelineEvent;
        EditorGUILayout.LabelField("Pipeline Settings:");
        target.enabledInPipeline = EditorGUILayout.Toggle("Enable In Pipeline", target.enabledInPipeline);
        base.OnInspectorGUI();
    }
}
