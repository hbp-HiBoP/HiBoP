#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Quest;
using HBP.UI.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.Quest
{
    public sealed class QuestDisplaySettingsTests
    {
        [Test]
        public void ControlsMutateTheSelectedSceneAndRefreshWithoutEmittingMutations()
        {
            using var f = new Fixture();
            int mutations = 0;
            f.Scene.OnSharedStateChanged.AddListener(() => mutations++);
            f.Control<Toggle>("edges").isOn = true;
            f.Control<Toggle>("showAllSites").isOn = true;
            f.Control<Toggle>("hideBlacklisted").isOn = true;
            f.Control<Toggle>("automaticCuts").isOn = true;
            f.Control<Slider>("siteSize").value = 1.7f;
            Assert.That(f.Scene.EdgeMode && f.Scene.ShowAllSites && f.Scene.HideBlacklistedSites && f.Scene.AutomaticCutAroundSelectedSite, Is.True);
            Assert.That(f.Scene.SiteGain, Is.EqualTo(1.7f));
            f.Scene.SiteGain = .4f;
            f.Scene.EdgeMode = false;
            Set(f.Scene, "m_Colormap", ColorType.Hot);
            int before = mutations;
            f.Panel.Refresh();
            Assert.That(f.Control<Slider>("siteSize").value, Is.EqualTo(.4f));
            Assert.That(f.Control<Toggle>("edges").isOn, Is.False);
            Assert.That(f.Control<Dropdown>("colormap").value, Is.EqualTo(1));
            Assert.That(mutations, Is.EqualTo(before), "Reading shared state must not replay UI setters.");
        }

        [Test]
        public void TransparencyRetainsOpacityAndUnavailableControlsStayDisabled()
        {
            using var f = new Fixture();
            Assert.That(f.Control<Slider>("opacity").interactable, Is.False);
            f.Control<Toggle>("transparent").isOn = true;
            f.Control<Slider>("opacity").value = .45f;
            Assert.That(f.Scene.BrainMaterials.Alpha, Is.EqualTo(.45f));
            f.Control<Toggle>("transparent").isOn = false;
            Assert.That(f.Control<Slider>("opacity").interactable, Is.False);
            Assert.That(f.Scene.BrainMaterials.Alpha, Is.EqualTo(.45f));
            Assert.That(f.Control<Toggle>("inflated").interactable, Is.False, "The fixture has no loaded native surface.");
            Assert.That(f.Control<Dropdown>("mri").interactable, Is.False);
            f.Scene.UpdateVisibleState(false, false);
            f.Panel.Refresh();
            Assert.That(f.Control<Slider>("siteSize").interactable, Is.False);
            f.Control<Slider>("siteSize").onValueChanged.Invoke(1.9f);
            Assert.That(f.Scene.SiteGain, Is.EqualTo(1));
        }

        [Test]
        public async Task UnavailableAtlasAndOpacityUseSelectableStatesAndRestoreTheirEnabledAppearance()
        {
            using var f = new Fixture();
            await UniTask.NextFrame();
            await UniTask.NextFrame();
            var dark = AssetDatabase.LoadAssetAtPath<HBP.Theme.Color>("Assets/Resources/Themes/Main/Settings/Colors/Disabled Color.asset").Value;
            foreach (string field in new[] { "marsAtlas", "juBrain", "ibc", "difumo" })
            {
                var toggle = f.Control<Toggle>(field);
                Assert.That(toggle.interactable, Is.False, field);
                Assert.That(toggle.GetComponent<HBP.Theme.SelectableStateSetter>(), Is.Not.Null, field);
                Assert.That(toggle.transform.Find("Icon").GetComponent<Image>().color, Is.EqualTo(dark), field);
                Assert.That(toggle.transform.Find("Label").GetComponent<Text>().color, Is.EqualTo(dark), field);
            }

            var opacity = f.Control<Slider>("opacity");
            Assert.That(opacity.fillRect.GetComponent<Image>().color, Is.EqualTo(dark));
            Assert.That(opacity.handleRect.GetComponent<Image>().color, Is.EqualTo(dark));
            f.Control<Toggle>("transparent").isOn = true;
            await UniTask.NextFrame();
            await UniTask.NextFrame();
            Assert.That(opacity.interactable, Is.True);
            Assert.That(opacity.fillRect.GetComponent<Image>().color, Is.Not.EqualTo(dark));
            Assert.That(opacity.handleRect.GetComponent<Image>().color, Is.Not.EqualTo(dark));
            f.Panel.enabled = false;
            var atlas = f.Control<Toggle>("marsAtlas");
            atlas.interactable = true;
            await UniTask.NextFrame();
            await UniTask.NextFrame();
            Assert.That(atlas.transform.Find("Icon").GetComponent<Image>().color, Is.Not.EqualTo(dark));
            Assert.That(atlas.transform.Find("Label").GetComponent<Text>().color, Is.Not.EqualTo(dark));
        }

        [Test]
        public void AtlasParametersFollowSharedFlagsAndRejectedChoicePreservesOverlay()
        {
            using var f = new Fixture();
            Set(f.Scene.FMRIManager, "m_DisplayIBCContrasts", true);
            Set(f.Scene.FMRIManager, "m_FMRIAlpha", .35f);
            f.Panel.Refresh();
            Assert.That(f.Control<GameObject>("ibcParameters").activeSelf, Is.True);
            Assert.That(f.Control<GameObject>("difumoParameters").activeSelf, Is.False);
            f.Scene.AtlasUseValidator = _ => throw new InvalidOperationException("Atlas is not ready on both devices.");
            f.Control<Toggle>("marsAtlas").onValueChanged.Invoke(true);
            Assert.That(f.Scene.FMRIManager.DisplayIBCContrasts, Is.True);
            Assert.That(f.Scene.AtlasManager.DisplayMarsAtlas, Is.False);
            Assert.That(f.Control<Text>("status").text, Does.Contain("not ready"));
            Set(f.Scene.FMRIManager, "m_DisplayIBCContrasts", false);
            Set(f.Scene.FMRIManager, "m_DisplayDiFuMo", true);
            f.Panel.Refresh();
            Assert.That(f.Control<GameObject>("ibcParameters").activeSelf, Is.False);
            Assert.That(f.Control<GameObject>("difumoParameters").activeSelf, Is.True);
            Set(f.Scene.FMRIManager, "m_DisplayDiFuMo", false);
            Set(f.Scene.FMRIManager, "m_DisplayLocalizers", true);
            f.Panel.Refresh();
            Assert.That(f.Control<GameObject>("localizerNotice").activeSelf, Is.True);
            Assert.That(f.Scene.FMRIManager.DisplayLocalizers, Is.True);
            Assert.That(Field(f.Scene.FMRIManager, "m_FMRIAlpha"), Is.EqualTo(.35f));
        }

        [Test]
        public void ChangingDifumoVersionUsesTheSharedContrastReset()
        {
            using var f = new Fixture();
            Set(f.Scene.FMRIManager, "m_SelectedDiFuMoArea", 7);
            f.Control<Dropdown>("difumoVersion").AddOptions(new System.Collections.Generic.List<string> { "64", "128" });
            f.Control<Dropdown>("difumoVersion").onValueChanged.Invoke(1);
            Assert.That(f.Scene.FMRIManager.SelectedDiFuMoAtlas, Is.EqualTo("128"));
            Assert.That(f.Scene.FMRIManager.SelectedDiFuMoArea, Is.Zero);
        }

        [Test]
        public void SelectionLossDisablesControlsAndRejectsStaleInput()
        {
            using var f = new Fixture();
            QuestSceneTestBinding.Clear(f.View);
            f.Control<Slider>("siteSize").onValueChanged.Invoke(1.8f);
            Assert.That(f.Scene.SiteGain, Is.EqualTo(1));
            f.Panel.Refresh();
            Assert.That(f.Control<Slider>("siteSize").interactable, Is.False);
        }

        [Test]
        public void RemoveProjectionClearsTheSharedRequestAndDisablesTheButton()
        {
            using var f = new Fixture();
            Assert.That(f.Control<Button>("removeProjection").interactable, Is.False);
            f.Scene.SetProjectionEnabled(true);
            f.Panel.Refresh();
            Assert.That(f.Control<Button>("removeProjection").interactable, Is.True);
            f.Control<Button>("removeProjection").onClick.Invoke();
            Assert.That(f.Scene.ProjectionRequested, Is.False);
            Assert.That(f.Scene.SceneInformation.GeneratorUpdateRequested, Is.False);
            Assert.That(f.Control<Button>("removeProjection").interactable, Is.False);
        }

        [Test]
        public async Task SharedInflationUsesTheSceneCoordinatorWithoutOpeningALocalLoader()
        {
            using var f = new Fixture();
            var loading = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Loading Manager.prefab"));
            try
            {
                int requests = 0;
                f.Scene.SurfaceRepresentationRequestHandler = (representation, progress, token, animate) =>
                {
                    requests++;
                    Assert.That(representation, Is.EqualTo(SurfaceRepresentation.Inflated));
                    Assert.That(animate, Is.True);
                    Assert.That(Field(loading.GetComponent<LoadingManager>(), "m_PresentationUsers"), Is.Zero);
                    return UniTask.CompletedTask;
                };
                Assert.That(SurfaceInflationLoading.RequiresLoadingManager(f.Scene, SurfaceRepresentation.Inflated), Is.False);
                await SurfaceInflationLoading.ChangeRepresentationAsync(f.Scene, SurfaceRepresentation.Inflated);
                Assert.That(requests, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(loading);
            }
        }

        [Test]
        public async Task ProductionLayoutScrollsAtMinimumHeightAndClosesPopupOnContextLoss()
        {
            using var f = new Fixture();
            var cameraRoot = new GameObject("Settings layout camera", typeof(Camera));
            var target = new RenderTexture(1200, 1100, 24);
            var pixels = new Texture2D(1200, 1100, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                var camera = cameraRoot.GetComponent<Camera>();
                camera.transform.position = new Vector3(0, 0, -3);
                camera.orthographic = true;
                camera.orthographicSize = 1.05f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.08f, .09f, .11f);
                camera.cullingMask = 1 << 31;
                camera.targetTexture = target;
                foreach (var child in f.Window.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                f.Window.GetComponent<Canvas>().worldCamera = camera;
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                var rows = f.Control<Dropdown>("mesh").transform.parent.parent.parent;
                Assert.That(((RectTransform)rows).rect.width, Is.GreaterThan(800));
                var colors = f.Control<Dropdown>("colormap").GetComponent<RectTransform>();
                Assert.That(colors.rect.width, Is.GreaterThan(200));
                Assert.That(((RectTransform)f.Control<Dropdown>("brainColor").transform).rect.width, Is.EqualTo(colors.rect.width).Within(.1f));
                Assert.That(((RectTransform)f.Control<Dropdown>("mriColor").transform).rect.width, Is.EqualTo(colors.rect.width).Within(.1f));
                foreach (var icon in rows.GetComponentsInChildren<Image>(true).Where(image => image.name == "Icon" || image.name == "Size Icon"))
                {
                    Assert.That(((RectTransform)icon.transform).rect.width, Is.EqualTo(40).Within(.1f), icon.transform.parent.name);
                    Assert.That(((RectTransform)icon.transform).rect.height, Is.EqualTo(40).Within(.1f), icon.transform.parent.name);
                }

                float sliderCenter = f.Control<Slider>("siteSize").transform.TransformPoint(((RectTransform)f.Control<Slider>("siteSize").transform).rect.center).y;
                foreach (string path in new[] { "Size Row/Size Icon", "Size Row/Size Caption/Label", "Size Row/Size Value/Label" })
                {
                    var element = (RectTransform)rows.Find(path);
                    Assert.That(element.TransformPoint(element.rect.center).y, Is.EqualTo(sliderCenter).Within(.001f), path);
                }

                Assert.That(((RectTransform)rows.Find("Visibility Row")).rect.height, Is.EqualTo(48).Within(.1f));
                Assert.That(f.Window.GetComponentInChildren<ScrollRect>().viewport.GetComponent<Image>().color.a, Is.Zero, "The full-window viewport must reveal the ordinary window background.");
                Directory.CreateDirectory("Logs/QuestUI");
                Capture("Display Settings.png");
                ((RectTransform)f.Window.transform).sizeDelta = new Vector2(900, 480);
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                var scroll = f.Window.GetComponentInChildren<ScrollRect>();
                Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
                scroll.verticalNormalizedPosition = 0;
                await UniTask.NextFrame();
                Capture("Display Settings minimum.png");
                scroll.verticalNormalizedPosition = 1;
                f.Window.GetComponentInChildren<QuestWindowContentZoom>(true).SetZoomPercent(150);
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
                Capture("Display Settings zoom.png");
                f.Window.GetComponentInChildren<QuestWindowContentZoom>(true).SetZoomPercent(100);
                ((RectTransform)f.Window.transform).sizeDelta = new Vector2(960, 900);
                await UniTask.NextFrame();
                f.Control<Dropdown>("colormap").Show();
                await UniTask.Delay(200, ignoreTimeScale: true);
                Assert.That(f.Control<Dropdown>("colormap").transform.Find("Dropdown List"), Is.Not.Null);
                Capture("Display Settings palettes.png");
                f.Control<Dropdown>("colormap").value = 1;
                Assert.That(f.Control<Dropdown>("colormap").captionImage.sprite, Is.SameAs(f.Control<Dropdown>("colormap").options[1].image));
                Assert.That(f.Control<Dropdown>("colormap").transform.Find("Label").GetComponent<Text>().text, Is.EqualTo("Functional"));
                QuestSceneTestBinding.Clear(f.View);
                f.Panel.Refresh();
                await UniTask.Delay(200, ignoreTimeScale: true);
                Assert.That(f.Control<Dropdown>("colormap").transform.Find("Dropdown List"), Is.Null);
                QuestSceneTestBinding.Attach(f.View, f.Scene);
                f.Panel.Refresh();
                ((RectTransform)f.Window.transform).sizeDelta = new Vector2(960, 900);
                await UniTask.NextFrame();
                f.Control<Dropdown>("mesh").AddOptions(Enumerable.Range(1, 30).Select(i => "Anatomical mesh " + i + " - long resource name").ToList());
                f.Control<Dropdown>("mesh").Show();
                await UniTask.Delay(200, ignoreTimeScale: true);
                Capture("Display Settings long dropdown.png");
                f.Control<Dropdown>("mesh").Hide();

                void Capture(string name)
                {
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, 1200, 1100), 0, 0);
                    File.WriteAllBytes(Path.Combine("Logs/QuestUI", name), pixels.EncodeToPNG());
                }
            }
            finally
            {
                RenderTexture.active = previous;
                cameraRoot.GetComponent<Camera>().targetTexture = null;
                Object.DestroyImmediate(cameraRoot);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
            }
        }

        private static object Field(object target, string field) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Set(object target, string field, object value) => QuestWindowInteractionTests.Set(target, field, value);

        private sealed class Fixture : IDisposable
        {
            private readonly GameObject data = new("Inactive settings scene");
            private readonly GameObject root = new("Settings window fixture");
            private readonly Mesh colors = new();
            private readonly BrainMaterials materials;
            public readonly Base3DScene Scene;
            public readonly QuestAnatomyView View;
            public readonly QuestWindow Window;
            public readonly QuestDisplaySettingsPanel Panel;
            public T Control<T>(string field) where T : Object => (T)Field(Panel, field);

            public Fixture()
            {
                data.SetActive(false);
                Scene = QuestSceneTestBinding.VisibleScene(data);
                Set(Scene, "m_MeshManager", data.AddComponent<MeshManager>());
                Scene.MeshManager.Meshes.Add(new SingleMesh3D { Name = "Synthetic mesh" });
                Set(Scene, "m_MRIManager", data.AddComponent<MRIManager>());
                Set(Scene, "m_ImplantationManager", data.AddComponent<ImplantationManager>());
                Set(Scene, "m_AtlasManager", data.AddComponent<AtlasManager>());
                Set(Scene.AtlasManager, "m_Scene", Scene);
                Set(Scene, "m_FMRIManager", data.AddComponent<FMRIManager>());
                Set(Scene.FMRIManager, "m_Scene", Scene);
                materials = new BrainMaterials();
                typeof(Base3DScene).GetProperty("BrainMaterials").SetValue(Scene, materials);
                var displayed = data.AddComponent<DisplayedObjects>();
                Set(Scene, "m_DisplayedObjects", displayed);
                var brain = new GameObject("Synthetic color surface", typeof(MeshFilter), typeof(MeshRenderer));
                brain.transform.SetParent(data.transform, false);
                brain.GetComponent<MeshFilter>().sharedMesh = colors;
                typeof(DisplayedObjects).GetProperty("Brain").SetValue(displayed, brain);
                var simplified = new GameObject("Synthetic simplified surface");
                simplified.transform.SetParent(data.transform, false);
                typeof(DisplayedObjects).GetProperty("SimplifiedBrain").SetValue(displayed, simplified);
                typeof(DisplayedObjects).GetProperty("BrainCutMeshes").SetValue(displayed, new System.Collections.Generic.List<GameObject>());
                Set(Scene.FMRIManager, "m_DisplayedObjects", displayed);
                View = data.AddComponent<QuestAnatomyView>();
                QuestSceneTestBinding.Attach(View, Scene);
                var window = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quest/UI/Quest Display Settings Window.prefab"), root.transform);
                Window = window.GetComponent<QuestWindow>();
                window.GetComponent<QuestWindowFollower>().enabled = false;
                window.transform.position = Vector3.zero;
                Panel = window.GetComponent<QuestDisplaySettingsPanel>();
                Set(Panel, "view", View);
                Window.Open();
                Panel.Refresh();
            }

            public void Dispose()
            {
                QuestSceneTestBinding.Clear(View);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(Scene.BrainColorMapTexture);
                Object.DestroyImmediate(data);
                Object.DestroyImmediate(colors);
                foreach (var field in typeof(BrainMaterials).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(f => f.FieldType == typeof(Material)))
                    Object.DestroyImmediate((Material)field.GetValue(materials));
            }
        }
    }
}
#endif
