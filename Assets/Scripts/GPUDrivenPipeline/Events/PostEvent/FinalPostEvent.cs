using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using PostProcessing = UnityEngine.Rendering.PostProcessing;
namespace MPipeline
{
    [PipelineEvent(false, true)]
    public class FinalPostEvent : PipelineEvent
    {
        public PostProcessProfile profile;
        public PostProcessResources resources;
        public Action finalizerAction;
        public Action renderAction;
        public PostSharedData sharedData;
        private ColorGradingData colorGradingData;
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
            foreach(var i in settingList)
            {
                allSettings.Add(i.GetType(), i);
            }
            PostProcessEffectSettings currentSetting;
            if(allSettings.TryGetValue(typeof(ColorGrading), out currentSetting))
            {
                ColorGradingFunction.InitializeColorGrading(currentSetting as ColorGrading, ref colorGradingData, resources.computeShaders.lut3DBaker);
                finalizerAction += () => ColorGradingFunction.Finalize(ref colorGradingData, sharedData.uberMaterial);
                renderAction += () => ColorGradingFunction.PrepareRender(ref colorGradingData, ref sharedData);
            }
            
        }
   
        protected override void Dispose()
        {
            Destroy(sharedData.uberMaterial);
            finalizerAction();
            finalizerAction = null;
        }

        public override void FrameUpdate(ref PipelineCommandData data)
        {
            sharedData.source = data.targets.renderTarget;
            sharedData.screenSize = new Vector2Int(data.cam.pixelWidth, data.cam.pixelHeight);
            renderAction();
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