using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
namespace MPipeline.Light
{
    public class SpotLightEvent : PipelineEvent
    {
        private Material spotLightMaterial;
        public MSpotLight[] mSpotlights;
        private Vector4[] corners = null;
        private ComputeBuffer indirectSpotBuffer;
        protected override void Awake()
        {
            base.Awake();
            corners = new Vector4[5];
            spotLightMaterial = new Material(Shader.Find("Hidden/SpotLight"));
            indirectSpotBuffer = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
            NativeArray<uint> newInt = new NativeArray<uint>(5, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            newInt[0] = 18;
            newInt[1] = 1;
            newInt[2] = 0;
            newInt[3] = 0;
            newInt[4] = 0;
            indirectSpotBuffer.SetData(newInt);
            newInt.Dispose();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Destroy(spotLightMaterial);
            indirectSpotBuffer.Dispose();
        }
        const float angleToReg = (float)(3.1415926536 / 360.0);
        public override void FrameUpdate(ref PipelineCommandData data)
        {
            spotLightMaterial.SetMatrix(ShaderIDs._InvVP, data.inverseVP);
            Graphics.SetRenderTarget(data.targets.colorBuffer, data.targets.depthBuffer);
            foreach (var i in mSpotlights)
            {
                Vector4 color = i.lightColor * i.intensity;
                color.w = i.range;
                spotLightMaterial.SetVector(ShaderIDs._LightFinalColor, color);
                Vector4 pos = i.transform.position;
                spotLightMaterial.SetVector(ShaderIDs._LightPos, pos);
                Vector4 dir = -i.transform.forward;
                dir.w =  i.angle * angleToReg;
                spotLightMaterial.SetVector(ShaderIDs._LightDir, dir);
                i.UpdateShadowCam();
                GetCorners(ref i.shadCam, corners);
                spotLightMaterial.SetVectorArray(ShaderIDs._WorldPoses, corners);
                spotLightMaterial.SetPass(0);
                Graphics.DrawProceduralIndirect(MeshTopology.Triangles, indirectSpotBuffer);
            }
        }

        public override void PreRenderFrame(Camera cam)
        {
            throw new System.NotImplementedException();
        }

        #region STATIC
        public static void GetCorners(ref PerspCam cam, Vector4[] corners)
        {
            Matrix4x4 invvp = (GL.GetGPUProjectionMatrix(cam.projectionMatrix, false) * cam.worldToCameraMatrix).inverse;
            corners[0] = cam.position;
            corners[1] = invvp.MultiplyPoint(new Vector3(-1, -1, 0));
            corners[2] = invvp.MultiplyPoint(new Vector3(1, -1, 0));
            corners[3] = invvp.MultiplyPoint(new Vector3(-1, 1, 0));
            corners[4] = invvp.MultiplyPoint(new Vector3(1, 1, 0));
        }
        #endregion
    }
}