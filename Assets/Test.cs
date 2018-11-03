using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [EasyButtons.Button]
    void ss()
    {
        Plane p = new Plane(Vector3.zero, Vector3.right, Vector3.one);
        Debug.Log(p.normal);
    }
}
