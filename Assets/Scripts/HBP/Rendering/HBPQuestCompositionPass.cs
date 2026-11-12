using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace HBP.Rendering
{
    internal sealed class HBPQuestCompositionPass : ScriptableRenderPass
    {
        private const int LayerCount = 4;
        private const int TimelineDepthPass = 1;
        private const int TimelineColorPass = 2;
        private static readonly ShaderTagId CompositionTag = new("HBPQuestComposition");
        private static readonly ShaderTagId PeelType = new("Peel");
        private static readonly List<ShaderTagId> ForwardPasses = new() { new("SRPDefaultUnlit"), new("UniversalForward"), new("UniversalForwardOnly") };
        private static readonly List<ShaderTagId> PeelPasses = new() { new("HBPQuestTransparent") };
        private static readonly List<ShaderTagId> SilhouettePasses = new() { new("HBPEdgeMask") };
        private static readonly int OpaqueDepthId = Shader.PropertyToID("_HBPQuestOpaqueDepth");
        private static readonly int PreviousDepthId = Shader.PropertyToID("_HBPQuestPreviousDepth");
        private static readonly int PreviousColorId = Shader.PropertyToID("_HBPQuestPreviousColor");
        private static readonly int HasPreviousLayerId = Shader.PropertyToID("_HBPQuestHasPreviousLayer");
        private static readonly int TailId = Shader.PropertyToID("_HBPQuestTail");
        private static readonly int TimelineColorId = Shader.PropertyToID("_HBPQuestTimelineColor");
        private static readonly int TimelineDepthId = Shader.PropertyToID("_HBPQuestTimelineDepth");
        private static readonly int EdgesEnabledId = Shader.PropertyToID("_HBPQuestEdgesEnabled");
        private static readonly int EdgeColorId = Shader.PropertyToID("_HBPQuestEdgeColor");
        private static readonly int EdgeParametersId = Shader.PropertyToID("_HBPQuestEdgeParameters");
        private static readonly int TargetSizeId = Shader.PropertyToID("_HBPQuestTargetSize");
        private static readonly int OpaqueEdgeDataId = Shader.PropertyToID("_HBPQuestOpaqueEdgeData");
        private static readonly int OpaqueEdgeDepthId = Shader.PropertyToID("_HBPQuestOpaqueEdgeDepth");
        private static readonly int TransparentEdgeDepthId = Shader.PropertyToID("_HBPQuestTransparentEdgeDepth");
        private static readonly int[] LayerColorIds = new int[LayerCount];
        private static readonly int[] LayerDepthIds = new int[LayerCount];
        private static readonly string[] LayerColorNames = new string[LayerCount];
        private static readonly string[] LayerDepthNames = new string[LayerCount];

        private Material compositionMaterial;
        private Shader panelShader;
        private HBPEdgeRendererFeature.Settings edges;

        static HBPQuestCompositionPass()
        {
            for (int index = 0; index < LayerCount; index++)
            {
                LayerColorIds[index] = Shader.PropertyToID($"_HBPQuestLayerColor{index}");
                LayerDepthIds[index] = Shader.PropertyToID($"_HBPQuestLayerDepth{index}");
                LayerColorNames[index] = $"HBP Quest Layer {index} Color";
                LayerDepthNames[index] = $"HBP Quest Layer {index} Depth";
            }
        }

        public HBPQuestCompositionPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            requiresIntermediateTexture = true;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup(Material composition, Shader panel, HBPEdgeRendererFeature.Settings edgeSettings)
        {
            compositionMaterial = composition;
            panelShader = panel;
            edges = edgeSettings;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (!resources.cameraDepthTexture.IsValid() || !resources.activeDepthTexture.IsValid()) return;

            var camera = frameData.Get<UniversalCameraData>();
            int cameraMask = camera.camera.cullingMask;
            var colorDesc = renderGraph.GetTextureDesc(resources.activeColorTexture);
            var inputs = new CompositionInputs
            {
                OpaqueDepth = resources.cameraDepthTexture,
                Size = new Vector4(colorDesc.width, colorDesc.height, 0, 0),
                EdgesEnabled = edges != null,
                EdgeColor = edges?.Color ?? Color.clear,
                EdgeParameters = edges == null ? Vector4.zero : new Vector4(edges.Thickness, edges.DepthThreshold, edges.NormalThreshold, 0)
            };

            colorDesc.msaaSamples = MSAASamples.None;
            colorDesc.bindTextureMS = false;
            colorDesc.depthBufferBits = DepthBits.None;
            colorDesc.clearBuffer = true;
            colorDesc.clearColor = Color.clear;
            var depthDesc = colorDesc;
            depthDesc.format = GraphicsFormat.None;
            depthDesc.depthBufferBits = DepthBits.Depth32;

            var nearestState = new RenderStateBlock(RenderStateMask.Blend | RenderStateMask.Depth)
            {
                blendState = BlendState.defaultValue,
                depthState = new DepthState(true, CompareFunction.LessEqual)
            };
            var blend = BlendState.defaultValue;
            blend.blendState0 = new RenderTargetBlendState(ColorWriteMask.All, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, BlendMode.One, BlendMode.OneMinusSrcAlpha);
            var tailState = new RenderStateBlock(RenderStateMask.Blend | RenderStateMask.Depth)
            {
                blendState = blend,
                depthState = new DepthState(false, CompareFunction.Always)
            };
            var tail = CreateList(renderGraph, frameData, PeelPasses, RenderQueueRange.transparent, cameraMask, SortingCriteria.BackToFront, state: tailState);

            TextureHandle previousDepth = TextureHandle.nullHandle;
            TextureHandle previousColor = TextureHandle.nullHandle;
            for (int index = 0; index < LayerCount; index++)
            {
                var nearest = CreateList(renderGraph, frameData, PeelPasses, RenderQueueRange.transparent, cameraMask, SortingCriteria.CommonOpaque, state: nearestState);
                colorDesc.name = LayerColorNames[index];
                depthDesc.name = LayerDepthNames[index];
                var color = inputs.Colors[index] = renderGraph.CreateTexture(colorDesc);
                var depth = inputs.Depths[index] = renderGraph.CreateTexture(depthDesc);
                RecordLayer(renderGraph, camera, colorDesc.name, nearest, inputs.OpaqueDepth, previousColor, previousDepth, color, depth);
                previousColor = color;
                previousDepth = depth;
            }

            colorDesc.name = "HBP Quest Far Transparency";
            inputs.Tail = renderGraph.CreateTexture(colorDesc);
            RecordLayer(renderGraph, camera, colorDesc.name, tail, inputs.OpaqueDepth, previousColor, previousDepth, inputs.Tail, TextureHandle.nullHandle);

            colorDesc.name = "HBP Quest Timeline Color";
            inputs.TimelineColor = renderGraph.CreateTexture(colorDesc);
            var timelineDepth = depthDesc;
            timelineDepth.name = "HBP Quest Timeline Depth";
            timelineDepth.format = GraphicsFormatUtility.GetDepthStencilFormat(24, 8);
            inputs.TimelineDepth = renderGraph.CreateTexture(timelineDepth);
            var timelineQueue = new RenderQueueRange(HBPEdgeRendererFeature.QuestTimelineRenderQueue, HBPEdgeRendererFeature.QuestTimelineRenderQueue);
            var timelineDepthList = CreateList(renderGraph, frameData, ForwardPasses, timelineQueue, cameraMask, SortingCriteria.CommonTransparent, shader: panelShader, shaderPass: TimelineDepthPass);
            var timelineColorList = CreateList(renderGraph, frameData, ForwardPasses, timelineQueue, cameraMask, SortingCriteria.CommonTransparent, shader: panelShader, shaderPass: TimelineColorPass);
            RecordTimeline(renderGraph, camera, inputs, timelineDepthList, timelineColorList);

            if (inputs.EdgesEnabled)
            {
                inputs.OpaqueEdgeData = HBPEdgeRendererFeature.CreateOpaqueEdgeData(renderGraph, frameData, resources, out inputs.OpaqueEdgeDepth);
                depthDesc.name = "HBP Quest Transparent Silhouette Depth";
                inputs.TransparentEdgeDepth = renderGraph.CreateTexture(depthDesc);
                var silhouette = CreateList(renderGraph, frameData, SilhouettePasses, RenderQueueRange.transparent, cameraMask, SortingCriteria.CommonOpaque, state: nearestState);
                RecordDraw(renderGraph, camera, depthDesc.name, silhouette, TextureHandle.nullHandle, inputs.TransparentEdgeDepth);
            }

            // Miscellaneous forward transparents (for example the 3D diagnostic cursor) retain their native shaders.
            // The science shaders provide their own peel passes and are skipped in this renderer list.
            var skipPeel = new RenderStateBlock(RenderStateMask.Depth) { depthState = new DepthState(false, CompareFunction.Never) };
            var other = CreateList(renderGraph, frameData, ForwardPasses, new RenderQueueRange(2501, HBPEdgeRendererFeature.QuestTimelineRenderQueue - 1), cameraMask, SortingCriteria.CommonTransparent, state: skipPeel, stateTag: PeelType);
            RecordDraw(renderGraph, camera, "HBP Quest Forward Transparencies", other, resources.activeColorTexture, resources.activeDepthTexture);

            using (var builder = renderGraph.AddRasterRenderPass<CompositeData>("HBP Quest Compose Science", out var passData))
            {
                passData.Inputs = inputs;
                passData.Material = compositionMaterial;
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.Write);
                UseInputs(builder, inputs);
                SetStereoFlags(builder, camera);
                builder.SetRenderFunc(static (CompositeData data, RasterGraphContext context) =>
                {
                    BindInputs(context.cmd, data.Inputs);
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, 0, MeshTopology.Triangles, 3, 1);
                });
            }

            var windowQueue = new RenderQueueRange(HBPEdgeRendererFeature.QuestWindowRenderQueue, HBPEdgeRendererFeature.QuestWindowRenderQueue);
            var panels = CreateList(renderGraph, frameData, ForwardPasses, windowQueue, cameraMask, SortingCriteria.CommonTransparent, shader: panelShader);
            RecordDraw(renderGraph, camera, "HBP Quest Window Stack", panels, resources.activeColorTexture, resources.activeDepthTexture, inputs);

            var overlays = CreateList(renderGraph, frameData, ForwardPasses, new RenderQueueRange(4000, 5000), cameraMask, SortingCriteria.CommonTransparent);
            RecordDraw(renderGraph, camera, "HBP Quest Overlays", overlays, resources.activeColorTexture, resources.activeDepthTexture);
        }

        private static RendererListHandle CreateList(RenderGraph graph, ContextContainer frameData, List<ShaderTagId> passes, RenderQueueRange queue, int layerMask, SortingCriteria sorting, Shader shader = null, int shaderPass = 0, RenderStateBlock? state = null, ShaderTagId stateTag = default)
        {
            var rendering = frameData.Get<UniversalRenderingData>();
            var camera = frameData.Get<UniversalCameraData>();
            var light = frameData.Get<UniversalLightData>();
            var drawing = RenderingUtils.CreateDrawingSettings(passes, rendering, camera, light, sorting);
            drawing.overrideShader = shader;
            drawing.overrideShaderPassIndex = shaderPass;
            var parameters = new RendererListParams(rendering.cullResults, drawing, new FilteringSettings(queue, layerMask));
            if (!state.HasValue) return graph.CreateRendererList(parameters);

            using var tags = new NativeArray<ShaderTagId>(new[] { stateTag }, Allocator.Temp);
            using var states = new NativeArray<RenderStateBlock>(new[] { state.Value }, Allocator.Temp);
            parameters.tagName = stateTag == ShaderTagId.none ? ShaderTagId.none : CompositionTag;
            parameters.tagValues = tags;
            parameters.stateBlocks = states;
            return graph.CreateRendererList(parameters);
        }

        private static void RecordTimeline(RenderGraph graph, UniversalCameraData camera, CompositionInputs inputs, RendererListHandle depthList, RendererListHandle colorList)
        {
            using var builder = graph.AddRasterRenderPass<TimelineData>("HBP Quest Timeline Surface", out var data);
            data.DepthList = depthList;
            data.ColorList = colorList;
            data.OpaqueDepth = inputs.OpaqueDepth;
            builder.UseRendererList(depthList);
            builder.UseRendererList(colorList);
            builder.UseTexture(inputs.OpaqueDepth, AccessFlags.Read);
            builder.SetRenderAttachment(inputs.TimelineColor, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(inputs.TimelineDepth, AccessFlags.Write);
            builder.AllowGlobalStateModification(true);
            SetStereoFlags(builder, camera);
            builder.SetRenderFunc(static (TimelineData timeline, RasterGraphContext context) =>
            {
                context.cmd.SetGlobalTexture(OpaqueDepthId, timeline.OpaqueDepth);
                // Resolve the visible plane before blending the canvas graphics in their own order.
                context.cmd.DrawRendererList(timeline.DepthList);
                context.cmd.DrawRendererList(timeline.ColorList);
            });
        }

        private static void RecordLayer(RenderGraph graph, UniversalCameraData camera, string name, RendererListHandle renderers, TextureHandle opaque, TextureHandle previousColor, TextureHandle previousDepth, TextureHandle color, TextureHandle depth)
        {
            using var builder = graph.AddRasterRenderPass<LayerData>(name, out var data);
            data.Renderers = renderers;
            data.OpaqueDepth = opaque;
            data.PreviousColor = previousColor;
            data.PreviousDepth = previousDepth;
            builder.UseRendererList(renderers);
            builder.UseTexture(opaque, AccessFlags.Read);
            if (previousDepth.IsValid())
            {
                builder.UseTexture(previousDepth, AccessFlags.Read);
                builder.UseTexture(previousColor, AccessFlags.Read);
            }

            builder.SetRenderAttachment(color, 0, AccessFlags.Write);
            if (depth.IsValid()) builder.SetRenderAttachmentDepth(depth, AccessFlags.Write);
            builder.AllowGlobalStateModification(true);
            SetStereoFlags(builder, camera);
            builder.SetRenderFunc(static (LayerData layer, RasterGraphContext context) =>
            {
                context.cmd.SetGlobalTexture(OpaqueDepthId, layer.OpaqueDepth);
                context.cmd.SetGlobalInt(HasPreviousLayerId, layer.PreviousDepth.IsValid() ? 1 : 0);
                if (layer.PreviousDepth.IsValid())
                {
                    context.cmd.SetGlobalTexture(PreviousDepthId, layer.PreviousDepth);
                    context.cmd.SetGlobalTexture(PreviousColorId, layer.PreviousColor);
                }

                context.cmd.DrawRendererList(layer.Renderers);
            });
        }

        private static void RecordDraw(RenderGraph graph, UniversalCameraData camera, string name, RendererListHandle renderers, TextureHandle color, TextureHandle depth, CompositionInputs inputs = null)
        {
            using var builder = graph.AddRasterRenderPass<DrawData>(name, out var data);
            data.Renderers = renderers;
            data.Inputs = inputs;
            builder.UseRendererList(renderers);
            if (color.IsValid()) builder.SetRenderAttachment(color, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(depth, color.IsValid() ? AccessFlags.ReadWrite : AccessFlags.Write);
            if (inputs != null) UseInputs(builder, inputs);
            SetStereoFlags(builder, camera);
            builder.SetRenderFunc(static (DrawData draw, RasterGraphContext context) =>
            {
                if (draw.Inputs != null) BindInputs(context.cmd, draw.Inputs);
                context.cmd.DrawRendererList(draw.Renderers);
            });
        }

        private static void SetStereoFlags(IRasterRenderGraphBuilder builder, UniversalCameraData camera)
        {
            if (camera.xr.enabled && camera.xr.multipassId == 0)
                builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible);
        }

        private static void UseInputs(IRasterRenderGraphBuilder builder, CompositionInputs inputs)
        {
            builder.AllowGlobalStateModification(true);
            for (int index = 0; index < LayerCount; index++)
            {
                builder.UseTexture(inputs.Colors[index], AccessFlags.Read);
                builder.UseTexture(inputs.Depths[index], AccessFlags.Read);
            }

            builder.UseTexture(inputs.OpaqueDepth, AccessFlags.Read);
            builder.UseTexture(inputs.Tail, AccessFlags.Read);
            builder.UseTexture(inputs.TimelineColor, AccessFlags.Read);
            builder.UseTexture(inputs.TimelineDepth, AccessFlags.Read);
            if (!inputs.EdgesEnabled) return;
            builder.UseTexture(inputs.OpaqueEdgeData, AccessFlags.Read);
            builder.UseTexture(inputs.OpaqueEdgeDepth, AccessFlags.Read);
            builder.UseTexture(inputs.TransparentEdgeDepth, AccessFlags.Read);
        }

        private static void BindInputs(RasterCommandBuffer command, CompositionInputs inputs)
        {
            for (int index = 0; index < LayerCount; index++)
            {
                command.SetGlobalTexture(LayerColorIds[index], inputs.Colors[index]);
                command.SetGlobalTexture(LayerDepthIds[index], inputs.Depths[index]);
            }

            command.SetGlobalTexture(OpaqueDepthId, inputs.OpaqueDepth);
            command.SetGlobalTexture(TailId, inputs.Tail);
            command.SetGlobalTexture(TimelineColorId, inputs.TimelineColor);
            command.SetGlobalTexture(TimelineDepthId, inputs.TimelineDepth, RenderTextureSubElement.Depth);
            command.SetGlobalInt(EdgesEnabledId, inputs.EdgesEnabled ? 1 : 0);
            if (!inputs.EdgesEnabled) return;
            command.SetGlobalVector(TargetSizeId, inputs.Size);
            command.SetGlobalColor(EdgeColorId, inputs.EdgeColor);
            command.SetGlobalVector(EdgeParametersId, inputs.EdgeParameters);
            command.SetGlobalTexture(OpaqueEdgeDataId, inputs.OpaqueEdgeData);
            command.SetGlobalTexture(OpaqueEdgeDepthId, inputs.OpaqueEdgeDepth);
            command.SetGlobalTexture(TransparentEdgeDepthId, inputs.TransparentEdgeDepth);
        }

        private sealed class LayerData
        {
            public RendererListHandle Renderers;
            public TextureHandle OpaqueDepth;
            public TextureHandle PreviousColor;
            public TextureHandle PreviousDepth;
        }

        private sealed class TimelineData
        {
            public RendererListHandle DepthList;
            public RendererListHandle ColorList;
            public TextureHandle OpaqueDepth;
        }

        private sealed class DrawData
        {
            public RendererListHandle Renderers;
            public CompositionInputs Inputs;
        }

        private sealed class CompositeData
        {
            public Material Material;
            public CompositionInputs Inputs;
        }

        private sealed class CompositionInputs
        {
            public readonly TextureHandle[] Colors = new TextureHandle[LayerCount];
            public readonly TextureHandle[] Depths = new TextureHandle[LayerCount];
            public TextureHandle OpaqueDepth;
            public TextureHandle Tail;
            public TextureHandle TimelineColor;
            public TextureHandle TimelineDepth;
            public bool EdgesEnabled;
            public Color EdgeColor;
            public Vector4 EdgeParameters;
            public Vector4 Size;
            public TextureHandle OpaqueEdgeData;
            public TextureHandle OpaqueEdgeDepth;
            public TextureHandle TransparentEdgeDepth;
        }
    }
}
