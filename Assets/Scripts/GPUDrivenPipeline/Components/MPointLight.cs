using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public unsafe class MPointLight : MonoBehaviour
{
    public static List<MPointLight> allPointLights = new List<MPointLight>();
    public bool useShadow = true;
    public float range = 5;
    public Color color = Color.white;
    public float intensity = 1;
    private int index;
    [System.NonSerialized]
    public Vector3 position;
    public RenderTexture shadowmapTexture
    {
        get
        {
            if(m_shadowmapTexture == null)
            {
                RenderTextureDescriptor des = new RenderTextureDescriptor();
                des.autoGenerateMips = false;
                des.bindMS = false;
                des.colorFormat = RenderTextureFormat.RHalf;
                des.depthBufferBits = 16;
                des.dimension = UnityEngine.Rendering.TextureDimension.Cube;
                des.enableRandomWrite = false;
                des.height = 1024;
                des.memoryless = RenderTextureMemoryless.None;
                des.msaaSamples = 1;
                des.shadowSamplingMode = UnityEngine.Rendering.ShadowSamplingMode.None;
                des.sRGB = false;
                des.useMipMap = false;
                des.width = 1024;
                des.vrUsage = VRTextureUsage.None;
                des.volumeDepth = 0;
                m_shadowmapTexture = new RenderTexture(des);
            }
            return m_shadowmapTexture;
        }
    }
    private RenderTexture m_shadowmapTexture;

    private void OnDestroy()
    {
        if(m_shadowmapTexture != null)
        {
            m_shadowmapTexture.Release();
            Destroy(m_shadowmapTexture);
        }
    }

    private void OnEnable()
    {
        position = transform.position;
        index = allPointLights.Count;
        allPointLights.Add(this);
    }

    private void OnDisable()
    {
        if (allPointLights.Count <= 1)
        {
            allPointLights.Clear();
            return;
        }
        int last = allPointLights.Count - 1;
        allPointLights[index] = allPointLights[last];
        allPointLights[index].index = index;
        allPointLights.RemoveAt(last);
    }
}
