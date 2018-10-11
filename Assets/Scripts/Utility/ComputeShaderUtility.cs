using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ComputeShaderUtility
{
    public static uint[] zero = new uint[] { 0 };
    public static void Dispatch(ComputeShader shader, int kernal, int count, float threadGroupCount)
    {
        int threadPerGroup = Mathf.CeilToInt(count / threadGroupCount);
        shader.SetInt(ShaderIDs._Count, count);
        shader.Dispatch(kernal, threadPerGroup, 1, 1);
    }
}