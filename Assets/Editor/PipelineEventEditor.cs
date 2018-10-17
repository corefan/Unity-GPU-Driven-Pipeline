using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace MPipeline
{
    [CustomEditor(typeof(PipelineEvent), true)]
    public class PipelineEventEditor : Editor
    {
        PipelineEvent targetObj;
        private void OnEnable()
        {
            targetObj = serializedObject.targetObject as PipelineEvent;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Pipeline Settings:");
            targetObj.enabledInPipeline = EditorGUILayout.Toggle("Enable In Pipeline", targetObj.enabledInPipeline);
            base.OnInspectorGUI();
        }
    }
}