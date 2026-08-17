using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Tests.PlayMode.Utilities;
using HBP.UI.Toolbar;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ToolbarROIManager = HBP.UI.Toolbar.ROIManager;
using DataROIManager = HBP.Data.Module3D.ROIManager;
using DataSite = HBP.Core.Data.Site;
using ModuleSphere = HBP.Data.Module3D.Sphere;
using ObjectSite = HBP.Core.Object3D.Site;
using ObjectSiteInformation = HBP.Core.Object3D.SiteInformation;
using ObjectSiteState = HBP.Core.Object3D.SiteState;

namespace HBP.Tests.PlayMode.Toolbar
{
    public class ToolbarCoveragePlayModeTests
    {
        [Test]
        [Category("PlayMode.Toolbar")]
        public void DisplayToolbarTools_UpdateSceneRotationCameraAndResetEvents()
        {
            using PlayModeSceneScope scene = new("ToolbarDisplayToolbar");
            using ToolbarSceneHarness harness = new(scene.Scene);
            Column3D selectedColumn = harness.CreateColumn("display-column", selected: true);
            View3D selectedView = harness.CreateDetachedView(lineID: 0, selected: true);

            Button autoButton = CreateButton("Auto Rotate Button");
            Toggle autoToggle = CreateToggle("Auto Rotate Toggle");
            Slider autoSlider = CreateSlider("Auto Rotate Slider");
            AutoRotate autoRotate = CreateTool<AutoRotate>("Auto Rotate", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Button", autoButton);
                SetPrivateField(tool, "m_Toggle", autoToggle);
                SetPrivateField(tool, "m_Slider", autoSlider);
            });
            autoRotate.Initialize();

            autoToggle.SetIsOnWithoutNotify(true);
            autoToggle.onValueChanged.Invoke(true);
            autoSlider.SetValueWithoutNotify(52.0f);
            autoSlider.onValueChanged.Invoke(52.0f);

            Assert.That(harness.Scene.AutomaticRotation, Is.True);
            Assert.That(harness.Scene.AutomaticRotationSpeed, Is.EqualTo(52.0f));

            Dropdown cameraDropdown = CreateDropdown("Camera Type", "Trackball", "Orbital");
            CameraTypes cameraTypes = CreateTool<CameraTypes>("Camera Types", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Dropdown", cameraDropdown));
            cameraTypes.Initialize();

            cameraDropdown.SetValueWithoutNotify((int)CameraControl.Orbital);
            cameraDropdown.onValueChanged.Invoke((int)CameraControl.Orbital);

            Assert.That(harness.Scene.CameraType, Is.EqualTo(CameraControl.Orbital));

            int resetEvents = 0;
            harness.Scene.OnResetViewPositions.AddListener(() => resetEvents++);
            Button resetButton = CreateButton("Reset Views");
            ResetViews resetViews = CreateTool<ResetViews>("Reset Views", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Button", resetButton));
            resetViews.Initialize();

            resetButton.onClick.Invoke();

            Assert.That(resetEvents, Is.EqualTo(1));
        }

        [Test]
        [Category("PlayMode.Toolbar")]
        public void SceneToolbarTools_UpdateSceneDisplayStateAndMaterials()
        {
            using PlayModeSceneScope scene = new("ToolbarSceneToolbar");
            using ToolbarSceneHarness harness = new(scene.Scene);
            Column3D selectedColumn = harness.CreateColumn("scene-column", selected: true);
            View3D selectedView = harness.CreateDetachedView(lineID: 0, selected: true);

            Dropdown brainColorDropdown = CreateDropdown("Brain Color", "Brain", "Default", "White", "Gray", "SoftGray");
            BrainColor brainColor = CreateTool<BrainColor>("Brain Color", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Dropdown", brainColorDropdown));
            brainColor.Initialize();
            brainColorDropdown.SetValueWithoutNotify(2);
            brainColorDropdown.onValueChanged.Invoke(2);

            Dropdown colormapDropdown = CreateDropdown("Colormap", "Gray", "Hot", "Winter", "Warm", "Surface", "Cool", "RedYellow", "BlueGreen", "ACTC", "Bone", "GE", "Gold", "XRain", "MatLab");
            Colormap colormap = CreateTool<Colormap>("Colormap", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Dropdown", colormapDropdown));
            colormap.Initialize();
            colormapDropdown.SetValueWithoutNotify(1);
            colormapDropdown.onValueChanged.Invoke(1);

            Toggle cutModeToggle = CreateToggle("Cut Mode");
            CutMode cutMode = CreateTool<CutMode>("Cut Mode", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Toggle", cutModeToggle));
            cutMode.Initialize();
            harness.Scene.SceneInformation.CutsNeedUpdate = false;
            cutModeToggle.SetIsOnWithoutNotify(true);
            cutModeToggle.onValueChanged.Invoke(true);

            Dropdown cutColorDropdown = CreateDropdown("Cut Color", "Default", "Grayscale");
            CutColor cutColor = CreateTool<CutColor>("Cut Color", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Dropdown", cutColorDropdown));
            cutColor.Initialize();
            harness.Scene.SceneInformation.CutsNeedUpdate = false;
            cutColorDropdown.SetValueWithoutNotify(1);
            cutColorDropdown.onValueChanged.Invoke(1);

            Toggle edgeToggle = CreateToggle("Edge Mode");
            EdgeMode edgeMode = CreateTool<EdgeMode>("Edge Mode", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Toggle", edgeToggle));
            edgeMode.Initialize();
            edgeToggle.SetIsOnWithoutNotify(true);
            edgeToggle.onValueChanged.Invoke(true);

            Toggle transparentToggle = CreateToggle("Transparent Brain");
            Slider transparentSlider = CreateSlider("Transparent Alpha");
            TransparentBrain transparentBrain = CreateTool<TransparentBrain>("Transparent Brain", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Toggle", transparentToggle);
                SetPrivateField(tool, "m_Slider", transparentSlider);
            });
            transparentBrain.Initialize();
            transparentSlider.SetValueWithoutNotify(0.42f);
            transparentSlider.onValueChanged.Invoke(0.42f);
            transparentToggle.SetIsOnWithoutNotify(true);
            transparentToggle.onValueChanged.Invoke(true);

            Assert.That(harness.Scene.BrainColor, Is.EqualTo(ColorType.White));
            Assert.That(harness.Scene.Colormap, Is.EqualTo(ColorType.Hot));
            Assert.That(harness.Scene.StrongCuts, Is.True);
            Assert.That(harness.Scene.BrainMaterials.BrainMaterial.GetInt("_StrongCuts"), Is.EqualTo(1));
            Assert.That(harness.Scene.CutColor, Is.EqualTo(ColorType.Grayscale));
            Assert.That(harness.Scene.EdgeMode, Is.True);
            Assert.That(harness.Scene.IsBrainTransparent, Is.True);
            Assert.That(harness.Scene.BrainMaterials.Alpha, Is.EqualTo(0.42f).Within(0.0001f));
            Assert.That(harness.Scene.SceneInformation.CutsNeedUpdate, Is.True);
        }

        [Test]
        [Category("PlayMode.Toolbar")]
        public void SiteToolbarTools_UpdateSiteSceneFlagsAndSelectionLabel()
        {
            using PlayModeSceneScope scene = new("ToolbarSiteToolbar");
            using ToolbarSceneHarness harness = new(scene.Scene);
            ToolbarTestColumn selectedColumn = harness.CreateColumn("site-column", selected: true);
            ObjectSite selectedSite = harness.CreateSite("A1", 0, new Vector3(1, 2, 3), selectedColumn);
            SelectSite(selectedColumn, selectedSite);
            View3D selectedView = harness.CreateDetachedView(lineID: 0, selected: true);

            Toggle showAllToggle = CreateToggle("Show All Sites");
            ShowAllSites showAllSites = CreateTool<ShowAllSites>("Show All Sites", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Toggle", showAllToggle));
            showAllSites.Initialize();
            showAllToggle.SetIsOnWithoutNotify(true);
            showAllToggle.onValueChanged.Invoke(true);

            Toggle blacklistedToggle = CreateToggle("Blacklisted Sites");
            BlacklistedSitesDisplay blacklistedSitesDisplay = CreateTool<BlacklistedSitesDisplay>("Blacklisted Sites", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Toggle", blacklistedToggle));
            blacklistedSitesDisplay.Initialize();
            blacklistedToggle.SetIsOnWithoutNotify(true);
            blacklistedToggle.onValueChanged.Invoke(true);

            Slider gainSlider = CreateSlider("Site Gain");
            SiteGain siteGain = CreateTool<SiteGain>("Site Gain", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Slider", gainSlider));
            siteGain.Initialize();
            gainSlider.SetValueWithoutNotify(1.7f);
            gainSlider.onValueChanged.Invoke(1.7f);

            Toggle cutAroundToggle = CreateToggle("Cut Around Site");
            CutAroundSite cutAroundSite = CreateTool<CutAroundSite>("Cut Around Site", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Toggle", cutAroundToggle));
            cutAroundSite.Initialize();
            cutAroundToggle.SetIsOnWithoutNotify(true);
            cutAroundToggle.onValueChanged.Invoke(true);

            Text selectedSiteText = CreateText("Selected Site Text");
            SelectedSite selectedSiteTool = CreateTool<SelectedSite>("Selected Site", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Text", selectedSiteText));
            selectedSiteTool.UpdateStatus();

            Assert.That(harness.Scene.ShowAllSites, Is.True);
            Assert.That(harness.Scene.HideBlacklistedSites, Is.True);
            Assert.That(harness.Scene.SiteGain, Is.EqualTo(1.7f).Within(0.0001f));
            Assert.That(harness.Scene.AutomaticCutAroundSelectedSite, Is.True);
            Assert.That(selectedSiteText.text, Does.Contain("A1"));
            Assert.That(harness.Scene.SceneInformation.SitesNeedUpdate, Is.True);
            Assert.That(harness.Scene.SceneInformation.CutsNeedUpdate, Is.True);
        }

        [Test]
        [Category("PlayMode.Toolbar")]
        public void ActivityToolbarTools_RequestComputationAndApplyActivityAlpha()
        {
            using PlayModeSceneScope scene = new("ToolbarActivityToolbar");
            using ToolbarSceneHarness harness = new(scene.Scene);
            ToolbarTestColumn selectedColumn = harness.CreateColumn("activity-column", selected: true);
            ToolbarTestColumn otherColumn = harness.CreateColumn("activity-column-2", selected: false);
            View3D selectedView = harness.CreateDetachedView(lineID: 0, selected: true);

            selectedColumn.ActivityAlpha = 0.8f;
            otherColumn.ActivityAlpha = 0.8f;

            Slider alphaSlider = CreateSlider("Activity Alpha");
            ActivityTransparency transparency = CreateTool<ActivityTransparency>("Activity Transparency", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Slider", alphaSlider));
            transparency.Initialize();
            alphaSlider.SetValueWithoutNotify(0.33f);
            alphaSlider.onValueChanged.Invoke(0.33f);

            Button computeButton = CreateButton("Compute Activity");
            Button removeButton = CreateButton("Remove Activity");
            ComputeActivity computeActivity = CreateTool<ComputeActivity>("Compute Activity", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Compute", computeButton);
                SetPrivateField(tool, "m_Remove", removeButton);
            });
            computeActivity.Initialize();
            harness.Scene.SceneInformation.GeneratorUpdateRequested = false;
            harness.Scene.SceneInformation.GeneratorNeedsUpdate = false;
            computeButton.onClick.Invoke();

            Assert.That(selectedColumn.ActivityAlpha, Is.EqualTo(0.33f).Within(0.0001f));
            Assert.That(otherColumn.ActivityAlpha, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(harness.Scene.SceneInformation.GeneratorUpdateRequested, Is.True);
            Assert.That(harness.Scene.SceneInformation.GeneratorNeedsUpdate, Is.True);

            int globalEvents = 0;
            bool globalValue = false;
            Toggle globalToggle = CreateToggle("Activity Global");
            ActivityGlobal activityGlobal = CreateTool<ActivityGlobal>("Activity Global", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Toggle", globalToggle));
            activityGlobal.OnChangeValue.AddListener(value =>
            {
                globalEvents++;
                globalValue = value;
            });
            activityGlobal.Initialize();
            globalToggle.SetIsOnWithoutNotify(true);
            globalToggle.onValueChanged.Invoke(true);

            Assert.That(globalEvents, Is.EqualTo(1));
            Assert.That(globalValue, Is.True);
        }

        [Test]
        [Category("PlayMode.Toolbar")]
        public void CCEPToolbarTools_SwitchModeAndSelectSourceSite()
        {
            using PlayModeSceneScope scene = new("ToolbarCCEPToolbar");
            using ToolbarSceneHarness harness = new(scene.Scene);
            Column3DCCEP ccepColumn = harness.CreateCCEPColumn("ccep-column", selected: true);
            ObjectSite site = harness.CreateSite("S1", 0, Vector3.one, ccepColumn);
            ccepColumn.Sources.Add(site);
            SelectSite(ccepColumn, site);
            View3D selectedView = harness.CreateDetachedView(lineID: 0, selected: true);

            Toggle siteMode = CreateToggle("CCEP Site Mode");
            Toggle marsMode = CreateToggle("CCEP Mars Mode");
            CCEPModeSelector modeSelector = CreateTool<CCEPModeSelector>("CCEP Mode", harness.Scene, ccepColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Site", siteMode);
                SetPrivateField(tool, "m_MarsAtlas", marsMode);
            });
            modeSelector.Initialize();
            marsMode.SetIsOnWithoutNotify(true);
            marsMode.onValueChanged.Invoke(true);
            siteMode.SetIsOnWithoutNotify(true);
            siteMode.onValueChanged.Invoke(true);

            Text sourceText = CreateText("CCEP Source Text");
            Button selectSource = CreateButton("Select Source");
            Button unselectSource = CreateButton("Unselect Source");
            CCEPSiteSourceSelector sourceSelector = CreateTool<CCEPSiteSourceSelector>("CCEP Source", harness.Scene, ccepColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Text", sourceText);
                SetPrivateField(tool, "m_SelectSource", selectSource);
                SetPrivateField(tool, "m_UnselectSource", unselectSource);
            });
            sourceSelector.Initialize();
            sourceSelector.UpdateInteractable();
            selectSource.onClick.Invoke();
            sourceSelector.UpdateStatus();

            Assert.That(ccepColumn.Mode, Is.EqualTo(Column3DCCEP.CCEPMode.Site));
            Assert.That(ccepColumn.SelectedSourceSite, Is.SameAs(site));
            Assert.That(sourceText.text, Does.Contain("S1"));

            unselectSource.onClick.Invoke();

            Assert.That(ccepColumn.SelectedSourceSite, Is.Null);
        }

        [Test]
        [Category("PlayMode.Toolbar")]
        public void TimelineToolbarTools_UpdateDynamicTimelineLocallyAndGlobally()
        {
            using PlayModeSceneScope scene = new("ToolbarTimelineToolbar");
            using ToolbarSceneHarness harness = new(scene.Scene);
            TestIEEGColumn selectedColumn = harness.CreateIEEGColumn("dynamic-column", selected: true);
            TestIEEGColumn otherColumn = harness.CreateIEEGColumn("dynamic-column-2", selected: false);
            View3D selectedView = harness.CreateDetachedView(lineID: 0, selected: true);
            harness.Scene.IsGeneratorUpToDate = true;

            Toggle playToggle = CreateToggle("Timeline Play");
            TimelinePlay timelinePlay = CreateTool<TimelinePlay>("Timeline Play", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Toggle", playToggle));
            timelinePlay.Initialize();
            playToggle.SetIsOnWithoutNotify(true);
            playToggle.onValueChanged.Invoke(true);

            Toggle loopToggle = CreateToggle("Timeline Loop");
            TimelineLoop timelineLoop = CreateTool<TimelineLoop>("Timeline Loop", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Toggle", loopToggle));
            timelineLoop.Initialize();
            timelineLoop.IsGlobal = true;
            loopToggle.SetIsOnWithoutNotify(true);
            loopToggle.onValueChanged.Invoke(true);

            Button minus = CreateButton("Timeline Minus");
            Button plus = CreateButton("Timeline Plus");
            InputField stepInput = CreateInputField("Timeline Step", "2");
            TimelineStep timelineStep = CreateTool<TimelineStep>("Timeline Step", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Minus", minus);
                SetPrivateField(tool, "m_Plus", plus);
                SetPrivateField(tool, "m_InputField", stepInput);
            });
            timelineStep.Initialize();
            timelineStep.IsGlobal = true;
            stepInput.text = "3";
            stepInput.onEndEdit.Invoke("3");
            plus.onClick.Invoke();

            Assert.That(selectedColumn.Timeline.IsPlaying, Is.True);
            Assert.That(otherColumn.Timeline.IsPlaying, Is.False);
            Assert.That(selectedColumn.Timeline.IsLooping, Is.True);
            Assert.That(otherColumn.Timeline.IsLooping, Is.True);
            Assert.That(selectedColumn.Timeline.Step, Is.EqualTo(3));
            Assert.That(otherColumn.Timeline.Step, Is.EqualTo(3));
            Assert.That(selectedColumn.Timeline.CurrentIndex, Is.EqualTo(3));
            Assert.That(otherColumn.Timeline.CurrentIndex, Is.EqualTo(3));

            int globalEvents = 0;
            Toggle globalToggle = CreateToggle("Timeline Global");
            TimelineGlobal timelineGlobal = CreateTool<TimelineGlobal>("Timeline Global", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Toggle", globalToggle));
            timelineGlobal.OnChangeValue.AddListener(_ => globalEvents++);
            timelineGlobal.Initialize();
            globalToggle.SetIsOnWithoutNotify(true);
            globalToggle.onValueChanged.Invoke(true);

            Assert.That(globalEvents, Is.EqualTo(1));
        }

        [Test]
        [Category("PlayMode.Toolbar")]
        public void ROIToolbarTools_AddRenameSelectAndRemoveROI()
        {
            using PlayModeSceneScope scene = new("ToolbarROIToolbar");
            using ToolbarSceneHarness harness = new(scene.Scene);
            Column3D selectedColumn = harness.CreateColumn("roi-column", selected: true);
            View3D selectedView = harness.CreateDetachedView(lineID: 0, selected: true);

            Button add = CreateButton("Add ROI");
            Dropdown roiSelector = CreateDropdown("ROI Selector", "None");
            RectTransform roiNameParent = CreateRectTransform("ROI Name Parent");
            InputField roiName = CreateInputField("ROI Name", "");
            Button remove = CreateButton("Remove ROI");
            Dropdown sphereSelector = CreateDropdown("Sphere Selector", "None");
            Button removeSphere = CreateButton("Remove Sphere");
            ToolbarROIManager roiTool = CreateTool<ToolbarROIManager>("ROI Tool", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_AddROI", add);
                SetPrivateField(tool, "m_ROISelector", roiSelector);
                SetPrivateField(tool, "m_ROINameParent", roiNameParent);
                SetPrivateField(tool, "m_ROIName", roiName);
                SetPrivateField(tool, "m_RemoveROI", remove);
                SetPrivateField(tool, "m_SphereSelector", sphereSelector);
                SetPrivateField(tool, "m_RemoveSphere", removeSphere);
            });
            roiTool.Initialize();

            AssertNoException("Add ROI toolbar click", add.onClick.Invoke);
            roiName.text = "Synthetic ROI";
            AssertNoException("Rename ROI toolbar edit", () => roiName.onEndEdit.Invoke("Synthetic ROI"));
            AssertNoException("Update ROI toolbar status", roiTool.UpdateStatus);

            Assert.That(harness.Scene.ROIManager.ROIs, Has.Count.EqualTo(1));
            Assert.That(harness.Scene.ROIManager.SelectedROI.Name, Is.EqualTo("Synthetic ROI"));
            Assert.That(roiSelector.options, Has.Count.EqualTo(2));
            Assert.That(remove.interactable, Is.True);

            AssertNoException("Remove ROI toolbar click", remove.onClick.Invoke);
            AssertNoException("Update ROI toolbar status after removal", roiTool.UpdateStatus);

            Assert.That(harness.Scene.ROIManager.ROIs, Is.Empty);
            Assert.That(harness.Scene.ROIManager.SelectedROI, Is.Null);

            Button import = CreateButton("Import ROI");
            Button export = CreateButton("Export ROI");
            ROIExport exportTool = CreateTool<ROIExport>("ROI Export", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Import", import);
                SetPrivateField(tool, "m_Export", export);
            });
            exportTool.UpdateInteractable();

            Assert.That(import.interactable, Is.True);
            Assert.That(export.interactable, Is.False);
        }

        [Test]
        [Category("PlayMode.Toolbar")]
        public void RemainingToolbarToolsExposeSafeStateWithoutOpeningDialogsOrNativeViewers()
        {
            using PlayModeSceneScope scene = new("ToolbarToolbarSafeStates");
            using ToolbarSceneHarness harness = new(scene.Scene);
            ToolbarTestColumn selectedColumn = harness.CreateColumn("safe-state-column", selected: true);
            ObjectSite selectedSite = harness.CreateSite("A2", 0, new Vector3(2, 0, 0), selectedColumn);
            SelectSite(selectedColumn, selectedSite);
            View3D selectedView = harness.CreateDetachedView(lineID: 0, selected: true);

            Dropdown brainSelectorDropdown = CreateDropdown("Brain Selector");
            BrainSelector brainSelector = CreateTool<BrainSelector>("Brain Selector", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Dropdown", brainSelectorDropdown));
            brainSelector.UpdateStatus();

            Toggle leftBrain = CreateToggle("Left Brain");
            Toggle rightBrain = CreateToggle("Right Brain");
            BrainMeshes brainMeshes = CreateTool<BrainMeshes>("Brain Meshes", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Left", leftBrain);
                SetPrivateField(tool, "m_Right", rightBrain);
            });
            brainMeshes.Initialize();
            brainMeshes.UpdateTool();
            leftBrain.SetIsOnWithoutNotify(false);
            rightBrain.SetIsOnWithoutNotify(true);
            leftBrain.onValueChanged.Invoke(false);

            Button exportActivityButton = CreateButton("Export Activity");
            ExportActivityToNifti exportActivity = CreateTool<ExportActivityToNifti>("Export Activity", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_OpenWindowButton", exportActivityButton));
            harness.Scene.IsGeneratorUpToDate = false;
            exportActivity.UpdateInteractable();
            bool exportDisabledWhenGeneratorStale = exportActivityButton.interactable;
            harness.Scene.IsGeneratorUpToDate = true;
            exportActivity.UpdateInteractable();

            Button saveConfig = CreateButton("Save Configuration");
            Button loadConfig = CreateButton("Load Configuration");
            Button resetConfig = CreateButton("Reset Configuration");
            ConfigurationLoaderSaver configuration = CreateTool<ConfigurationLoaderSaver>("Configuration Loader Saver", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Save", saveConfig);
                SetPrivateField(tool, "m_Load", loadConfig);
                SetPrivateField(tool, "m_Reset", resetConfig);
            });
            configuration.UpdateTool();

            Button copyVisualizationButton = CreateButton("Copy Visualization");
            CopyVisualization copyVisualization = CreateTool<CopyVisualization>("Copy Visualization", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Copy", copyVisualizationButton));
            copyVisualization.UpdateTool();

            Toggle ibc = CreateToggle("IBC Atlas");
            Toggle jubrain = CreateToggle("JuBrain Atlas");
            Toggle mars = CreateToggle("Mars Atlas");
            Toggle difumo = CreateToggle("DiFuMo Atlas");
            Toggle localizers = CreateToggle("Localizers Atlas");
            AtlasState atlasState = CreateTool<AtlasState>("Atlas State", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_IBCToggle", ibc);
                SetPrivateField(tool, "m_JubrainToggle", jubrain);
                SetPrivateField(tool, "m_MarsAtlasToggle", mars);
                SetPrivateField(tool, "m_DiFuMoToggle", difumo);
                SetPrivateField(tool, "m_LocalizersToggle", localizers);
            });
            atlasState.UpdateInteractable();

            Button loadPatient = CreateButton("Load Patient");
            LoadPatient loadPatientTool = CreateTool<LoadPatient>("Load Patient", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Button", loadPatient));
            loadPatientTool.UpdateInteractable();

            Button openInteractiveViewer = CreateButton("Open Interactive Viewer");
            OpenInteractiveViewer interactiveViewer = CreateTool<OpenInteractiveViewer>("Open Interactive Viewer", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Button", openInteractiveViewer));
            interactiveViewer.UpdateInteractable();

            Button siteStateImport = CreateButton("Import Site State");
            Button siteStateExport = CreateButton("Export Site State");
            SiteStateExport siteStateExportTool = CreateTool<SiteStateExport>("Site State Export", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Import", siteStateImport);
                SetPrivateField(tool, "m_Export", siteStateExport);
            });
            siteStateExportTool.UpdateInteractable();

            Assert.That(brainSelectorDropdown.options, Has.Count.EqualTo(2));
            Assert.That(brainSelectorDropdown.options[0].text, Is.EqualTo("Synthetic LeftRight"));
            Assert.That(leftBrain.interactable, Is.True);
            Assert.That(rightBrain.interactable, Is.True);
            Assert.That(harness.Scene.MeshManager.MeshPartToDisplay, Is.EqualTo(MeshPart.Right));
            Assert.That(exportDisabledWhenGeneratorStale, Is.False);
            Assert.That(exportActivityButton.interactable, Is.True);
            Assert.That(saveConfig.interactable, Is.True);
            Assert.That(loadConfig.interactable, Is.True);
            Assert.That(resetConfig.interactable, Is.True);
            Assert.That(copyVisualizationButton.interactable, Is.True);
            Assert.That(ibc.interactable, Is.False);
            Assert.That(jubrain.interactable, Is.False);
            Assert.That(mars.interactable, Is.False);
            Assert.That(difumo.interactable, Is.False);
            Assert.That(localizers.interactable, Is.False);
            Assert.That(loadPatient.interactable, Is.False);
            Assert.That(openInteractiveViewer.interactable, Is.True);
            Assert.That(siteStateImport.interactable, Is.True);
            Assert.That(siteStateExport.interactable, Is.True);
        }

        [Test]
        [Category("PlayMode.Toolbar")]
        public void StaticLabelSelector_UpdatesLabelsSelectionAndVisibility()
        {
            using PlayModeSceneScope scene = new("ToolbarStaticLabelSelector");
            using ToolbarSceneHarness harness = new(scene.Scene);
            TestStaticColumn staticColumn = harness.CreateStaticColumn("static-column", selected: true, "alpha", "beta", "gamma");
            View3D selectedView = harness.CreateDetachedView(lineID: 0, selected: true);
            harness.Scene.IsGeneratorUpToDate = true;

            Dropdown labelDropdown = CreateDropdown("Static Label Selector");
            StaticLabelSelector selector = CreateTool<StaticLabelSelector>("Static Label Selector", harness.Scene, staticColumn, selectedView, tool => SetPrivateField(tool, "m_Dropdown", labelDropdown));
            selector.Initialize();
            selector.UpdateInteractable();
            selector.UpdateStatus();

            labelDropdown.SetValueWithoutNotify(2);
            labelDropdown.onValueChanged.Invoke(2);

            Assert.That(labelDropdown.gameObject.activeSelf, Is.True);
            Assert.That(labelDropdown.options, Has.Count.EqualTo(3));
            Assert.That(labelDropdown.options[1].text, Is.EqualTo("beta"));
            Assert.That(staticColumn.SelectedLabelIndex, Is.EqualTo(2));

            harness.Scene.IsGeneratorUpToDate = false;
            selector.UpdateInteractable();

            Assert.That(labelDropdown.gameObject.activeSelf, Is.False);
        }

        [Test]
        [Category("PlayMode.Toolbar")]
        public void TimelineSliderRecordAndScreenshot_ExposeSafeDynamicControls()
        {
            using PlayModeSceneScope scene = new("ToolbarTimelineSliderRecord");
            using ToolbarSceneHarness harness = new(scene.Scene);
            TestIEEGColumn selectedColumn = harness.CreateIEEGColumn("timeline-slider-column", selected: true);
            TestIEEGColumn otherColumn = harness.CreateIEEGColumn("timeline-slider-column-2", selected: false);
            View3D selectedView = harness.CreateDetachedView(lineID: 0, selected: true);
            harness.Scene.IsGeneratorUpToDate = true;

            Slider slider = CreateSlider("Timeline Slider");
            RectTransform subTimelines = CreateRectTransform("Timeline SubTimelines");
            GameObject timelinePrefab = new("Timeline Prefab");
            timelinePrefab.AddComponent<RectTransform>();
            timelinePrefab.AddComponent<HBP.UI.Toolbar.SubTimeline>();
            TimelineSlider timelineSlider = CreateTool<TimelineSlider>("Timeline Slider", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Slider", slider);
                SetPrivateField(tool, "m_SubTimelines", subTimelines);
                SetPrivateField(tool, "m_TimelinePrefab", timelinePrefab);
            });
            timelineSlider.Initialize();
            timelineSlider.UpdateInteractable();
            timelineSlider.UpdateStatus();

            slider.SetValueWithoutNotify(4);
            slider.onValueChanged.Invoke(4);

            Assert.That(slider.interactable, Is.True);
            Assert.That(slider.maxValue, Is.EqualTo(9));
            Assert.That(selectedColumn.Timeline.CurrentIndex, Is.EqualTo(4));
            Assert.That(otherColumn.Timeline.CurrentIndex, Is.EqualTo(0));

            timelineSlider.IsGlobal = true;
            slider.SetValueWithoutNotify(6);
            slider.onValueChanged.Invoke(6);

            Assert.That(selectedColumn.Timeline.CurrentIndex, Is.EqualTo(6));
            Assert.That(otherColumn.Timeline.CurrentIndex, Is.EqualTo(6));

            Button recordVideo = CreateButton("Record Timeline Video");
            TimelineRecord timelineRecord = CreateTool<TimelineRecord>("Timeline Record", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_RecordVideo", recordVideo));
            timelineRecord.UpdateInteractable();

            Button singleScreenshot = CreateButton("Single Screenshot");
            Button multiScreenshots = CreateButton("Multi Screenshot");
            Screenshot screenshot = CreateTool<Screenshot>("Screenshot", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_SingleScreenshot", singleScreenshot);
                SetPrivateField(tool, "m_MultiScreenshots", multiScreenshots);
            });
            screenshot.UpdateInteractable();

            Assert.That(recordVideo.interactable, Is.True);
            Assert.That(singleScreenshot.interactable, Is.True);
            Assert.That(multiScreenshots.interactable, Is.True);
        }

        [Test]
        [Category("PlayMode.Toolbar")]
        public void SiteStateImportMoveSitesAndSiteWindows_UpdateSafeState()
        {
            using PlayModeSceneScope scene = new("ToolbarSiteStateImportMove");
            using ToolbarSceneHarness harness = new(scene.Scene);
            ToolbarTestColumn selectedColumn = harness.CreateColumn("site-state-column", selected: true);
            ToolbarTestColumn otherColumn = harness.CreateColumn("site-state-column-2", selected: false);
            ObjectSite selectedSite = harness.CreateSite("A3", 0, new Vector3(3, 0, 0), selectedColumn);
            ObjectSite otherSite = harness.CreateSite("A3", 0, new Vector3(6, 0, 0), otherColumn);
            SelectSite(selectedColumn, selectedSite);
            View3D selectedView = harness.CreateDetachedView(lineID: 0, selected: true);

            Button import = CreateButton("Import Site State");
            Button export = CreateButton("Export Site State");
            SiteStateExport siteStateExport = CreateTool<SiteStateExport>("Site State Export", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Import", import);
                SetPrivateField(tool, "m_Export", export);
            });
            string csvPath = Path.Combine(Application.temporaryCachePath, $"toolbar_site_states_{Guid.NewGuid():N}.csv");
            File.WriteAllLines(csvPath, new[]
            {
                "ID,Blacklisted,Highlighted,Color,Labels",
                $"{selectedSite.Information.FullID},True,True,#112233FF,Imported;Toolbar"
            });

            siteStateExport.LoadSiteStates(csvPath, allColumns: false);

            Assert.That(selectedSite.State.IsBlackListed, Is.True);
            Assert.That(selectedSite.State.IsHighlighted, Is.True);
            Assert.That(selectedSite.State.Labels, Is.EquivalentTo(new[] { "Imported", "Toolbar" }));
            Assert.That(otherSite.State.IsBlackListed, Is.False);

            siteStateExport.LoadSiteStates(csvPath, allColumns: true);

            Assert.That(otherSite.State.IsBlackListed, Is.True);
            Assert.That(otherSite.State.IsHighlighted, Is.True);
            Assert.That(otherSite.State.Labels, Is.EquivalentTo(new[] { "Imported", "Toolbar" }));

            Button movePanel = CreateButton("Move Sites Panel");
            Button moveLeft = CreateButton("Move Sites Left");
            Button moveRight = CreateButton("Move Sites Right");
            Button reset = CreateButton("Reset Sites");
            MoveSites moveSites = CreateTool<MoveSites>("Move Sites", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Button", movePanel);
                SetPrivateField(tool, "m_MoveToLeftHemisphere", moveLeft);
                SetPrivateField(tool, "m_MoveToRightHemisphere", moveRight);
                SetPrivateField(tool, "m_Reset", reset);
            });
            moveSites.Initialize();
            moveSites.UpdateInteractable();
            selectedSite.transform.localPosition = new Vector3(99, 0, 0);
            reset.onClick.Invoke();

            Button openFilters = CreateButton("Open Filters");
            Button resetFilters = CreateButton("Reset Filters");
            SiteFilters siteFilters = CreateTool<SiteFilters>("Site Filters", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_OpenFiltersButton", openFilters);
                SetPrivateField(tool, "m_ResetFiltersButton", resetFilters);
            });
            siteFilters.UpdateInteractable();

            Button openTools = CreateButton("Open Site Tools");
            OpenSiteTools openSiteTools = CreateTool<OpenSiteTools>("Open Site Tools", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_OpenToolsButton", openTools));
            openSiteTools.UpdateInteractable();

            Assert.That(movePanel.interactable, Is.True);
            Assert.That(selectedSite.transform.localPosition, Is.EqualTo(selectedSite.Information.DefaultPosition));
            Assert.That(openFilters.interactable, Is.True);
            Assert.That(resetFilters.interactable, Is.True);
            Assert.That(openTools.interactable, Is.True);
        }

        [Test]
        [Category("PlayMode.Toolbar")]
        public void SelectorAndCorrelationTools_GateUnavailableDataWithoutOpeningWindows()
        {
            using PlayModeSceneScope scene = new("ToolbarSelectorGating");
            using ToolbarSceneHarness harness = new(scene.Scene);
            TestIEEGColumn selectedColumn = harness.CreateIEEGColumn("selector-ieeg-column", selected: true);
            View3D selectedView = harness.CreateDetachedView(lineID: 0, selected: true);

            Button addView = CreateButton("Add View");
            Button removeView = CreateButton("Remove View");
            Views views = CreateTool<Views>("Views", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Add", addView);
                SetPrivateField(tool, "m_Remove", removeView);
            });
            views.UpdateInteractable();

            Button standardView = CreateButton("Standard Views");
            StandardViews standardViews = CreateTool<StandardViews>("Standard Views", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Button", standardView));
            standardViews.UpdateInteractable();

            Dropdown mriDropdown = CreateDropdown("MRI Selector");
            MRISelector mriSelector = CreateTool<MRISelector>("MRI Selector", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Dropdown", mriDropdown));
            mriSelector.UpdateInteractable();
            mriSelector.UpdateStatus();

            Dropdown implantationDropdown = CreateDropdown("Implantation Selector");
            ImplantationSelector implantationSelector = CreateTool<ImplantationSelector>("Implantation Selector", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Dropdown", implantationDropdown));
            implantationSelector.UpdateInteractable();
            implantationSelector.UpdateStatus();

            Dropdown ibcDropdown = CreateDropdown("IBC Selector");
            IBCSelector ibcSelector = CreateTool<IBCSelector>("IBC Selector", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Dropdown", ibcDropdown));
            ibcSelector.UpdateInteractable();

            Dropdown difumoAtlas = CreateDropdown("DiFuMo Atlas");
            Dropdown difumoArea = CreateDropdown("DiFuMo Area");
            DiFuMoSelector difumoSelector = CreateTool<DiFuMoSelector>("DiFuMo Selector", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_AtlasDropdown", difumoAtlas);
                SetPrivateField(tool, "m_AreaDropdown", difumoArea);
            });
            difumoSelector.UpdateInteractable();

            Dropdown localizerProtocol = CreateDropdown("Localizer Protocol");
            Dropdown localizerData = CreateDropdown("Localizer Data");
            Dropdown localizerBloc = CreateDropdown("Localizer Bloc");
            LocalizersSelector localizersSelector = CreateTool<LocalizersSelector>("Localizers Selector", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_ProtocolDropdown", localizerProtocol);
                SetPrivateField(tool, "m_DataDropdown", localizerData);
                SetPrivateField(tool, "m_BlocDropdown", localizerBloc);
            });
            localizersSelector.UpdateInteractable();

            Button computeCorrelations = CreateButton("Compute Correlations");
            Button loadCorrelations = CreateButton("Load Correlations");
            Button saveCorrelations = CreateButton("Save Correlations");
            Button resetCorrelations = CreateButton("Reset Correlations");
            Toggle displayCorrelations = CreateToggle("Display Correlations");
            SiteCorrelations siteCorrelations = CreateTool<SiteCorrelations>("Site Correlations", harness.Scene, selectedColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Compute", computeCorrelations);
                SetPrivateField(tool, "m_Load", loadCorrelations);
                SetPrivateField(tool, "m_Save", saveCorrelations);
                SetPrivateField(tool, "m_Reset", resetCorrelations);
                SetPrivateField(tool, "m_Display", displayCorrelations);
            });
            siteCorrelations.Initialize();
            siteCorrelations.UpdateInteractable();
            harness.Scene.DisplayCorrelations = true;
            siteCorrelations.UpdateStatus();

            Assert.That(addView.interactable, Is.True);
            Assert.That(removeView.interactable, Is.False);
            Assert.That(standardView.interactable, Is.True);
            Assert.That(mriDropdown.interactable, Is.True);
            Assert.That(implantationDropdown.interactable, Is.True);
            Assert.That(ibcDropdown.gameObject.activeSelf, Is.False);
            Assert.That(difumoSelector.gameObject.activeSelf, Is.False);
            Assert.That(localizersSelector.gameObject.activeSelf, Is.False);
            Assert.That(siteCorrelations.gameObject.activeSelf, Is.True);
            Assert.That(computeCorrelations.interactable, Is.True);
            Assert.That(loadCorrelations.interactable, Is.True);
            Assert.That(saveCorrelations.interactable, Is.False);
            Assert.That(resetCorrelations.interactable, Is.False);
            Assert.That(displayCorrelations.isOn, Is.True);
        }

        [Test]
        [Category("PlayMode.Toolbar")]
        public void RemainingParameterSelectors_GateControlsWithoutHeavyThresholdPanels()
        {
            using PlayModeSceneScope scene = new("ToolbarRemainingParameterSelectors");
            using ToolbarSceneHarness harness = new(scene.Scene);
            TestIEEGColumn ieegColumn = harness.CreateIEEGColumn("remaining-ieeg-column", selected: true);
            TestStaticColumn staticColumn = harness.CreateStaticColumn("remaining-static-column", selected: false, "alpha", "beta");
            Column3DCCEP ccepColumn = harness.CreateCCEPColumn("remaining-ccep-column", selected: false);
            View3D selectedView = harness.CreateDetachedView(lineID: 0, selected: true);

            Button defaultViewButton = CreateButton("Default View");
            DefaultView defaultView = CreateTool<DefaultView>("Default View", harness.Scene, ieegColumn, selectedView, tool => SetPrivateField(tool, "m_Button", defaultViewButton));
            defaultView.UpdateInteractable();

            Dropdown fmriDropdown = CreateDropdown("FMRI Selector");
            FMRISelector fmriSelector = CreateTool<FMRISelector>("FMRI Selector", harness.Scene, ieegColumn, selectedView, tool => SetPrivateField(tool, "m_Dropdown", fmriDropdown));
            harness.Scene.IsGeneratorUpToDate = true;
            fmriSelector.UpdateInteractable();

            Dropdown megDropdown = CreateDropdown("MEG Selector");
            MEGSelector megSelector = CreateTool<MEGSelector>("MEG Selector", harness.Scene, ieegColumn, selectedView, tool => SetPrivateField(tool, "m_Dropdown", megDropdown));
            megSelector.UpdateInteractable();

            InputField dynamicInfluence = CreateInputField("Dynamic Influence", "");
            Button dynamicThreshold = CreateButton("Dynamic Threshold");
            Button dynamicAuto = CreateButton("Dynamic Auto");
            DynamicParameters dynamicParameters = CreateTool<DynamicParameters>("Dynamic Parameters", harness.Scene, ieegColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_InputField", dynamicInfluence);
                SetPrivateField(tool, "m_Button", dynamicThreshold);
                SetPrivateField(tool, "m_Auto", dynamicAuto);
            });
            dynamicParameters.UpdateInteractable();

            InputField staticInfluence = CreateInputField("Static Influence", "");
            Button staticThreshold = CreateButton("Static Threshold");
            Button staticAuto = CreateButton("Static Auto");
            DynamicParameters staticParameters = CreateTool<DynamicParameters>("Static Parameters", harness.Scene, staticColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_InputField", staticInfluence);
                SetPrivateField(tool, "m_Button", staticThreshold);
                SetPrivateField(tool, "m_Auto", staticAuto);
            });
            staticParameters.UpdateInteractable();

            Button fmriThreshold = CreateButton("FMRI Threshold");
            Toggle lower = CreateToggle("Lower Values");
            Toggle middle = CreateToggle("Middle Values");
            Toggle higher = CreateToggle("Higher Values");
            FMRIParameters fmriParameters = CreateTool<FMRIParameters>("FMRI Parameters", harness.Scene, ieegColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Button", fmriThreshold);
                SetPrivateField(tool, "m_LowerToggle", lower);
                SetPrivateField(tool, "m_MiddleToggle", middle);
                SetPrivateField(tool, "m_HigherToggle", higher);
            });
            fmriParameters.UpdateInteractable();

            Button mriContrastButton = CreateButton("MRI Contrast");
            MRIContrast mriContrast = CreateTool<MRIContrast>("MRI Contrast", harness.Scene, ieegColumn, selectedView, tool => SetPrivateField(tool, "m_Button", mriContrastButton));
            mriContrast.UpdateInteractable();

            Dropdown ccepAreaDropdown = CreateDropdown("CCEP Area Source");
            CCEPAreaSourceSelector ccepAreaSelector = CreateTool<CCEPAreaSourceSelector>("CCEP Area Source", harness.Scene, ccepColumn, selectedView, tool => SetPrivateField(tool, "m_MarsAtlasDropdown", ccepAreaDropdown));
            ccepAreaSelector.UpdateInteractable();
            ccepColumn.Mode = Column3DCCEP.CCEPMode.MarsAtlas;
            ccepAreaSelector.UpdateInteractable();

            Slider atlasAlpha = CreateSlider("Atlas Alpha");
            FMRIAtlasParameters fmriAtlasParameters = CreateTool<FMRIAtlasParameters>("FMRI Atlas Parameters", harness.Scene, ieegColumn, selectedView, tool => SetPrivateField(tool, "m_AlphaSlider", atlasAlpha));
            fmriAtlasParameters.UpdateInteractable();

            Button localizerAuto = CreateButton("Localizer Auto");
            LocalizersParameters localizersParameters = CreateTool<LocalizersParameters>("Localizer Parameters", harness.Scene, ieegColumn, selectedView, tool => SetPrivateField(tool, "m_Auto", localizerAuto));
            localizersParameters.UpdateInteractable();

            Slider localizersSlider = CreateSlider("Localizers Timeline Slider");
            RectTransform localizersTimelineContainer = CreateRectTransform("Localizers Timeline Container");
            GameObject zeroMarkerPrefab = new("Localizers Zero Marker");
            zeroMarkerPrefab.AddComponent<RectTransform>();
            Text localizersStartTime = CreateText("Localizers Start Time");
            Text localizersCurrentTime = CreateText("Localizers Current Time");
            Text localizersEndTime = CreateText("Localizers End Time");
            LocalizersTimeline localizersTimeline = CreateTool<LocalizersTimeline>("Localizers Timeline", harness.Scene, ieegColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Slider", localizersSlider);
                SetPrivateField(tool, "m_TimelineContainer", localizersTimelineContainer);
                SetPrivateField(tool, "m_ZeroMarkerPrefab", zeroMarkerPrefab);
                SetPrivateField(tool, "m_StartTimeText", localizersStartTime);
                SetPrivateField(tool, "m_CurrentTimeText", localizersCurrentTime);
                SetPrivateField(tool, "m_EndTimeText", localizersEndTime);
            });
            localizersTimeline.Initialize();
            SetPrivateField(harness.Scene.FMRIManager, "m_DisplayLocalizers", true);
            localizersTimeline.UpdateInteractable();

            Button saveTriangleMask = CreateButton("Save Triangle Mask");
            Button loadTriangleMask = CreateButton("Load Triangle Mask");
            TriangleErasingLoaderSaver triangleLoaderSaver = CreateTool<TriangleErasingLoaderSaver>("Triangle Loader Saver", harness.Scene, ieegColumn, selectedView, tool =>
            {
                SetPrivateField(tool, "m_Save", saveTriangleMask);
                SetPrivateField(tool, "m_Load", loadTriangleMask);
            });
            triangleLoaderSaver.UpdateInteractable();

            Assert.That(defaultViewButton.interactable, Is.True);
            Assert.That(fmriDropdown.gameObject.activeSelf, Is.False);
            Assert.That(megDropdown.gameObject.activeSelf, Is.False);
            Assert.That(dynamicParameters.gameObject.activeSelf, Is.True);
            Assert.That(dynamicInfluence.interactable, Is.True);
            Assert.That(dynamicThreshold.interactable, Is.True);
            Assert.That(staticParameters.gameObject.activeSelf, Is.True);
            Assert.That(staticInfluence.interactable, Is.True);
            Assert.That(staticThreshold.interactable, Is.True);
            Assert.That(fmriParameters.gameObject.activeSelf, Is.False);
            Assert.That(fmriThreshold.interactable, Is.False);
            Assert.That(lower.interactable, Is.False);
            Assert.That(middle.interactable, Is.False);
            Assert.That(higher.interactable, Is.False);
            Assert.That(mriContrastButton.interactable, Is.True);
            Assert.That(ccepAreaSelector.gameObject.activeSelf, Is.True);
            Assert.That(ccepAreaDropdown.interactable, Is.True);
            Assert.That(fmriAtlasParameters.gameObject.activeSelf, Is.False);
            Assert.That(localizersParameters.gameObject.activeSelf, Is.False);
            Assert.That(localizersTimeline.gameObject.activeSelf, Is.True);
            Assert.That(localizersSlider.interactable, Is.False);
            Assert.That(saveTriangleMask.interactable, Is.True);
            Assert.That(loadTriangleMask.interactable, Is.True);
        }

        [Test]
        [Category("PlayMode.Toolbar")]
        public void ExternalCommandClickPaths_UseAdaptersWithoutOpeningNativeUI()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModeSceneScope scene = new("ToolbarExternalToolbarCommands");
            using ToolbarSceneHarness harness = new(scene.Scene);
            ToolbarTestColumn selectedColumn = harness.CreateColumn("external-command-column", selected: true);
            ObjectSite selectedSite = harness.CreateSite("A4", 0, new Vector3(4, 0, 0), selectedColumn);
            SelectSite(selectedColumn, selectedSite);
            View3D selectedView = harness.CreateDetachedView(lineID: 0, selected: true);
            ApplicationState.LoadedProject = new Project("toolbar-toolbar-project", new ProjectPreferences());
            ApplicationState.LoadedProject.AddVisualization(harness.Scene.Visualization);

            List<string> saveFileRequests = new();
            List<string> loadFileRequests = new();
            List<string> openedWindows = new();
            List<bool> screenshotRequests = new();
            int videoRequests = 0;
            int loadRequests = 0;
            int siteFilterRequests = 0;
            int siteToolRequests = 0;
            int visualizationSelectorRequests = 0;
            int cancelableLoadRequests = 0;
            string openedUrl = null;

            ToolbarExternalActions.GetSavedFileNameAsync = (filters, message) =>
            {
                saveFileRequests.Add(message);
                return UniTask.FromResult(string.Empty);
            };
            ToolbarExternalActions.GetExistingFileNameAsync = (filters, message) =>
            {
                loadFileRequests.Add(message);
                return UniTask.FromResult(string.Empty);
            };
            ToolbarExternalActions.OpenWindow = name => openedWindows.Add(name);
            ToolbarExternalActions.Screenshot = (selectedScene, multi) => screenshotRequests.Add(multi);
            ToolbarExternalActions.RecordVideo = _ => videoRequests++;
            ToolbarExternalActions.LoadVisualization = (_, _) => loadRequests++;
            ToolbarExternalActions.LoadSinglePatientVisualization = (_, _) => loadRequests++;
            ToolbarExternalActions.OpenSiteFilters = _ => siteFilterRequests++;
            ToolbarExternalActions.OpenSiteTools = _ => siteToolRequests++;
            ToolbarExternalActions.SelectVisualization = (_, _) => visualizationSelectorRequests++;
            ToolbarExternalActions.LoadCancelable = _ => cancelableLoadRequests++;
            ToolbarExternalActions.OpenURL = url => openedUrl = url;

            try
            {
                Button roiImport = CreateButton("ROI Import External");
                Button roiExport = CreateButton("ROI Export External");
                ROIExport roiExportTool = CreateTool<ROIExport>("ROI Export External", harness.Scene, selectedColumn, selectedView, tool =>
                {
                    SetPrivateField(tool, "m_Import", roiImport);
                    SetPrivateField(tool, "m_Export", roiExport);
                });
                roiExportTool.Initialize();
                AssertNoException("ROI import external click", roiImport.onClick.Invoke);
                AssertNoException("ROI export external click", roiExport.onClick.Invoke);

                Button siteStateImport = CreateButton("Site State Import External");
                Button siteStateExport = CreateButton("Site State Export External");
                SiteStateExport siteStateExportTool = CreateTool<SiteStateExport>("Site State Export External", harness.Scene, selectedColumn, selectedView, tool =>
                {
                    SetPrivateField(tool, "m_Import", siteStateImport);
                    SetPrivateField(tool, "m_Export", siteStateExport);
                });
                siteStateExportTool.Initialize();
                AssertNoException("Site state import external click", siteStateImport.onClick.Invoke);
                AssertNoException("Site state export external click", siteStateExport.onClick.Invoke);

                Button triangleSave = CreateButton("Triangle Save External");
                Button triangleLoad = CreateButton("Triangle Load External");
                TriangleErasingLoaderSaver triangleLoaderSaver = CreateTool<TriangleErasingLoaderSaver>("Triangle Loader Saver External", harness.Scene, selectedColumn, selectedView, tool =>
                {
                    SetPrivateField(tool, "m_Save", triangleSave);
                    SetPrivateField(tool, "m_Load", triangleLoad);
                });
                triangleLoaderSaver.Initialize();
                AssertNoException("Triangle mask save external click", triangleSave.onClick.Invoke);
                AssertNoException("Triangle mask load external click", triangleLoad.onClick.Invoke);

                Button screenshotSingle = CreateButton("Screenshot Single External");
                Button screenshotMulti = CreateButton("Screenshot Multi External");
                Screenshot screenshot = CreateTool<Screenshot>("Screenshot External", harness.Scene, selectedColumn, selectedView, tool =>
                {
                    SetPrivateField(tool, "m_SingleScreenshot", screenshotSingle);
                    SetPrivateField(tool, "m_MultiScreenshots", screenshotMulti);
                });
                screenshot.Initialize();
                AssertNoException("Screenshot single external click", screenshotSingle.onClick.Invoke);
                AssertNoException("Screenshot multi external click", screenshotMulti.onClick.Invoke);

                Button recordVideo = CreateButton("Record External");
                TimelineRecord timelineRecord = CreateTool<TimelineRecord>("Timeline Record External", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_RecordVideo", recordVideo));
                timelineRecord.Initialize();
                AssertNoException("Timeline record external click", recordVideo.onClick.Invoke);

                Button exportActivity = CreateButton("Export Activity External");
                ExportActivityToNifti exportActivityToNifti = CreateTool<ExportActivityToNifti>("Export Activity External", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_OpenWindowButton", exportActivity));
                exportActivityToNifti.Initialize();
                AssertNoException("Export activity window external click", exportActivity.onClick.Invoke);

                Button configSave = CreateButton("Config Save External");
                Button configLoad = CreateButton("Config Load External");
                Button configReset = CreateButton("Config Reset External");
                ConfigurationLoaderSaver configuration = CreateTool<ConfigurationLoaderSaver>("Configuration External", harness.Scene, selectedColumn, selectedView, tool =>
                {
                    SetPrivateField(tool, "m_Save", configSave);
                    SetPrivateField(tool, "m_Load", configLoad);
                    SetPrivateField(tool, "m_Reset", configReset);
                });
                configuration.Initialize();
                AssertNoException("Configuration load external click", configLoad.onClick.Invoke);

                Button computeCorrelations = CreateButton("Compute Correlations External");
                Button loadCorrelations = CreateButton("Load Correlations External");
                Button saveCorrelations = CreateButton("Save Correlations External");
                Button resetCorrelations = CreateButton("Reset Correlations External");
                Toggle displayCorrelations = CreateToggle("Display Correlations External");
                SiteCorrelations siteCorrelations = CreateTool<SiteCorrelations>("Site Correlations External", harness.Scene, harness.CreateIEEGColumn("external-ieeg-column", selected: false), selectedView, tool =>
                {
                    SetPrivateField(tool, "m_Compute", computeCorrelations);
                    SetPrivateField(tool, "m_Load", loadCorrelations);
                    SetPrivateField(tool, "m_Save", saveCorrelations);
                    SetPrivateField(tool, "m_Reset", resetCorrelations);
                    SetPrivateField(tool, "m_Display", displayCorrelations);
                });
                siteCorrelations.Initialize();
                AssertNoException("Site correlations compute external click", computeCorrelations.onClick.Invoke);
                AssertNoException("Site correlations load external click", loadCorrelations.onClick.Invoke);

                Button loadPatient = CreateButton("Load Patient External");
                LoadPatient loadPatientTool = CreateTool<LoadPatient>("Load Patient External", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_Button", loadPatient));
                loadPatientTool.Initialize();
                AssertNoException("Load patient external click", loadPatient.onClick.Invoke);

                Button openViewer = CreateButton("Open Viewer External");
                OpenInteractiveViewer openInteractiveViewer = CreateTool<OpenInteractiveViewer>("Open Viewer External", harness.Scene, selectedColumn, null, tool => SetPrivateField(tool, "m_Button", openViewer));
                openInteractiveViewer.Initialize();
                AssertNoException("Open interactive viewer external click", openViewer.onClick.Invoke);

                Button openFilters = CreateButton("Open Filters External");
                Button resetFilters = CreateButton("Reset Filters External");
                SiteFilters siteFilters = CreateTool<SiteFilters>("Site Filters External", harness.Scene, selectedColumn, selectedView, tool =>
                {
                    SetPrivateField(tool, "m_OpenFiltersButton", openFilters);
                    SetPrivateField(tool, "m_ResetFiltersButton", resetFilters);
                });
                siteFilters.Initialize();
                AssertNoException("Open site filters external click", openFilters.onClick.Invoke);

                Button openTools = CreateButton("Open Tools External");
                OpenSiteTools openSiteTools = CreateTool<OpenSiteTools>("Open Site Tools External", harness.Scene, selectedColumn, selectedView, tool => SetPrivateField(tool, "m_OpenToolsButton", openTools));
                openSiteTools.Initialize();
                AssertNoException("Open site tools external click", openTools.onClick.Invoke);
            }
            finally
            {
                ToolbarExternalActions.Reset();
            }

            Assert.That(loadFileRequests, Does.Contain("Load ROI file"));
            Assert.That(loadFileRequests, Does.Contain("Load site states"));
            Assert.That(loadFileRequests, Does.Contain("Load brain state from"));
            Assert.That(loadFileRequests, Does.Contain("Load correlations"));
            Assert.That(saveFileRequests, Does.Contain("Save ROI to"));
            Assert.That(saveFileRequests, Does.Contain("Save site states to"));
            Assert.That(saveFileRequests, Does.Contain("Save brain state to"));
            Assert.That(screenshotRequests, Is.EqualTo(new[] { false, true }));
            Assert.That(videoRequests, Is.EqualTo(1));
            Assert.That(openedWindows, Is.EqualTo(new[] { "Export activity to nifti window" }));
            Assert.That(visualizationSelectorRequests, Is.EqualTo(1));
            Assert.That(cancelableLoadRequests, Is.EqualTo(1));
            Assert.That(loadRequests, Is.EqualTo(1));
            Assert.That(siteFilterRequests, Is.EqualTo(1));
            Assert.That(siteToolRequests, Is.EqualTo(1));
            Assert.That(openedUrl, Does.StartWith("https://kg.humanbrainproject.org/viewer/"));
        }

        private static T CreateTool<T>(string name, Base3DScene scene, Column3D column, View3D view, Action<T> configure) where T : Tool
        {
            GameObject gameObject = new(name);
            T tool = gameObject.AddComponent<T>();
            configure(tool);
            tool.SelectedScene = scene;
            tool.SelectedColumn = column;
            tool.SelectedView = view;
            return tool;
        }

        private static Button CreateButton(string name)
        {
            GameObject gameObject = new(name);
            gameObject.AddComponent<RectTransform>();
            gameObject.AddComponent<Image>();
            return gameObject.AddComponent<Button>();
        }

        private static Toggle CreateToggle(string name)
        {
            GameObject gameObject = new(name);
            gameObject.AddComponent<RectTransform>();
            gameObject.AddComponent<Image>();
            return gameObject.AddComponent<Toggle>();
        }

        private static Slider CreateSlider(string name)
        {
            GameObject gameObject = new(name);
            gameObject.AddComponent<RectTransform>();
            return gameObject.AddComponent<Slider>();
        }

        private static Dropdown CreateDropdown(string name, params string[] options)
        {
            GameObject gameObject = new(name);
            gameObject.AddComponent<RectTransform>();
            Dropdown dropdown = gameObject.AddComponent<Dropdown>();
            dropdown.options = new List<Dropdown.OptionData>();
            foreach (string option in options)
            {
                dropdown.options.Add(new Dropdown.OptionData(option));
            }

            return dropdown;
        }

        private static InputField CreateInputField(string name, string text)
        {
            GameObject gameObject = new(name);
            gameObject.AddComponent<RectTransform>();
            InputField inputField = gameObject.AddComponent<InputField>();
            inputField.text = text;
            return inputField;
        }

        private static Text CreateText(string name)
        {
            GameObject gameObject = new(name);
            gameObject.AddComponent<RectTransform>();
            return gameObject.AddComponent<Text>();
        }

        private static RectTransform CreateRectTransform(string name)
        {
            GameObject gameObject = new(name);
            return gameObject.AddComponent<RectTransform>();
        }

        private static void SelectSite(Column3D column, ObjectSite site)
        {
            SetPrivateField(column, "<SelectedSite>k__BackingField", site);
            site.IsSelected = true;
        }

        private static void AssertNoException(string label, Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Assert.Fail($"{label} threw {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}");
            }
        }

        private static void SetPrivateField<T>(T target, string fieldName, object value)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            field.SetValue(target, value);
        }

        private static void SetAutoProperty<T>(T target, string propertyName, object value)
        {
            SetPrivateField(target, $"<{propertyName}>k__BackingField", value);
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (field != null) return field;
                type = type.BaseType;
            }

            Assert.Fail($"Missing field {fieldName}");
            return null;
        }

        private sealed class ToolbarSceneHarness : IDisposable
        {
            private readonly GameObject m_Root;
            private readonly DataROIManager m_ROIManager;
            private readonly DisplayedObjects m_DisplayedObjects;
            private readonly MeshManager m_MeshManager;

            public Base3DScene Scene { get; }

            public ToolbarSceneHarness(UnityEngine.SceneManagement.Scene unityScene)
            {
                m_Root = new GameObject("Toolbar Toolbar Harness");
                SceneManager.MoveGameObjectToScene(m_Root, unityScene);

                GameObject sceneObject = new("Base3DScene_Toolbar");
                sceneObject.transform.SetParent(m_Root.transform, false);
                Scene = sceneObject.AddComponent<Base3DScene>();
                SetAutoProperty(Scene, "Visualization", CreateVisualization());
                SetAutoProperty(Scene, "BrainMaterials", new BrainMaterials());

                GameObject displayedObject = new("DisplayedObjects_Toolbar");
                displayedObject.SetActive(false);
                displayedObject.transform.SetParent(m_Root.transform, false);
                m_DisplayedObjects = displayedObject.AddComponent<DisplayedObjects>();
                SetPrivateField(m_DisplayedObjects, "m_Scene", Scene);
                SetPrivateField(m_DisplayedObjects, "m_BrainSurfaceMeshesParent", CreateChild("Brain Surface Meshes").transform);
                SetPrivateField(m_DisplayedObjects, "m_BrainCutMeshesParent", CreateChild("Brain Cut Meshes").transform);
                SetPrivateField(m_DisplayedObjects, "m_SitesMeshesParent", CreateChild("Sites").transform);
                SetPrivateField(m_DisplayedObjects, "m_ROIParent", CreateChild("ROIs").transform);
                SetPrivateField(m_DisplayedObjects, "m_BrainPrefab", CreateMeshPrefab("Brain Prefab"));
                SetPrivateField(m_DisplayedObjects, "m_SimplifiedBrainPrefab", CreateMeshPrefab("Simplified Brain Prefab"));
                SetPrivateField(m_DisplayedObjects, "m_InvisibleBrainPrefab", CreateMeshPrefab("Invisible Brain Prefab"));
                SetPrivateField(m_DisplayedObjects, "m_CutPrefab", CreateMeshPrefab("Cut Prefab"));
                SetPrivateField(m_DisplayedObjects, "m_SitePrefab", CreateSitePrefab());
                SetPrivateField(m_DisplayedObjects, "m_ROIPrefab", CreateROIPrefab());
                displayedObject.SetActive(true);
                m_DisplayedObjects.InstantiateBrain();
                m_DisplayedObjects.InstantiateSimplifiedBrain();

                GameObject meshManagerObject = new("MeshManager_Toolbar");
                meshManagerObject.transform.SetParent(m_Root.transform, false);
                m_MeshManager = meshManagerObject.AddComponent<MeshManager>();
                SetPrivateField(m_MeshManager, "m_Scene", Scene);
                SetPrivateField(m_MeshManager, "m_DisplayedObjects", m_DisplayedObjects);
                m_MeshManager.Meshes.Add(new LeftRightMesh3D { Name = "Synthetic LeftRight" });
                m_MeshManager.Meshes.Add(new LeftRightMesh3D { Name = "Synthetic Alternate" });

                GameObject roiManagerObject = new("ROIManager_Toolbar");
                roiManagerObject.transform.SetParent(m_Root.transform, false);
                m_ROIManager = roiManagerObject.AddComponent<DataROIManager>();
                SetPrivateField(m_ROIManager, "m_Scene", Scene);
                SetPrivateField(m_ROIManager, "m_DisplayedObjects", m_DisplayedObjects);

                GameObject implantationObject = new("ImplantationManager_Toolbar");
                implantationObject.transform.SetParent(m_Root.transform, false);
                ImplantationManager implantationManager = implantationObject.AddComponent<ImplantationManager>();
                SetPrivateField(implantationManager, "m_Scene", Scene);

                GameObject atlasObject = new("AtlasManager_Toolbar");
                atlasObject.transform.SetParent(m_Root.transform, false);
                AtlasManager atlasManager = atlasObject.AddComponent<AtlasManager>();
                SetPrivateField(atlasManager, "m_Scene", Scene);
                SetPrivateField(atlasManager, "m_DisplayedObjects", m_DisplayedObjects);

                GameObject fmriObject = new("FMRIManager_Toolbar");
                fmriObject.transform.SetParent(m_Root.transform, false);
                FMRIManager fmriManager = fmriObject.AddComponent<FMRIManager>();
                SetPrivateField(fmriManager, "m_Scene", Scene);
                SetPrivateField(fmriManager, "m_DisplayedObjects", m_DisplayedObjects);

                GameObject mriObject = new("MRIManager_Toolbar");
                mriObject.transform.SetParent(m_Root.transform, false);
                MRIManager mriManager = mriObject.AddComponent<MRIManager>();
                SetPrivateField(mriManager, "m_Scene", Scene);

                SetPrivateField(Scene, "m_MeshManager", m_MeshManager);
                SetPrivateField(Scene, "m_ROIManager", m_ROIManager);
                SetPrivateField(Scene, "m_DisplayedObjects", m_DisplayedObjects);
                SetPrivateField(Scene, "m_ImplantationManager", implantationManager);
                SetPrivateField(Scene, "m_AtlasManager", atlasManager);
                SetPrivateField(Scene, "m_FMRIManager", fmriManager);
                SetPrivateField(Scene, "m_MRIManager", mriManager);
            }

            public ToolbarTestColumn CreateColumn(string name, bool selected)
            {
                GameObject columnObject = new(name);
                columnObject.transform.SetParent(m_Root.transform, false);
                ToolbarTestColumn column = columnObject.AddComponent<ToolbarTestColumn>();
                column.Setup(name);
                column.IsSelected = selected;
                Scene.Columns.Add(column);
                return column;
            }

            public TestIEEGColumn CreateIEEGColumn(string name, bool selected)
            {
                GameObject columnObject = new(name);
                columnObject.transform.SetParent(m_Root.transform, false);
                TestIEEGColumn column = columnObject.AddComponent<TestIEEGColumn>();
                column.Setup(name);
                column.IsSelected = selected;
                Scene.Columns.Add(column);
                return column;
            }

            public TestStaticColumn CreateStaticColumn(string name, bool selected, params string[] labels)
            {
                GameObject columnObject = new(name);
                columnObject.transform.SetParent(m_Root.transform, false);
                TestStaticColumn column = columnObject.AddComponent<TestStaticColumn>();
                column.Setup(name, labels);
                column.IsSelected = selected;
                Scene.Columns.Add(column);
                return column;
            }

            public Column3DCCEP CreateCCEPColumn(string name, bool selected)
            {
                GameObject columnObject = new(name);
                columnObject.transform.SetParent(m_Root.transform, false);
                Column3DCCEP column = columnObject.AddComponent<Column3DCCEP>();
                SetAutoProperty(column, "Sites", new List<ObjectSite>());
                column.IsSelected = selected;
                Scene.Columns.Add(column);
                return column;
            }

            public ObjectSite CreateSite(string name, int index, Vector3 position, Column3D column)
            {
                GameObject siteObject = new($"Site_{name}");
                siteObject.transform.SetParent(m_Root.transform, false);
                siteObject.transform.localPosition = position;
                ObjectSite site = siteObject.AddComponent<ObjectSite>();
                site.Information = new ObjectSiteInformation
                {
                    SiteData = new DataSite(name, new[] { new Coordinate("toolbar-space", position, $"coord-{name}") }, Array.Empty<BaseTagValue>(), $"site-{name}"),
                    Patient = Scene.Visualization.Patients[0],
                    Name = name,
                    Index = index,
                    DefaultPosition = position
                };
                site.State = new ObjectSiteState();
                site.Configuration = new SiteConfiguration();
                column.Sites.Add(site);
                column.SiteStateBySiteID[site.Information.FullID] = site.State;
                return site;
            }

            public View3D CreateDetachedView(int lineID, bool selected)
            {
                GameObject viewObject = new($"Detached View {lineID}");
                viewObject.transform.SetParent(m_Root.transform, false);
                View3D view = viewObject.AddComponent<View3D>();
                view.LineID = lineID;
                view.IsSelected = selected;
                return view;
            }

            public void Dispose()
            {
                if (m_Root != null)
                {
                    UnityEngine.Object.Destroy(m_Root);
                }
            }

            private GameObject CreateChild(string name)
            {
                GameObject child = new(name);
                child.transform.SetParent(m_Root.transform, false);
                return child;
            }

            private static Visualization CreateVisualization()
            {
                Patient patient = new("toolbar-patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<DataSite>(), Array.Empty<BaseTagValue>(), string.Empty, "toolbar-patient-id");
                return new Visualization("toolbar-visualization", new[] { patient }, Array.Empty<Column>(), new VisualizationConfiguration(), "toolbar-visualization-id");
            }

            private static GameObject CreateMeshPrefab(string name)
            {
                GameObject prefab = new(name);
                prefab.SetActive(false);
                prefab.AddComponent<MeshFilter>().sharedMesh = new Mesh();
                prefab.AddComponent<MeshRenderer>();
                prefab.AddComponent<MeshCollider>();
                return prefab;
            }

            private static GameObject CreateSitePrefab()
            {
                GameObject prefab = CreateMeshPrefab("Site Prefab");
                prefab.AddComponent<ObjectSite>();
                return prefab;
            }

            private static GameObject CreateROIPrefab()
            {
                GameObject prefab = new("ROI Prefab");
                ROI roi = prefab.AddComponent<ROI>();
                GameObject spherePrefab = new("ROI Sphere Prefab");
                spherePrefab.SetActive(false);
                spherePrefab.transform.SetParent(prefab.transform, false);
                spherePrefab.AddComponent<ModuleSphere>();
                SetPrivateField(roi, "m_SpherePrefab", spherePrefab);
                return prefab;
            }
        }

        private class ToolbarTestColumn : Column3D
        {
            public void Setup(string name)
            {
                ColumnData = new AnatomicColumn(name, new BaseConfiguration());
                Layer = "Default";
                Sites = new List<ObjectSite>();
                BrainMesh = new GameObject($"{name} Brain Mesh");
                BrainMesh.transform.SetParent(transform, false);
                BrainMesh.AddComponent<MeshFilter>().sharedMesh = new Mesh();
                BrainMesh.AddComponent<MeshRenderer>();
            }

            public override void ComputeSurfaceBrainUVWithActivity()
            {
            }
        }

        private sealed class TestStaticColumn : Column3DStatic
        {
            public void Setup(string name, string[] labels)
            {
                ColumnData = new StaticColumn(name, new BaseConfiguration());
                Layer = "Default";
                Sites = new List<ObjectSite>();
                SetAutoProperty(this, "Labels", labels);
                SelectedLabelIndex = 0;
            }
        }

        private sealed class TestIEEGColumn : Column3DIEEG
        {
            private readonly Timeline m_Timeline = CreateTimeline();

            public override Timeline Timeline => m_Timeline;

            public void Setup(string name)
            {
                ColumnData = new AnatomicColumn(name, new BaseConfiguration());
                Layer = "Default";
                Sites = new List<ObjectSite>();
            }

            public override void ComputeSurfaceBrainUVWithActivity()
            {
            }

            private static Timeline CreateTimeline()
            {
                Timeline timeline = (Timeline)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Timeline));
                SetAutoProperty(timeline, "Length", 10);
                SetAutoProperty(timeline, "Unit", "ms");
                SetAutoProperty(timeline, "TimeLength", 9.0f);
                timeline.SubTimelinesBySubBloc = new Dictionary<SubBloc, HBP.Core.Data.SubTimeline>();
                timeline.OnUpdateCurrentIndex = new UnityEngine.Events.UnityEvent();
                timeline.OnStopTimelinePlay = new UnityEngine.Events.UnityEvent();
                timeline.Step = 1;
                return timeline;
            }
        }
    }
}
