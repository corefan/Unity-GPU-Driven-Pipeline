﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using System.Threading;

public unsafe struct NativeListData
{
    public int count;
    public int capacity;
    public Allocator allocator;
    public void* ptr;
}
public unsafe struct NativeList<T> : IEnumerable<T> where T : unmanaged
{
    [NativeDisableUnsafePtrRestriction]
    private NativeListData* data;
    public NativeList(int capacity, Allocator alloc)
    {
        capacity = Mathf.Max(capacity, 1);
        data = (NativeListData*)UnsafeUtility.Malloc(sizeof(NativeListData), 16, alloc);
        data->count = 0;
        data->capacity = capacity;
        data->allocator = alloc;
        data->ptr = UnsafeUtility.Malloc(sizeof(T) * capacity, 16, alloc);
    }

    public NativeList(int count, Allocator alloc, T defaultValue)
    {
        data = (NativeListData*)UnsafeUtility.Malloc(sizeof(NativeListData), 16, alloc);
        data->count = count;
        data->capacity = count;
        data->allocator = alloc;
        data->ptr = UnsafeUtility.Malloc(sizeof(T) * count, 16, alloc);
        T* add = (T*)data->ptr;
        for(int i = 0; i < count; ++i)
        {
            add[i] = defaultValue;
        }
    }

    public Allocator allocator
    {
        get
        {
            return data->allocator;
        }
    }
    private void Resize()
    {
        if (data->count <= data->capacity) return;
        int lastcapacity = data->capacity;
        data->capacity *= 2;
        void* newPtr = UnsafeUtility.Malloc(sizeof(T) * data->capacity, 16, data->allocator);
        UnsafeUtility.MemCpy(newPtr, data->ptr, sizeof(T) * lastcapacity);
        UnsafeUtility.Free(data->ptr, data->allocator);
        data->ptr = newPtr;
    }
    public int Length
    {
        get
        {
            return data->count;
        }
    }
    public int Capacity
    {
        get
        {
            return data->capacity;
        }
    }
    public T* unsafePtr
    {
        get
        {
            return (T*)data->ptr;
        }
    }
    public void Dispose()
    {
        Allocator alloc = data->allocator;
        UnsafeUtility.Free(data->ptr, alloc);
        UnsafeUtility.Free(data, alloc);
    }
    public ref T this[int id]
    {
        get
        {
            T* ptr = (T*)data->ptr;
            return ref *(ptr + id);
        }
    }
    public int ConcurrentAdd(T value)
    {
        int last = Interlocked.Increment(ref data->count);
        //Concurrent Resize
        if (last > data->capacity)
        {
            return -1;
        }
        last--;
        Resize();
        T* ptr = (T*)data->ptr;
        *(ptr + last) = value;
        return last;
    }
    public int ConcurrentAdd(ref T value)
    {
        int last = Interlocked.Increment(ref data->count);
        //Concurrent Resize
        if (last > data->capacity)
        {
            return -1;
        }
        last--;
        Resize();
        T* ptr = (T*)data->ptr;
        *(ptr + last) = value;
        return last;
    }
    public void Add(T value)
    {
        int last = data->count;
        data->count++;
        Resize();
        T* ptr = (T*)data->ptr;
        *(ptr + last) = value;
    }
    public void Add(ref T value)
    {
        int last = data->count;
        data->count++;
        Resize();
        T* ptr = (T*)data->ptr;
        *(ptr + last) = value;
    }
    public void Remove(int i)
    {
        ref int count = ref data->count;
        if (count == 0) return;
        count--;
        this[i] = this[count];
    }
    public void Clear()
    {
        data->count = 0;
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    public IEnumerator<T> GetEnumerator()
    {
        return new ListIenumerator<T>(data);
    }
}

public unsafe class ListIenumerator<T> : IEnumerator<T> where T : unmanaged
{
    [NativeDisableUnsafePtrRestriction]
    private NativeListData* data;
    private int iteIndex;
    public ListIenumerator(NativeListData* dataPtr)
    {
        data = dataPtr;
        iteIndex = -1;
    }
    object IEnumerator.Current
    {
        get
        {
            return ((T*)data->ptr)[iteIndex];
        }
    }

    public T Current
    {
        get
        {
            return ((T*)data->ptr)[iteIndex];
        }
    }

    public bool MoveNext()
    {
        return (++iteIndex < (data->count));
    }

    public void Reset()
    {
        iteIndex = -1;
    }

    public void Dispose()
    {
    }
}
