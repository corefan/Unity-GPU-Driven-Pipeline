using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
namespace MPipeline
{
    public class PostProcessEvent : PipelineEvent
    {
        [Tooltip("The diameter (in texels) inside which jitter samples are spread. Smaller values result in crisper but more aliased output, while larger values result in more stable but blurrier output.")]
        [Range(0.1f, 1f)]
        public float jitterSpread = 0.75f;

        [Tooltip("Controls the amount of sharpening applied to the color buffer. High values may introduce dark-border artifacts.")]
        [Range(0f, 3f)]
        public float sharpness = 0.25f;

        [Tooltip("The blend coefficient for a stationary fragment. Controls the percentage of history sample blended into the final color.")]
        [Range(0f, 0.99f)]
        public float stationaryBlending = 0.95f;

        [Tooltip("The blend coefficient for a fragment with significant motion. Controls the percentage of history sample blended into the final color.")]
        [Range(0f, 0.99f)]
        public float motionBlending = 0.85f;
        private Vector2 jitter;
        private int sampleIndex = 0;
        const int k_SampleCount = 8;
        public List<PostProcessingBase> processingBase;
        private Material taaMat;
        private RenderTexture historyTex;
        protected override void Awake()
        {
            base.Awake();
            taaMat = new Material(Shader.Find("Hidden/PostProcessing/TemporalAntialiasing"));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Destroy(taaMat);
        }

        public override void FrameUpdate(ref PipelineCommandData data)
        {
            SetHistory(data.cam, data.targets.renderTarget);
            //TAA Start
            const float kMotionAmplification = 100f * 60f;
            taaMat.SetVector(ShaderIDs._Jitter, jitter);
            taaMat.SetFloat(ShaderIDs._Sharpness, sharpness);
            taaMat.SetVector(ShaderIDs._FinalBlendParameters, new Vector4(stationaryBlending, motionBlending, kMotionAmplification, 0f));
            taaMat.SetTexture(ShaderIDs._HistoryTex, historyTex);
            taaMat.SetTexture(ShaderIDs._CameraMotionVectorsTexture, data.targets.motionVectorTexture);
            RenderTexture source = data.targets.renderTarget;
            RenderTexture dest = data.targets.backupTarget;
            taaMat.Blit(source, dest, 0);
            Graphics.Blit(dest, historyTex);
            data.targets.renderTarget = dest;
            data.targets.backupTarget = source;
            data.targets.colorBuffer = dest.colorBuffer;
            //Other Post
            foreach (var i in processingBase)
            {
                source = data.targets.renderTarget;
                dest = data.targets.backupTarget;
                i.Render(ref data, source, dest);
                data.targets.renderTarget = dest;
                data.targets.backupTarget = source;
                data.targets.colorBuffer = dest.colorBuffer;
            }
        }

        public override void PreRenderFrame(Camera cam)
        {
            cam.ResetProjectionMatrix();
            ConfigureJitteredProjectionMatrix(cam);
        }

        Vector2 GenerateRandomOffset()
        {
            var offset = new Vector2(
                    HaltonSeq.Get((sampleIndex & 1023) + 1, 2) - 0.5f,
                    HaltonSeq.Get((sampleIndex & 1023) + 1, 3) - 0.5f
                );

            if (++sampleIndex >= k_SampleCount)
                sampleIndex = 0;

            return offset;
        }

        public Matrix4x4 GetJitteredProjectionMatrix(Camera camera)
        {
            Matrix4x4 cameraProj;
            jitter = GenerateRandomOffset();
            jitter *= jitterSpread;
            cameraProj = camera.orthographic
                ? RuntimeUtilities.GetJitteredOrthographicProjectionMatrix(camera, jitter)
                : RuntimeUtilities.GetJitteredPerspectiveProjectionMatrix(camera, jitter);
            jitter = new Vector2(jitter.x / camera.pixelWidth, jitter.y / camera.pixelHeight);
            return cameraProj;
        }

        public void ConfigureJitteredProjectionMatrix(Camera camera)
        {
            camera.nonJitteredProjectionMatrix = camera.projectionMatrix;
            camera.projectionMatrix = GetJitteredProjectionMatrix(camera);
            camera.useJitteredProjectionMatrixForTransparentRendering = false;
        }

        public void SetHistory(Camera cam, RenderTexture renderTarget)
        {
            if (historyTex == null)
            {
                historyTex = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                historyTex.filterMode = FilterMode.Bilinear;
                Graphics.Blit(renderTarget, historyTex);
            }
            else if (historyTex.width != cam.pixelWidth || historyTex.height != cam.pixelHeight)
            {
                historyTex.Release();
                Destroy(historyTex);
                historyTex = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                Graphics.Blit(renderTarget, historyTex);
            }
        }
    }
}