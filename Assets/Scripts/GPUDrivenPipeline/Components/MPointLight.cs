using System.Collections.Generic;
using UnityEngine;

public class MPointLight : MonoBehaviour
{
    public static List<MPointLight> allPointLights = new List<MPointLight>();
    public float range = 5;
    public Color color = Color.white;
    public float intensity = 1;
    private int index;
    [System.NonSerialized]
    public Vector3 position;
    private void OnEnable()
    {
        position = transform.position;
        index = allPointLights.Count;
        allPointLights.Add(this);
    }

    private void Update()
    {
#if UNITY_EDITOR
        position = transform.position;
        
#endif
    }

    private void OnDisable()
    {
        if (allPointLights.Count <= 1)
        {
            allPointLights.Clear();
            return;
        }
        int last = allPointLights.Count - 1;
        allPointLights[index] = allPointLights[last];
        allPointLights[index].index = index;
        allPointLights.RemoveAt(last);
    }
}
