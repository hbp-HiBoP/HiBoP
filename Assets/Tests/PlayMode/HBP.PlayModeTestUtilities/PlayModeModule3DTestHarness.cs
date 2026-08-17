using System;
using System.Collections.Generic;
using System.Reflection;
using HBP.Core.Data;
using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.SceneManagement;
using ObjectSite = HBP.Core.Object3D.Site;
using ObjectSiteInformation = HBP.Core.Object3D.SiteInformation;
using ObjectSiteState = HBP.Core.Object3D.SiteState;

namespace HBP.Tests.PlayMode.Utilities
{
    public sealed class PlayModeModule3DTestHarness : IDisposable
    {
        private readonly GameObject m_Root;

        public Base3DScene Scene { get; }
        public ImplantationManager ImplantationManager { get; }
        public TestColumn3D SourceColumn { get; }
        public TestColumn3D TargetColumn { get; }
        public ObjectSite SourceSiteA { get; }
        public ObjectSite SourceSiteB { get; }
        public ObjectSite TargetSiteA { get; }
        public ObjectSite TargetSiteB { get; }
        public Patient Patient { get; }

        public PlayModeModule3DTestHarness(UnityEngine.SceneManagement.Scene scene)
        {
            m_Root = new GameObject("PlayMode Module3D Test Harness");
            SceneManager.MoveGameObjectToScene(m_Root, scene);

            GameObject sceneObject = new("Base3DScene_PlayModeTest");
            sceneObject.transform.SetParent(m_Root.transform, false);
            Scene = sceneObject.AddComponent<Base3DScene>();

            GameObject implantationObject = new("ImplantationManager_PlayModeTest");
            implantationObject.transform.SetParent(m_Root.transform, false);
            ImplantationManager = implantationObject.AddComponent<ImplantationManager>();
            SetPrivateField(Scene, "m_ImplantationManager", ImplantationManager);
            SetPrivateField(ImplantationManager, "m_Scene", Scene);

            Patient = new Patient("playmode-patient-alpha", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), Array.Empty<BaseTagValue>(), string.Empty, "playmode-patient-alpha-id");

            SourceSiteA = CreateSite("A1", 0, new Vector3(1, 2, 3), "source");
            SourceSiteB = CreateSite("B1", 1, new Vector3(4, 5, 6), "source");
            TargetSiteA = CreateSite("A1", 0, new Vector3(1, 2, 3), "target");
            TargetSiteB = CreateSite("B1", 1, new Vector3(4, 5, 6), "target");

            SourceColumn = CreateColumn("source-column", SourceSiteA, SourceSiteB);
            TargetColumn = CreateColumn("target-column", TargetSiteA, TargetSiteB);
            Scene.Columns.Add(SourceColumn);
            Scene.Columns.Add(TargetColumn);
            SourceColumn.IsSelected = true;
        }

        public void Dispose()
        {
            if (m_Root != null)
            {
                UnityEngine.Object.Destroy(m_Root);
            }
        }

        private ObjectSite CreateSite(string name, int index, Vector3 position, string suffix)
        {
            GameObject siteObject = new($"Site_{suffix}_{name}");
            siteObject.transform.SetParent(m_Root.transform, false);
            siteObject.transform.localPosition = position;
            ObjectSite site = siteObject.AddComponent<ObjectSite>();
            Site siteData = new(name, new[] { new Coordinate("playmode-space", position, $"coordinate-{suffix}-{name}") }, Array.Empty<BaseTagValue>(), $"site-{name}");
            site.Information = new ObjectSiteInformation
            {
                SiteData = siteData,
                Patient = Patient,
                Name = name,
                Index = index,
                DefaultPosition = position
            };
            site.State = new ObjectSiteState();
            site.Configuration = new SiteConfiguration();
            return site;
        }

        private TestColumn3D CreateColumn(string name, params ObjectSite[] sites)
        {
            GameObject columnObject = new(name);
            columnObject.transform.SetParent(m_Root.transform, false);
            TestColumn3D column = columnObject.AddComponent<TestColumn3D>();
            column.Setup(name, sites);
            return column;
        }

        private static void SetPrivateField<T>(T target, string fieldName, object value)
        {
            FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(target, value);
        }

        public sealed class TestColumn3D : Column3D
        {
            public void Setup(string name, IEnumerable<ObjectSite> sites)
            {
                ColumnData = new AnatomicColumn(name, new BaseConfiguration());
                Sites = new List<ObjectSite>(sites);
                SiteStateBySiteID.Clear();
                foreach (ObjectSite site in Sites)
                {
                    SiteStateBySiteID[site.Information.FullID] = site.State;
                }
            }

            public void SelectSite(ObjectSite site)
            {
                FieldInfo field = typeof(Column3D).GetField("<SelectedSite>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                field.SetValue(this, site);
                site.IsSelected = true;
                OnSelectSite.Invoke(site);
            }

            public override void ComputeSurfaceBrainUVWithActivity()
            {
            }
        }
    }
}
