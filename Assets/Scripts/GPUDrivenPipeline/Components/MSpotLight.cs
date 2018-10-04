using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MPipeline.Light
{
    public class MSpotLight : MLight
    {
        [Range(1f, 175f)]
        public float angle = 50;
        public PerspCam shadCam;
        public void UpdateShadowCam()
        {
            shadCam.aspect = 1;
            shadCam.fov = angle;
            shadCam.farClipPlane = range;
            shadCam.nearClipPlane = 0.3f;
            shadCam.position = transform.position;
            shadCam.right = transform.right;
            shadCam.up = transform.up;
            shadCam.forward = transform.forward;
            shadCam.UpdateProjectionMatrix();
            shadCam.UpdateTRSMatrix();
        }
    }
}