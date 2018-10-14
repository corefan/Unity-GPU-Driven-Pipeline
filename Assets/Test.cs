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
        NativeList<int> values = new NativeList<int>(999, Allocator.Temp);
        TestJob tst;
        tst.values = values;
        tst.Schedule(999, 1).Complete();
        BinarySort<int> sorter = new BinarySort<int>(999, Allocator.Temp);
        for(int i = 0; i < 999; ++i)
        {
            int* currentValue = values.unsafePtr + i;
            sorter.Add(*currentValue, currentValue);
        }
        sorter.Sort();
        int** sortedValue = sorter.SortedResult;
        values.Dispose();
        sorter.Dispose();
    }
}

public struct TestJob : IJobParallelFor
{
    public NativeList<int> values;
    public void Execute(int index)
    {
        values.ConcurrentAdd(index);
    }
}