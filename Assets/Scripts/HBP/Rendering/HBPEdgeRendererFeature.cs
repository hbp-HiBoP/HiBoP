using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HBP.Rendering
{
    public sealed class HBPEdgeRendererFeature : ScriptableRendererFeature
    {
        public const int QuestTimelineRenderQueue = 3998;
        public const int QuestWindowRenderQueue = 3999;

        [System.Serializable]
        public sealed class Settings
        {
            public Color Color = new(0.03f, 0.03f, 0.03f, 1.0f);
            [Range(0.5f, 4.0f)] public float Thickness = 1.0f;
            [Range(0.0001f, 0.05f)] public float DepthThreshold = 0.0025f;
            [Range(0.01f, 1.0f)] public float NormalThreshold = 0.18f;
        }

        [SerializeField] private Settings m_Settings = new();
        [SerializeField] private Shader m_Shader;
        [SerializeField] private Shader m_QuestCompositionShader;
        [SerializeField] private Shader m_QuestPanelShader;

        private Material m_Material;
        private Material m_QuestCompositionMaterial;
        private HBPQuestCompositionPass m_QuestPass;
        private HBPTransparentBrainRenderPass m_TransparentBrainPass;
        private HBPEdgeRenderPass m_Pass;

        public Settings EdgeSettings => m_Settings;

        public override void Create()
        {
            CoreUtils.Destroy(m_Material);
            CoreUtils.Destroy(m_QuestCompositionMaterial);
            m_QuestCompositionMaterial = m_QuestCompositionShader != null ? CoreUtils.CreateEngineMaterial(m_QuestCompositionShader) : null;
            m_QuestPass = m_QuestCompositionMaterial != null ? new HBPQuestCompositionPass() : null;
            Shader shader = m_Shader != null ? m_Shader : Shader.Find("Hidden/HBP/Edges");
            m_Material = shader == null ? null : CoreUtils.CreateEngineMaterial(shader);
            m_TransparentBrainPass = new HBPTransparentBrainRenderPass();
            m_Pass = new HBPEdgeRenderPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_QuestPass != null && m_QuestPanelShader != null)
            {
                var settings = renderingData.cameraData.camera.GetComponent<HBPEdgeCameraSettings>();
                m_QuestPass.Setup(m_QuestCompositionMaterial, m_QuestPanelShader, settings != null && settings.EdgesEnabled ? m_Settings : null);
                renderer.EnqueuePass(m_QuestPass);
                return;
            }

            if (m_Material == null || renderingData.cameraData.cameraType != CameraType.Game)
                return;

            m_TransparentBrainPass.Setup(m_Material);
            renderer.EnqueuePass(m_TransparentBrainPass);

            HBPEdgeCameraSettings cameraSettings = renderingData.cameraData.camera.GetComponent<HBPEdgeCameraSettings>();
            if (cameraSettings == null || !cameraSettings.EdgesEnabled)
                return;

            m_Pass.Setup(m_Material, m_Settings);
            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_Material);
            CoreUtils.Destroy(m_QuestCompositionMaterial);
            m_Material = null;
            m_QuestCompositionMaterial = null;
        }

        internal static TextureHandle CreateOpaqueEdgeData(RenderGraph graph, ContextContainer frameData, UniversalResourceData resources, out TextureHandle depth) => HBPEdgeRenderPass.CreateOpaqueData(graph, frameData, resources, out depth);

        private sealed class HBPTransparentBrainRenderPass : ScriptableRenderPass
        {
            private static readonly ShaderTagId TransparentBrainSurfaceTag = new("HBPTransparentBrainSurface");
            private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
            private static readonly int TransparentBrainSurfaceId = Shader.PropertyToID("_HBPTransparentBrainSurface");
            private static readonly int TransparentBrainDepthId = Shader.PropertyToID("_HBPTransparentBrainDepth");
            private static readonly int SceneDepthId = Shader.PropertyToID("_HBPSceneDepth");
            private static readonly MaterialPropertyBlock CompositeProperties = new();
            private const int CompositePassIndex = 1;

            private Material m_Material;

            public HBPTransparentBrainRenderPass()
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
                requiresIntermediateTexture = true;
            }

            public void Setup(Material material)
            {
                m_Material = material;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer || !resourceData.activeColorTexture.IsValid() || !resourceData.activeDepthTexture.IsValid())
                {
                    return;
                }

                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(TransparentBrainSurfaceTag, renderingData, cameraData, lightData, SortingCriteria.CommonTransparent);
                FilteringSettings filteringSettings = new(RenderQueueRange.transparent, cameraData.camera.cullingMask);
                RendererListParams rendererListParams = new(renderingData.cullResults, drawingSettings, filteringSettings);
                RendererListHandle rendererList = renderGraph.CreateRendererList(rendererListParams);
                if (!rendererList.IsValid())
                    return;

                TextureDesc surfaceDescriptor = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
                surfaceDescriptor.name = "HBP Transparent Brain Surface";
                surfaceDescriptor.depthBufferBits = DepthBits.None;
                surfaceDescriptor.msaaSamples = MSAASamples.None;
                surfaceDescriptor.clearBuffer = true;
                surfaceDescriptor.clearColor = Color.clear;
                TextureHandle surface = renderGraph.CreateTexture(surfaceDescriptor);

                TextureDesc depthDescriptor = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
                depthDescriptor.name = "HBP Transparent Brain Depth";
                depthDescriptor.format = GraphicsFormat.None;
                depthDescriptor.depthBufferBits = DepthBits.Depth24;
                depthDescriptor.msaaSamples = MSAASamples.None;
                depthDescriptor.clearBuffer = true;
                TextureHandle depth = renderGraph.CreateTexture(depthDescriptor);

                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<SurfacePassData>("HBP Transparent Brain Surface", out SurfacePassData passData))
                {
                    passData.RendererList = rendererList;
                    builder.UseRendererList(rendererList);
                    builder.SetRenderAttachment(surface, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(depth, AccessFlags.Write);
                    builder.SetRenderFunc(static (SurfacePassData data, RasterGraphContext context) =>
                    {
                        context.cmd.ClearRenderTarget(RTClearFlags.All, Color.clear, 1.0f, 0);
                        context.cmd.DrawRendererList(data.RendererList);
                    });
                }

                TextureHandle source = resourceData.activeColorTexture;
                TextureHandle sceneDepth = resourceData.activeDepthTexture;
                TextureDesc destinationDescriptor = renderGraph.GetTextureDesc(source);
                destinationDescriptor.name = "HBP Transparent Brain Camera Color";
                destinationDescriptor.clearBuffer = false;
                TextureHandle destination = renderGraph.CreateTexture(destinationDescriptor);

                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<CompositePassData>("HBP Transparent Brain Composite", out CompositePassData passData))
                {
                    passData.Material = m_Material;
                    passData.Source = source;
                    passData.Surface = surface;
                    passData.BrainDepth = depth;
                    passData.SceneDepth = sceneDepth;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.UseTexture(surface, AccessFlags.Read);
                    builder.UseTexture(depth, AccessFlags.Read);
                    builder.UseTexture(sceneDepth, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) => ExecuteComposite(data, context));
                }

                resourceData.cameraColor = destination;
            }

            private static void ExecuteComposite(CompositePassData data, RasterGraphContext context)
            {
                MaterialPropertyBlock properties = CompositeProperties;
                properties.Clear();
                properties.SetTexture(BlitTextureId, data.Source);
                properties.SetTexture(TransparentBrainSurfaceId, data.Surface);
                properties.SetTexture(TransparentBrainDepthId, data.BrainDepth);
                properties.SetTexture(SceneDepthId, data.SceneDepth);
                context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, CompositePassIndex, MeshTopology.Triangles, 3, 1, properties);
            }

            private sealed class SurfacePassData
            {
                public RendererListHandle RendererList;
            }

            private sealed class CompositePassData
            {
                public Material Material;
                public TextureHandle Source;
                public TextureHandle Surface;
                public TextureHandle BrainDepth;
                public TextureHandle SceneDepth;
            }
        }

        private sealed class HBPEdgeRenderPass : ScriptableRenderPass
        {
            private static readonly ShaderTagId OpaqueDataTag = new("HBPEdgeData");
            private static readonly ShaderTagId TransparentMaskTag = new("HBPEdgeMask");
            private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
            private static readonly int BlitTextureTexelSizeId = Shader.PropertyToID("_BlitTexture_TexelSize");
            private static readonly int OpaqueDataId = Shader.PropertyToID("_HBPEdgeOpaqueData");
            private static readonly int TransparentMaskId = Shader.PropertyToID("_HBPEdgeTransparentMask");
            private static readonly int EdgeColorId = Shader.PropertyToID("_HBPEdgeColor");
            private static readonly int EdgeThicknessId = Shader.PropertyToID("_HBPEdgeThickness");
            private static readonly int DepthThresholdId = Shader.PropertyToID("_HBPEdgeDepthThreshold");
            private static readonly int NormalThresholdId = Shader.PropertyToID("_HBPEdgeNormalThreshold");
            private static readonly MaterialPropertyBlock CompositeProperties = new();

            private Material m_Material;
            private Color m_Color;
            private float m_Thickness;
            private float m_DepthThreshold;
            private float m_NormalThreshold;

            public HBPEdgeRenderPass()
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                requiresIntermediateTexture = true;
            }

            public void Setup(Material material, Settings settings)
            {
                m_Material = material;
                m_Color = settings.Color;
                m_Thickness = settings.Thickness;
                m_DepthThreshold = settings.DepthThreshold;
                m_NormalThreshold = settings.NormalThreshold;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer || !resourceData.activeColorTexture.IsValid())
                    return;

                TextureHandle opaqueData = CreateOpaqueData(renderGraph, frameData, resourceData, out _);
                TextureHandle transparentMask = CreateTransparentMask(renderGraph, frameData, resourceData);
                if (!opaqueData.IsValid() || !transparentMask.IsValid())
                    return;

                TextureHandle source = resourceData.activeColorTexture;
                TextureDesc destinationDescriptor = renderGraph.GetTextureDesc(source);
                destinationDescriptor.name = "HBP Edges Camera Color";
                destinationDescriptor.clearBuffer = false;
                TextureHandle destination = renderGraph.CreateTexture(destinationDescriptor);

                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<CompositePassData>("HBP Edges Composite", out CompositePassData passData))
                {
                    passData.Material = m_Material;
                    passData.Source = source;
                    passData.OpaqueData = opaqueData;
                    passData.TransparentMask = transparentMask;
                    passData.SourceTexelSize = new Vector4(1.0f / destinationDescriptor.width, 1.0f / destinationDescriptor.height, destinationDescriptor.width, destinationDescriptor.height);
                    passData.Color = m_Color;
                    passData.Thickness = m_Thickness;
                    passData.DepthThreshold = m_DepthThreshold;
                    passData.NormalThreshold = m_NormalThreshold;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.UseTexture(opaqueData, AccessFlags.Read);
                    builder.UseTexture(transparentMask, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) => ExecuteComposite(data, context));
                }

                resourceData.cameraColor = destination;
            }

            internal static TextureHandle CreateOpaqueData(RenderGraph renderGraph, ContextContainer frameData, UniversalResourceData resourceData, out TextureHandle depth)
            {
                depth = TextureHandle.nullHandle;
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(OpaqueDataTag, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
                FilteringSettings filteringSettings = new(RenderQueueRange.opaque, cameraData.camera.cullingMask);
                RendererListParams rendererListParams = new(renderingData.cullResults, drawingSettings, filteringSettings);
                RendererListHandle rendererList = renderGraph.CreateRendererList(rendererListParams);
                if (!rendererList.IsValid())
                    return TextureHandle.nullHandle;

                TextureDesc dataDescriptor = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
                dataDescriptor.name = "HBP Opaque Edge Data";
                dataDescriptor.format = GraphicsFormat.R16G16B16A16_SFloat;
                dataDescriptor.depthBufferBits = DepthBits.None;
                dataDescriptor.msaaSamples = MSAASamples.None;
                dataDescriptor.bindTextureMS = false;
                dataDescriptor.clearBuffer = true;
                dataDescriptor.clearColor = Color.clear;
                TextureHandle data = renderGraph.CreateTexture(dataDescriptor);

                TextureDesc depthDescriptor = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
                depthDescriptor.name = "HBP Opaque Edge Depth";
                depthDescriptor.format = GraphicsFormat.None;
                depthDescriptor.depthBufferBits = DepthBits.Depth24;
                depthDescriptor.msaaSamples = MSAASamples.None;
                depthDescriptor.bindTextureMS = false;
                depthDescriptor.clearBuffer = true;
                depth = renderGraph.CreateTexture(depthDescriptor);

                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<MaskPassData>("HBP Opaque Edge Data", out MaskPassData passData))
                {
                    passData.RendererList = rendererList;
                    builder.UseRendererList(rendererList);
                    builder.SetRenderAttachment(data, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(depth, AccessFlags.Write);
                    builder.SetRenderFunc(static (MaskPassData pass, RasterGraphContext context) =>
                    {
                        context.cmd.ClearRenderTarget(RTClearFlags.All, Color.clear, 1.0f, 0);
                        context.cmd.DrawRendererList(pass.RendererList);
                    });
                }

                return data;
            }

            private static TextureHandle CreateTransparentMask(RenderGraph renderGraph, ContextContainer frameData, UniversalResourceData resourceData)
            {
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(TransparentMaskTag, renderingData, cameraData, lightData, SortingCriteria.CommonTransparent);
                FilteringSettings filteringSettings = new(RenderQueueRange.transparent, cameraData.camera.cullingMask);
                RendererListParams rendererListParams = new(renderingData.cullResults, drawingSettings, filteringSettings);
                RendererListHandle rendererList = renderGraph.CreateRendererList(rendererListParams);
                if (!rendererList.IsValid())
                    return TextureHandle.nullHandle;

                TextureDesc maskDescriptor = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
                maskDescriptor.name = "HBP Transparent Edge Mask";
                maskDescriptor.format = GraphicsFormat.R8_UNorm;
                maskDescriptor.msaaSamples = MSAASamples.None;
                maskDescriptor.clearBuffer = true;
                maskDescriptor.clearColor = Color.clear;
                TextureHandle mask = renderGraph.CreateTexture(maskDescriptor);

                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<MaskPassData>("HBP Transparent Edge Mask", out MaskPassData passData))
                {
                    passData.RendererList = rendererList;
                    builder.UseRendererList(rendererList);
                    builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                    builder.SetRenderFunc(static (MaskPassData data, RasterGraphContext context) =>
                    {
                        context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear, 1.0f, 0);
                        context.cmd.DrawRendererList(data.RendererList);
                    });
                }

                return mask;
            }

            private static void ExecuteComposite(CompositePassData data, RasterGraphContext context)
            {
                MaterialPropertyBlock properties = CompositeProperties;
                properties.Clear();
                properties.SetTexture(BlitTextureId, data.Source);
                properties.SetVector(BlitTextureTexelSizeId, data.SourceTexelSize);
                properties.SetTexture(OpaqueDataId, data.OpaqueData);
                properties.SetTexture(TransparentMaskId, data.TransparentMask);
                properties.SetColor(EdgeColorId, data.Color);
                properties.SetFloat(EdgeThicknessId, data.Thickness);
                properties.SetFloat(DepthThresholdId, data.DepthThreshold);
                properties.SetFloat(NormalThresholdId, data.NormalThreshold);
                context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, 0, MeshTopology.Triangles, 3, 1, properties);
            }

            private sealed class MaskPassData
            {
                public RendererListHandle RendererList;
            }

            private sealed class CompositePassData
            {
                public Material Material;
                public TextureHandle Source;
                public TextureHandle OpaqueData;
                public TextureHandle TransparentMask;
                public Vector4 SourceTexelSize;
                public Color Color;
                public float Thickness;
                public float DepthThreshold;
                public float NormalThreshold;
            }
        }
    }
}
