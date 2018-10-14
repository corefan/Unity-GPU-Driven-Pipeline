using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReflectionCube : MonoBehaviour
{
    public float intensity = 1;
    public float importance = 1;
    public Vector3 size = new Vector3(1, 1, 1);
    public bool useBoxProjection = false;
    private void OnDrawGizmosSelected()
    {   
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawCube(transform.position, new Vector3(1, 1, 1));
    }
}

public struct fk
{
    public Vector3 sb;
    public Vector3 ss;
}