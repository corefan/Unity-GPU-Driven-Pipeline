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
        private Material uberMaterial;
        private ColorGradingData colorGradingData;
        private Dictionary<Type, PostProcessEffectSettings> allSettings = new Dictionary<Type, PostProcessEffectSettings>(7);
        private PostProcessAction uberAction;
        protected override void Awake()
        {
            base.Awake();
            finalizerAction = null;
            renderAction = null;
            uberMaterial = new Material(Shader.Find("Hidden/PostProcessing/Uber"));
            uberAction = RenderToScreen;
            var settingList = profile.settings;
            foreach(var i in settingList)
            {
                allSettings.Add(i.GetType(), i);
            }
            PostProcessEffectSettings currentSetting;
            if(allSettings.TryGetValue(typeof(PostProcessing.ColorGrading), out currentSetting))
            {
                ColorGrading.InitializeColorGrading(currentSetting as PostProcessing.ColorGrading, resources.computeShaders.lut3DBaker, ref colorGradingData);
                finalizerAction += () => ColorGrading.Finalize(ref colorGradingData, uberMaterial);
                renderAction += () => ColorGrading.PrepareRender(ref colorGradingData, uberMaterial);
            }
        }

        private void RenderToScreen(ref PipelineCommandData data, RenderTexture source, RenderTexture dest)
        {
            Graphics.SetRenderTarget(dest);
            uberMaterial.SetTexture(ShaderIDs._MainTex, source);
            uberMaterial.SetPass(0);
            Graphics.DrawMeshNow(GraphicsUtility.mesh, Matrix4x4.identity);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Destroy(uberMaterial);
            finalizerAction();
            finalizerAction = null;
        }

        public override void FrameUpdate(ref PipelineCommandData data)
        {
            renderAction();
            PostFunctions.Blit(ref data, uberAction);
        }
        public override void PreRenderFrame(Camera cam)
        {
            throw new System.NotImplementedException();
        }
    }
}