using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace MPipeline
{
    [CustomEditor(typeof(PipelineEvent), true)]
    public class PipelineEventEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            PipelineEvent target = serializedObject.targetObject as PipelineEvent;
            EditorGUILayout.LabelField("Pipeline Settings:");
            target.enabledInPipeline = EditorGUILayout.Toggle("Enable In Pipeline", target.enabledInPipeline);
            target.enableBeforePipeline = EditorGUILayout.Toggle("Enable Before Pipeline", target.enableBeforePipeline);
            base.OnInspectorGUI();
        }
    }
}