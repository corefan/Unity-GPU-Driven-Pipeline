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
                SetIn(value);
            }
        }
        public bool enableBeforePipeline
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
        public void InitEvent(PipelineResources resources)
        { 
            if (m_enabledInPipeline)
            {
                RenderPipeline.drawEvents.InsertTo(this, compareFunc);
            }
            if (m_enableBeforePipeline)
            {
                RenderPipeline.preRenderEvents.InsertTo(this, compareFunc);
            }
            Init(resources);
        }
        public void DisposeEvent()
        {
            if (m_enabledInPipeline)
                RenderPipeline.drawEvents.Remove(this);
            if (m_enableBeforePipeline)
                RenderPipeline.preRenderEvents.Remove(this);
            Dispose();
        }
        protected abstract void Init(PipelineResources resources);
        protected abstract void Dispose();
        public abstract void FrameUpdate(ref PipelineCommandData data);
        public abstract void PreRenderFrame(Camera cam);
    }
}