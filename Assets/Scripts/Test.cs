using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Test : MonoBehaviour
{
    public RenderTexture rt;
    public Material mat;
    
    public void Start()
    {
        rt = new RenderTexture(256, 256, 16);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_Color", Color.green);
        CommandBuffer buffer = new CommandBuffer();
        buffer.SetRenderTarget(rt);
        buffer.DrawMesh(GraphicsUtility.mesh, Matrix4x4.identity, mat, 0, 0, block);
        block.SetColor("_Color", Color.red);
        Graphics.ExecuteCommandBuffer(buffer);
        buffer.Dispose();
    }
}
