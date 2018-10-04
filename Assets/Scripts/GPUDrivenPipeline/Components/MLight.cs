using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MPipeline.Light
{
    public class MLight : MonoBehaviour
    {
        public Color lightColor = Color.white;
        public float range = 5;
        public float intensity = 1;
        public enum ShadowType
        {
            None, Shadow
        }
        public ShadowType shadowType = ShadowType.None;
    }
}