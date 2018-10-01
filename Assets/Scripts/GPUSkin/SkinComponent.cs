using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
[RequireComponent(typeof(SkinnedMeshRenderer))]
public unsafe class SkinComponent : MonoBehaviour
{
    public SkinRenderer skr;
    private int index;
    private void Awake()
    {
        skr = new SkinRenderer();
        SkinnedMeshRenderer meshR = GetComponent<SkinnedMeshRenderer>();
        Transform[] bones = meshR.bones;
        skr.bones = new NativeArray<Matrix4x4>(bones.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        skr.mesh = meshR.sharedMesh;
        skr.bonesTrans = bones;
        skr.meshIndex = 0;
        skr.boneBuffers = new ComputeBuffer(bones.Length, SkinFunction.MATRIXSIZE);
        SkinManager.InitSkinRenderer(ref skr);
        ulong size = (ulong)(bones.Length * SkinFunction.MATRIXSIZE);
        ulong bonesPtr = (ulong)skr.bones.GetUnsafePtr();
        ulong indCount = 0;
        for(int i = 0; i < bones.Length; i++)
        {
            *(Matrix4x4*)(bonesPtr + indCount) = bones[i].localToWorldMatrix;
            indCount += SkinFunction.MATRIXSIZE;
        }
        index = SkinManager.AddComponent(ref skr);
    }

    void Update()
    {
        SkinFunction.UpdateBonePos(ref skr);
    }

    private void OnDestroy()
    {
        skr.boneBuffers.Dispose();
        skr.bones.Dispose();
        SkinManager.RemoveComponent(index);
    }
}
