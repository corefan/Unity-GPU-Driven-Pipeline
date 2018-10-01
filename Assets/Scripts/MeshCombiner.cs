using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode]
public class MeshCombiner : MonoBehaviour
{
    public Material mat;
    void OnDisable()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);
            i++;
        }
        transform.GetComponent<MeshFilter>().sharedMesh = new Mesh();
        transform.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine);
        transform.gameObject.SetActive(true);
        GetComponent<MeshRenderer>().sharedMaterial = mat;
    }
}