using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MPipeline
{
    public class PipelineResources : ScriptableObject
    {
        public Shader taaShader;
        public Shader indirectDepthShader;
        public Shader HizLodShader;
        public ComputeShader occluderCulling;
        public ComputeShader gpuFrustumCulling;
        public Shader spotlightShader;
        public Shader motionVectorShader;
        public Shader shadowMaskShader;
    }
}