using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
[RequireComponent(typeof(SkinnedMeshRenderer))]
public unsafe class SkinComponent : MonoBehaviour
{
    private int index;
    private SkinnedMeshRenderer skinMeshRender;
    private void Awake()
    {
        skinMeshRender = GetComponent<SkinnedMeshRenderer>();
    }

    private void OnDestroy()
    {
    }
}
