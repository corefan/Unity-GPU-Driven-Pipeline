using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [EasyButtons.Button]
    void Try()
    {
        Debug.Log(SystemInfo.supportsAsyncCompute);
    }
}
