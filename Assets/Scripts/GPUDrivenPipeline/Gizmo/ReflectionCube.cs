using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MPipeline
{
    public class ReflectionCube : MonoBehaviour
    {
        public float intensity = 1;
        public float importance = 1;
        public bool useBoxProjection = false;
        public static List<ReflectionCube> allCubes = new List<ReflectionCube>(100);
        private int currentIndex;
        private void Awake()
        {
            currentIndex = allCubes.Count;
            allCubes.Add(this);
        }

        private void OnDestroy()
        {
            if(allCubes.Count <= 1)
            {
                allCubes.Clear();
                return;
            }
            allCubes[currentIndex] = allCubes[allCubes.Count - 1];
            allCubes[currentIndex].currentIndex = currentIndex;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawMesh(GraphicsUtility.cubeMesh, transform.position, transform.rotation, transform.localScale);
        }
    }
}