using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
[ExecuteInEditMode]
public unsafe class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        Debug.Log("Awake");
    }
}

public struct TestJob : IJobParallelFor
{
    public static object obj;
    [NativeDisableParallelForRestriction]
    public NativeList<int> values;
    public void Execute(int index)
    {
        values.ConcurrentAdd(index, obj);
    }
}