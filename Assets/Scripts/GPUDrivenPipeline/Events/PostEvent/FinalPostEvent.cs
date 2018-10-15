using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using PostProcessing = UnityEngine.Rendering.PostProcessing;
using Functional;
namespace MPipeline
{
    [PipelineEvent(false, true)]
    public class FinalPostEvent : PipelineEvent
    {
        public PostProcessProfile profile;
        public PostProcessResources resources;
        public Action finalizerAction;
        public Function<PipelineCommandData> renderAction;
        public PostSharedData sharedData;
        private ColorGradingData colorGradingData;
        private MotionBlurData motionBlurData;
        private Dictionary<Type, PostProcessEffectSettings> allSettings = new Dictionary<Type, PostProcessEffectSettings>(7);
        private PostProcessAction uberAction;
        protected override void Init(PipelineResources res)
        {
            finalizerAction = null;
            renderAction = null;
            PostFunctions.InitSharedData(ref sharedData, resources);
            uberAction = (ref PipelineCommandData data, RenderTexture source, RenderTexture dest) =>
            {
                Graphics.SetRenderTarget(dest);
                sharedData.uberMaterial.SetTexture(ShaderIDs._MainTex, source);
                sharedData.uberMaterial.SetPass(0);
                Graphics.DrawMeshNow(GraphicsUtility.mesh, Matrix4x4.identity);
            };
            var settingList = profile.settings;
            foreach (var i in settingList)
            {
                allSettings.Add(i.GetType(), i);
            }
            PostProcessEffectSettings currentSetting;
            if (allSettings.TryGetValue(typeof(ColorGrading), out currentSetting))
            {
                ColorGradingFunction.InitializeColorGrading(currentSetting as ColorGrading, ref colorGradingData, resources.computeShaders.lut3DBaker);
                finalizerAction += () => ColorGradingFunction.Finalize(ref colorGradingData, sharedData.uberMaterial);
                renderAction += (ref PipelineCommandData useless) => ColorGradingFunction.PrepareRender(ref colorGradingData, ref sharedData);
            }
            /*
            if (allSettings.TryGetValue(typeof(MotionBlur), out currentSetting))
            {
                MotionBlur settings = currentSetting as MotionBlur;
                motionBlurData.motionBlurMat = new Material(resources.shaders.motionBlur);
                motionBlurData.resetHistory = true;
                motionBlurData.settings = settings;
                finalizerAction += () => Destroy(motionBlurData.motionBlurMat);
                PostProcessAction motionBlurAction = (ref PipelineCommandData data, RenderTexture source, RenderTexture dest) => MotionBlurFunction.Render(ref sharedData, ref motionBlurData, source, dest);
                renderAction += (ref PipelineCommandData commandData) => PostFunctions.RunPostProcess(ref commandData, motionBlurAction);
            }*/
        }

        protected override void Dispose()
        {
            Destroy(sharedData.uberMaterial);
            finalizerAction();
            allSettings.Clear();
        }

        public override void FrameUpdate(ref PipelineCommandData data)
        {
            sharedData.autoExposureTexture = RuntimeUtilities.whiteTexture;
            sharedData.screenSize = new Vector2Int(data.cam.pixelWidth, data.cam.pixelHeight);
            sharedData.uberMaterial.SetTexture(PostProcessing.ShaderIDs.AutoExposureTex, sharedData.autoExposureTexture);
            renderAction(ref data);
            if (sharedData.keywordsTransformed)
            {
                sharedData.keywordsTransformed = false;
                sharedData.uberMaterial.shaderKeywords = sharedData.shaderKeywords.ToArray();
            }
            PostFunctions.RunPostProcess(ref data, uberAction);
            PipelineFunctions.ReleaseRenderTarget(sharedData.temporalRT);
        }
        public override void PreRenderFrame(Camera cam)
        {
            throw new System.NotImplementedException();
        }
    }
}