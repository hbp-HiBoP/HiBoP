using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.UI.Module3D;
using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Toolbar
{
    public static class ToolbarExternalActions
    {
        public static Func<string[], string, UniTask<string>> GetSavedFileNameAsync { get; set; } = DefaultGetSavedFileNameAsync;
        public static Func<string[], string, UniTask<string>> GetExistingFileNameAsync { get; set; } = DefaultGetExistingFileNameAsync;
        public static Action<string> OpenWindow { get; set; } = DefaultOpenWindow;
        public static Action<IEnumerable<Visualization>, Action<Visualization>> SelectVisualization { get; set; } = DefaultSelectVisualization;
        public static Action<Base3DScene, bool> Screenshot { get; set; } = DefaultScreenshot;
        public static Action<Base3DScene> RecordVideo { get; set; } = DefaultRecordVideo;
        public static Action<Base3DScene, Visualization> LoadVisualization { get; set; } = DefaultLoadVisualization;
        public static Action<Base3DScene, Patient> LoadSinglePatientVisualization { get; set; } = DefaultLoadSinglePatientVisualization;
        public static Action<Base3DScene> OpenSiteFilters { get; set; } = DefaultOpenSiteFilters;
        public static Action<Base3DScene> OpenSiteTools { get; set; } = DefaultOpenSiteTools;
        public static Action<string> OpenURL { get; set; } = Application.OpenURL;
        public static Action<Func<Action<float, float, LoadingText>, CancellationToken, UniTask>> LoadCancelable { get; set; } = DefaultLoadCancelable;

        public static void Reset()
        {
            GetSavedFileNameAsync = DefaultGetSavedFileNameAsync;
            GetExistingFileNameAsync = DefaultGetExistingFileNameAsync;
            OpenWindow = DefaultOpenWindow;
            SelectVisualization = DefaultSelectVisualization;
            Screenshot = DefaultScreenshot;
            RecordVideo = DefaultRecordVideo;
            LoadVisualization = DefaultLoadVisualization;
            LoadSinglePatientVisualization = DefaultLoadSinglePatientVisualization;
            OpenSiteFilters = DefaultOpenSiteFilters;
            OpenSiteTools = DefaultOpenSiteTools;
            OpenURL = Application.OpenURL;
            LoadCancelable = DefaultLoadCancelable;
        }

        private static UniTask<string> DefaultGetSavedFileNameAsync(string[] filters, string message)
        {
            return FileBrowser.GetSavedFileNameAsync(filters, message);
        }

        private static UniTask<string> DefaultGetExistingFileNameAsync(string[] filters, string message)
        {
            return FileBrowser.GetExistingFileNameAsync(filters, message);
        }

        private static void DefaultOpenWindow(string name)
        {
            WindowsManager.Open(name, null);
        }

        private static void DefaultSelectVisualization(IEnumerable<Visualization> visualizations, Action<Visualization> onSelected)
        {
            ObjectSelector<Visualization> selector = WindowsManager.OpenSelector(visualizations, null, false);
            selector.OnOk.AddListener(() =>
            {
                if (selector.ObjectsSelected.Length > 0)
                {
                    onSelected(selector.ObjectsSelected[0]);
                }
            });
        }

        private static void DefaultScreenshot(Base3DScene scene, bool multi)
        {
            Module3DUI.Scenes[scene].Screenshot(multi).Forget();
        }

        private static void DefaultRecordVideo(Base3DScene scene)
        {
            Module3DUI.Scenes[scene].Video();
        }

        private static void DefaultLoadVisualization(Base3DScene scene, Visualization visualization)
        {
            LoadingManager.Load((update, token) => Module3DMain.LoadAsync(new[] { visualization }, update, token));
        }

        private static void DefaultLoadSinglePatientVisualization(Base3DScene scene, Patient patient)
        {
            Visualization visualization = Module3DMain.PrepareSinglePatientVisualizationFromMultiPatientScene(scene.Visualization, patient);
            DefaultLoadVisualization(scene, visualization);
        }

        private static void DefaultOpenSiteFilters(Base3DScene scene)
        {
            var sites = scene.Columns.SelectMany(c => c.Sites).Where(s => !s.State.IsMasked).Select(s => (object)s).ToList();

            if (sites.Count == 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No sites", "There are no unmasked sites in the current scene to filter.").Forget();
                return;
            }

            var siteFilters = WindowsManager.Open("Site Filters window", null).GetComponent<SiteFiltersWindow>();
            siteFilters.FilteringObjects = sites;
            siteFilters.SetPreset(PersistentDataManager.FilterConditionsPresets.GetCurrentPreset(typeof(HBP.Core.Object3D.Site)));
            siteFilters.OnApplyFilters.AddListener(mask =>
            {
                for (int i = 0; i < mask.Length; i++) ((HBP.Core.Object3D.Site)siteFilters.FilteringObjects[i]).State.IsFiltered = mask[i];
                Module3DMain.OnRequestUpdateInSiteList.Invoke();
            });
            Module3DMain.OnRemoveScene.AddSafeListener(s =>
            {
                if (scene == s) siteFilters.Close();
            }, siteFilters.gameObject);
        }

        private static void DefaultOpenSiteTools(Base3DScene scene)
        {
            var siteTools = WindowsManager.Open("Site Tools window", null).GetComponent<SiteToolsWindow>();
            siteTools.Scene = scene;
            siteTools.OnToolApplied.AddListener(Module3DMain.OnRequestUpdateInSiteList.Invoke);

            Module3DMain.OnRemoveScene.AddSafeListener(s =>
            {
                if (scene == s) siteTools.Close();
            }, siteTools.gameObject);
        }

        private static void DefaultLoadCancelable(Func<Action<float, float, LoadingText>, CancellationToken, UniTask> task)
        {
            LoadingManager.Load(task);
        }
    }
}
