using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Threading;
using System;
namespace MPipeline
{
    public unsafe struct SortElement
    {
        public int leftValue;
        public int rightValue;
        public float sign;
        public void* ptr;
    }
    public unsafe struct BinarySort<T> where T : unmanaged
    {
        private NativeArray<SortElement> elements;
        private NativeArray<ulong> results;
        public int count;
        public BinarySort(int capacity, Allocator alloc)
        {
            elements = new NativeArray<SortElement>(capacity, alloc, NativeArrayOptions.UninitializedMemory);
            results = new NativeArray<ulong>(capacity, alloc, NativeArrayOptions.UninitializedMemory);
            count = 0;
        }

        public void Add(float sign, T* value)
        {
            if (count > elements.Length) return;
            int last = Interlocked.Increment(ref count) - 1;
            SortElement curt;
            curt.sign = sign;
            curt.ptr = value;
            curt.leftValue = -1;
            curt.rightValue = -1;
            elements[last] = curt;
        }

        public T** SortedResult
        {
            get
            {
                return (T**)results.GetUnsafePtr();
            }
        }

        public void Clear()
        {
            count = 0;
        }

        public void Dispose()
        {
            elements.Dispose();
            results.Dispose();
        }

        public void Sort()
        {
            if (elements.Length == 0) return;
            for (int i = 1; i < elements.Length; ++i)
            {
                int currentIndex = 0;
                STARTFIND:
                SortElement* currentIndexValue = (SortElement*)elements.GetUnsafePtr() + currentIndex;
                if (((SortElement*)elements.GetUnsafePtr() + i)->sign < currentIndexValue->sign)
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
            int start = 0;
            Iterate(0, ref start);
        }

        private void Iterate(int i, ref int targetLength)
        {
            int leftValue = elements[i].leftValue;
            if (leftValue >= 0)
            {
                Iterate(leftValue, ref targetLength);
            }
            results[targetLength] = (ulong)(((SortElement*)elements.GetUnsafePtr() + i)->ptr);
            targetLength++;
            int rightValue = elements[i].rightValue;
            if (rightValue >= 0)
            {
                Iterate(rightValue, ref targetLength);
            }
        }
    }
}