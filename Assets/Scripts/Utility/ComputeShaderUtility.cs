using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class ComputeShaderUtility
{
    public static uint[] zero = new uint[] { 0 };
    public static void Dispatch(ComputeShader shader, CommandBuffer buffer, int kernal, int count, float threadGroupCount)
    {
        int threadPerGroup = Mathf.CeilToInt(count / threadGroupCount);
        buffer.SetComputeIntParam(shader, ShaderIDs._Count, count);
        buffer.DispatchCompute(shader, kernal, threadPerGroup, 1, 1);
    }
}