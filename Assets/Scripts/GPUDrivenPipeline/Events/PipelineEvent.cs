using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace MPipeline
{
    public abstract class PipelineEvent : MonoBehaviour
    {
        public static Func<PipelineEvent, PipelineEvent, int> compareFunc = (x, y) =>
        {
            if (x.layer > y.layer) return -1;
            if (x.layer < y.layer) return 1;
            return 0;
        };
        [HideInInspector]
        public bool m_enabledInPipeline = false;
        public float layer = 2000;
        private bool pre = false;
        private bool post = false;
        //Will Cause GC stress
        //So only run in editor or Awake
        private void SetAttribute()
        {
            pre = false;
            post = false;
            object[] allAttribute = GetType().GetCustomAttributes(typeof(PipelineEventAttribute), false);
            foreach (var i in allAttribute)
            {
                if (i.GetType() == typeof(PipelineEventAttribute))
                {
                    PipelineEventAttribute att = i as PipelineEventAttribute;
                    pre = att.preRender;
                    post = att.postRender;
                    break;
                }
            }
        }
        private void SetEnable(bool value)
        {
            if (value)
            {
                if (post) RenderPipeline.drawEvents.InsertTo(this, compareFunc);
                if (pre) RenderPipeline.preRenderEvents.InsertTo(this, compareFunc);
            }
            else
            {
#if UNITY_EDITOR
                RenderPipeline.drawEvents.Remove(this);
                RenderPipeline.preRenderEvents.Remove(this);
#else
                if (post) RenderPipeline.drawEvents.Remove(this);
                if (pre) RenderPipeline.preRenderEvents.Remove(this);
#endif
            }
        }
        public bool enabledInPipeline
        {
            get
            {
                return m_enabledInPipeline;
            }
            set
            {
                if (m_enabledInPipeline == value) return;
                m_enabledInPipeline = value;
#if UNITY_EDITOR
                SetAttribute();
#endif
                SetEnable(value);
            }
        }

        public void InitEvent(PipelineResources resources)
        {
            SetAttribute();
            if (m_enabledInPipeline)
            {
                SetEnable(true);
            }
            Init(resources);
        }
        public void DisposeEvent()
        {
            if (m_enabledInPipeline)
            {
                SetEnable(false);
            }
            Dispose();
        }
        protected abstract void Init(PipelineResources resources);
        protected abstract void Dispose();
        public abstract void FrameUpdate(ref PipelineCommandData data);
        public abstract void PreRenderFrame(Camera cam);
    }
}