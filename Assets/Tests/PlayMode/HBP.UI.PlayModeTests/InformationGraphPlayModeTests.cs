using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Enums;
using HBP.Core.Errors;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Data.Informations;
using HBP.Data.Informations.Graphs;
using HBP.Tests.PlayMode.Utilities;
using HBP.UI.Informations;
using HBP.UI.Informations.Graphs;
using HBP.UI.Module3D;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using CoreBloc = HBP.Core.Data.Bloc;
using CoreEvent = HBP.Core.Data.Event;
using CoreIcon = HBP.Core.Data.Icon;
using CoreIEEGData = HBP.Core.Data.IEEGData;
using CoreSubBloc = HBP.Core.Data.SubBloc;
using Object = UnityEngine.Object;
using UITrialMatrixBloc = HBP.UI.Informations.TrialMatrix.Bloc;
using UITrialMatrixChannelBloc = HBP.UI.Informations.TrialMatrix.ChannelBloc;
using UITrialMatrixChannelHeader = HBP.UI.Informations.TrialMatrix.ChannelHeader;
using UITrialMatrixData = HBP.UI.Informations.TrialMatrix.Data;
using UITrialMatrixGrid = HBP.UI.Informations.TrialMatrix.TrialMatrixGrid;
using UITrialMatrixSubBloc = HBP.UI.Informations.TrialMatrix.SubBloc;
using UITrialMatrixTimeBloc = HBP.UI.Informations.TrialMatrix.TimeBloc;
using UITrialMatrixTimeLegend = HBP.UI.Informations.TrialMatrix.TimeLegend;
using DBInformationPanels = HBP.UI.Database.InformationPanels;
using DBTagDisplaySettingsContextMenu = HBP.UI.Database.TagDisplaySettingsContextMenu;
using DBTagSelectionItem = HBP.UI.Database.TagSelectionItem;
using DBTrialMatrixActionsContextMenu = HBP.UI.Database.TrialMatrixActionsContextMenu;
using DBTrialMatrixDisplayer = HBP.UI.Database.TrialMatrixDisplayer;
using DBTrialMatrixGrid = HBP.UI.Database.TrialMatrixGrid;
using ObjectSite = HBP.Core.Object3D.Site;

namespace HBP.Tests.PlayMode.UI
{
    public class InformationGraphPlayModeTests
    {
        [Test]
        [Category("PlayMode.InformationGraph")]
        public void StructWrapper_SetNestedCurves_EmitsLegendsAndEnabledCurveData()
        {
            using PlayModeSceneScope scene = new("InformationGraphStructWrapper");
            GameObject wrapperObject = new("StructWrapper");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wrapperObject, scene.Scene);
            StructWrapper wrapper = wrapperObject.AddComponent<StructWrapper>();
            InitializeSerializedEvents(wrapper);

            CurveData enabledData = CurveData.CreateInstance(new[] { new Vector2(0, 1), new Vector2(1, 2) }, Color.red, 2f);
            CurveData disabledData = CurveData.CreateInstance(new[] { new Vector2(0, -1), new Vector2(1, -2) }, Color.blue, 1f);
            LegendsGestion.Legend[] legends = null;
            CurveData[] curveDatas = null;
            wrapper.OnLegendResult.AddListener(value => legends = value);
            wrapper.OnCurveDataResult.AddListener(value => curveDatas = value);

            try
            {
                Graph.Curve enabled = new("Enabled curve", enabledData, true, "enabled-id", Array.Empty<Graph.Curve>(), Color.red);
                Graph.Curve disabled = new("Disabled curve", disabledData, false, "disabled-id", Array.Empty<Graph.Curve>(), Color.blue);
                Graph.Curve group = new("Group", null, true, "group-id", new[] { enabled, disabled }, Color.gray);

                wrapper.Set(new[] { group });

                Assert.That(legends, Has.Length.EqualTo(1));
                Assert.That(legends[0].ID, Is.EqualTo("group-id"));
                Assert.That(legends[0].SubLegends.Select(legend => legend.ID), Is.EquivalentTo(new[] { "enabled-id", "disabled-id" }));
                Assert.That(curveDatas, Is.EqualTo(new[] { enabledData }));
            }
            finally
            {
                Object.Destroy(enabledData);
                Object.Destroy(disabledData);
            }
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public void Graph_AddSyntheticCurves_ExportsCsvAndSvgAndTracksEnabledState()
        {
            using PlayModeSceneScope scene = new("InformationGraphGraph");
            GameObject graphObject = new("Graph");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(graphObject, scene.Scene);
            Graph graph = graphObject.AddComponent<Graph>();
            InitializeSerializedEvents(graph);

            CurveData firstData = CurveData.CreateInstance(new[] { new Vector2(0, 1), new Vector2(1, 3), new Vector2(2, 2) }, Color.green, 3f);
            CurveData secondData = CurveData.CreateInstance(new[] { new Vector2(0, -1), new Vector2(1, -2), new Vector2(2, -1) }, Color.yellow, 1f);

            try
            {
                graph.Title = "Synthetic graph";
                graph.AbscissaLabel = "Time";
                graph.AbscissaUnit = "ms";
                graph.OrdinateLabel = "Signal";
                graph.OrdinateUnit = "uV";
                graph.AbscissaDisplayRange = new Vector2(0, 2);
                graph.OrdinateDisplayRange = new Vector2(-3, 4);
                graph.AddCurve(new Graph.Curve("First", firstData, true, "first-id", Array.Empty<Graph.Curve>(), Color.green));
                graph.AddCurve(new Graph.Curve("Second", secondData, true, "second-id", Array.Empty<Graph.Curve>(), Color.yellow));

                Dictionary<string, string> csvByCurve = graph.ToCSV();
                string svg = graph.ToSVG();

                Assert.That(graph.Curves, Has.Count.EqualTo(2));
                Assert.That(graph.GetEnabledCurvesName(), Is.EquivalentTo(new[] { "First", "Second" }));
                Assert.That(csvByCurve.Keys, Is.EquivalentTo(new[] { "first-id", "second-id" }));
                Assert.That(csvByCurve["first-id"], Does.Contain("X\tY\tSEM"));
                Assert.That(csvByCurve["first-id"], Does.Contain("0\t1\t0"));
                Assert.That(csvByCurve["first-id"], Does.Contain("1\t3\t0"));
                Assert.That(csvByCurve["second-id"], Does.Contain("1\t-2\t0"));
                Assert.That(svg, Does.Contain("<svg"));
                Assert.That(svg, Does.Contain("Synthetic graph"));
                Assert.That(svg, Does.Contain("Time (ms)"));
                Assert.That(svg, Does.Contain("Signal (uV)"));
                Assert.That(svg, Does.Contain("first-id"));
                Assert.That(svg, Does.Contain("second-id"));

                graph.SetEnabled("second-id", false);

                Assert.That(graph.GetEnabledCurvesName(), Is.EqualTo(new[] { "First" }));
                Assert.That(graph.ToCSV().Keys, Is.EqualTo(new[] { "first-id" }));
                Assert.That(graph.ToSVG(), Does.Contain("first-id").And.Not.Contain("second-id"));
            }
            finally
            {
                Object.Destroy(firstData);
                Object.Destroy(secondData);
            }
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public async Task LocalizersGraphsWorker_VoxelWithoutSelectedBlocs_ReturnsEmptyWithoutSceneAccess()
        {
            LocalizersGraphsWorker worker = new();
            int progressCalls = 0;

            Dictionary<HBP.Data.Informations.ChannelStruct, List<LocalizerCurveData>> result = await worker.GenerateLocalizersGraphsVoxelAsync("synthetic", new List<ProtocolItem>(), new RescalingParameters(false, 0f, 1f, 0f), (_, _, _) => progressCalls++, CancellationToken.None);

            Assert.That(result, Is.Empty);
            Assert.That(progressCalls, Is.Zero);
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public async Task LocalizersGraphsWorker_RegionWithoutSelectedBlocs_ReturnsEmptyWithoutSceneAccess()
        {
            LocalizersGraphsWorker worker = new();
            int progressCalls = 0;

            Dictionary<HBP.Data.Informations.ChannelStruct, List<LocalizerCurveData>> result = await worker.GenerateLocalizersGraphsRegionAsync(1, "synthetic", new List<ProtocolItem>(), new RescalingParameters(false, 0f, 1f, 0f), (_, _, _) => progressCalls++, CancellationToken.None);

            Assert.That(result, Is.Empty);
            Assert.That(progressCalls, Is.Zero);
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public async Task LocalizersGraphsWorker_VoxelWithSyntheticLocalizerVolume_BuildsRescaledCurves()
        {
            using PlayModeSceneScope scene = new("InformationGraphLocalizersVoxel");
            HBP.Core.Object3D.LocalizersObjects previousLocalizers = HBP.Core.Object3D.Object3DManager.Localizers;
            HBP.Core.Object3D.Object3DManager.Localizers = new HBP.Core.Object3D.LocalizersObjects();

            try
            {
                const string protocolName = "synthetic-protocol";
                const string dataType = "synthetic-data";
                const string blocName = "synthetic-bloc";
                AddSyntheticLocalizerBloc(protocolName, dataType, blocName);

                Patient patient = new("patient-localizer", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<HBP.Core.Data.Site>(), Array.Empty<BaseTagValue>(), string.Empty, "patient-localizer-id");
                ObjectSite site = CreateRuntimeSite(scene, "A1", patient, new Vector3(1, 0, 0));
                SyntheticLocalizersGraphsWorker worker = new(new[] { site });
                ProtocolItem protocolItem = CreateSelectedProtocolItem(scene, protocolName, blocName);

                Dictionary<ChannelStruct, List<LocalizerCurveData>> result = await worker.GenerateLocalizersGraphsVoxelAsync(dataType, new List<ProtocolItem> { protocolItem }, new RescalingParameters(true, 1f, 2f, -1f), null, CancellationToken.None);

                ChannelStruct channel = new(site.Information.Name, patient);
                Assert.That(result, Does.ContainKey(channel));
                Assert.That(result[channel], Has.Count.EqualTo(1));
                LocalizerCurveData curve = result[channel].Single();
                Assert.That(curve.DataType, Is.EqualTo(dataType));
                Assert.That(curve.ProtocolName, Is.EqualTo(protocolName));
                Assert.That(curve.BlocName, Is.EqualTo(blocName));
                Assert.That(curve.Points.Select(point => point.x), Is.EqualTo(new[] { -1f, 0f, 1f }));
                Assert.That(curve.Points.Select(point => point.y), Is.EqualTo(new[] { 0f, 2f, 4f }));
                Assert.That(curve.SEM, Is.Null);
            }
            finally
            {
                HBP.Core.Object3D.Object3DManager.Localizers = previousLocalizers;
            }
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public async Task LocalizersGraphsWorker_RegionWithSyntheticLocalizerVolume_BuildsMeanAndSemCurves()
        {
            using PlayModeSceneScope scene = new("InformationGraphLocalizersRegion");
            HBP.Core.Object3D.LocalizersObjects previousLocalizers = HBP.Core.Object3D.Object3DManager.Localizers;
            HBP.Core.Object3D.Object3DManager.Localizers = new HBP.Core.Object3D.LocalizersObjects();

            try
            {
                const string protocolName = "synthetic-protocol";
                const string dataType = "synthetic-data";
                const string blocName = "synthetic-bloc";
                AddSyntheticLocalizerBloc(protocolName, dataType, blocName);

                Patient patient = new("patient-region", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<HBP.Core.Data.Site>(), Array.Empty<BaseTagValue>(), string.Empty, "patient-region-id");
                ObjectSite site = CreateRuntimeSite(scene, "R1", patient, new Vector3(2, 0, 0));
                SyntheticLocalizersGraphsWorker worker = new(new[] { site });
                ProtocolItem protocolItem = CreateSelectedProtocolItem(scene, protocolName, blocName);

                Dictionary<ChannelStruct, List<LocalizerCurveData>> result = await worker.GenerateLocalizersGraphsRegionAsync(1, dataType, new List<ProtocolItem> { protocolItem }, new RescalingParameters(false, 0f, 1f, 0f), null, CancellationToken.None);

                LocalizerCurveData curve = result[new ChannelStruct(site.Information.Name, patient)].Single();
                Assert.That(curve.Points.Select(point => point.y), Is.EqualTo(new[] { 10f, 20f, 30f }));
                Assert.That(curve.SEM, Has.Length.EqualTo(3));
                Assert.That(curve.SEM.All(value => value > 0f), Is.True);
            }
            finally
            {
                HBP.Core.Object3D.Object3DManager.Localizers = previousLocalizers;
            }
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        [Category("NativeDll")]
        public async Task LocalizersGraphsWorker_VoxelWithNativeLocalizerFixture_BuildsCurveFromNiftiVolume()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("InformationGraphNativeLocalizersVoxel");
            HBP.Core.Object3D.LocalizersObjects previousLocalizers = HBP.Core.Object3D.Object3DManager.Localizers;
            HBP.Core.Object3D.Object3DManager.Localizers = new HBP.Core.Object3D.LocalizersObjects();
            HBP.Core.Object3D.FMRI fmri = new("bloc-alpha", NativeFixturePath("Localizers", "protocol-alpha", "signal-alpha", "bloc-alpha.nii"), NativeFixturePath("Localizers", "protocol-alpha", "signal-alpha", "bloc-alpha_MASK.nii"), false);

            try
            {
                CopyDirectory(NativeFixturePath("Localizers"), Path.Combine(ApplicationState.DataPath, "Atlases", "Localizers"));
                await ExecuteNativeOrIgnoreAsync(async () => await fmri.LoadAsync(), "native localizer NIfTI");

                const string protocolName = "protocol-alpha";
                const string dataType = "signal-alpha";
                const string blocName = "bloc-alpha";
                AddLoadedLocalizerBloc(protocolName, dataType, blocName, fmri);

                Patient patient = new("native-localizer-patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<HBP.Core.Data.Site>(), Array.Empty<BaseTagValue>(), string.Empty, "information-graph-native-localizer-patient-001");
                ObjectSite site = CreateRuntimeSite(scene, "N1", patient, new Vector3(2, 2, 2));
                NativeLocalizersGraphsWorker worker = new(new[] { site });
                ProtocolItem protocolItem = CreateSelectedProtocolItem(scene, protocolName, blocName);

                Dictionary<ChannelStruct, List<LocalizerCurveData>> result = await worker.GenerateLocalizersGraphsVoxelAsync(dataType, new List<ProtocolItem> { protocolItem }, new RescalingParameters(false, 0f, 1f, 0f), null, CancellationToken.None);

                ChannelStruct channel = new(site.Information.Name, patient);
                Assert.That(result, Does.ContainKey(channel));
                LocalizerCurveData curve = result[channel].Single();
                Assert.That(curve.DataType, Is.EqualTo(dataType));
                Assert.That(curve.ProtocolName, Is.EqualTo(protocolName));
                Assert.That(curve.BlocName, Is.EqualTo(blocName));
                Assert.That(curve.Points, Is.Not.Empty);
                Assert.That(curve.Points.All(point => !float.IsNaN(point.y) && !float.IsInfinity(point.y)), Is.True);
            }
            finally
            {
                HBP.Core.Object3D.Object3DManager.Localizers.Protocols.Clear();
                fmri.Clean();
                HBP.Core.Object3D.Object3DManager.Localizers = previousLocalizers;
            }
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public async Task LocalizersGraphsWorker_AtlasWithSyntheticAtlasData_BuildsMaskedRegionCurves()
        {
            using PlayModeSceneScope scene = new("InformationGraphLocalizersAtlas");
            HBP.Core.Object3D.LocalizersObjects previousLocalizers = HBP.Core.Object3D.Object3DManager.Localizers;
            HBP.Core.Object3D.Object3DManager.Localizers = new HBP.Core.Object3D.LocalizersObjects();

            try
            {
                const string protocolName = "synthetic-protocol";
                const string dataType = "synthetic-data";
                const string blocName = "synthetic-bloc";
                AddSyntheticLocalizerBloc(protocolName, dataType, blocName);

                Patient patient = new("patient-atlas", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<HBP.Core.Data.Site>(), Array.Empty<BaseTagValue>(), string.Empty, "patient-atlas-id");
                ObjectSite site = CreateRuntimeSite(scene, "AT1", patient, new Vector3(3, 0, 0));
                SyntheticLocalizersGraphsWorker worker = new(new[] { site });
                ProtocolItem protocolItem = CreateSelectedProtocolItem(scene, protocolName, blocName);

                Dictionary<ChannelStruct, List<LocalizerCurveData>> result = await worker.GenerateLocalizersGraphsAtlasAsync(LocalizersGraphsAtlas.MarsAtlas, dataType, new List<ProtocolItem> { protocolItem }, new RescalingParameters(true, 10f, 2f, 1f), null, CancellationToken.None);

                LocalizerCurveData curve = result[new ChannelStruct(site.Information.Name, patient)].Single();
                Assert.That(curve.Points.Select(point => point.y), Is.EqualTo(new[] { 15f, 35f, 55f }));
                Assert.That(curve.SEM, Has.Length.EqualTo(3));
                Assert.That(curve.SEM.All(value => value > 0f), Is.True);
            }
            finally
            {
                HBP.Core.Object3D.Object3DManager.Localizers = previousLocalizers;
            }
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public void GraphsGrid_DisplaySyntheticIEEGColumn_CreatesSelectableGraphAndEmitsRequests()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("InformationGraphGraphsGrid");
            IEEGGraphFixture fixture = CreateInjectedIEGGraphFixture();
            try
            {
                GraphsGrid grid = CreateGraphsGridHarness(scene);
                HBP.Data.Informations.Column column = new("Column A", new HBP.Data.Informations.IEEGData(fixture.Dataset, fixture.DataInfo.Name, fixture.Bloc), Array.Empty<ChannelStructsGroup>());
                ChannelStruct channel = new(fixture.Channel, fixture.Patient);
                ChannelStruct[] displayRequests = null;
                ChannelStruct[] filterRequests = null;
                Graph.Curve[] setCurves = null;
                grid.OnRequestDisplayChannelsOnGraph.AddListener(channels => displayRequests = channels);
                grid.OnRequestFilterChannels.AddListener(channels => filterRequests = channels);
                grid.OnSetGraphs.AddListener(curves => setCurves = curves);

                grid.Display(new[] { channel }, new[] { column });

                Assert.That(grid.Graphs, Has.Count.EqualTo(1));
                Assert.That(grid.NumberOfGridLines, Is.EqualTo(1));
                Assert.That(grid.Graphs[0].Title, Is.EqualTo("A1 (patient-grid)"));
                Assert.That(grid.Graphs[0].ChannelStruct, Is.EqualTo(channel));
                Assert.That(grid.Graphs[0].Curves, Has.Count.EqualTo(1));
                Assert.That(grid.Graphs[0].Curves[0].Data.Points.Select(point => point.y), Is.EqualTo(new[] { 30f, 31f }));
                Assert.That(setCurves, Has.Length.EqualTo(1));

                grid.Graphs[0].IsSelected = true;
                grid.DisplaySelectedGraphs();
                grid.FilterSelectedSites();

                Assert.That(displayRequests, Is.EqualTo(new[] { channel }));
                Assert.That(filterRequests, Is.EqualTo(new[] { channel }));

                grid.UnselectAll();
                Assert.That(grid.Graphs[0].IsSelected, Is.False);
            }
            finally
            {
                DataManager.Clear();
            }
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public void TrialMatrixGrid_DisplaySyntheticIEEGData_RendersChannelsBlocsCellsAndSelectionMasks()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("InformationGraphTrialMatrixGrid");
            IEEGTrialMatrixFixture fixture = CreateInjectedTrialMatrixFixture();
            Texture2D colormap = CreateColormap();

            try
            {
                PersistentDataManager.UserPreferences.Visualization.TrialMatrix.TrialSmoothing = false;
                UITrialMatrixGrid grid = CreateTrialMatrixGridHarness(scene);
                ChannelStruct channel = new(fixture.Channel, fixture.Patient);
                HBP.Data.Informations.TrialMatrix.TrialMatrixGrid.IEEGTrialMatrixData dataStruct = new(fixture.Dataset, fixture.DataInfo.Name, fixture.Protocol.OrderedBlocs.ToList());

                grid.Display(new HBP.Data.Informations.TrialMatrix.TrialMatrixGrid(new[] { channel }, new HBP.Data.Informations.TrialMatrix.TrialMatrixGrid.TrialMatrixData[] { dataStruct }), colormap);

                Assert.That(grid.Data, Has.Count.EqualTo(1));
                UITrialMatrixData renderedData = grid.Data[0];
                Assert.That(renderedData.Title, Is.EqualTo("dataset-trial-matrix trial-matrix-ieeg"));
                Assert.That(renderedData.Blocs, Has.Count.EqualTo(1));

                UITrialMatrixBloc renderedBloc = renderedData.Blocs[0];
                Assert.That(renderedBloc.ChannelBlocs, Has.Count.EqualTo(1));
                UITrialMatrixChannelBloc renderedChannelBloc = renderedBloc.ChannelBlocs[0];
                Assert.That(renderedChannelBloc.Title, Is.EqualTo("A1 (patient-trial-matrix)"));
                Assert.That(renderedChannelBloc.TrialIsSelected, Is.EqualTo(new[] { true, true }));
                Assert.That(renderedChannelBloc.SubBlocs, Has.Count.EqualTo(1));
                Assert.That(renderedChannelBloc.MainSubBloc.Data.SubTrials, Has.Length.EqualTo(2));

                RawImage subBlocImage = renderedChannelBloc.MainSubBloc.GetComponentInChildren<RawImage>();
                Assert.That(subBlocImage.texture, Is.Not.Null);
                Assert.That(subBlocImage.texture.width, Is.EqualTo(2));
                Assert.That(subBlocImage.texture.height, Is.EqualTo(2));

                int selectionEvents = 0;
                renderedChannelBloc.OnChangeTrialSelected.AddListener(() => selectionEvents++);
                renderedChannelBloc.TrialIsSelected = new[] { true, false };

                RectTransform selectionContainer = GetPrivateField<RectTransform>(renderedChannelBloc, "m_SelectionContainer");
                Assert.That(selectionEvents, Is.EqualTo(1));
                Assert.That(selectionContainer.childCount, Is.EqualTo(1));
            }
            finally
            {
                Object.Destroy(colormap);
                DataManager.Clear();
            }
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public void GraphZone_DisplayUsesTrialMatrixSelectionAndUpdatesCurveWhenSelectionChanges()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("InformationGraphGraphZone");
            IEEGTrialMatrixFixture fixture = CreateInjectedTrialMatrixFixture();
            Texture2D colormap = CreateColormap();

            try
            {
                PersistentDataManager.UserPreferences.Visualization.TrialMatrix.TrialSmoothing = false;
                UITrialMatrixGrid trialMatrixGrid = CreateTrialMatrixGridHarness(scene);
                ChannelStruct channel = new(fixture.Channel, fixture.Patient);
                HBP.Data.Informations.TrialMatrix.TrialMatrixGrid.IEEGTrialMatrixData dataStruct = new(fixture.Dataset, fixture.DataInfo.Name, fixture.Protocol.OrderedBlocs.ToList());
                trialMatrixGrid.Display(new HBP.Data.Informations.TrialMatrix.TrialMatrixGrid(new[] { channel }, new HBP.Data.Informations.TrialMatrix.TrialMatrixGrid.TrialMatrixData[] { dataStruct }), colormap);

                GraphZone graphZone = CreateGraphZoneHarness(scene, trialMatrixGrid);
                graphZone.CreateGraphPool(1);
                HBP.Data.Informations.Column column = new("Column A", new HBP.Data.Informations.IEEGData(fixture.Dataset, fixture.DataInfo.Name, fixture.Bloc), Array.Empty<ChannelStructsGroup>());

                graphZone.Display(new[] { channel }, new[] { column });

                List<Graph> graphs = GetPrivateField<List<Graph>>(graphZone, "m_Graphs");
                Assert.That(graphs, Has.Count.EqualTo(1));
                Graph.Curve channelCurve = FlattenCurves(graphs[0].Curves).Single(curve => curve.Name == "A1");
                Assert.That(channelCurve.Data, Is.Not.Null);

                UITrialMatrixChannelBloc renderedChannelBloc = trialMatrixGrid.Data[0].Blocs[0].ChannelBlocs[0];
                renderedChannelBloc.TrialIsSelected = new[] { true, false };
                Assert.That(channelCurve.Data.Points.Select(point => point.y), Is.EqualTo(new[] { 10f, 12f }));

                renderedChannelBloc.TrialIsSelected = new[] { false, true };

                Assert.That(channelCurve.Data.Points.Select(point => point.y), Is.EqualTo(new[] { 20f, 22f }));
            }
            finally
            {
                Object.Destroy(colormap);
                DataManager.Clear();
            }
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public void TrialMatrixZone_DisplayVisibleColumnsBuildsGridAndPreservesCustomLimits()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("InformationGraphTrialMatrixZone");
            IEEGTrialMatrixFixture fixture = CreateInjectedTrialMatrixFixture();
            Texture2D colormap = CreateColormap();

            try
            {
                PersistentDataManager.UserPreferences.Visualization.TrialMatrix.ShowWholeProtocol = false;
                PersistentDataManager.UserPreferences.Visualization.TrialMatrix.TrialSmoothing = false;
                UITrialMatrixGrid trialMatrixGrid = CreateTrialMatrixGridHarness(scene);
                trialMatrixGrid.Colormap = colormap;
                TrialMatrixZone zone = CreateTrialMatrixZoneHarness(scene, trialMatrixGrid);
                ChannelStruct channel = new(fixture.Channel, fixture.Patient);
                HBP.Data.Informations.IEEGData columnData = new(fixture.Dataset, fixture.DataInfo.Name, fixture.Bloc);

                zone.Display(new[] { channel }, new HBP.Data.Informations.Data[] { columnData });

                Assert.That(trialMatrixGrid.gameObject.activeSelf, Is.True);
                Assert.That(trialMatrixGrid.Data, Has.Count.EqualTo(1));
                Assert.That(trialMatrixGrid.Data[0].GridData.DataStruct.Blocs, Is.EqualTo(new[] { fixture.Bloc }));

                trialMatrixGrid.Data[0].UseDefaultLimits = false;
                trialMatrixGrid.Data[0].Limits = new Vector2(-5f, 5f);

                zone.Display(new[] { channel }, new HBP.Data.Informations.Data[] { columnData });

                Assert.That(trialMatrixGrid.Data, Has.Count.EqualTo(1));
                Assert.That(trialMatrixGrid.Data[0].UseDefaultLimits, Is.False);
                Assert.That(trialMatrixGrid.Data[0].Limits, Is.EqualTo(new Vector2(-5f, 5f)));
            }
            finally
            {
                Object.Destroy(colormap);
                DataManager.Clear();
            }
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public void TrialMatrixExplorerGrid_DisplaySyntheticData_RendersTitleAndMatrixData()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("InformationGraphTrialMatrixExplorerGrid");
            IEEGTrialMatrixFixture fixture = CreateInjectedTrialMatrixFixture();
            Texture2D colormap = CreateColormap();

            try
            {
                PersistentDataManager.UserPreferences.Visualization.TrialMatrix.TrialSmoothing = false;
                DBTrialMatrixGrid grid = CreateExplorerTrialMatrixGridHarness(scene);
                ChannelStruct channel = new(fixture.Channel, fixture.Patient);
                HBP.Data.Informations.TrialMatrix.TrialMatrixGrid.IEEGTrialMatrixData dataStruct = new(fixture.Dataset, fixture.DataInfo.Name, fixture.Protocol.OrderedBlocs.ToList());

                grid.Display(new HBP.Data.Informations.TrialMatrix.TrialMatrixGrid(new[] { channel }, new HBP.Data.Informations.TrialMatrix.TrialMatrixGrid.TrialMatrixData[] { dataStruct }), "patient-trial-matrix - protocol-trial-matrix - trial-matrix-ieeg - A1", colormap);

                RectTransform titleContainer = GetPrivateField<RectTransform>(grid, "m_TitleHeaderContainer");
                Assert.That(titleContainer.childCount, Is.EqualTo(1));
                Assert.That(titleContainer.GetChild(0).GetComponentInChildren<Text>().text, Does.Contain("trial-matrix-ieeg - A1"));
                Assert.That(grid.Data, Has.Count.EqualTo(1));
                Assert.That(grid.Data[0].GridData.DataStruct, Is.SameAs(dataStruct));
                Assert.That(grid.Data[0].Blocs[0].ChannelBlocs[0].MainSubBloc.GetComponentInChildren<RawImage>().texture, Is.Not.Null);
            }
            finally
            {
                Object.Destroy(colormap);
                DataManager.Clear();
            }
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public void TrialMatrixExplorerInformationPanels_RespectPatientAndSiteTagDisplaySettings()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSceneScope scene = new("InformationGraphTrialMatrixExplorerInformationPanels");

            BoolTag patientTag = new("Included", "information-graph-info-patient-tag-001");
            StringTag siteTag = new("Location", "information-graph-info-site-tag-001");
            PersistentDataManager.Tags.SetPatientTags(new BaseTag[] { patientTag }, false);
            PersistentDataManager.Tags.SetSiteTags(new BaseTag[] { siteTag }, false);

            Site site = new("A1", new[] { new Coordinate("information-graph-space", Vector3.one, "information-graph-info-coordinate-001") }, new BaseTagValue[] { new StringTagValue(siteTag, "temporal", "information-graph-info-site-tag-value-001") }, "information-graph-info-site-001");
            Patient patient = new("patient-info", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), new[] { site }, new BaseTagValue[] { new BoolTagValue(patientTag, true, "information-graph-info-patient-tag-value-001") }, string.Empty, "information-graph-info-patient-001");

            DBInformationPanels panels = CreateInformationPanelsHarness(scene);
            DBTagDisplaySettingsContextMenu patientMenu = GetPrivateField<DBTagDisplaySettingsContextMenu>(panels, "m_PatientTagDisplaySettingsContextMenu");
            DBTagDisplaySettingsContextMenu siteMenu = GetPrivateField<DBTagDisplaySettingsContextMenu>(panels, "m_SiteTagDisplaySettingsContextMenu");
            Text patientText = GetPrivateField<Text>(panels, "m_PatientInformationText");
            Text siteText = GetPrivateField<Text>(panels, "m_SiteInformationText");

            patientMenu.SelectAll();
            siteMenu.SelectAll();
            panels.Set(new ChannelStruct("A1", patient));

            Assert.That(patientText.text, Does.Contain("Included"));
            Assert.That(patientText.text, Does.Contain("True"));
            Assert.That(siteText.text, Does.Contain("Location"));
            Assert.That(siteText.text, Does.Contain("temporal"));

            siteMenu.DeselectAll();
            panels.Refresh();

            Assert.That(patientText.text, Does.Contain("Included"));
            Assert.That(siteText.text, Is.EqualTo("No site information available."));
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public async Task DisplayGraphSection_ApplyRequestsFilteredSitesGraphWithStoredName()
        {
            using PlayModeSceneScope scene = new("InformationGraphDisplayGraphSection");
            using PlayModeModule3DTestHarness module3D = new(scene.Scene);
            DisplayGraphSection section = CreateDisplayGraphSectionHarness(scene, module3D.Scene);
            section.ApplyFor = ApplyFor.FilteredSites;
            GetPrivateField<InputField>(section, "m_NameInputField").text = "information-graph filtered graph";

            module3D.SourceSiteA.State.IsFiltered = true;
            module3D.SourceSiteB.State.IsFiltered = false;
            module3D.SourceSiteA.State.IsMasked = false;

            string requestedName = null;
            List<ObjectSite> requestedSites = null;
            module3D.Scene.OnRequestFilteredSitesGraph.AddListener((name, sites) =>
            {
                requestedName = name;
                requestedSites = sites.ToList();
            });

            await section.ApplyAsync();

            Assert.That(requestedName, Is.EqualTo("information-graph filtered graph"));
            Assert.That(requestedSites, Is.EqualTo(new[] { module3D.SourceSiteA }));

            section.StoreSettings();
            GetPrivateField<InputField>(section, "m_NameInputField").text = string.Empty;
            section.LoadSettings();
            Assert.That(GetPrivateField<InputField>(section, "m_NameInputField").text, Is.EqualTo("information-graph filtered graph"));
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public async Task OpenTrialMatrixExplorerSection_ApplyRequestsExplorerForFilteredSitesAndSelectedData()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            TestOpenTrialMatrixExplorerSection section = null;
            ObjectSite site = null;

            try
            {
                IEEGTrialMatrixFixture fixture = CreateInjectedTrialMatrixFixture();
                section = CreateOpenTrialMatrixExplorerSectionHarness();
                site = CreateRuntimeSite("A1", fixture.Patient, new Vector3(1, 0, 0));
                section.TestSites = new List<ObjectSite> { site };
                Dropdown dataNameDropdown = GetPrivateField<Dropdown>(section, "m_DataNameDropdown");
                dataNameDropdown.options = new List<Dropdown.OptionData> { new(fixture.DataInfo.Name) };
                dataNameDropdown.value = 0;
                SetPrivateField(section, "m_IEEGDataInfos", new List<IEEGDataInfo> { fixture.DataInfo });

                await section.ApplyAsync();

                Assert.That(section.OpenedDataName, Is.EqualTo(fixture.DataInfo.Name));
                Assert.That(section.OpenedDataInfos, Is.Not.Null);
                Assert.That(section.OpenedDataInfos.Single(), Is.SameAs(fixture.DataInfo));
                Assert.That(section.OpenedChannels, Is.Not.Null);
                Assert.That(section.OpenedChannels.Single().Channel, Is.EqualTo(site.Information.Name));
                Assert.That(section.OpenedChannels.Single().Patient, Is.SameAs(site.Information.Patient));
            }
            catch (Exception exception)
            {
                Assert.Fail(exception.ToString());
            }
            finally
            {
                if (section != null) Object.Destroy(section.gameObject);
                if (site != null) Object.Destroy(site.gameObject);
                DataManager.Clear();
            }
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public void TrialMatrixActionsContextMenu_AddCurrentPatientWithoutSelectionReportsError()
        {
            using PlayModeSceneScope scene = new("InformationGraphTrialMatrixActionsContextMenu");
            GameObject displayerObject = new("TrialMatrixDisplayer");
            displayerObject.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(displayerObject, scene.Scene);
            DBTrialMatrixDisplayer displayer = displayerObject.AddComponent<DBTrialMatrixDisplayer>();

            GameObject menuObject = new("TrialMatrixActionsContextMenu");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(menuObject, scene.Scene);
            TestTrialMatrixActionsContextMenu menu = menuObject.AddComponent<TestTrialMatrixActionsContextMenu>();
            SetPrivateField(menu, "m_TrialMatrixDisplayer", displayer);
            menuObject.SetActive(true);

            menu.AddCurrentPatientToProjectGroup();

            Assert.That(menuObject.activeSelf, Is.False);
            Assert.That(menu.LastDialogTitle, Is.EqualTo("No patient selected"));
            Assert.That(menu.LastDialogMessage, Does.Contain("select a patient"));
        }

        [Test]
        [Category("PlayMode.InformationGraph")]
        public void LocalizersPanel_RescalingControlsUpdateParametersFormulaAndRejectInvalidGain()
        {
            using PlayModeSceneScope scene = new("InformationGraphLocalizersPanelRescaling");
            LocalizersPanel panel = CreateLocalizersPanelRescalingHarness(scene);
            Toggle enableToggle = GetPrivateField<Toggle>(panel, "m_EnableRescalingToggle");
            InputField baselineInput = GetPrivateField<InputField>(panel, "m_BaselineValueInputField");
            InputField gainInput = GetPrivateField<InputField>(panel, "m_GainFactorInputField");
            InputField offsetInput = GetPrivateField<InputField>(panel, "m_OffsetInputField");
            Text formulaText = GetPrivateField<Text>(panel, "m_RescalingFormulaText");
            Transform rescalingContainer = GetPrivateField<Transform>(panel, "m_RescalingContainer");

            InvokePrivate(panel, "InitializeRescaling");

            Assert.That(panel.EnableRescaling, Is.False);
            Assert.That(rescalingContainer.gameObject.activeSelf, Is.False);
            Assert.That(formulaText.text, Is.EqualTo("No rescaling applied"));

            enableToggle.isOn = true;
            baselineInput.onEndEdit.Invoke("2.5");
            gainInput.onEndEdit.Invoke("3");
            offsetInput.onEndEdit.Invoke("-1");

            Assert.That(panel.EnableRescaling, Is.True);
            Assert.That(rescalingContainer.gameObject.activeSelf, Is.True);
            Assert.That(panel.BaselineValue, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(panel.GainFactor, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(panel.Offset, Is.EqualTo(-1f).Within(0.0001f));
            Assert.That(formulaText.text, Does.Contain("2.5").Or.Contain("2,5"));
            Assert.That(formulaText.text, Does.Contain("3"));
            Assert.That(formulaText.text, Does.Contain("-1"));

            gainInput.onEndEdit.Invoke("0");

            Assert.That(panel.GainFactor, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(gainInput.text, Is.EqualTo("3"));
        }

        private static void InitializeSerializedEvents(MonoBehaviour component)
        {
            foreach (FieldInfo field in component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (typeof(UnityEventBase).IsAssignableFrom(field.FieldType) && field.GetValue(component) == null)
                {
                    field.SetValue(component, Activator.CreateInstance(field.FieldType));
                }
            }
        }

        private static void AddSyntheticLocalizerBloc(string protocolName, string dataType, string blocName)
        {
            HBP.Core.Object3D.LocalizerProtocol protocol = new(protocolName, string.Empty, false);
            HBP.Core.Object3D.LocalizerData data = new(dataType, string.Empty, false);
            HBP.Core.Object3D.LocalizerBloc bloc = (HBP.Core.Object3D.LocalizerBloc)FormatterServices.GetUninitializedObject(typeof(HBP.Core.Object3D.LocalizerBloc));
            SetAutoProperty(bloc, "Name", blocName);
            data.Blocs.Add(bloc);
            protocol.Datas.Add(data);
            HBP.Core.Object3D.Object3DManager.Localizers.Protocols.Add(protocol);
        }

        private static void AddLoadedLocalizerBloc(string protocolName, string dataType, string blocName, HBP.Core.Object3D.FMRI fmri)
        {
            HBP.Core.Object3D.LocalizerProtocol protocol = new(protocolName, string.Empty, false);
            HBP.Core.Object3D.LocalizerData data = new(dataType, string.Empty, false);
            HBP.Core.Object3D.LocalizerBloc bloc = (HBP.Core.Object3D.LocalizerBloc)FormatterServices.GetUninitializedObject(typeof(HBP.Core.Object3D.LocalizerBloc));
            SetAutoProperty(bloc, "Name", blocName);
            SetAutoProperty(bloc, "FMRI", fmri);
            data.Blocs.Add(bloc);
            protocol.Datas.Add(data);
            HBP.Core.Object3D.Object3DManager.Localizers.Protocols.Add(protocol);
        }

        private static ProtocolItem CreateSelectedProtocolItem(PlayModeSceneScope scene, string protocolName, string blocName)
        {
            GameObject protocolObject = new("ProtocolItem", typeof(RectTransform));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(protocolObject, scene.Scene);
            ProtocolItem protocolItem = protocolObject.AddComponent<ProtocolItem>();
            Text protocolText = CreateTextChild(protocolObject.transform, "ProtocolName");
            protocolText.text = protocolName;
            SetPrivateField(protocolItem, "m_ProtocolNameText", protocolText);

            GameObject blocObject = new("BlocItem", typeof(RectTransform));
            blocObject.transform.SetParent(protocolObject.transform);
            BlocItem blocItem = blocObject.AddComponent<BlocItem>();
            Text blocText = CreateTextChild(blocObject.transform, "BlocName");
            blocText.text = blocName;
            Toggle toggle = CreateToggle("Selected", blocObject.transform);
            toggle.isOn = true;
            SetPrivateField(blocItem, "m_BlocNameText", blocText);
            SetPrivateField(blocItem, "m_Toggle", toggle);
            SetPrivateField(protocolItem, "m_Blocs", new List<BlocItem> { blocItem });
            return protocolItem;
        }

        private static ObjectSite CreateRuntimeSite(PlayModeSceneScope scene, string name, Patient patient, Vector3 position)
        {
            GameObject siteObject = new($"Site_{name}");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(siteObject, scene.Scene);
            return ConfigureRuntimeSite(siteObject, name, patient, position);
        }

        private static ObjectSite CreateRuntimeSite(string name, Patient patient, Vector3 position)
        {
            GameObject siteObject = new($"Site_{name}");
            return ConfigureRuntimeSite(siteObject, name, patient, position);
        }

        private static ObjectSite ConfigureRuntimeSite(GameObject siteObject, string name, Patient patient, Vector3 position)
        {
            ObjectSite site = siteObject.AddComponent<ObjectSite>();
            site.Information = new HBP.Core.Object3D.SiteInformation
            {
                SiteData = new HBP.Core.Data.Site(name, new[] { new Coordinate("synthetic-space", position, $"coordinate-{name}") }, Array.Empty<BaseTagValue>(), $"site-{name}"),
                Patient = patient,
                Name = name,
                Index = 0,
                DefaultPosition = position
            };
            site.State = new HBP.Core.Object3D.SiteState();
            site.Configuration = new SiteConfiguration();
            return site;
        }

        private static GraphsGrid CreateGraphsGridHarness(PlayModeSceneScope scene)
        {
            GameObject gridObject = new("GraphsGrid");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gridObject, scene.Scene);
            GraphsGrid grid = gridObject.AddComponent<GraphsGrid>();
            InitializeSerializedEvents(grid);

            GameObject scrollObject = new("ScrollRect", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(gridObject.transform);
            ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
            RectTransform viewport = CreateRectTransformChild(scrollObject.transform, "Viewport");
            RectTransform content = CreateRectTransformChild(viewport, "Content");
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            GameObject containerPrefab = CreateGraphsGridContainerPrefab();
            SetPrivateField(grid, "m_ItemAndContainerPrefab", containerPrefab);
            SetPrivateField(grid, "m_ScrollRect", scrollRect);
            SetPrivateField(grid, "m_UseDefaultOrdinateRange", true);
            SetPrivateField(grid, "m_AbscissaDisplayRange", new Vector2(0f, 2f));
            return grid;
        }

        private static GameObject CreateGraphsGridContainerPrefab()
        {
            GameObject containerObject = new("GraphContainerPrefab", typeof(RectTransform));
            GraphsGridContainer container = containerObject.AddComponent<GraphsGridContainer>();
            GameObject graphObject = new("SimpleGraph", typeof(RectTransform));
            graphObject.transform.SetParent(containerObject.transform);
            SimpleGraph graph = graphObject.AddComponent<SimpleGraph>();
            InitializeSerializedEvents(graph);
            container.Content = graphObject;
            return containerObject;
        }

        private static RectTransform CreateRectTransformChild(Transform parent, string name)
        {
            GameObject child = new(name, typeof(RectTransform));
            child.transform.SetParent(parent);
            RectTransform rectTransform = child.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(600, 400);
            return rectTransform;
        }

        private static UITrialMatrixGrid CreateTrialMatrixGridHarness(PlayModeSceneScope scene)
        {
            GameObject gridObject = new("TrialMatrixGrid", typeof(RectTransform));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gridObject, scene.Scene);
            UITrialMatrixGrid grid = gridObject.AddComponent<UITrialMatrixGrid>();

            RectTransform dataContainer = CreateRectTransformChild(gridObject.transform, "DataContainer");
            RectTransform channelHeaderContainer = CreateRectTransformChild(gridObject.transform, "ChannelHeaderContainer");
            SetPrivateField(grid, "m_DataContainer", dataContainer);
            SetPrivateField(grid, "m_DataPrefab", CreateTrialMatrixDataPrefab());
            SetPrivateField(grid, "m_ChannelHeaderContainer", channelHeaderContainer);
            SetPrivateField(grid, "m_ChannelHeaderPrefab", CreateChannelHeaderPrefab());
            return grid;
        }

        private static DBTrialMatrixGrid CreateExplorerTrialMatrixGridHarness(PlayModeSceneScope scene)
        {
            GameObject gridObject = new("ExplorerTrialMatrixGrid", typeof(RectTransform));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gridObject, scene.Scene);
            DBTrialMatrixGrid grid = gridObject.AddComponent<DBTrialMatrixGrid>();

            RectTransform dataContainer = CreateRectTransformChild(gridObject.transform, "DataContainer");
            RectTransform titleHeaderContainer = CreateRectTransformChild(gridObject.transform, "TitleHeaderContainer");
            SetPrivateField(grid, "m_DataContainer", dataContainer);
            SetPrivateField(grid, "m_DataPrefab", CreateTrialMatrixDataPrefab());
            SetPrivateField(grid, "m_TitleHeaderContainer", titleHeaderContainer);
            SetPrivateField(grid, "m_TitleHeaderPrefab", CreateTitleHeaderPrefab());
            return grid;
        }

        private static GameObject CreateTitleHeaderPrefab()
        {
            GameObject headerObject = new("TitleHeaderPrefab", typeof(RectTransform));
            CreateTextChild(headerObject.transform, "Title");
            return headerObject;
        }

        private static DBInformationPanels CreateInformationPanelsHarness(PlayModeSceneScope scene)
        {
            GameObject panelsObject = new("InformationPanels", typeof(RectTransform));
            panelsObject.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(panelsObject, scene.Scene);
            DBInformationPanels panels = panelsObject.AddComponent<DBInformationPanels>();
            SetPrivateField(panels, "m_PatientInformationText", CreateTextChild(panelsObject.transform, "PatientText"));
            SetPrivateField(panels, "m_PatientTagDisplaySettingsContextMenu", CreateTagDisplaySettingsContextMenu(panelsObject.transform, DBTagDisplaySettingsContextMenu.TagsType.Patient));
            SetPrivateField(panels, "m_SiteInformationText", CreateTextChild(panelsObject.transform, "SiteText"));
            SetPrivateField(panels, "m_SiteTagDisplaySettingsContextMenu", CreateTagDisplaySettingsContextMenu(panelsObject.transform, DBTagDisplaySettingsContextMenu.TagsType.Site));
            panelsObject.SetActive(true);
            return panels;
        }

        private static DBTagDisplaySettingsContextMenu CreateTagDisplaySettingsContextMenu(Transform parent, DBTagDisplaySettingsContextMenu.TagsType tagsType)
        {
            GameObject menuObject = new(tagsType + "TagDisplaySettings", typeof(RectTransform));
            menuObject.SetActive(false);
            menuObject.transform.SetParent(parent);
            DBTagDisplaySettingsContextMenu menu = menuObject.AddComponent<DBTagDisplaySettingsContextMenu>();
            SetPrivateField(menu, "m_TagSelectionItemParent", CreateRectTransformChild(menuObject.transform, "Items"));
            SetPrivateField(menu, "m_TagSelectionItemPrefab", CreateTagSelectionItemPrefab());
            SetPrivateField(menu, "m_TagsType", tagsType);
            menuObject.SetActive(true);
            return menu;
        }

        private static GameObject CreateTagSelectionItemPrefab()
        {
            GameObject itemObject = new("TagSelectionItemPrefab", typeof(RectTransform));
            DBTagSelectionItem item = itemObject.AddComponent<DBTagSelectionItem>();
            SetPrivateField(item, "m_Text", CreateTextChild(itemObject.transform, "Label"));
            GameObject toggleObject = new("Toggle", typeof(RectTransform), typeof(Toggle));
            toggleObject.transform.SetParent(itemObject.transform);
            SetPrivateField(item, "m_Toggle", toggleObject.GetComponent<Toggle>());
            return itemObject;
        }

        private static DisplayGraphSection CreateDisplayGraphSectionHarness(PlayModeSceneScope scene, Base3DScene baseScene)
        {
            GameObject sectionObject = new("DisplayGraphSection", typeof(RectTransform));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sectionObject, scene.Scene);
            DisplayGraphSection section = sectionObject.AddComponent<DisplayGraphSection>();
            GameObject inputObject = new("NameInput", typeof(RectTransform), typeof(InputField));
            inputObject.transform.SetParent(sectionObject.transform);
            SetPrivateField(section, "m_NameInputField", inputObject.GetComponent<InputField>());
            section.Scene = baseScene;
            return section;
        }

        private static TestOpenTrialMatrixExplorerSection CreateOpenTrialMatrixExplorerSectionHarness(PlayModeSceneScope scene, Base3DScene baseScene)
        {
            GameObject sectionObject = new("OpenTrialMatrixExplorerSection", typeof(RectTransform));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sectionObject, scene.Scene);
            TestOpenTrialMatrixExplorerSection section = sectionObject.AddComponent<TestOpenTrialMatrixExplorerSection>();
            GameObject dataSourceDropdownObject = new("DataSourceDropdown", typeof(RectTransform), typeof(Dropdown));
            dataSourceDropdownObject.transform.SetParent(sectionObject.transform);
            GameObject dataNameDropdownObject = new("DataNameDropdown", typeof(RectTransform), typeof(Dropdown));
            dataNameDropdownObject.transform.SetParent(sectionObject.transform);
            SetPrivateField(section, "m_DataSourceDropdown", dataSourceDropdownObject.GetComponent<Dropdown>());
            SetPrivateField(section, "m_DataNameDropdown", dataNameDropdownObject.GetComponent<Dropdown>());
            section.Scene = baseScene;
            return section;
        }

        private static TestOpenTrialMatrixExplorerSection CreateOpenTrialMatrixExplorerSectionHarness()
        {
            GameObject sectionObject = new("OpenTrialMatrixExplorerSection", typeof(RectTransform));
            TestOpenTrialMatrixExplorerSection section = sectionObject.AddComponent<TestOpenTrialMatrixExplorerSection>();
            GameObject dataSourceDropdownObject = new("DataSourceDropdown", typeof(RectTransform), typeof(Dropdown));
            dataSourceDropdownObject.transform.SetParent(sectionObject.transform);
            GameObject dataNameDropdownObject = new("DataNameDropdown", typeof(RectTransform), typeof(Dropdown));
            dataNameDropdownObject.transform.SetParent(sectionObject.transform);
            SetPrivateField(section, "m_DataSourceDropdown", dataSourceDropdownObject.GetComponent<Dropdown>());
            SetPrivateField(section, "m_DataNameDropdown", dataNameDropdownObject.GetComponent<Dropdown>());
            return section;
        }

        private static LocalizersPanel CreateLocalizersPanelRescalingHarness(PlayModeSceneScope scene)
        {
            GameObject panelObject = new("LocalizersPanel", typeof(RectTransform));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(panelObject, scene.Scene);
            LocalizersPanel panel = panelObject.AddComponent<LocalizersPanel>();
            SetPrivateField(panel, "m_RescalingContainer", CreateRectTransformChild(panelObject.transform, "RescalingContainer"));
            SetPrivateField(panel, "m_EnableRescalingToggle", CreateToggle("EnableRescaling", panelObject.transform));
            SetPrivateField(panel, "m_BaselineValueInputField", CreateInputField("Baseline", panelObject.transform));
            SetPrivateField(panel, "m_GainFactorInputField", CreateInputField("Gain", panelObject.transform));
            SetPrivateField(panel, "m_OffsetInputField", CreateInputField("Offset", panelObject.transform));
            SetPrivateField(panel, "m_RescalingFormulaText", CreateTextChild(panelObject.transform, "Formula"));
            return panel;
        }

        private static Toggle CreateToggle(string name, Transform parent)
        {
            GameObject toggleObject = new(name, typeof(RectTransform), typeof(Toggle));
            toggleObject.transform.SetParent(parent);
            return toggleObject.GetComponent<Toggle>();
        }

        private static InputField CreateInputField(string name, Transform parent)
        {
            GameObject inputObject = new(name, typeof(RectTransform), typeof(InputField));
            inputObject.transform.SetParent(parent);
            return inputObject.GetComponent<InputField>();
        }

        private static GameObject CreateTrialMatrixDataPrefab()
        {
            GameObject dataObject = new("DataPrefab", typeof(RectTransform), typeof(LayoutElement));
            UITrialMatrixData data = dataObject.AddComponent<UITrialMatrixData>();
            InitializeSerializedEvents(data);

            SetPrivateField(data, "m_BlocContainer", CreateRectTransformChild(dataObject.transform, "BlocContainer"));
            SetPrivateField(data, "m_BlocPrefab", CreateTrialMatrixBlocPrefab());
            SetPrivateField(data, "m_TimeLegendContainer", CreateRectTransformChild(dataObject.transform, "TimeLegendContainer"));
            SetPrivateField(data, "m_TimeLegendPrefab", CreateTimeLegendPrefab());
            SetPrivateField(data, "m_LayoutElement", dataObject.GetComponent<LayoutElement>());
            return dataObject;
        }

        private static GameObject CreateTrialMatrixBlocPrefab()
        {
            GameObject blocObject = new("BlocPrefab", typeof(RectTransform));
            UITrialMatrixBloc bloc = blocObject.AddComponent<UITrialMatrixBloc>();
            InitializeSerializedEvents(bloc);

            SetPrivateField(bloc, "m_ChannelBlocContainer", CreateRectTransformChild(blocObject.transform, "ChannelBlocContainer"));
            SetPrivateField(bloc, "m_ChannelBlocPrefab", CreateChannelBlocPrefab());
            return blocObject;
        }

        private static GameObject CreateChannelBlocPrefab()
        {
            GameObject channelBlocObject = new("ChannelBlocPrefab", typeof(RectTransform));
            UITrialMatrixChannelBloc channelBloc = channelBlocObject.AddComponent<UITrialMatrixChannelBloc>();
            InitializeSerializedEvents(channelBloc);

            SetPrivateField(channelBloc, "m_SubBlocContainer", CreateRectTransformChild(channelBlocObject.transform, "SubBlocContainer"));
            SetPrivateField(channelBloc, "m_SubBlocPrefab", CreateSubBlocPrefab());
            SetPrivateField(channelBloc, "m_SelectionContainer", CreateRectTransformChild(channelBlocObject.transform, "SelectionContainer"));
            SetPrivateField(channelBloc, "m_SelectionPrefab", new GameObject("SelectionMaskPrefab", typeof(RectTransform), typeof(Image)));
            return channelBlocObject;
        }

        private static GameObject CreateSubBlocPrefab()
        {
            GameObject subBlocObject = new("SubBlocPrefab", typeof(RectTransform), typeof(LayoutElement), typeof(EventTrigger));
            UITrialMatrixSubBloc subBloc = subBlocObject.AddComponent<UITrialMatrixSubBloc>();
            InitializeSerializedEvents(subBloc);

            LayoutElement leftFiller = CreateRectTransformChild(subBlocObject.transform, "LeftFiller").gameObject.AddComponent<LayoutElement>();
            RectTransform mainTexture = CreateRectTransformChild(subBlocObject.transform, "MainTexture");
            LayoutElement mainTextureLayout = mainTexture.gameObject.AddComponent<LayoutElement>();
            RawImage rawImage = mainTexture.gameObject.AddComponent<RawImage>();
            LayoutElement rightFiller = CreateRectTransformChild(subBlocObject.transform, "RightFiller").gameObject.AddComponent<LayoutElement>();
            RectTransform eventContainer = CreateRectTransformChild(subBlocObject.transform, "EventContainer");

            SetPrivateField(subBloc, "m_RawImage", rawImage);
            SetPrivateField(subBloc, "m_MainTextureLayoutElement", mainTextureLayout);
            SetPrivateField(subBloc, "m_LeftFillerLayoutElement", leftFiller);
            SetPrivateField(subBloc, "m_RightFillerLayoutElement", rightFiller);
            SetPrivateField(subBloc, "m_EventPrefab", new GameObject("EventPrefab", typeof(RectTransform), typeof(Image)));
            SetPrivateField(subBloc, "m_EventContainer", eventContainer);
            SetPrivateField(subBloc, "m_LayoutElement", subBlocObject.GetComponent<LayoutElement>());
            return subBlocObject;
        }

        private static GameObject CreateChannelHeaderPrefab()
        {
            GameObject headerObject = new("ChannelHeaderPrefab", typeof(RectTransform));
            UITrialMatrixChannelHeader header = headerObject.AddComponent<UITrialMatrixChannelHeader>();
            Text text = CreateTextChild(headerObject.transform, "Label");
            SetPrivateField(header, "m_Text", text);
            return headerObject;
        }

        private static GameObject CreateTimeLegendPrefab()
        {
            GameObject legendObject = new("TimeLegendPrefab", typeof(RectTransform));
            UITrialMatrixTimeLegend legend = legendObject.AddComponent<UITrialMatrixTimeLegend>();
            SetPrivateField(legend, "m_TimeBlocPrefab", CreateTimeBlocPrefab());
            return legendObject;
        }

        private static GameObject CreateTimeBlocPrefab()
        {
            GameObject timeBlocObject = new("TimeBlocPrefab", typeof(RectTransform), typeof(LayoutElement));
            UITrialMatrixTimeBloc timeBloc = timeBlocObject.AddComponent<UITrialMatrixTimeBloc>();
            SetPrivateField(timeBloc, "m_StartText", CreateTextChild(timeBlocObject.transform, "Start"));
            SetPrivateField(timeBloc, "m_EndText", CreateTextChild(timeBlocObject.transform, "End"));
            SetPrivateField(timeBloc, "m_LayoutElement", timeBlocObject.GetComponent<LayoutElement>());
            return timeBlocObject;
        }

        private static Text CreateTextChild(Transform parent, string name)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent);
            return textObject.GetComponent<Text>();
        }

        private static GraphZone CreateGraphZoneHarness(PlayModeSceneScope scene, UITrialMatrixGrid trialMatrixGrid)
        {
            GameObject graphZoneObject = new("GraphZone", typeof(RectTransform));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(graphZoneObject, scene.Scene);
            GraphZone graphZone = graphZoneObject.AddComponent<GraphZone>();

            RectTransform graphContainer = CreateRectTransformChild(graphZoneObject.transform, "GraphContainer");
            RectTransform toggleContainer = CreateRectTransformChild(graphZoneObject.transform, "ToggleContainer");
            toggleContainer.gameObject.AddComponent<ToggleGroup>();
            SetPrivateField(graphZone, "m_TrialMatrixGrid", trialMatrixGrid);
            SetPrivateField(graphZone, "m_GraphContainer", graphContainer);
            SetPrivateField(graphZone, "m_GraphPrefab", CreateGraphPrefab());
            SetPrivateField(graphZone, "m_ToggleContainer", toggleContainer);
            SetPrivateField(graphZone, "m_TogglesPrefab", CreateTogglePrefab());
            return graphZone;
        }

        private static TrialMatrixZone CreateTrialMatrixZoneHarness(PlayModeSceneScope scene, UITrialMatrixGrid trialMatrixGrid)
        {
            GameObject zoneObject = new("TrialMatrixZone", typeof(RectTransform));
            zoneObject.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(zoneObject, scene.Scene);
            TrialMatrixZone zone = zoneObject.AddComponent<TrialMatrixZone>();
            SetPrivateField(zone, "m_TrialMatrixGrid", trialMatrixGrid);
            zoneObject.SetActive(true);
            return zone;
        }

        private static GameObject CreateGraphPrefab()
        {
            GameObject graphObject = new("GraphPrefab", typeof(RectTransform));
            Graph graph = graphObject.AddComponent<Graph>();
            InitializeSerializedEvents(graph);
            return graphObject;
        }

        private static GameObject CreateTogglePrefab()
        {
            GameObject toggleObject = new("TogglePrefab", typeof(RectTransform), typeof(Toggle));
            CreateTextChild(toggleObject.transform, "Label");
            return toggleObject;
        }

        private static Texture2D CreateColormap()
        {
            Texture2D texture = new(2, 1, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.blue, Color.red });
            texture.Apply();
            return texture;
        }

        private static IEnumerable<Graph.Curve> FlattenCurves(IEnumerable<Graph.Curve> curves)
        {
            foreach (Graph.Curve curve in curves)
            {
                yield return curve;
                foreach (Graph.Curve subCurve in FlattenCurves(curve.SubCurves))
                {
                    yield return subCurve;
                }
            }
        }

        private static IEEGGraphFixture CreateInjectedIEGGraphFixture()
        {
            Patient patient = new("patient-grid", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), Array.Empty<BaseTagValue>(), string.Empty, "information-graph-grid-patient-001");
            CoreSubBloc subBloc = new("response", 0, MainSecondaryEnum.Main, new TimeWindow(0, 2), new TimeWindow(0, 1), new[] { new CoreEvent("stim", new[] { 1 }, MainSecondaryEnum.Main, "information-graph-grid-event-001") }, Array.Empty<CoreIcon>(), Array.Empty<Treatment>(), "information-graph-grid-subbloc-001");
            CoreBloc bloc = new("grid-response-bloc", 0, string.Empty, "response_stim_CODE", new[] { subBloc }, "information-graph-grid-bloc-001");
            Protocol protocol = new("protocol-grid", new[] { bloc }, "information-graph-grid-protocol-001");
            IEEGDataInfo dataInfo = new("grid-ieeg", protocol, new Elan(), Array.Empty<Error>(), Array.Empty<Warning>(), patient, NormalizationType.None, "information-graph-db", "information-graph-grid-ieeg-001");
            Dataset dataset = new("dataset-grid", protocol, new[] { dataInfo }, "information-graph-grid-dataset-001");

            BlocData blocData = (BlocData)FormatterServices.GetUninitializedObject(typeof(BlocData));
            blocData.Frequency = new Frequency(1000);
            blocData.Trials = new[]
            {
                new Trial(new Dictionary<CoreSubBloc, HBP.Core.Data.SubTrial> { { subBloc, CreateCoreSubTrial(subBloc, new[] { 30f, 31f }) } })
            };

            CoreIEEGData data = (CoreIEEGData)FormatterServices.GetUninitializedObject(typeof(CoreIEEGData));
            data.DataByBloc = new Dictionary<CoreBloc, BlocData> { { bloc, blocData } };
            data.UnitByChannel = new Dictionary<string, string> { { "A1", "uV" } };
            data.Frequency = new Frequency(1000);

            AddCacheEntry("m_DataByRequest", CreateRequest("Request", dataInfo), data);
            AddCacheEntry("m_BlocDataByRequest", CreateRequest("BlocRequest", dataInfo, bloc), blocData);

            return new IEEGGraphFixture
            {
                Patient = patient,
                Dataset = dataset,
                DataInfo = dataInfo,
                Bloc = bloc,
                Channel = "A1"
            };
        }

        private static IEEGTrialMatrixFixture CreateInjectedTrialMatrixFixture()
        {
            Patient patient = new("patient-trial-matrix", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), Array.Empty<BaseTagValue>(), string.Empty, "information-graph-trial-matrix-patient-001");
            CoreSubBloc subBloc = new("response", 0, MainSecondaryEnum.Main, new TimeWindow(0, 2), new TimeWindow(0, 1), new[] { new CoreEvent("stim", new[] { 1 }, MainSecondaryEnum.Main, "information-graph-trial-matrix-event-001") }, Array.Empty<CoreIcon>(), Array.Empty<Treatment>(), "information-graph-trial-matrix-subbloc-001");
            CoreBloc bloc = new("trial-matrix-response-bloc", 0, string.Empty, "response_stim_CODE", new[] { subBloc }, "information-graph-trial-matrix-bloc-001");
            Protocol protocol = new("protocol-trial-matrix", new[] { bloc }, "information-graph-trial-matrix-protocol-001");
            IEEGDataInfo dataInfo = new("trial-matrix-ieeg", protocol, new Elan(), Array.Empty<Error>(), Array.Empty<Warning>(), patient, NormalizationType.None, "information-graph-db", "information-graph-trial-matrix-ieeg-001");
            Dataset dataset = new("dataset-trial-matrix", protocol, new[] { dataInfo }, "information-graph-trial-matrix-dataset-001");

            BlocData blocData = (BlocData)FormatterServices.GetUninitializedObject(typeof(BlocData));
            blocData.Frequency = new Frequency(1000);
            blocData.Trials = new[]
            {
                new Trial(new Dictionary<CoreSubBloc, HBP.Core.Data.SubTrial> { { subBloc, CreateCoreSubTrial(subBloc, new[] { 10f, 12f }) } }),
                new Trial(new Dictionary<CoreSubBloc, HBP.Core.Data.SubTrial> { { subBloc, CreateCoreSubTrial(subBloc, new[] { 20f, 22f }) } })
            };

            CoreIEEGData data = (CoreIEEGData)FormatterServices.GetUninitializedObject(typeof(CoreIEEGData));
            data.DataByBloc = new Dictionary<CoreBloc, BlocData> { { bloc, blocData } };
            data.UnitByChannel = new Dictionary<string, string> { { "A1", "uV" } };
            data.Frequency = new Frequency(1000);

            AddCacheEntry("m_DataByRequest", CreateRequest("Request", dataInfo), data);
            AddCacheEntry("m_BlocDataByRequest", CreateRequest("BlocRequest", dataInfo, bloc), blocData);

            return new IEEGTrialMatrixFixture
            {
                Patient = patient,
                Protocol = protocol,
                Dataset = dataset,
                DataInfo = dataInfo,
                Bloc = bloc,
                Channel = "A1"
            };
        }

        private static HBP.Core.Data.SubTrial CreateCoreSubTrial(CoreSubBloc subBloc, float[] values)
        {
            float[] baselineValues = { 0f, 0f };
            var informationsByEvent = new Dictionary<CoreEvent, EventInformation>
            {
                {
                    subBloc.MainEvent,
                    new EventInformation(new[]
                    {
                        new EventInformation.EventOccurence(subBloc.MainEvent.Codes[0], 0, 0, 0, 10f, 10f, 0)
                    })
                }
            };
            EpochDescriptor descriptor = new(new EpochRange(0, values.Length - 1), new EpochRange(values.Length, values.Length + baselineValues.Length - 1), 0, 0, 0, informationsByEvent);
            return new HBP.Core.Data.SubTrial(new Dictionary<string, float[]> { { "A1", values.Concat(baselineValues).ToArray() } }, new Dictionary<string, string> { { "A1", "uV" } }, descriptor, subBloc, new Frequency(1000));
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = GetFieldInHierarchy(target.GetType(), fieldName);
            field.SetValue(target, value);
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            FieldInfo field = GetFieldInHierarchy(target.GetType(), $"<{propertyName}>k__BackingField");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = GetFieldInHierarchy(target.GetType(), fieldName);
            return (T)field.GetValue(target);
        }

        private static FieldInfo GetFieldInHierarchy(Type type, string fieldName)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null) return field;
            }

            throw new MissingFieldException(type.FullName, fieldName);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, Array.Empty<object>());
        }

        private static void AddCacheEntry(string fieldName, object key, object value)
        {
            GetCache(fieldName).Add(key, value);
        }

        private static IDictionary GetCache(string fieldName)
        {
            FieldInfo field = typeof(DataManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            return (IDictionary)field.GetValue(null);
        }

        private static object CreateRequest(string nestedTypeName, params object[] args)
        {
            Type requestType = typeof(DataManager).GetNestedType(nestedTypeName, BindingFlags.NonPublic);
            return Activator.CreateInstance(requestType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null);
        }

        private static async Task ExecuteNativeOrIgnoreAsync(Func<Task> action, string context)
        {
            try
            {
                await action();
            }
            catch (Exception exception) when (IsMissingNativeDependency(exception))
            {
                Assert.Ignore($"Native dependency unavailable for {context}: {exception.Message}");
            }
        }

        private static bool IsMissingNativeDependency(Exception exception)
        {
            for (Exception current = exception; current != null; current = current.InnerException)
            {
                if (current is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
                {
                    return true;
                }
            }

            return false;
        }

        private static string NativeFixturePath(params string[] parts)
        {
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Assets", "Tests", "Fixtures", "Native");
            foreach (string part in parts)
            {
                path = Path.Combine(path, part);
            }

            return path;
        }

        private static void CopyDirectory(string sourceDirectory, string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);
            foreach (string directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(directory.Replace(sourceDirectory, targetDirectory));
            }

            foreach (string file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                File.Copy(file, file.Replace(sourceDirectory, targetDirectory), true);
            }
        }

        private sealed class SyntheticLocalizersGraphsWorker : LocalizersGraphsWorker
        {
            private readonly List<ObjectSite> m_Sites;

            public SyntheticLocalizersGraphsWorker(IEnumerable<ObjectSite> sites)
            {
                m_Sites = sites.ToList();
            }

            protected override List<ObjectSite> GetSceneSites()
            {
                return m_Sites;
            }

            protected override bool TryGetSelectedAtlas(LocalizersGraphsAtlas atlas, out HBP.Core.DLL.BrainAtlas selectedAtlas)
            {
                selectedAtlas = null;
                return true;
            }

            protected override bool IsAtlasLoaded(HBP.Core.DLL.BrainAtlas atlas)
            {
                return true;
            }

            protected override void LoadAtlas(HBP.Core.DLL.BrainAtlas atlas)
            {
            }

            protected override int GetClosestAreaIndex(HBP.Core.DLL.BrainAtlas atlas, Vector3 position, int precision)
            {
                return position.x >= 0 ? 7 : -1;
            }

            protected override Vector3[] GetAreaCoordinates(HBP.Core.DLL.BrainAtlas atlas, int regionIndex)
            {
                return new[] { new Vector3(1, 0, 0), new Vector3(3, 0, 0), new Vector3(-1, 0, 0) };
            }

            protected override bool IsInsideMask(Vector3 position, HBP.Core.Object3D.LocalizerBloc bloc)
            {
                return position.x >= 0;
            }

            protected override float GetMaskValue(Vector3 position, HBP.Core.Object3D.LocalizerBloc bloc)
            {
                return position.x >= 0 ? 1f : 0f;
            }

            protected override int GetVolumeCount(HBP.Core.Object3D.LocalizerBloc bloc)
            {
                return 3;
            }

            protected override float GetVolumeValue(Vector3 voxel, HBP.Core.Object3D.LocalizerBloc bloc, int volumeIndex)
            {
                return (volumeIndex + 1) * 10f + voxel.x;
            }

            protected override float[] GetTimes(HBP.Core.Object3D.LocalizerBloc bloc)
            {
                return new[] { -1f, 0f, 1f };
            }

            protected override float[] GetVoxelData(Vector3 voxel, HBP.Core.Object3D.LocalizerBloc bloc)
            {
                return new[] { voxel.x, voxel.x + 1f, voxel.x + 2f };
            }

            protected override (float[], float[][]) GetRegionData(Vector3 voxel, HBP.Core.Object3D.LocalizerBloc bloc, int precision)
            {
                return (new[] { 10f, 20f, 30f }, new[]
                {
                    new[] { 8f, 12f },
                    new[] { 18f, 22f },
                    new[] { 28f, 32f }
                });
            }
        }

        private sealed class NativeLocalizersGraphsWorker : LocalizersGraphsWorker
        {
            private readonly List<ObjectSite> m_Sites;

            public NativeLocalizersGraphsWorker(IEnumerable<ObjectSite> sites)
            {
                m_Sites = sites.ToList();
            }

            protected override List<ObjectSite> GetSceneSites()
            {
                return m_Sites;
            }

            protected override bool IsInsideMask(Vector3 position, HBP.Core.Object3D.LocalizerBloc bloc)
            {
                return true;
            }
        }

        private sealed class TestOpenTrialMatrixExplorerSection : OpenTrialMatrixExplorerSection
        {
            [NonSerialized] public List<ChannelStruct> OpenedChannels;
            [NonSerialized] public List<IEEGDataInfo> OpenedDataInfos;
            [NonSerialized] public string OpenedDataName;
            [NonSerialized] public List<ObjectSite> TestSites = new();

            protected override List<ObjectSite> Sites => TestSites;

            protected override void OpenTrialMatrixExplorer(List<ChannelStruct> channelStructs, List<IEEGDataInfo> dataInfos, string dataName)
            {
                OpenedChannels = channelStructs;
                OpenedDataInfos = dataInfos;
                OpenedDataName = dataName;
            }
        }

        private sealed class TestTrialMatrixActionsContextMenu : DBTrialMatrixActionsContextMenu
        {
            public string LastDialogTitle;
            public string LastDialogMessage;

            protected override void OpenDialog(DialogBoxType type, string title, string message)
            {
                LastDialogTitle = title;
                LastDialogMessage = message;
            }
        }

        private sealed class IEEGGraphFixture
        {
            public Patient Patient;
            public Dataset Dataset;
            public IEEGDataInfo DataInfo;
            public CoreBloc Bloc;
            public string Channel;
        }

        private sealed class IEEGTrialMatrixFixture
        {
            public Patient Patient;
            public Protocol Protocol;
            public Dataset Dataset;
            public IEEGDataInfo DataInfo;
            public CoreBloc Bloc;
            public string Channel;
        }
    }
}
