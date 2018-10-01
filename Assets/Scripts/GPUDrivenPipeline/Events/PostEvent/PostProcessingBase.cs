using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PostProcessingBase : MonoBehaviour
{
    public abstract void Render(ref PipelineCommandData data, RenderTexture source, RenderTexture dest);
}
