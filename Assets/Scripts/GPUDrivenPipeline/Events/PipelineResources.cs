using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MPipeline
{
    public class PipelineResources : ScriptableObject
    {
        public ComputeShader occluderCulling;
        public ComputeShader gpuFrustumCulling;
        public ComputeShader skinCulling;
        public ComputeShader temporalHizCompute;
        public Shader taaShader;
        public Shader indirectDepthShader;
        public Shader HizLodShader;
        public Shader spotlightShader;
        public Shader motionVectorShader;
        public Shader shadowMaskShader;
        public Shader reflectionShader;
        public Shader linearDepthShader;
        public Mesh occluderMesh;
    }
}