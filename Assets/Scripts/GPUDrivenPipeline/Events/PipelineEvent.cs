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
        [HideInInspector]
        public bool m_enableBeforePipeline = false;
        public float layer = 2000;
        private bool pre = false;
        private bool post = false;
        //Will Cause GC stress
        //So only run in editor or Awake
        public void SetAttribute()
        {
            object[] allAttribute = GetType().GetCustomAttributes(typeof(PipelineEventAttribute), false);
            foreach (var i in allAttribute)
            {
                if (i.GetType() == typeof(PipelineEventAttribute))
                {
                    PipelineEventAttribute att = i as PipelineEventAttribute;
                    pre = att.preRender;
                    post = att.postRender;
                    return;
                }
            }
            pre = false;
            post = false;
            
        }

        public bool EnableEvent
        {
            get
            {
                return m_enabledInPipeline || m_enableBeforePipeline;
            }
            set
            {
                enabledInPipeline = post && value;
                enableBeforePipeline = pre && value;
            }
        }

        private bool enabledInPipeline
        {
            get
            {
                return m_enabledInPipeline;
            }
            set
            {
                if (m_enabledInPipeline == value) return;
                m_enabledInPipeline = value;
                SetIn(value);
            }
        }

        private bool enableBeforePipeline
        {
            get
            {
                return m_enableBeforePipeline;
            }
            set
            {
                if (m_enableBeforePipeline == value) return;
                m_enableBeforePipeline = value;
                SetBefore(value);
            }
        }

        private void SetIn(bool value)
        {
            if (value)
            {
                RenderPipeline.drawEvents.InsertTo(this, compareFunc);
            }
            else
            {
                RenderPipeline.drawEvents.Remove(this);
            }
        }

        private void SetBefore(bool value)
        {
            if (value)
            {
                RenderPipeline.preRenderEvents.InsertTo(this, compareFunc);
            }
            else
            {
                RenderPipeline.preRenderEvents.Remove(this);
            }
        }

        public void InitEvent(PipelineResources resources)
        {
            SetAttribute();
            if (m_enabledInPipeline)
            {
                SetIn(true);
            }
            if(m_enableBeforePipeline)
            {
                SetBefore(true);
            }
            Init(resources);
        }
        public void DisposeEvent()
        {
            if (m_enabledInPipeline)
            {
                SetIn(false);
            }
            if (m_enableBeforePipeline)
            {
                SetBefore(false);
            }
            Dispose();
        }
        protected virtual void Init(PipelineResources resources) { }
        protected virtual void Dispose() { }
        public virtual void FrameUpdate(PipelineCamera cam, ref PipelineCommandData data) { }
        public virtual void PreRenderFrame(PipelineCamera cam, ref PipelineCommandData data) { }
    }
}