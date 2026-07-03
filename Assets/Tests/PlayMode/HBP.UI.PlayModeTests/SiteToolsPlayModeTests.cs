using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Tests.PlayMode.Utilities;
using HBP.UI.Module3D;
using HBP.UI.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.PlayMode.UI
{
    public class SiteToolsPlayModeTests
    {
        private const string SiteToolsWindowResource = "Prefabs/UI/Windows/Site Tools window";

        [Test]
        [Category("PlayMode.SiteTools")]
        public async Task SiteToolsWindow_ChangeAttributesSection_AppliesOnlyFilteredSelectedColumnSites()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSelectionManagerScope selectionManager = new();
            using PlayModeSceneScope scene = new("SiteToolsSiteToolsChangeAttributes");
            using PlayModeModule3DTestHarness module3D = new(scene.Scene);
            PlayModeWindowHarness window = new(scene.Scene, "SiteTools Site Tools Window Harness");
            PlayModeProjectHarness.CreateAndLoadMinimalProject("site-tools-site-tools-change");
            SiteToolsWindow siteToolsWindow = InstantiateWindow<SiteToolsWindow>(SiteToolsWindowResource, window.Root.transform);
            siteToolsWindow.Scene = module3D.Scene;
            ChangeSitesAttributesSection section = GetSection<ChangeSitesAttributesSection>(siteToolsWindow);
            section.ApplyFor = ApplyFor.FilteredSites;

            module3D.SourceSiteA.State.IsFiltered = true;
            module3D.SourceSiteB.State.IsFiltered = false;
            module3D.TargetSiteA.State.IsFiltered = true;
            SetPrivateField(section, "m_HighlightToggle", true);
            SetPrivateField(section, "m_BlacklistToggle", true);
            SetPrivateField(section, "m_ColorToggle", true);
            GetPrivateField<Image>(section, "m_ColorPickedImage").color = Color.green;
            SetPrivateField(section, "m_AddLabelToggle", true);
            GetPrivateField<InputField>(section, "m_AddLabelInputField").text = "site-tools, window";
            GetPrivateField<Dropdown>(section, "m_ScopeDropdown").value = 0;

            await InvokeChangeSitesAttributesApplyAsync(section);

            Assert.That(module3D.SourceSiteA.State.IsHighlighted, Is.True);
            Assert.That(module3D.SourceSiteA.State.IsBlackListed, Is.True);
            Assert.That(module3D.SourceSiteA.State.Color, Is.EqualTo(Color.green));
            Assert.That(module3D.SourceSiteA.State.Labels, Is.EquivalentTo(new[] { "site-tools", "window" }));
            Assert.That(module3D.SourceSiteB.State.IsHighlighted, Is.False);
            Assert.That(module3D.SourceSiteB.State.Labels, Is.Empty);
            Assert.That(module3D.TargetSiteA.State.IsHighlighted, Is.False);

            Object.Destroy(siteToolsWindow.gameObject);
            await UniTask.Yield();
        }

        [Test]
        [Category("PlayMode.SiteTools")]
        public async Task SiteToolsWindow_CopyAttributesSection_CopiesSourceColumnSiteStateToOtherColumns()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSelectionManagerScope selectionManager = new();
            using PlayModeSceneScope scene = new("SiteToolsSiteToolsCopyAttributes");
            using PlayModeModule3DTestHarness module3D = new(scene.Scene);
            PlayModeWindowHarness window = new(scene.Scene, "SiteTools Site Tools Copy Harness");
            PlayModeProjectHarness.CreateAndLoadMinimalProject("site-tools-site-tools-copy");
            SiteToolsWindow siteToolsWindow = InstantiateWindow<SiteToolsWindow>(SiteToolsWindowResource, window.Root.transform);
            siteToolsWindow.Scene = module3D.Scene;
            CopyAttributesSection section = GetSection<CopyAttributesSection>(siteToolsWindow);
            Dropdown columnDropdown = GetPrivateField<Dropdown>(section, "m_ColumnDropdown");

            module3D.SourceSiteA.State.ApplyState(true, true, Color.yellow, new[] { "source-label" });
            module3D.TargetSiteA.State.ApplyState(false, false, Color.white, Array.Empty<string>());
            Assert.That(columnDropdown.options.Select(option => option.text), Does.Contain(module3D.SourceColumn.Name));

            module3D.Scene.ApplySiteStatesToOtherColumns(module3D.SourceColumn);

            Assert.That(module3D.TargetSiteA.State.IsBlackListed, Is.True);
            Assert.That(module3D.TargetSiteA.State.IsHighlighted, Is.True);
            Assert.That(module3D.TargetSiteA.State.Color, Is.EqualTo(Color.yellow));
            Assert.That(module3D.TargetSiteA.State.Labels, Is.EquivalentTo(new[] { "source-label" }));

            Object.Destroy(siteToolsWindow.gameObject);
            await UniTask.Yield();
        }

        [Test]
        [Category("PlayMode.SiteTools")]
        public async Task SiteToolsWindow_ExportToCsvSection_WritesSelectedSiteAttributes()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSelectionManagerScope selectionManager = new();
            using PlayModeSceneScope scene = new("SiteToolsSiteToolsExportCsv");
            using PlayModeModule3DTestHarness module3D = new(scene.Scene);
            PlayModeWindowHarness window = new(scene.Scene, "SiteTools Site Tools Export Harness");
            PlayModeProjectHarness.CreateAndLoadMinimalProject("site-tools-site-tools-export");
            SiteToolsWindow siteToolsWindow = InstantiateWindow<SiteToolsWindow>(SiteToolsWindowResource, window.Root.transform);
            siteToolsWindow.Scene = module3D.Scene;
            ExportToCSVSection section = GetSection<ExportToCSVSection>(siteToolsWindow);
            section.ApplyFor = ApplyFor.AllSites;

            module3D.SourceSiteA.State.ApplyState(true, true, Color.red, new[] { "alpha", "beta" });
            SetPrivateField(section, "m_ExportHighlighted", true);
            SetPrivateField(section, "m_ExportBlacklisted", true);
            SetPrivateField(section, "m_ExportColor", true);
            SetPrivateField(section, "m_ExportLabels", true);
            SetPrivateField(section, "m_ExportPosition", false);
            SetPrivateField(section, "m_ExportData", false);
            SetPrivateField(section, "m_ExportTags", false);
            string csvPath = temp.GetPath("site-tools-sites.csv");

            await InvokeExportSitesAsync(section, module3D.SourceColumn.Sites, csvPath);

            string csv = File.ReadAllText(csvPath);
            Assert.That(csv, Does.Contain("Site,Highlighted,Blacklisted,Color,Labels"));
            Assert.That(csv, Does.Contain($"{module3D.SourceSiteA.Information.FullID},True,True,#FF0000,alpha;beta"));

            Object.Destroy(siteToolsWindow.gameObject);
            await UniTask.Yield();
        }

        private sealed class PlayModeSelectionManagerScope : IDisposable
        {
            private readonly GameObject m_GameObject;

            public PlayModeSelectionManagerScope()
            {
                ResetSingleton();
                m_GameObject = new GameObject("SelectionManager_PlayModeTest");
                m_GameObject.AddComponent<SelectionManager>();
            }

            public void Dispose()
            {
                if (m_GameObject != null)
                {
                    Object.Destroy(m_GameObject);
                }
                ResetSingleton();
            }

            private static void ResetSingleton()
            {
                FieldInfo field = typeof(Singleton<SelectionManager>).GetField("m_Instance", BindingFlags.NonPublic | BindingFlags.Static);
                field.SetValue(null, null);
            }
        }

        private static T InstantiateWindow<T>(string resourcePath, Transform parent) where T : Component
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            Assert.That(prefab, Is.Not.Null, resourcePath);
            GameObject instance = Object.Instantiate(prefab, parent);
            T component = instance.GetComponent<T>();
            Assert.That(component, Is.Not.Null, resourcePath);
            return component;
        }

        private static T GetSection<T>(SiteToolsWindow window) where T : SiteToolSection
        {
            SiteToolSection[] sections = GetPrivateField<SiteToolSection[]>(window, "m_SiteToolSections");
            T section = sections.OfType<T>().SingleOrDefault();
            Assert.That(section, Is.Not.Null, typeof(T).Name);
            return section;
        }

        private static async UniTask InvokeChangeSitesAttributesApplyAsync(ChangeSitesAttributesSection section)
        {
            MethodInfo method = typeof(ChangeSitesAttributesSection).GetMethod("ApplyAsync", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Action<float, float, LoadingText>), typeof(CancellationToken) }, null);
            Assert.That(method, Is.Not.Null);
            var task = (UniTask)method.Invoke(section, new object[] { (Action<float, float, LoadingText>)NoProgress, CancellationToken.None });
            await task;
        }

        private static async UniTask InvokeExportSitesAsync(ExportToCSVSection section, System.Collections.Generic.List<Site> sites, string csvPath)
        {
            MethodInfo method = typeof(ExportToCSVSection).GetMethod("ExportSitesAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);
            var task = (UniTask)method.Invoke(section, new object[] { sites, csvPath, (Action<float, float, LoadingText>)NoProgress, CancellationToken.None });
            try
            {
                await task;
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"{target.GetType().FullName}.{fieldName}");
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, bool value)
        {
            GetPrivateField<Toggle>(target, fieldName).isOn = value;
        }

        private static void NoProgress(float progress, float duration, LoadingText text)
        {
        }
    }
}
