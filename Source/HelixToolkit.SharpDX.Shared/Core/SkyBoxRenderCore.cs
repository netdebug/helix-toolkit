﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX.Core
#else
namespace HelixToolkit.UWP.Core
#endif
{
    using Shaders;
    using System.IO;
    using Utilities;
    using Render;

    public class SkyBoxRenderCore : GeometryRenderCore<int>, ISkyboxRenderParams
    {
        #region Default Mesh
        private static readonly Vector3Collection BoxPositions = new Vector3Collection()
        {
            new Vector3(-10.0f,  10.0f, -10.0f),
            new Vector3( -10.0f, -10.0f, -10.0f),
            new Vector3( 10.0f, -10.0f, -10.0f),
            new Vector3(  10.0f, -10.0f, -10.0f),
            new Vector3(  10.0f,  10.0f, -10.0f),
            new Vector3( -10.0f,  10.0f, -10.0f),

            new Vector3( -10.0f, -10.0f,  10.0f),
            new Vector3(-10.0f, -10.0f, -10.0f),
            new Vector3(  -10.0f,  10.0f, -10.0f),
            new Vector3(  -10.0f,  10.0f, -10.0f),
            new Vector3(  -10.0f,  10.0f,  10.0f),
            new Vector3(  -10.0f, -10.0f,  10.0f),

            new Vector3(   10.0f, -10.0f, -10.0f),
            new Vector3(   10.0f, -10.0f,  10.0f),
            new Vector3(   10.0f,  10.0f,  10.0f),
            new Vector3(   10.0f,  10.0f,  10.0f),
            new Vector3(   10.0f,  10.0f, -10.0f),
            new Vector3(   10.0f, -10.0f, -10.0f),

            new Vector3(  -10.0f, -10.0f,  10.0f),
            new Vector3(  -10.0f,  10.0f,  10.0f),
            new Vector3(   10.0f,  10.0f,  10.0f),
            new Vector3(   10.0f,  10.0f,  10.0f),
            new Vector3(   10.0f, -10.0f,  10.0f),
            new Vector3(  -10.0f, -10.0f,  10.0f),

            new Vector3(  -10.0f,  10.0f, -10.0f),
            new Vector3(   10.0f,  10.0f, -10.0f),
            new Vector3(   10.0f,  10.0f,  10.0f),
            new Vector3(   10.0f,  10.0f,  10.0f),
            new Vector3(  -10.0f,  10.0f,  10.0f),
            new Vector3(  -10.0f,  10.0f, -10.0f),

            new Vector3(  -10.0f, -10.0f, -10.0f),
            new Vector3(  -10.0f, -10.0f,  10.0f),
            new Vector3(   10.0f, -10.0f, -10.0f),
            new Vector3(   10.0f, -10.0f, -10.0f),
            new Vector3( -10.0f, -10.0f,  10.0f),
            new Vector3(   10.0f, -10.0f,  10.0f)
        };
        #endregion

        private Stream cubeTexture = null;

        public Stream CubeTexture
        {
            set
            {
                if(SetAffectsRender(ref cubeTexture, value) && IsAttached)
                {
                    cubeTextureRes.CreateView(value);
                }
            }
            get
            {
                return cubeTexture;
            }
        }

        private SamplerStateDescription samplerDescription = DefaultSamplers.CubeSampler;
        public SamplerStateDescription SamplerDescription
        {
            set
            {
                if(SetAffectsRender(ref samplerDescription, value) && IsAttached)
                {
                    RemoveAndDispose(ref textureSampler);
                    textureSampler = Collect(EffectTechnique.EffectsManager.StateManager.Register(value));
                }
            }
            get
            {
                return samplerDescription;
            }
        }

        public string ShaderCubeTextureName { set; get; } = DefaultBufferNames.CubeMapTB; 
        public string ShaderCubeTextureSamplerName { set; get; } = DefaultSamplerStateNames.CubeMapSampler;

        private ShaderResouceViewProxy cubeTextureRes;
        private int cubeTextureSlot;
        private SamplerState textureSampler;
        private int textureSamplerSlot;

        public SkyBoxRenderCore()
        {
            RasterDescription = DefaultRasterDescriptions.RSSkybox;
        }

        private Vector3[] CreateVertexArray(PointGeometry3D geometry)
        {
            return geometry.Positions.ToArray();
        }

        protected override bool OnAttach(IRenderTechnique technique)
        {
            if (base.OnAttach(technique))
            {
                var buffer = Collect(new PointGeometryBufferModel<Vector3>(Vector3.SizeInBytes));
                buffer.Geometry = new PointGeometry3D() { Positions = BoxPositions };
                buffer.OnBuildVertexArray = CreateVertexArray;
                buffer.Topology = PrimitiveTopology.TriangleList;
                GeometryBuffer = buffer;
                cubeTextureRes = Collect(new ShaderResouceViewProxy(Device));
                if (cubeTexture != null)
                {
                    cubeTextureRes.CreateView(cubeTexture);
                }
                textureSampler = Collect(technique.EffectsManager.StateManager.Register(SamplerDescription));
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override ConstantBufferDescription GetModelConstantBufferDescription()
        {
            return null;
        }

        protected override void OnUploadPerModelConstantBuffers(DeviceContext context)
        {

        }

        protected override void OnDefaultPassChanged(IShaderPass pass)
        {
            cubeTextureSlot = pass.GetShader(ShaderStage.Pixel).ShaderResourceViewMapping.TryGetBindSlot(ShaderCubeTextureName);
            textureSamplerSlot = pass.GetShader(ShaderStage.Pixel).SamplerMapping.TryGetBindSlot(ShaderCubeTextureSamplerName);
        }

        protected override void OnRender(IRenderContext context, DeviceContextProxy deviceContext)
        {
            DefaultShaderPass.BindShader(deviceContext);
            DefaultShaderPass.BindStates(deviceContext, StateType.BlendState | StateType.DepthStencilState);
            DefaultShaderPass.GetShader(ShaderStage.Pixel).BindTexture(deviceContext, cubeTextureSlot, cubeTextureRes);
            DefaultShaderPass.GetShader(ShaderStage.Pixel).BindSampler(deviceContext, textureSamplerSlot, textureSampler);
            deviceContext.DeviceContext.Draw(GeometryBuffer.VertexBuffer.ElementCount, 0);
        }

        protected override void OnUpdatePerModelStruct(ref int model, IRenderContext context)
        {

        }
    }
}