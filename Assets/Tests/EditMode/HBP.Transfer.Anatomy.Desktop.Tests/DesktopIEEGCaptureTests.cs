using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Transfer.Anatomy.Desktop;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HBP.Tests.Transfer.Anatomy.Desktop
{
    public class DesktopIEEGCaptureTests
    {
        private GameObject root;
        private Column3DIEEG column;
        private object previousPreferences;
        private static readonly FieldInfo Preferences = typeof(Singleton<PersistentDataManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Prepared iEEG test");
            root.SetActive(false);
            previousPreferences = Preferences.GetValue(null);
            var manager = root.AddComponent<PersistentDataManager>();
            Preferences.SetValue(null, manager);
            typeof(PersistentDataManager).GetField("m_Aliases", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, new AliasCollection());
            typeof(PersistentDataManager).GetField("m_UserPreferences", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, new UserPreferences());
            PersistentDataManager.UserPreferences.Data.EEG.TemporalSampling = TemporalSamplingPolicy.Interpolate;
            column = root.AddComponent<Column3DIEEG>();
            var main = new HBP.Core.Data.Event("event", new[] { 1 }, MainSecondaryEnum.Main, "event");
            var sub = new SubBloc("sub", 0, MainSecondaryEnum.Main, new TimeWindow(0, 30), new TimeWindow(0, 0), new[] { main }, Array.Empty<Icon>(), Array.Empty<Treatment>(), "sub");
            var bloc = new Bloc("bloc", 0, "", "sub_event_CODE", new[] { sub }, "bloc");
            var protocol = new Protocol("protocol", new[] { bloc }, "protocol");
            var dataset = new Dataset("dataset", protocol, Array.Empty<DataInfo>(), "dataset");
            var data = new IEEGColumn("iEEG", new BaseConfiguration(), dataset, "synthetic", bloc, new DynamicConfiguration(), "column");
            typeof(Column3D).GetProperty("ColumnData").SetValue(column, data);
            var stats = new Dictionary<SubBloc, List<SubBlocEventsStatistics>> { [sub] = new() { new SubBlocEventsStatistics { StatisticsByEvent = new() { [main] = new EventStatistics() } } } };
            var indices = new Dictionary<SubBloc, int> { [sub] = 0 };
            data.Data.Timeline = new Timeline(bloc, stats, indices, new Frequency(200));
            data.Data.ProjectionTimeline = new Timeline(bloc, stats, indices, new Frequency(100));
            var patient = new Patient { ID = "patient" };
            var sites = new List<HBP.Core.Object3D.Site>();
            for (int i = 0; i < 5; i++)
            {
                var site = root.AddComponent<HBP.Core.Object3D.Site>();
                site.Information = new SiteInformation { Patient = patient, Index = i, Name = "S" + i };
                site.State = new SiteState();
                sites.Add(site);
                data.Data.UnitByChannelID[site.Information.FullID] = i < 3 ? "uV" : "";
            }

            data.Data.ProcessedValuesByChannel["patient_S0"] = new float[] { -10, -1, -3, -10 };
            data.Data.ProcessedValuesByChannel["patient_S1"] = new float[] { 0, 0, 2, 0 };
            data.Data.ProcessedValuesByChannel["patient_S2"] = new float[] { 10, 1, 3, 10 };
            data.Data.ProcessedValuesByChannel["patient_S4"] = Array.Empty<float>();
            typeof(Column3D).GetProperty("Sites").SetValue(column, sites);
            typeof(Column3DIEEG).GetMethod("SetActivityData", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(column, null);
            sites[2].State.IsBlackListed = true;
            column.DynamicParameters.SetSpanValues(-10, 0, 10);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Preferences.SetValue(null, previousPreferences);
        }

        [TestCase(2, 0f, -1f, 0f, 1f)]
        [TestCase(3, .5f, -2f, 1f, 2f)]
        [TestCase(6, 0f, -10f, 0f, 10f)]
        public void CaptureUsesActualTimeMajorInputsAndExistingSampling(int index, float alpha, float negative, float zero, float positive)
        {
            column.Timeline.CurrentIndex = index;
            var capture = DesktopIEEGCapture.Capture(column);
            Assert.That(capture.NavigationIndex, Is.EqualTo(index));
            Assert.That(capture.LocalTimeMilliseconds, Is.EqualTo(index * 5));
            Assert.That(capture.ProjectionIndex, Is.EqualTo(index / 2));
            Assert.That(capture.Alpha, Is.EqualTo(alpha));
            Assert.That(capture.SurfaceValues.ToArray(), Is.EqualTo(column.Sites.Select((_, i) => column.ActivityValues[(index / 2) * 5 + i]).ToArray()));
            Assert.That(capture.SiteValues.ToArray(), Is.EqualTo(new[] { negative, zero, positive, 0, 0 }));
            Assert.That(capture.Availability.ToArray(), Is.EqualTo(new byte[] { 2, 2, 2, 0, 1 }));
            Assert.That(capture.SpanMin, Is.EqualTo(-10));
            Assert.That(capture.SpanMax, Is.EqualTo(10));
            column.ActivityValuesBySiteID[0][1] = 999;
            column.DynamicParameters.SetSpanValues(-1, 0, 1);
            Assert.That(capture.SpanMax, Is.EqualTo(10));
            Assert.That(capture.SiteValues[0], Is.EqualTo(negative));
        }

        [Test]
        public void FullPreparationHashIsStableAcrossInstants()
        {
            var first = DesktopIEEGCapture.Capture(column);
            column.Timeline.CurrentIndex = 3;
            Assert.That(DesktopIEEGCapture.Capture(column).PreparedSha256, Is.EqualTo(first.PreparedSha256));
        }

        [Test]
        public void DivergentNativeBufferRejected()
        {
            column.ActivityValues[1] = 88;
            Assert.Throws<InvalidOperationException>(() => DesktopIEEGCapture.Capture(column));
        }

        [Test]
        public void ReorderedSitesRejected()
        {
            column.Sites.Reverse();
            Assert.Throws<InvalidOperationException>(() => DesktopIEEGCapture.Capture(column));
        }
    }
}
