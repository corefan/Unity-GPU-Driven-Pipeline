using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace MPipeline
{
    [CustomEditor(typeof(PipelineEvent), true)]
    public class PipelineEventEditor : Editor
    {
        PipelineEvent target;
        PipelineEventAttribute ppAttribute = null;
        private void OnEnable()
        {
            target = serializedObject.targetObject as PipelineEvent;
            object[] allAttris = target.GetType().GetCustomAttributes(typeof(PipelineEventAttribute), true);
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
                    target.enabledInPipeline = EditorGUILayout.Toggle("Enable In Pipeline", target.enabledInPipeline);
                if (ppAttribute.preRender)
                    target.enableBeforePipeline = EditorGUILayout.Toggle("Enable Before Pipeline", target.enableBeforePipeline);
            }
            base.OnInspectorGUI();
        }

        
    }
}