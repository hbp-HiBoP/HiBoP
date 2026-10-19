using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Tests.PlayMode.Utilities;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using HBP.Core.Enums;
using HBP.Core.Preferences;
using HBP.UI.Main;
using HBP.UI.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using DataManager = HBP.Core.Data.DataManager;

namespace HBP.Tests.PlayMode.UI
{
    public sealed class SessionPreferencesUiTests
    {
        [Test]
        public async Task AtlasLoadButtonReceivesPointerClicksInTheCompletePreferencesWindow()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModePersistentDataScope preferences = new(temp.Path);
            using PlayModeSceneScope scene = new("AtlasPreferencesPointer");
            var canvas = new PlayModeWindowHarness(scene.Scene, "Preferences canvas");
            var selectionRoot = SelectionManager.IsInitialized ? null : new GameObject("Atlas pointer selection");
            if (selectionRoot != null) selectionRoot.AddComponent<SelectionManager>();
            var root = Object.Instantiate(Resources.Load<GameObject>("Prefabs/UI/Windows/User preferences window"), canvas.Root.transform);
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = Vector2.zero;
                var atlas = root.GetComponentInChildren<AtlasesPreferencesSubModifier>(true);
                root.GetComponent<UserPreferencesModifier>().Object = PersistentDataManager.UserPreferences;
                root.GetComponent<UserPreferencesModifier>().Interactable = true;
                var navigation = root.GetComponent<ToggleNavigator>();
                navigation.Navigate("Data");
                navigation.Navigate("Data_Atlas");
                await UniTask.NextFrame();
                Canvas.ForceUpdateCanvases();
                var button = (Button)typeof(AtlasesPreferencesSubModifier).GetField("m_LoadMarsAtlas", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(atlas);
                Assert.That(button.IsActive() && button.IsInteractable(), Is.True);
                var buttonRect = button.GetComponent<RectTransform>();
                var pointer = new PointerEventData(canvas.EventSystem) { position = RectTransformUtility.WorldToScreenPoint(null, buttonRect.TransformPoint(buttonRect.rect.center)) };
                var hits = new List<RaycastResult>();
                canvas.EventSystem.RaycastAll(pointer, hits);
                Assert.That(hits, Is.Not.Empty);
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), Is.SameAs(button.gameObject), "The frontmost UI element must deliver the click to the atlas Load button.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (selectionRoot != null) Object.DestroyImmediate(selectionRoot);
            }
        }

        [TestCase("mars", "m_LoadMarsAtlas", false, false), TestCase("mars", "m_LoadMarsAtlas", true, false)]
        [TestCase("ibc", "m_LoadIBC", false, false), TestCase("ibc", "m_LoadIBC", true, false)]
        [TestCase("difumo:64", "m_LoadDiFuMo64", false, false), TestCase("difumo:64", "m_LoadDiFuMo64", true, false)]
        [TestCase("ibc", "m_LoadIBC", true, true), TestCase("difumo:64", "m_LoadDiFuMo64", true, true)]
        public async Task AtlasLoadButtonsLoadAndUnloadFromTheAuthoredPreferences(string id, string buttonField, bool questManagerExists, bool queued)
        {
            var dataPath = typeof(ApplicationState).GetProperty(nameof(ApplicationState.DataPath));
            string previousPath = ApplicationState.DataPath;
            var previousAtlas = Object3DManager.MarsAtlas;
            var previousIBC = Object3DManager.IBC;
            var previousDiFuMo = Object3DManager.DiFuMo;
            var questSingleton = typeof(Singleton<HBP.Quest.Desktop.QuestManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previousQuest = questSingleton.GetValue(null);
            var managerRoot = new GameObject("Atlas preferences Quest fixture");
            managerRoot.SetActive(false);
            var manager = questManagerExists ? managerRoot.AddComponent<HBP.Quest.Desktop.QuestManager>() : null;
            questSingleton.SetValue(null, manager);
            var prefab = Resources.Load<GameObject>("Prefabs/UI/Windows/User preferences window");
            var modifier = UnityEngine.Object.Instantiate(prefab.GetComponentInChildren<AtlasesPreferencesSubModifier>(true));
            using var timeout = new CancellationTokenSource(System.TimeSpan.FromSeconds(30));
            var gate = manager == null ? null : (SemaphoreSlim)manager.GetType().GetField("sessionOperations", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            Task pending = null;
            bool held = false;
            int updates = 0;
            UnityEngine.Events.UnityAction updated = () => updates++;
            HBP.Data.Module3D.Module3DMain.OnRequestUpdateInToolbar.AddListener(updated);

            async Task ClickAsync(Button button, bool load)
            {
                if (queued)
                {
                    await gate.WaitAsync(timeout.Token);
                    held = true;
                    pending = ((UniTask)typeof(AtlasesPreferencesSubModifier).GetMethod("ToggleAtlasAsync", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(modifier, new object[] { id })).AsTask();
                    await UniTask.WaitUntil(() => pending.IsCompleted, cancellationToken: timeout.Token);
                    await pending;
                    Assert.That(AtlasResources.IsLoaded(id), Is.EqualTo(load), "Desktop load/unload must finish even while pairing owns the Quest gate.");
                    Assert.That(gate.CurrentCount, Is.Zero, "The local operation must not release another operation's session gate.");
                    gate.Release();
                    held = false;
                }
                else button.onClick.Invoke();

                await UniTask.WaitUntil(() => AtlasResources.IsLoaded(id) == load, cancellationToken: timeout.Token);
                await UniTask.NextFrame();
            }

            try
            {
                dataPath.SetValue(null, Path.GetFullPath("Assets/Data"));
                Object3DManager.MarsAtlas = new HBP.Core.DLL.MarsAtlas();
                Object3DManager.IBC = new IBCObjects();
                Object3DManager.DiFuMo = new DiFuMoObjects();
                modifier.Object = new AtlasesPreferences();
                modifier.Interactable = true;
                modifier.gameObject.SetActive(true);
                await UniTask.NextFrame();
                var button = (Button)typeof(AtlasesPreferencesSubModifier).GetField(buttonField, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(modifier);
                Assert.That(button.IsInteractable(), Is.True);
                Assert.That(button.GetComponentInChildren<Text>().text, Is.EqualTo("Load"));
                await ClickAsync(button, true);
                Assert.That(button.IsInteractable(), Is.True);
                Assert.That(button.GetComponentInChildren<Text>().text, Is.EqualTo("Unload"));
                Assert.That(button.GetComponent<Tooltip>(), Is.Not.Null, "Atlas availability must be explained on the authored Load/Unload button.");
                Assert.That(button.GetComponent<Tooltip>().Text, Does.Contain("Desktop"));
                await ClickAsync(button, false);
                Assert.That(button.GetComponentInChildren<Text>().text, Is.EqualTo("Load"));
                Assert.That(updates, Is.EqualTo(2), "Each click must complete exactly one operation, including queued loads and unloads.");
            }
            finally
            {
                if (held) gate.Release();
                if (pending != null) await pending;
                HBP.Data.Module3D.Module3DMain.OnRequestUpdateInToolbar.RemoveListener(updated);
                UnityEngine.Object.DestroyImmediate(modifier.gameObject);
                UnityEngine.Object.DestroyImmediate(managerRoot);
                questSingleton.SetValue(null, previousQuest);
                Object3DManager.MarsAtlas.Dispose();
                Object3DManager.MarsAtlas = previousAtlas;
                Object3DManager.IBC.Clean();
                Object3DManager.IBC = previousIBC;
                Object3DManager.DiFuMo.Clean();
                Object3DManager.DiFuMo = previousDiFuMo;
                dataPath.SetValue(null, previousPath);
            }
        }

        [Test]
        public void TagParsingRefreshKeepsValuesSeparatedWithoutChangingTheDraft()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/UI/Windows/User preferences window");
            var modifier = Object.Instantiate(prefab.GetComponentInChildren<TagImportPreferencesSubModifier>(true));
            modifier.gameObject.SetActive(false);
            try
            {
                var preferences = new TagImportPreferences();
                preferences.TrueValues.Add(" TRUE ");
                var originalValues = preferences.TrueValues.ToArray();
                var input = modifier.transform.Find("True values/InputField").GetComponent<InputField>();
                int changeCount = 0;
                input.onValueChanged.AddListener(_ => changeCount++);

                modifier.Object = preferences;
                modifier.Refresh();
                modifier.Refresh();

                Assert.That(input.text, Is.EqualTo("true,yes"));
                Assert.That(preferences.TrueValues, Is.EqualTo(originalValues));
                Assert.That(changeCount, Is.Zero);
                Assert.That(modifier.transform.Find("Section Title/Label").GetComponent<Text>().text, Is.EqualTo("Tags parsing"));
                Assert.That(modifier.transform.childCount, Is.EqualTo(4));
                foreach (string label in new[] { "True values", "False values", "Ignored values" })
                {
                    var row = modifier.transform.Find(label);
                    Assert.That(row.Find("Prefix/Label").GetComponent<Text>().text, Is.EqualTo(label));
                    Assert.That(row.Find("InputField").GetComponent<Tooltip>().Text, Does.Contain("\nMatching ignores case and surrounding whitespace."));
                }
            }
            finally
            {
                Object.DestroyImmediate(modifier.gameObject);
            }
        }

        [TestCase("True values", " yes ,YES, true, ,yes, ", "yes,true")]
        [TestCase("False values", " no ,NO,false, ,no, ", "no,false")]
        [TestCase("Ignored values", " n/a ,N/A, -, ,-, ", "n/a,-")]
        public void TagParsingNormalizesOnEndEditAndKeepsTheCurrentDraft(string label, string entered, string expected)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/UI/Windows/User preferences window");
            var modifier = Object.Instantiate(prefab.GetComponentInChildren<TagImportPreferencesSubModifier>(true));
            modifier.gameObject.SetActive(false);
            try
            {
                var firstDraft = new TagImportPreferences();
                modifier.Object = firstDraft;
                var currentDraft = new TagImportPreferences();
                modifier.Object = currentDraft;
                var input = modifier.transform.Find(label + "/InputField").GetComponent<InputField>();
                input.text = entered;
                Assert.That(input.text, Is.EqualTo(entered), "Typing must not rewrite the field before editing ends.");

                input.onEndEdit.Invoke(input.text);
                Assert.That(input.text, Is.EqualTo(expected));
                var property = typeof(TagImportPreferences).GetProperty(label.Split(' ')[0] + "Values");
                Assert.That(property.GetValue(currentDraft), Is.EqualTo(expected.Split(',')));
                Assert.That(property.GetValue(firstDraft), Is.EqualTo(property.GetValue(new TagImportPreferences())));
                modifier.Refresh();
                Assert.That(input.text, Is.EqualTo(expected));
                Assert.That(currentDraft.CreatePolicy(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(modifier.gameObject);
            }
        }

        [Test]
        public void AuthoredPreferencesControlsKeepNormalizationInDraftWithoutQuestAtlasControls()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/UI/Windows/User preferences window");
            Assert.That(prefab, Is.Not.Null);
            var originalNormalization = DataManager.DefaultNormalization;
            var originalAveraging = DataManager.DefaultAveraging;
            var selectionRoot = SelectionManager.IsInitialized ? null : new GameObject("Preferences UI selection manager");
            if (selectionRoot != null) selectionRoot.AddComponent<SelectionManager>();
            var root = Object.Instantiate(prefab);
            root.SetActive(false);
            try
            {
                var atlas = root.GetComponentInChildren<AtlasesPreferencesSubModifier>(true);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                Assert.That(atlas, Is.Not.Null);
                foreach (var child in atlas.GetComponentsInChildren<Transform>(true))
                    Assert.That(child.name, Is.Not.EqualTo("Quest Atlas Retry").And.Not.EqualTo("Session Atlas Status"));
                var eeg = root.GetComponentInChildren<EEGPreferencesSubModifier>(true);
                eeg.Object = new EEGPreferences();
                var dropdown = (Dropdown)typeof(EEGPreferencesSubModifier).GetField("m_EEGNormalizationDropdown", flags).GetValue(eeg);
                dropdown.value = (int)NormalizationType.Protocol;
                Assert.That(eeg.RequestedNormalization, Is.EqualTo(NormalizationType.Protocol));
                Assert.That(DataManager.DefaultNormalization, Is.EqualTo(NormalizationType.None));
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (selectionRoot != null) Object.DestroyImmediate(selectionRoot);
                DataManager.DefaultNormalization = originalNormalization;
                DataManager.DefaultAveraging = originalAveraging;
            }
        }
    }
}
