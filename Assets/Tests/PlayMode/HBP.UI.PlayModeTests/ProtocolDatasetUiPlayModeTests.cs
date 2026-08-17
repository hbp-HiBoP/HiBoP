using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Tests.PlayMode.Utilities;
using HBP.UI.Main;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HBP.Tests.PlayMode.UI
{
    public class ProtocolDatasetUiPlayModeTests
    {
        private const string ProtocolSelectorResource = "Prefabs/UI/Windows/Protocol selector window";
        private const string DatasetSelectorResource = "Prefabs/UI/Windows/Dataset selector window";
        private const string DataInfoSelectorResource = "Prefabs/UI/Windows/DataInfo selector window";

        [Test]
        [Category("PlayMode.ProtocolDataset")]
        public async Task ProtocolDatasetAndDataInfoSelectors_DisplaySyntheticProtocolDatasetObjects()
        {
            using PlayModeTempDirectoryScope temp = new();
            using PlayModeApplicationStateScope appState = new(temp.Path);
            using PlayModePersistentDataScope persistentData = new(temp.Path);
            using PlayModeSelectionManagerScope selectionManager = new();
            using PlayModeSceneScope scene = new("ProtocolDatasetProtocolDatasetLists");
            PlayModeWindowHarness window = new(scene.Scene, "ProtocolDataset Protocol Dataset Lists Harness");
            Project project = PlayModeProjectHarness.CreateAndLoadCompleteProject();
            Protocol protocol = project.Datasets.Single().Protocol;
            Dataset dataset = project.Datasets.Single();

            ProtocolSelector protocolSelector = InstantiateWindow<ProtocolSelector>(ProtocolSelectorResource, window.Root.transform);
            DatasetSelector datasetSelector = InstantiateWindow<DatasetSelector>(DatasetSelectorResource, window.Root.transform);
            DataInfoSelector dataInfoSelector = InstantiateWindow<DataInfoSelector>(DataInfoSelectorResource, window.Root.transform);
            ProtocolList protocolList = protocolSelector.GetComponentInChildren<ProtocolList>(true);
            DatasetList datasetList = datasetSelector.GetComponentInChildren<DatasetList>(true);
            DataInfoList dataInfoList = dataInfoSelector.GetComponentInChildren<DataInfoList>(true);
            Assert.That(protocolList, Is.Not.Null);
            Assert.That(datasetList, Is.Not.Null);
            Assert.That(dataInfoList, Is.Not.Null);

            protocolSelector.Objects = new[] { protocol };
            datasetSelector.Objects = new[] { dataset };
            dataInfoSelector.Objects = dataset.Data.ToArray();
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            Canvas.ForceUpdateCanvases();
            await UniTask.Yield();

            protocolList.SortByBlocs(BaseList.Sorting.Ascending);
            datasetList.SortByData(BaseList.Sorting.Descending);
            dataInfoList.SortByType(BaseList.Sorting.Ascending);

            Assert.That(protocolSelector.Objects.Single(), Is.SameAs(protocol));
            Assert.That(datasetSelector.Objects.Single(), Is.SameAs(dataset));
            Assert.That(dataInfoSelector.Objects, Has.Length.EqualTo(8));
            Assert.That(dataInfoList.Objects.OfType<PatientDataInfo>().Select(dataInfo => dataInfo.Patient), Is.All.SameAs(project.Patients.Single()));
            Assert.That(protocolList.Items, Is.Not.Empty);
            Assert.That(datasetList.Items, Is.Not.Empty);
            Assert.That(dataInfoList.Items, Is.Not.Empty);
            Assert.That(GetTexts(protocolList.gameObject), Does.Contain(protocol.Name));
            Assert.That(GetTexts(datasetList.gameObject), Does.Contain(dataset.Name));
            Assert.That(GetTexts(dataInfoList.gameObject), Does.Contain("playmode-signal-alpha"));

            Object.Destroy(protocolSelector.gameObject);
            Object.Destroy(datasetSelector.gameObject);
            Object.Destroy(dataInfoSelector.gameObject);
            await UniTask.Yield();
            await UniTask.Yield();
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

        private static IEnumerable<string> GetTexts(GameObject root)
        {
            return root.GetComponentsInChildren<Text>(true).Select(text => text.text);
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
            }

            private static void ResetSingleton()
            {
                FieldInfo field = typeof(Singleton<SelectionManager>).GetField("m_Instance", BindingFlags.NonPublic | BindingFlags.Static);
                field.SetValue(null, null);
            }
        }
    }
}
