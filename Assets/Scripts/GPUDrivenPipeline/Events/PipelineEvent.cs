using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
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
            SetEnable(value);
        }
    }
    private void SetEnable(bool value)
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
    protected virtual void Awake()
    {
        if (m_enabledInPipeline)
        {
            RenderPipeline.drawEvents.InsertTo(this, compareFunc);
        }
    }
    protected virtual void OnDestroy()
    {
        RenderPipeline.drawEvents.Remove(this);
    }
    public abstract void FrameUpdate(ref PipelineCommandData data);
}
