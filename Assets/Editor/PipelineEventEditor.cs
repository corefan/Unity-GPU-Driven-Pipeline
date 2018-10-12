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
        PipelineEventAttribute ppAttribute = null;
        private void OnEnable()
        {
            targetObj = serializedObject.targetObject as PipelineEvent;
            object[] allAttris = targetObj.GetType().GetCustomAttributes(typeof(PipelineEventAttribute), true);
            foreach (var i in allAttris)
            {
                if (i.GetType() == typeof(PipelineEventAttribute))
                {
                    ppAttribute = i as PipelineEventAttribute;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Pipeline Settings:");
            if (ppAttribute != null)
            {
                if (ppAttribute.postRender)
                    targetObj.enabledInPipeline = EditorGUILayout.Toggle("Enable In Pipeline", targetObj.enabledInPipeline);
                if (ppAttribute.preRender)
                    targetObj.enableBeforePipeline = EditorGUILayout.Toggle("Enable Before Pipeline", targetObj.enableBeforePipeline);
            }
            base.OnInspectorGUI();
        }

        
    }
}