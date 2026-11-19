using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;
using HBP.Core.DLL;
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
        public async Task DesktopProjectionPreservesAllSamplesAndTemporalPolicies()
        {
            using var volume = new Volume();
            Assert.That(volume.LoadNIFTIFile(Path.Combine(Application.dataPath, "Data/IRM/MNI.nii")), Is.True);
            using var grid = ActivityProjectionGrid.Create(volume, 24, VolumeInterpolation.Trilinear);
            using var surface = new HBP.Core.DLL.Surface();
            surface.SetBuffers(new[] { new Vector3(10, 0, 0), new Vector3(11, 1, 0), new Vector3(9, 0, 1) }, new[] { 0, 1, 2 });
            using var generator = new IEEGGenerator();
            using var reference = new IEEGGenerator();
            using var projection = new SurfaceGenerator();
            using var referenceProjection = new SurfaceGenerator();
            generator.Initialize(grid);
            reference.Initialize(grid);
            projection.Initialize(generator, surface, 0, 1);
            referenceProjection.Initialize(reference, surface, 0, 1);
            typeof(Column3D).GetProperty("ActivityGenerator").SetValue(column, generator);
            column.SurfaceGenerator = projection;
            for (int i = 0; i < column.Sites.Count; ++i)
            {
                column.Sites[i].State.IsFiltered = true;
                column.RawElectrodes.AddSite("S" + i, new Vector3(-10 + i, i, 0), 0, i);
            }

            try
            {
                column.Sites[1].State.IsOutOfROI = true;
                foreach (bool roiActive in new[] { false, true })
                {
                    var values = column.ActivityValues;
                    float distance = column.DynamicParameters.InfluenceDistance;
                    for (int i = 0; i < column.Sites.Count; ++i)
                        column.RawElectrodes.UpdateMask(i, column.Sites[i].State.IsEffectivelyMasked(roiActive));
                    reference.ComputeActivity(column.RawElectrodes, distance, values, column.ProjectionTimeline.Length, column.Sites.Count, SiteInfluenceByDistanceType.Quadratic);
                    reference.AdjustValues(0, -10, 10);
                    var prepared = column.PrepareActivityComputation(roiActive, SiteInfluenceByDistanceType.Quadratic, false);
                    // After capture the worker must not consult the current UI or new buffers.
                    typeof(Column3DDynamic).GetProperty("ActivityValues").SetValue(column, Array.Empty<float>());
                    column.DynamicParameters.SetSpanValues(-100, 5, 100);
                    column.DynamicParameters.InfluenceDistance = 1;
                    await Task.Run(prepared.Compute);
                    typeof(Column3DDynamic).GetProperty("ActivityValues").SetValue(column, values);
                    column.DynamicParameters.SetSpanValues(-10, 0, 10);
                    column.DynamicParameters.InfluenceDistance = distance;
                    foreach (TemporalSamplingPolicy policy in Enum.GetValues(typeof(TemporalSamplingPolicy)))
                    {
                        PersistentDataManager.UserPreferences.Data.EEG.TemporalSampling = policy;
                        for (int index = 0; index < column.Timeline.Length; ++index)
                        {
                            column.Timeline.CurrentIndex = index;
                            column.ComputeSurfaceBrainUVWithActivity();
                            referenceProjection.ComputeActivityUV(column.CurrentProjectionSample.Index, column.ActivityAlpha);
                            Assert.That(projection.ActivityUV, Is.EqualTo(referenceProjection.ActivityUV), $"{policy}, navigation index {index}, ROI {roiActive}");
                            Assert.That(projection.AlphaUV, Is.EqualTo(referenceProjection.AlphaUV));
                        }
                    }
                }
            }
            finally
            {
                column.RawElectrodes.Dispose();
            }
        }

        [Test]
        public void ScientificAppearanceUsesSiteSamplingAndIgnoresLocalSelectionAndPlacement()
        {
            foreach (TemporalSamplingPolicy policy in Enum.GetValues(typeof(TemporalSamplingPolicy)))
            {
                PersistentDataManager.UserPreferences.Data.EEG.TemporalSampling = policy;
                for (int index = 0; index < column.Timeline.Length; index++)
                {
                    column.Timeline.CurrentIndex = index;
                    var instant = DesktopIEEGCapture.Capture(column);
                    for (int site = 0; site < column.Sites.Count; site++)
                    {
                        var state = column.Sites[site].State;
                        var appearance = column.EvaluateSiteAppearance(site, false, false, true);
                        var expected = SiteAppearanceTests.Legacy(instant.SiteValues[site], -10, 0, 10, state.IsMasked, state.IsOutOfROI, state.IsFiltered, state.IsBlackListed, false, false, true);
                        Assert.That(appearance.Visible, Is.EqualTo(expected.Visible));
                        if (appearance.Visible)
                        {
                            Assert.That(appearance.Scale, Is.EqualTo(expected.Scale));
                            Assert.That(appearance.Type, Is.EqualTo(expected.Type));
                        }

                        column.Sites[site].IsSelected = !column.Sites[site].IsSelected;
                        state.IsHighlighted = !state.IsHighlighted;
                        root.transform.SetPositionAndRotation(new Vector3(10, 20, 30), Quaternion.Euler(45, 90, 180));
                        root.transform.localScale = Vector3.one * 7;
                        Assert.That(column.EvaluateSiteAppearance(site, false, false, true), Is.EqualTo(appearance));
                    }
                }
            }
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
