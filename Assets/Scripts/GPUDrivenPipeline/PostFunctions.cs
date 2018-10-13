using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
namespace MPipeline
{
    public delegate void PostProcessAction(ref PipelineCommandData data, RenderTexture source, RenderTexture dest);
    public struct PostSharedData
    {
        public Material uberMaterial;
        public Vector2Int screenSize;
        public PostProcessResources resources;
        public Texture autoExposureTexture;
        public RenderTexture source;
        public List<RenderTexture> temporalRT;
        public List<string> shaderKeywords;
        public bool keywordsTransformed;
    }
    public static class PostFunctions
    {
        public static void InitSharedData(ref PostSharedData data, PostProcessResources resources)
        {
            data = default(PostSharedData);
            data.uberMaterial = new Material(Shader.Find("Hidden/PostProcessing/Uber"));
            data.resources = resources;
            data.temporalRT = new List<RenderTexture>(20);
            data.shaderKeywords = new List<string>(10);
            data.keywordsTransformed = true;
        }

        public static void RunPostProcess(ref PipelineCommandData data, PostProcessAction renderFunc)
        {
            RenderTexture source = data.targets.renderTarget;
            RenderTexture dest = data.targets.backupTarget;
            renderFunc(ref data, source, dest);
            data.targets.renderTarget = dest;
            data.targets.backupTarget = source;
            data.targets.colorBuffer = dest.colorBuffer;
        }

        public static void BlitFullScreen(RenderTexture source, RenderTexture dest, Material mat, int pass)
        {
            mat.SetTexture(ShaderIDs._MainTex, source);
            mat.SetPass(pass);
            Graphics.DrawMeshNow(GraphicsUtility.mesh, Matrix4x4.identity);
        }
    }
}