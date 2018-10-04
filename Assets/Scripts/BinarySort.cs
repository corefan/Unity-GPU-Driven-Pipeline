using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Threading;
using System;
namespace MPipeline
{
    public struct DrawingPolicy
    {
        public uint rendererID;
        public Vector3 extent;
        public Matrix4x4 localToWorldMatrix;
    }

    public unsafe struct BinarySort
    {
        public struct Element
        {
            public float sign;
            public DrawingPolicy policy;
            public int leftValue;
            public int rightValue;
        }
        public NativeArray<Element> elements;
        public NativeArray<DrawingPolicy> results;
        public int count;
        public BinarySort(int capacity)
        {
            count = 0;
            capacity = Mathf.Max(10, capacity);
            elements = new NativeArray<Element>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            results = new NativeArray<DrawingPolicy>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }
        /// <summary>
        /// Add a value into binary tree
        /// </summary>
        /// <param name="sign"></param> Sort Sign
        /// <param name="value"></param> value
        /// <param name="mutex"></param> Thread safe mutex
        public void Add(float sign, ref DrawingPolicy value, Mutex mutex)
        {
            int currentCount = Interlocked.Increment(ref count) - 1;
            if (currentCount >= elements.Length)
            {
                mutex.WaitOne();
                NativeArray<Element> newElements = new NativeArray<Element>(elements.Length * 2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                long size = UnsafeUtility.SizeOf<Element>() * elements.Length;
                void* dest = newElements.GetUnsafePtr();
                void* source = elements.GetUnsafePtr();
                UnsafeUtility.MemCpy(dest, source, size);
                elements.Dispose();
                elements = newElements;
                results.Dispose();
                results = new NativeArray<DrawingPolicy>(elements.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                mutex.ReleaseMutex();
            }
            Element newElement;
            newElement.sign = sign;
            newElement.policy = value;
            newElement.leftValue = -1;
            newElement.rightValue = -1;
            *((Element*)elements.GetUnsafePtr() + currentCount) = newElement;
        }
        public void Clear()
        {
            count = 0;
        }
        public void Sort()
        {
            for (int i = 1; i < count; ++i)
            {
                int currentIndex = 0;
                STARTFIND:
                Element* currentIndexValue = ((Element*)elements.GetUnsafePtr() + currentIndex);
                if (((Element*)elements.GetUnsafePtr() + i)->sign < currentIndexValue->sign)
                {
                    if (currentIndexValue->leftValue < 0)
                    {
                        currentIndexValue->leftValue = i;
                    }
                    else
                    {
                        currentIndex = currentIndexValue->leftValue;
                        goto STARTFIND;
                    }
                }
                else
                {
                    if (currentIndexValue->rightValue < 0)
                    {
                        currentIndexValue->rightValue = i;
                    }
                    else
                    {
                        currentIndex = currentIndexValue->rightValue;
                        goto STARTFIND;
                    }
                }
            }
            if (count <= 0) return;
            int start = 0;
            Iterate(0, ref start);
        }
        public void Dispose()
        {
            elements.Dispose();
            results.Dispose();
        }
        private void Iterate(int i, ref int targetLength)
        {
            int leftValue = elements[i].leftValue;
            if (leftValue >= 0)
            {
                Iterate(leftValue, ref targetLength);
            }
            results[targetLength] = ((Element*)elements.GetUnsafePtr() + (ulong)i)->policy;
            targetLength++;
            int rightValue = elements[i].rightValue;
            if (rightValue >= 0)
            {
                Iterate(rightValue, ref targetLength);
            }
        }
    }
}