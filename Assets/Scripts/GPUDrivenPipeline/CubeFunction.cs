using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
namespace MPipeline
{
    public unsafe static class CubeFunction
    {
        const int initLength = 1;
        const int GetFrustumPlane = 0;
        const int RunFrustumCull = 1;
        const int ClearCluster = 2;
        const float spreadLengthRate = 1.2f;
        public static void Init(ref CubeCullingBuffer buffer)
        {
            buffer.currentLength = initLength;
            buffer.planes = new ComputeBuffer(initLength * 6, sizeof(Vector4));
            buffer.lightPositionBuffer = new ComputeBuffer(initLength, sizeof(Vector4));
            buffer.indirectDrawBuffer = new ComputeBuffer(initLength * 5, sizeof(int), ComputeBufferType.IndirectArguments);
        }

        public static void UpdateLength(ref CubeCullingBuffer buffer, int targetLength)
        {
            if (targetLength <= buffer.currentLength) return;
            buffer.currentLength = (int)(buffer.currentLength * spreadLengthRate);
            buffer.currentLength = Mathf.Max(buffer.currentLength, targetLength);
            buffer.indirectDrawBuffer.Dispose();
            buffer.planes.Dispose();
            buffer.lightPositionBuffer.Dispose();
            buffer.planes = new ComputeBuffer(buffer.currentLength * 6, sizeof(Vector4));
            buffer.lightPositionBuffer = new ComputeBuffer(buffer.currentLength, sizeof(Vector4));
            buffer.indirectDrawBuffer = new ComputeBuffer(buffer.currentLength * 5, sizeof(int), ComputeBufferType.IndirectArguments);
        }

        public static void SetBuffer(ref CubeCullingBuffer buffer, ref PipelineBaseBuffer baseBuffer, ComputeShader shader)
        {
            shader.SetBuffer(ClearCluster, ShaderIDs.instanceCountBuffer, buffer.indirectDrawBuffer);
            shader.SetBuffer(RunFrustumCull, ShaderIDs.instanceCountBuffer, buffer.indirectDrawBuffer);
            shader.SetBuffer(RunFrustumCull, ShaderIDs.clusterBuffer, baseBuffer.clusterBuffer);
            shader.SetBuffer(RunFrustumCull, ShaderIDs.resultBuffer, baseBuffer.resultBuffer);
            shader.SetBuffer(RunFrustumCull, ShaderIDs.planes, buffer.planes);
            shader.SetBuffer(GetFrustumPlane, ShaderIDs.planes, buffer.planes);
            shader.SetBuffer(GetFrustumPlane, ShaderIDs.lightPositionBuffer, buffer.lightPositionBuffer);
        }

        public static void PrepareDispatch(ref CubeCullingBuffer buffer, ComputeShader shader, NativeArray<Vector4> positions)
        {
            int targetLength = positions.Length;
            buffer.lightPositionBuffer.SetData(positions);
            ComputeShaderUtility.Dispatch(shader, ClearCluster, targetLength, 64);
            ComputeShaderUtility.Dispatch(shader, GetFrustumPlane, targetLength, 16);
        }

        public static void DrawShadow(MPointLight lit, ref CubeCullingBuffer buffer, ref PipelineBaseBuffer baseBuffer, ComputeShader shader, int offset, Material depthMaterial)
        {
            shader.SetInt(ShaderIDs._LightOffset, offset);
            ComputeShaderUtility.Dispatch(shader, RunFrustumCull, baseBuffer.clusterCount, 64);
            PerspCam cam = new PerspCam();
            cam.aspect = 1;
            cam.farClipPlane = lit.range;
            cam.nearClipPlane = 0.3f;
            cam.position = lit.position;
            cam.fov = 90f;
            Matrix4x4 vpMatrix;
            depthMaterial.SetVector(ShaderIDs._LightPos, new Vector4(lit.position.x, lit.position.y, lit.position.z, lit.range));
            PipelineFunctions.SetShaderBuffer(ref baseBuffer, depthMaterial);
            //Forward
            cam.forward = Vector3.forward;
            cam.up = Vector3.down;
            cam.right = Vector3.left;
            cam.position = lit.position;
            cam.UpdateTRSMatrix();
            cam.UpdateProjectionMatrix();
            vpMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true) * cam.worldToCameraMatrix;
            Graphics.SetRenderTarget(lit.shadowmapTexture, 0, CubemapFace.NegativeZ);
            GL.Clear(true, true, Color.white);
            depthMaterial.SetMatrix(ShaderIDs._VP, vpMatrix);
            depthMaterial.SetPass(0);
            offset = offset * 20;

            Graphics.DrawProceduralIndirect(MeshTopology.Triangles, buffer.indirectDrawBuffer, offset);
            //Back
            cam.forward = Vector3.back;
            cam.up = Vector3.down;
            cam.right = Vector3.right;
            cam.UpdateTRSMatrix();
            cam.UpdateProjectionMatrix();
            vpMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true) * cam.worldToCameraMatrix;
            Graphics.SetRenderTarget(lit.shadowmapTexture, 0, CubemapFace.PositiveZ);
            GL.Clear(true, true, Color.white);
            depthMaterial.SetMatrix(ShaderIDs._VP, vpMatrix);
            depthMaterial.SetPass(0);
            
            Graphics.DrawProceduralIndirect(MeshTopology.Triangles, buffer.indirectDrawBuffer, offset);
            //Up
            cam.forward = Vector3.up;
            cam.up = Vector3.back;
            cam.right = Vector3.right;
            cam.UpdateTRSMatrix();
            cam.UpdateProjectionMatrix();
            vpMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true) * cam.worldToCameraMatrix;
            Graphics.SetRenderTarget(lit.shadowmapTexture, 0, CubemapFace.PositiveY);
            GL.Clear(true, true, Color.white);
            depthMaterial.SetMatrix(ShaderIDs._VP, vpMatrix);
            depthMaterial.SetPass(0);
            Graphics.DrawProceduralIndirect(MeshTopology.Triangles, buffer.indirectDrawBuffer, offset);
            //Down
            cam.forward = Vector3.down;
            cam.up = Vector3.forward;
            cam.right = Vector3.right;
            cam.UpdateTRSMatrix();
            cam.UpdateProjectionMatrix();
            vpMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true) * cam.worldToCameraMatrix;
            Graphics.SetRenderTarget(lit.shadowmapTexture, 0, CubemapFace.NegativeY);
            GL.Clear(true, true, Color.white);
            depthMaterial.SetMatrix(ShaderIDs._VP, vpMatrix);
            depthMaterial.SetPass(0);
            Graphics.DrawProceduralIndirect(MeshTopology.Triangles, buffer.indirectDrawBuffer, offset);
            //Right
            cam.forward = Vector3.right;
            cam.up = Vector3.down;
            cam.right = Vector3.forward;
            cam.UpdateTRSMatrix();
            cam.UpdateProjectionMatrix();
            vpMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true) * cam.worldToCameraMatrix;
            Graphics.SetRenderTarget(lit.shadowmapTexture, 0, CubemapFace.PositiveX);
            GL.Clear(true, true, Color.white);
            depthMaterial.SetMatrix(ShaderIDs._VP, vpMatrix);
            depthMaterial.SetPass(0);
            Graphics.DrawProceduralIndirect(MeshTopology.Triangles, buffer.indirectDrawBuffer, offset);
            //Left
            cam.forward = Vector3.left;
            cam.up = Vector3.down;
            cam.right = Vector3.back;
            cam.UpdateTRSMatrix();
            cam.UpdateProjectionMatrix();
            vpMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true) * cam.worldToCameraMatrix;
            Graphics.SetRenderTarget(lit.shadowmapTexture, 0, CubemapFace.NegativeX);
            GL.Clear(true, true, Color.white);
            depthMaterial.SetMatrix(ShaderIDs._VP, vpMatrix);
            depthMaterial.SetPass(0);
            Graphics.DrawProceduralIndirect(MeshTopology.Triangles, buffer.indirectDrawBuffer, offset);
        }

        public static void Dispose(ref CubeCullingBuffer buffer)
        {
            buffer.indirectDrawBuffer.Dispose();
            buffer.planes.Dispose();
            buffer.lightPositionBuffer.Dispose();
        }
    }

    public struct CubeCullingBuffer
    {
        public ComputeBuffer planes;
        public ComputeBuffer lightPositionBuffer;
        public ComputeBuffer indirectDrawBuffer;
        public int currentLength;
    }
}