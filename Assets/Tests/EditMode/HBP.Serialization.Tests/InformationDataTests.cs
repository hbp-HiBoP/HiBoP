using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.Core.Enums;
using HBP.Core.Errors;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Data.Informations;
using HBP.Data.Informations.Graphs;
using HBP.Data.Informations.TrialMatrix;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CoreBloc = HBP.Core.Data.Bloc;
using CoreCCEPData = HBP.Core.Data.CCEPData;
using CoreEvent = HBP.Core.Data.Event;
using CoreIEEGData = HBP.Core.Data.IEEGData;
using CoreSubBloc = HBP.Core.Data.SubBloc;
using TrialSubBloc = HBP.Data.Informations.TrialMatrix.SubBloc;
using TrialSubTrial = HBP.Data.Informations.TrialMatrix.SubTrial;
using UnityObject = UnityEngine.Object;

namespace HBP.Tests.Serialization
{
    public class InformationDataTests
    {
        [TearDown]
        public void TearDown()
        {
            DataManager.Clear();
        }

        [Test]
        public void CurveData_CreatesStableCurveFromSyntheticPoints()
        {
            Vector2[] points = { new(0, 1), new(1, 3), new(2, -2) };
            CurveData curve = CurveData.CreateInstance(points, Color.cyan, 2.5f);

            try
            {
                curve.Label = "Synthetic curve";

                Assert.That(curve.Label, Is.EqualTo("Synthetic curve"));
                Assert.That(curve.Color, Is.EqualTo(Color.cyan));
                Assert.That(curve.Thickness, Is.EqualTo(2.5f));
                Assert.That(curve.Points, Is.EqualTo(points));
            }
            finally
            {
                UnityObject.DestroyImmediate(curve);
            }
        }

        [Test]
        public void ShapedCurveData_AcceptsEnumerableShapesAndFallsBackWhenSemHidden()
        {
            using TempDirectoryScope temp = new();
            using PersistentDataTestScope persistentData = new(temp.Path);
            Vector2[] points = { new(0, 0), new(1, 1), new(2, 4) };
            List<float> shapes = new() { 0.1f, 0.2f, 0.3f };
            PersistentDataManager.UserPreferences.Visualization.Graph.ShowSEM = true;

            CurveData shaped = ShapedCurveData.CreateInstance(points, shapes, Color.magenta, 4.0f);

            try
            {
                Assert.That(shaped, Is.TypeOf<ShapedCurveData>());
                Assert.That(((ShapedCurveData)shaped).Shapes, Is.EqualTo(shapes));
                Assert.That(shaped.Points, Is.EqualTo(points));
                Assert.That(shaped.Color, Is.EqualTo(Color.magenta));
                Assert.That(shaped.Thickness, Is.EqualTo(4.0f));
            }
            finally
            {
                UnityObject.DestroyImmediate(shaped);
            }

            PersistentDataManager.UserPreferences.Visualization.Graph.ShowSEM = false;
            CurveData unshaped = ShapedCurveData.CreateInstance(points, shapes, Color.yellow, 1.0f);

            try
            {
                Assert.That(unshaped, Is.TypeOf<CurveData>());
                Assert.That(unshaped, Is.Not.TypeOf<ShapedCurveData>());
                Assert.That(unshaped.Points, Is.EqualTo(points));
            }
            finally
            {
                UnityObject.DestroyImmediate(unshaped);
            }
        }

        [Test]
        public void GraphAndTrialMatrixPreferences_CloneAndJsonRoundTripPreserveSettings()
        {
            using TempDirectoryScope temp = new();
            UserPreferences preferences = new();
            preferences.Visualization.Graph.ShowCurvesOfMinimizedColumns = false;
            preferences.Visualization.Graph.ShowSEM = false;
            preferences.Visualization.Graph.SiteColors.SetColor(0, 0, Color.red);
            preferences.Visualization.Graph.UpdateMaxDimensions(3, 9, 2);
            preferences.Visualization.TrialMatrix.ShowWholeProtocol = false;
            preferences.Visualization.TrialMatrix.TrialsSynchronization = false;
            preferences.Visualization.TrialMatrix.TrialSmoothing = false;
            preferences.Visualization.TrialMatrix.NumberOfIntermediateValues = 7;
            preferences.Visualization.TrialMatrix.SubBlocFormat = BlocFormatType.ProtocolRatio;
            preferences.Visualization.TrialMatrix.TrialHeight = 17;
            preferences.Visualization.TrialMatrix.TrialRatio = 0.02f;
            preferences.Visualization.TrialMatrix.BlocRatio = 0.4f;
            preferences.Visualization.TrialMatrix.ProtocolRatio = 1.2f;

            string path = temp.GetPath("information-data-preferences.json");
            Assert.That(ClassLoaderSaver.SaveToJSon(preferences, path, true), Is.True);
            UserPreferences loaded = ClassLoaderSaver.LoadFromJson<UserPreferences>(path);

            Assert.That(loaded.Visualization.Graph.ShowCurvesOfMinimizedColumns, Is.False);
            Assert.That(loaded.Visualization.Graph.ShowSEM, Is.False);
            Assert.That(loaded.Visualization.Graph.MaxSites, Is.EqualTo(3));
            Assert.That(loaded.Visualization.Graph.MaxColumns, Is.EqualTo(9));
            Assert.That(loaded.Visualization.Graph.MaxGroups, Is.EqualTo(2));
            Assert.That(loaded.Visualization.Graph.SiteColors.GetColor(0, 0), Is.EqualTo(Color.red));
            Assert.That(loaded.Visualization.TrialMatrix.ShowWholeProtocol, Is.False);
            Assert.That(loaded.Visualization.TrialMatrix.TrialsSynchronization, Is.False);
            Assert.That(loaded.Visualization.TrialMatrix.TrialSmoothing, Is.False);
            Assert.That(loaded.Visualization.TrialMatrix.NumberOfIntermediateValues, Is.EqualTo(7));
            Assert.That(loaded.Visualization.TrialMatrix.SubBlocFormat, Is.EqualTo(BlocFormatType.ProtocolRatio));
            Assert.That(loaded.Visualization.TrialMatrix.TrialHeight, Is.EqualTo(17));
            Assert.That(loaded.Visualization.TrialMatrix.TrialRatio, Is.EqualTo(0.02f).Within(0.0001f));
            Assert.That(loaded.Visualization.TrialMatrix.BlocRatio, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(loaded.Visualization.TrialMatrix.ProtocolRatio, Is.EqualTo(1.2f).Within(0.0001f));

            GraphPreferences clonedGraph = loaded.Visualization.Graph.Clone() as GraphPreferences;
            TrialMatrixPreferences clonedTrialMatrix = loaded.Visualization.TrialMatrix.Clone() as TrialMatrixPreferences;

            Assert.That(clonedGraph.SiteColors.GetColor(0, 0), Is.EqualTo(Color.red));
            Assert.That(clonedGraph.MaxColumns, Is.EqualTo(9));
            Assert.That(clonedTrialMatrix.NumberOfIntermediateValues, Is.EqualTo(7));
            Assert.That(clonedTrialMatrix.SubBlocFormat, Is.EqualTo(BlocFormatType.ProtocolRatio));
        }

        [Test]
        public void TrialMatrixGrid_DataStructEqualityUsesDatasetAndName()
        {
            using TempDirectoryScope temp = new();
            using PersistentDataTestScope persistentData = new(temp.Path);
            Patient patient = CreatePatient("patient-a");
            CoreBloc bloc = CreateBloc("bloc-a", 0, CreateSubBloc("main", 0, -100, 200));
            Protocol protocol = new("protocol-a", new[] { bloc });
            Dataset dataset = new("dataset-a", protocol, new DataInfo[0]);
            Dataset sameDataset = dataset;
            Dataset otherDataset = new("dataset-b", protocol, new DataInfo[0]);
            ChannelStruct source = new("S1", patient);

            TrialMatrixGrid.TrialMatrixData first = new TrialMatrixGrid.IEEGTrialMatrixData(dataset, "data", new List<CoreBloc> { bloc });
            TrialMatrixGrid.TrialMatrixData same = new TrialMatrixGrid.IEEGTrialMatrixData(sameDataset, "data", new List<CoreBloc> { bloc });
            TrialMatrixGrid.TrialMatrixData differentName = new TrialMatrixGrid.IEEGTrialMatrixData(dataset, "other", new List<CoreBloc> { bloc });
            TrialMatrixGrid.TrialMatrixData differentDataset = new TrialMatrixGrid.IEEGTrialMatrixData(otherDataset, "data", new List<CoreBloc> { bloc });
            TrialMatrixGrid.CCEPTrialMatrixData ccep = new(dataset, "data", new List<CoreBloc> { bloc }, source);

            Assert.That(first, Is.EqualTo(same));
            Assert.That(first == same, Is.True);
            Assert.That(first != differentName, Is.True);
            Assert.That(first, Is.Not.EqualTo(differentDataset));
            Assert.That(ccep.Source, Is.EqualTo(source));
            Assert.That(ccep.Blocs, Is.EqualTo(new[] { bloc }));
        }

        [Test]
        public void TrialMatrixData_CreatesFallbackSubBlocsWhenChannelDataIsMissing()
        {
            using TempDirectoryScope temp = new();
            using PersistentDataTestScope persistentData = new(temp.Path);
            Patient patient = CreatePatient("patient-a");
            CoreSubBloc firstSubBloc = CreateSubBloc("baseline", 0, -100, 0);
            CoreSubBloc secondSubBloc = CreateSubBloc("response", 1, 0, 250);
            CoreBloc bloc = CreateBloc("bloc-a", 0, firstSubBloc, secondSubBloc);
            Protocol protocol = new("protocol-a", new[] { bloc });
            Dataset dataset = new("dataset-a", protocol, new DataInfo[0]);
            ChannelStruct channel = new("A1", patient);
            TrialMatrixGrid.IEEGTrialMatrixData dataStruct = new(dataset, "missing-data", new List<CoreBloc> { bloc });

            TrialMatrixGrid grid = new(new[] { channel }, new TrialMatrixGrid.TrialMatrixData[] { dataStruct });

            Assert.That(grid.Channels, Is.EqualTo(new[] { channel }));
            Assert.That(grid.DataStructs, Is.EqualTo(new[] { dataStruct }));
            Assert.That(grid.Data, Has.Length.EqualTo(1));
            Assert.That(grid.Data[0].Title, Is.EqualTo("dataset-a missing-data"));
            Assert.That(grid.Data[0].Blocs, Has.Length.EqualTo(1));
            Assert.That(grid.Data[0].Blocs[0].Title, Is.EqualTo("bloc-a"));
            Assert.That(grid.Data[0].Blocs[0].ChannelBlocs, Has.Length.EqualTo(1));
            Assert.That(grid.Data[0].Blocs[0].ChannelBlocs[0].IsFound, Is.False);
            Assert.That(grid.Data[0].Blocs[0].ChannelBlocs[0].SubBlocs, Has.Length.EqualTo(2));
            Assert.That(grid.Data[0].Blocs[0].ChannelBlocs[0].SubBlocs[0].SubBlocProtocol, Is.SameAs(firstSubBloc));
            Assert.That(grid.Data[0].Blocs[0].ChannelBlocs[0].SubBlocs[0].SubTrials, Is.Empty);
            Assert.That(grid.Data[0].Limits, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void TrialMatrixData_UsesInjectedIEEGChannelDataWhenAvailable()
        {
            using TempDirectoryScope temp = new();
            using PersistentDataTestScope persistentData = new(temp.Path);
            IEEGEpochFixture fixture = CreateInjectedIEEGFixture();
            Dataset dataset = new("dataset-a", fixture.Protocol, new[] { fixture.DataInfo });
            ChannelStruct channel = new(fixture.Channel, fixture.Patient);
            TrialMatrixGrid.IEEGTrialMatrixData dataStruct = new(dataset, fixture.DataInfo.Name, fixture.Protocol.OrderedBlocs.ToList());

            TrialMatrixGrid grid = new(new[] { channel }, new TrialMatrixGrid.TrialMatrixData[] { dataStruct });

            Assert.That(grid.Data, Has.Length.EqualTo(1));
            Assert.That(grid.Data[0].Blocs, Has.Length.EqualTo(1));
            Assert.That(grid.Data[0].Blocs[0].ChannelBlocs, Has.Length.EqualTo(1));

            ChannelBloc channelBloc = grid.Data[0].Blocs[0].ChannelBlocs[0];
            Assert.That(channelBloc.IsFound, Is.True);
            Assert.That(channelBloc.SubBlocs, Has.Length.EqualTo(1));
            Assert.That(channelBloc.SubBlocs[0].SubTrials, Has.Length.EqualTo(2));
            Assert.That(channelBloc.SubBlocs[0].SubTrials[0].Data.Values, Is.EqualTo(new[] { 10f, 11f }));
            Assert.That(channelBloc.SubBlocs[0].SubTrials[1].Data.Values, Is.EqualTo(new[] { 12f, 13f }));
            Assert.That(grid.Data[0].Limits.x, Is.LessThan(10f));
            Assert.That(grid.Data[0].Limits.y, Is.GreaterThan(13f));
        }

        [Test]
        public void TrialMatrixData_UsesInjectedCCEPChannelDataForMatchingSource()
        {
            using TempDirectoryScope temp = new();
            using PersistentDataTestScope persistentData = new(temp.Path);
            CCEPEpochFixture fixture = CreateInjectedCCEPFixture();
            Dataset dataset = new("dataset-ccep", fixture.Protocol, new[] { fixture.DataInfo });
            ChannelStruct targetChannel = new(fixture.TargetChannel, fixture.Patient);
            ChannelStruct sourceChannel = new(fixture.SourceChannel, fixture.Patient);
            TrialMatrixGrid.CCEPTrialMatrixData dataStruct = new(dataset, fixture.DataInfo.Name, fixture.Protocol.OrderedBlocs.ToList(), sourceChannel);

            TrialMatrixGrid grid = new(new[] { targetChannel }, new TrialMatrixGrid.TrialMatrixData[] { dataStruct });

            Assert.That(grid.DataStructs.Single(), Is.SameAs(dataStruct));
            Assert.That(dataStruct.Source, Is.EqualTo(sourceChannel));
            Assert.That(grid.Data, Has.Length.EqualTo(1));

            ChannelBloc channelBloc = grid.Data[0].Blocs[0].ChannelBlocs[0];
            Assert.That(channelBloc.IsFound, Is.True);
            Assert.That(channelBloc.Channel, Is.EqualTo(targetChannel));
            Assert.That(channelBloc.SubBlocs.Single().SubTrials, Has.Length.EqualTo(2));
            Assert.That(channelBloc.SubBlocs.Single().SubTrials[0].Data.Values, Is.EqualTo(new[] { 20f, 21f }));
            Assert.That(channelBloc.SubBlocs.Single().SubTrials[1].Data.Values, Is.EqualTo(new[] { 22f, 23f }));
            Assert.That(grid.Data[0].Limits.x, Is.LessThan(20f));
            Assert.That(grid.Data[0].Limits.y, Is.GreaterThan(23f));
        }

        [Test]
        public void TrialMatrixSubBloc_FillerStateReflectsMissingProtocolTrialsOrWindow()
        {
            CoreSubBloc protocolSubBloc = CreateSubBloc("response", 0, 0, 200);
            HBP.Core.Data.ChannelSubTrial channelSubTrial = new(new[] { 1.0f, 2.0f }, "uV", true, new Dictionary<CoreEvent, EventInformation>());
            TrialSubBloc empty = new();
            TrialSubBloc missingTrials = new(protocolSubBloc, new TrialSubTrial[0]);
            TrialSubBloc missingWindow = new(protocolSubBloc, new[] { new TrialSubTrial(channelSubTrial) });
            TrialSubBloc complete = new(protocolSubBloc, new[] { new TrialSubTrial(channelSubTrial) }, new TimeWindow(0, 200));

            Assert.That(empty.IsFiller, Is.True);
            Assert.That(missingTrials.IsFiller, Is.True);
            Assert.That(missingWindow.IsFiller, Is.True);
            Assert.That(complete.IsFiller, Is.False);
        }

        [Test]
        public void InformationPrefab_WiresGraphGridAndTrialMatrixRenderingSurfaces()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Informations/Informations.prefab");
            Assert.That(prefab, Is.Not.Null);

            Component graphZone = FindComponent(prefab, "HBP.UI.Informations.GraphZone");
            AssertSerializedReferences(graphZone, "m_TrialMatrixGrid", "m_GraphPrefab", "m_GraphContainer", "m_TogglesPrefab", "m_ToggleContainer");

            Component graphGrid = FindComponent(prefab, "HBP.UI.Informations.Graphs.GraphsGrid");
            AssertSerializedReferences(graphGrid, "m_ItemAndContainerPrefab", "m_ScrollRect");

            Component trialMatrixZone = FindComponent(prefab, "HBP.UI.Informations.TrialMatrixZone");
            AssertSerializedReferences(trialMatrixZone, "m_TrialMatrixGrid");

            Component trialMatrixGrid = FindComponent(prefab, "HBP.UI.Informations.TrialMatrix.TrialMatrixGrid");
            AssertSerializedReferences(trialMatrixGrid, "m_DataContainer", "m_DataPrefab", "m_ChannelHeaderContainer", "m_ChannelHeaderPrefab");
        }

        [Test]
        public void TrialMatrixGridPrefabs_WireRenderingDependencies()
        {
            GameObject gridPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Informations/TrialMatrix/Grid/pref_TrialMatrixGrid.prefab");
            GameObject gridV2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Informations/TrialMatrix/Grid/pref_TrialMatrixGrid_V2.prefab");
            GameObject dataPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Informations/TrialMatrix/Grid/pref_Data.prefab");

            Assert.That(gridPrefab, Is.Not.Null);
            Assert.That(gridV2Prefab, Is.Not.Null);
            Assert.That(dataPrefab, Is.Not.Null);

            AssertSerializedReferences(FindComponent(gridPrefab, "HBP.UI.Informations.TrialMatrix.TrialMatrixGrid"), "m_DataContainer", "m_DataPrefab", "m_ChannelHeaderContainer", "m_ChannelHeaderPrefab");
            AssertSerializedReferences(FindComponent(gridV2Prefab, "HBP.UI.Informations.TrialMatrix.TrialMatrixGrid"), "m_DataContainer", "m_DataPrefab", "m_ChannelHeaderContainer", "m_ChannelHeaderPrefab");
            AssertSerializedReferences(FindComponent(dataPrefab, "HBP.UI.Informations.TrialMatrix.Data"), "m_BlocPrefab", "m_BlocContainer", "m_TimeLegendPrefab", "m_TimeLegendContainer", "m_LayoutElement");
        }

        [Test]
        public void InformationWindowPrefabs_WireGraphSettingsExplorerAndSiteActions()
        {
            GameObject graphSettingsWindow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/Windows/Graph settings window.prefab");
            GameObject trialMatrixExplorerWindow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/Windows/Trial matrix explorer window.prefab");
            GameObject siteToolsWindow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/UI/Windows/Site Tools window.prefab");

            Assert.That(graphSettingsWindow, Is.Not.Null);
            Assert.That(trialMatrixExplorerWindow, Is.Not.Null);
            Assert.That(siteToolsWindow, Is.Not.Null);

            Component graphSettings = FindComponent(graphSettingsWindow, "HBP.UI.Informations.GraphSettingsWindow");
            AssertSerializedReferences(graphSettings, "m_ChannelStructGroupsPanel", "m_LocalizersPanel", "m_ColorsPanel");
            AssertSerializedReferences(
                FindComponent(graphSettingsWindow, "HBP.UI.Informations.ChannelStructGroupsPanel"),
                "m_ChannelStructsGroupListGestion");
            AssertSerializedReferences(
                FindComponent(graphSettingsWindow, "HBP.UI.Informations.LocalizersPanel"),
                "m_LocalizersGraphsModeDropdown",
                "m_LocalizersGraphsAtlasDropdown",
                "m_LocalizersGraphsPrecisionSlider",
                "m_LocalizersGraphsVoxelSettingsContainer",
                "m_LocalizersGraphsRegionSettingsContainer",
                "m_LocalizersGraphsAtlasSettingsContainer",
                "m_RescalingContainer",
                "m_EnableRescalingToggle",
                "m_BaselineValueInputField",
                "m_GainFactorInputField",
                "m_OffsetInputField",
                "m_RescalingFormulaText",
                "m_DataTypeDropdown",
                "m_ProtocolItemPrefab",
                "m_ProtocolsContainer",
                "m_GenerateLocalizersGraphsButton");

            Component explorer = FindComponent(trialMatrixExplorerWindow, "HBP.UI.Database.TrialMatrixExplorerWindow");
            AssertSerializedReferences(explorer, "m_SelectPatientsButton", "m_PatientsSelectedText", "m_DataDropdown", "m_DisplayMatrixButton", "m_TrialMatrixDisplayer", "m_ConfigurationContainer");
            AssertSerializedReferences(
                FindComponent(trialMatrixExplorerWindow, "HBP.UI.Database.TrialMatrixDisplayer"),
                "m_TrialMatrixGrid",
                "m_TrialMatrixGridContainer",
                "m_NoDataContainer",
                "m_NoDataText",
                "m_ChannelList",
                "m_PatientDropdown",
                "m_TrialMatrixActionsButton",
                "m_ProtocolDropdown",
                "m_InformationPanels",
                "m_Colormap");
            AssertSerializedReferences(
                FindComponent(trialMatrixExplorerWindow, "HBP.UI.Database.TrialMatrixGrid"),
                "m_DataContainer",
                "m_DataPrefab",
                "m_TitleHeaderContainer",
                "m_TitleHeaderPrefab");
            AssertSerializedReferences(
                FindComponent(trialMatrixExplorerWindow, "HBP.UI.Database.InformationPanels"),
                "m_PatientInformationText",
                "m_PatientTagDisplaySettingsContextMenu",
                "m_SiteInformationText",
                "m_SiteTagDisplaySettingsContextMenu");

            Component siteTools = FindComponent(siteToolsWindow, "HBP.UI.Module3D.SiteToolsWindow");
            AssertSerializedReferences(siteTools, "m_SelectToolDropdown", "m_ApplyForDropdown", "m_ApplyChangesButton");
            AssertSerializedReferenceArray(siteTools, "m_SiteToolSections");
            AssertSerializedReferences(FindComponent(siteToolsWindow, "HBP.UI.Module3D.DisplayGraphSection"), "m_NameInputField");
            AssertSerializedReferences(FindComponent(siteToolsWindow, "HBP.UI.Module3D.OpenTrialMatrixExplorerSection"), "m_DataSourceDropdown", "m_DataNameDropdown");
        }

        private static Patient CreatePatient(string name)
        {
            return new Patient(name, new BaseMesh[0], new MRI[0], new Site[0], new BaseTagValue[0], string.Empty);
        }

        private static CoreBloc CreateBloc(string name, int order, params CoreSubBloc[] subBlocs)
        {
            return new CoreBloc(name, order, string.Empty, string.Empty, subBlocs);
        }

        private static CoreSubBloc CreateSubBloc(string name, int order, int min, int max)
        {
            return new CoreSubBloc(name, order, MainSecondaryEnum.Main, new TimeWindow(min, max), new TimeWindow(min, 0), new CoreEvent[0], new Icon[0], new Treatment[0]);
        }

        private static Component FindComponent(GameObject root, string fullTypeName)
        {
            Component component = root.GetComponentsInChildren<Component>(true).FirstOrDefault(item => item != null && item.GetType().FullName == fullTypeName);
            Assert.That(component, Is.Not.Null, $"{root.name} should contain {fullTypeName}");
            return component;
        }

        private static void AssertSerializedReferences(Component component, params string[] propertyNames)
        {
            SerializedObject serializedObject = new(component);
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyName);
                Assert.That(property, Is.Not.Null, $"{component.GetType().FullName}.{propertyName} should exist");
                Assert.That(property.propertyType, Is.EqualTo(SerializedPropertyType.ObjectReference), $"{component.GetType().FullName}.{propertyName} should be an object reference");
                Assert.That(property.objectReferenceValue, Is.Not.Null, $"{component.GetType().FullName}.{propertyName} should be assigned");
            }
        }

        private static void AssertSerializedReferenceArray(Component component, string propertyName)
        {
            SerializedObject serializedObject = new(component);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"{component.GetType().FullName}.{propertyName} should exist");
            Assert.That(property.isArray, Is.True, $"{component.GetType().FullName}.{propertyName} should be an array");
            Assert.That(property.arraySize, Is.GreaterThan(0), $"{component.GetType().FullName}.{propertyName} should not be empty");
            for (int index = 0; index < property.arraySize; index++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(index);
                Assert.That(element.objectReferenceValue, Is.Not.Null, $"{component.GetType().FullName}.{propertyName}[{index}] should be assigned");
            }
        }

        private static IEEGEpochFixture CreateInjectedIEEGFixture()
        {
            Patient patient = new("patient-a", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), Array.Empty<BaseTagValue>(), string.Empty, "information-data-patient-001");
            CoreSubBloc subBloc = new(
                "response",
                0,
                MainSecondaryEnum.Main,
                new TimeWindow(0, 2),
                new TimeWindow(0, 1),
                new[] { new CoreEvent("stim", new[] { 1 }, MainSecondaryEnum.Main, "information-data-event-001") },
                Array.Empty<Icon>(),
                Array.Empty<Treatment>(),
                "information-data-subbloc-001");
            CoreBloc bloc = new("response-bloc", 0, string.Empty, "response_stim_CODE", new[] { subBloc }, "information-data-bloc-001");

            Protocol protocol = new("protocol-a", new[] { bloc }, "information-data-protocol-001");
            IEEGDataInfo dataInfo = new(
                "ieeg-data",
                protocol,
                new Elan(),
                Array.Empty<Error>(),
                Array.Empty<Warning>(),
                patient,
                NormalizationType.None,
                "information-data-db",
                "information-data-ieeg-001");

            BlocData blocData = (BlocData)FormatterServices.GetUninitializedObject(typeof(BlocData));
            blocData.Frequency = new Frequency(1000);
            blocData.Trials = new[]
            {
                new Trial(new Dictionary<CoreSubBloc, HBP.Core.Data.SubTrial> { { subBloc, CreateCoreSubTrial(subBloc, new[] { 10f, 11f }, 10f) } }),
                new Trial(new Dictionary<CoreSubBloc, HBP.Core.Data.SubTrial> { { subBloc, CreateCoreSubTrial(subBloc, new[] { 12f, 13f }, 20f) } })
            };

            CoreIEEGData data = (CoreIEEGData)FormatterServices.GetUninitializedObject(typeof(CoreIEEGData));
            data.DataByBloc = new Dictionary<CoreBloc, BlocData> { { bloc, blocData } };
            data.UnitByChannel = new Dictionary<string, string> { { "A1", "uV" } };
            data.Frequency = new Frequency(1000);

            AddCacheEntry("m_DataByRequest", CreateRequest("Request", dataInfo), data);
            AddCacheEntry("m_BlocDataByRequest", CreateRequest("BlocRequest", dataInfo, bloc), blocData);

            return new IEEGEpochFixture
            {
                Patient = patient,
                Protocol = protocol,
                DataInfo = dataInfo,
                Channel = "A1"
            };
        }

        private static CCEPEpochFixture CreateInjectedCCEPFixture()
        {
            Patient patient = new("patient-ccep", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), Array.Empty<BaseTagValue>(), string.Empty, "information-data-ccep-patient-001");
            CoreSubBloc subBloc = new(
                "response",
                0,
                MainSecondaryEnum.Main,
                new TimeWindow(0, 2),
                new TimeWindow(0, 1),
                new[] { new CoreEvent("stim", new[] { 1 }, MainSecondaryEnum.Main, "information-data-ccep-event-001") },
                Array.Empty<Icon>(),
                Array.Empty<Treatment>(),
                "information-data-ccep-subbloc-001");
            CoreBloc bloc = new("ccep-response-bloc", 0, string.Empty, "response_stim_CODE", new[] { subBloc }, "information-data-ccep-bloc-001");

            Protocol protocol = new("protocol-ccep", new[] { bloc }, "information-data-ccep-protocol-001");
            CCEPDataInfo dataInfo = new(
                "ccep-data",
                protocol,
                new Elan(),
                Array.Empty<Error>(),
                Array.Empty<Warning>(),
                patient,
                "Stim",
                "information-data-db",
                "information-data-ccep-001");

            BlocData blocData = (BlocData)FormatterServices.GetUninitializedObject(typeof(BlocData));
            blocData.Frequency = new Frequency(1000);
            blocData.Trials = new[]
            {
                new Trial(new Dictionary<CoreSubBloc, HBP.Core.Data.SubTrial> { { subBloc, CreateCoreSubTrial(subBloc, "A1", new[] { 20f, 21f }, 10f) } }),
                new Trial(new Dictionary<CoreSubBloc, HBP.Core.Data.SubTrial> { { subBloc, CreateCoreSubTrial(subBloc, "A1", new[] { 22f, 23f }, 20f) } })
            };

            CoreCCEPData data = (CoreCCEPData)FormatterServices.GetUninitializedObject(typeof(CoreCCEPData));
            data.DataByBloc = new Dictionary<CoreBloc, BlocData> { { bloc, blocData } };
            data.UnitByChannel = new Dictionary<string, string> { { "A1", "uV" } };
            data.Frequency = new Frequency(1000);
            data.StimulatedChannel = "Stim";

            AddCacheEntry("m_DataByRequest", CreateRequest("Request", dataInfo), data);
            AddCacheEntry("m_BlocDataByRequest", CreateRequest("BlocRequest", dataInfo, bloc), blocData);

            return new CCEPEpochFixture
            {
                Patient = patient,
                Protocol = protocol,
                DataInfo = dataInfo,
                SourceChannel = "Stim",
                TargetChannel = "A1"
            };
        }

        private static HBP.Core.Data.SubTrial CreateCoreSubTrial(CoreSubBloc subBloc, float[] values, float eventTimeFromStart)
        {
            return CreateCoreSubTrial(subBloc, "A1", values, eventTimeFromStart);
        }

        private static HBP.Core.Data.SubTrial CreateCoreSubTrial(CoreSubBloc subBloc, string channel, float[] values, float eventTimeFromStart)
        {
            Dictionary<string, float[]> valuesByChannel = new() { { channel, values.ToArray() } };
            HBP.Core.Data.SubTrial subTrial = new(
                new Dictionary<CoreEvent, EventInformation>
                {
                    {
                        subBloc.MainEvent,
                        new EventInformation(new[]
                        {
                            new EventInformation.EventOccurence(subBloc.MainEvent.Codes[0], 0, 0, 0, eventTimeFromStart, eventTimeFromStart, 0)
                        })
                    }
                },
                new Dictionary<string, string> { { channel, "uV" } },
                valuesByChannel,
                new Dictionary<string, float[]> { { channel, new[] { 0f, 0f } } },
                true);
            subTrial.ValuesByChannel = valuesByChannel.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            return subTrial;
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

        private sealed class IEEGEpochFixture
        {
            public Patient Patient;
            public Protocol Protocol;
            public IEEGDataInfo DataInfo;
            public string Channel;
        }

        private sealed class CCEPEpochFixture
        {
            public Patient Patient;
            public Protocol Protocol;
            public CCEPDataInfo DataInfo;
            public string SourceChannel;
            public string TargetChannel;
        }
    }
}
