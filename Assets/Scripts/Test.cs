using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;

public unsafe class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Fk f = new Fk(true);
        f.Schedule().Complete();
        f.ar.Dispose();
    }
}

public unsafe struct Fk : IJob
{
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<int> ar;
    public Fk(bool a)
    {
        ar = new NativeArray<int>();
        Debug.Log(ar.IsCreated);
    }
    public void Execute()
    {
        ar = new NativeArray<int>(10, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        foreach(var i in ar)
        {
            Debug.Log(i);
        }
    }
}