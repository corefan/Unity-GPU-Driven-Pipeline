using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinManager : MonoBehaviour
{
    #region STATIC
    public static SkinRenderer[] allComponents = new SkinRenderer[100];
    public static int rendererCount = 0;
    public static SkinManager current;
    public static int AddComponent(ref SkinRenderer skr)
    {
        if(allComponents.Length <= rendererCount)
        {
            SkinRenderer[] newComponents = new SkinRenderer[allComponents.Length * 2];
            for(int i = 0; i < allComponents.Length; ++i)
            {
                newComponents[i] = allComponents[i];
            }
            allComponents = newComponents;
        }
        allComponents[rendererCount] = skr;
        return rendererCount++;
    }
    public static void RemoveComponent(int index)
    {
        if (rendererCount > 1)
        {
            int lastOne = rendererCount - 1;
            allComponents[index] = allComponents[lastOne];
            rendererCount--;
        }
        else
        {
            rendererCount = 0;
        }
    }

    public static void InitSkinRenderer(ref SkinRenderer rend)
    {
        if (!current) return;
        SkinFunction.InitSkinRendererIndex(ref rend, ref current.dataCollect, ref current.splitData);
    }
    #endregion
    public MeshSplitData splitData;
    public SkinDataCollect dataCollect;
    private ComputeShader skinShader;
    public int maximumVertexInScreen = 1000000;
    private void Awake()
    {
        if(current)
        {
            Destroy(this);
            Debug.LogError("Skin Manager should be Singleton!");
            return;
        }
        skinShader = Resources.Load<ComputeShader>("ComputeSkin");
        splitData.normal = new List<Vector3>(10000);
        splitData.tangent = new List<Vector4>(15000);
        splitData.triangle = new List<int>(10000);
        splitData.uv = new List<Vector2>(10000);
        splitData.vertices = new List<Vector3>(10000);
        splitData.weight = new List<BoneWeight>(10000);
        dataCollect.allVerticesBuffer = new ComputeBuffer(maximumVertexInScreen, Point.SIZE);
        dataCollect.meshLength = 0;
        dataCollect.meshToSkin = new Dictionary<Mesh, int>(101);
        dataCollect.skinMeshes = new SkinMesh[101];
        current = this;
        for(int i = 0; i < rendererCount; ++i)
        {
            SkinFunction.InitSkinRendererIndex(ref allComponents[i], ref dataCollect, ref splitData);
        }
    }

    private void LateUpdate()
    {
        //SkinFunction.GpuSkin(skinShader, dataCollect.allVerticesBuffer, allComponents, dataCollect.skinMeshes);
    }

    private void OnDestroy()
    {
        if (current != this) return;
        current = null;
        dataCollect.allVerticesBuffer.Dispose();
        Resources.UnloadAsset(skinShader);
        for(int i = 0; i < dataCollect.meshLength; ++i)
        {
            ref SkinMesh skr = ref dataCollect.skinMeshes[i];
            skr.verticesBuffer.Dispose();
            skr.weightsBuffer.Dispose();
        }
    }
}
