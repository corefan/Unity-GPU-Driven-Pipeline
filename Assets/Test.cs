using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using MPipeline;
public unsafe class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        NativeList<int> values = new NativeList<int>(1, Allocator.Temp);
        TestJob tst;
        tst.values = values;
        TestJob.obj = this;
        tst.Schedule(10000, 1).Complete();
        foreach(var i in values)
        {
            Debug.Log(i);
        }
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