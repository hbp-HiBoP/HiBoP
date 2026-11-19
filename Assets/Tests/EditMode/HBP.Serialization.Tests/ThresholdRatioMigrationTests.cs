using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Tests.Serialization.Helpers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class ThresholdRatioMigrationTests
    {
        private const string Legacy = "\"Span Min\":90,\"Middle\":100,\"Span Max\":110";
        private const string Ratios = "\"Span Min Ratio\":0.25,\"Middle Ratio\":0.5,\"Span Max Ratio\":0.75";
        private GameObject m_Object;
        private ThresholdMigrationStaticColumn m_Column;
        private DynamicDataParameters Parameters => m_Column.StaticParameters;

        [SetUp]
        public void SetUp()
        {
            m_Object = new GameObject("Threshold migration data");
            m_Column = m_Object.AddComponent<ThresholdMigrationStaticColumn>();
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(m_Object);

        [TestCase(typeof(DynamicConfiguration))]
        [TestCase(typeof(CCEPConfiguration))]
        [TestCase(typeof(StaticConfiguration))]
        public void UnopenedLegacyConfiguration_SurvivesProjectSerialization(Type type)
        {
            BaseData configuration = Read(type, Legacy);
            JObject saved = Write(configuration);
            Assert.That(saved["Span Min"].Value<float>(), Is.EqualTo(90));
            Assert.That(saved["Middle"].Value<float>(), Is.EqualTo(100));
            Assert.That(saved["Span Max"].Value<float>(), Is.EqualTo(110));
            Assert.That(saved["Span Min Ratio"], Is.Null);
            Assert.That(Read(type, saved.ToString()).ID, Is.EqualTo(configuration.ID));
        }

        [TestCase(typeof(DynamicConfiguration))]
        [TestCase(typeof(CCEPConfiguration))]
        [TestCase(typeof(StaticConfiguration))]
        public void LegacyThresholds_MigrateOnceDataExists_AndReloadAgainstNewNormalization(Type type)
        {
            BaseData configuration = Read(type, Legacy);
            Load(configuration);
            Capture(configuration);
            Assert.That(Write(configuration)["Span Min"].Value<float>(), Is.EqualTo(90));

            m_Column.SetRange(80, 120);
            AssertSpan(90, 100, 110);
            Capture(configuration);
            JObject saved = Write(configuration);
            AssertRatioOnly(saved, .25f, .5f, .75f);

            Parameters.ResetSpanValues(m_Column);
            m_Column.SetRange(-10, 10);
            Load(Read(type, saved.ToString()));
            AssertSpan(-5, 0, 5);
            m_Column.SetRange(-20, 20);
            AssertSpan(-10, 0, 10);
        }

        [TestCase(typeof(DynamicConfiguration), false)]
        [TestCase(typeof(DynamicConfiguration), true)]
        [TestCase(typeof(CCEPConfiguration), false)]
        [TestCase(typeof(CCEPConfiguration), true)]
        [TestCase(typeof(StaticConfiguration), false)]
        [TestCase(typeof(StaticConfiguration), true)]
        public void RatiosWinOverLegacy_RegardlessOfJsonOrder(Type type, bool ratiosFirst)
        {
            BaseData configuration = Read(type, ratiosFirst ? Ratios + "," + Legacy : Legacy + "," + Ratios);
            Load(configuration);
            m_Column.SetRange(-10, 10);
            AssertSpan(-5, 0, 5);
            AssertRatioOnly(Write(configuration), .25f, .5f, .75f);
        }

        [TestCase(typeof(DynamicConfiguration), false)]
        [TestCase(typeof(DynamicConfiguration), true)]
        [TestCase(typeof(CCEPConfiguration), false)]
        [TestCase(typeof(CCEPConfiguration), true)]
        [TestCase(typeof(StaticConfiguration), false)]
        [TestCase(typeof(StaticConfiguration), true)]
        public void CloneAndCopy_PreserveBothPendingLegacyAndRatios(Type type, bool migrated)
        {
            BaseData source = Read(type, migrated ? Ratios : Legacy);
            if (migrated)
            {
                source = type == typeof(StaticConfiguration) ? new StaticConfiguration(15, 90, 100, 110, .25f, .5f, .75f, 4, "threshold-config") :
                    type == typeof(CCEPConfiguration) ? new CCEPConfiguration(15, 90, 100, 110, .25f, .5f, .75f, true, "patient_A1", 12, "threshold-config") :
                    new DynamicConfiguration(15, 90, 100, 110, .25f, .5f, .75f, "threshold-config");
            }
            BaseData copy = (BaseData)Activator.CreateInstance(type);
            // Copy must also clear a previously stored ratio triplet when the source is legacy.
            copy.Copy(Read(type, Ratios));
            copy.Copy(source);
            foreach (BaseData candidate in new[] { (BaseData)source.Clone(), copy })
            {
                Assert.That(candidate.ID, Is.EqualTo(source.ID));
                Assert.That(JToken.DeepEquals(Write(candidate), Write(source)), Is.True);
                Load(candidate);
                m_Column.SetRange(80, 120);
                AssertSpan(90, 100, 110);
            }
        }

        private static IEnumerable<TestCaseData> InvalidRatios()
        {
            string[] fields =
            {
                "\"Span Min Ratio\":0.25",
                "\"Span Min Ratio\":0.25,\"Middle Ratio\":0.5",
                "\"Span Min Ratio\":null,\"Middle Ratio\":0.5,\"Span Max Ratio\":0.75",
                "\"Span Min Ratio\":0.75,\"Middle Ratio\":0.5,\"Span Max Ratio\":1",
                "\"Span Min Ratio\":0,\"Middle Ratio\":\"NaN\",\"Span Max Ratio\":1",
                "\"Span Min Ratio\":0,\"Middle Ratio\":0.5,\"Span Max Ratio\":\"Infinity\""
            };
            foreach (Type type in new[] { typeof(DynamicConfiguration), typeof(CCEPConfiguration), typeof(StaticConfiguration) })
            for (int i = 0; i < fields.Length; i++)
                yield return new TestCaseData(type, Legacy + "," + fields[i]).SetName($"InvalidRatios_{type.Name}_{i}");
        }

        [TestCaseSource(nameof(InvalidRatios))]
        public void InvalidRatioTriplet_IsRejectedInsteadOfFallingBackToLegacy(Type type, string fields)
        {
            Assert.That(() => Read(type, fields), Throws.Exception);
        }

        [TestCase(typeof(DynamicConfiguration))]
        [TestCase(typeof(CCEPConfiguration))]
        [TestCase(typeof(StaticConfiguration))]
        public void ZeroRatios_AreValid_WhileLegacyZeroMeansAutomatic(Type type)
        {
            m_Column.SetRange(80, 120);
            Load(Read(type, "\"Span Min\":0,\"Middle\":0,\"Span Max\":0"));
            m_Column.SetRange(80, 120);
            Assert.That(Parameters.Middle, Is.EqualTo(100));

            BaseData ratios = Read(type, "\"Span Min Ratio\":0,\"Middle Ratio\":0,\"Span Max Ratio\":0");
            Load(ratios);
            AssertSpan(80, 80, 80);
            Capture(ratios);
            AssertRatioOnly(Write(ratios), 0, 0, 0);
        }

        [TestCase(typeof(DynamicConfiguration))]
        [TestCase(typeof(CCEPConfiguration))]
        [TestCase(typeof(StaticConfiguration))]
        public void AutomaticLegacyDefaults_BecomeRelativeOnceDataIsAvailable(Type type)
        {
            BaseData configuration = Read(type, "\"Span Min\":0,\"Middle\":0,\"Span Max\":0");
            Load(configuration);
            m_Column.SetValues(80, 80, 80, 120);
            AssertSpan(80, 90, 120);
            Capture(configuration);
            AssertRatioOnly(Write(configuration), 0, .25f, 1);
            m_Column.SetValues(-10, 10, 10, 10);
            AssertSpan(-10, -5, 10);
        }

        [TestCase(typeof(DynamicConfiguration))]
        [TestCase(typeof(CCEPConfiguration))]
        [TestCase(typeof(StaticConfiguration))]
        public void EmptyOrConstantData_DefersLegacyConversion_WithoutLosingThresholds(Type type)
        {
            BaseData configuration = Read(type, Legacy);
            Load(configuration);
            m_Column.SetRange(-1, 1, false);
            Capture(configuration);
            Assert.That(Write(configuration)["Middle"].Value<float>(), Is.EqualTo(100));
            m_Column.SetRange(100, 100);
            Capture(configuration);
            Assert.That(Write(configuration)["Span Min Ratio"], Is.Null);
            AssertSpan(90, 100, 110);
            m_Column.SetRange(80, 120);
            Capture(configuration);
            AssertRatioOnly(Write(configuration), .25f, .5f, .75f);
        }

        [TestCase(typeof(DynamicConfiguration))]
        [TestCase(typeof(CCEPConfiguration))]
        [TestCase(typeof(StaticConfiguration))]
        public void OutOfRangeRatios_SurviveMissingData_AndUserEditsReplaceThem(Type type)
        {
            BaseData configuration = Read(type, "\"Span Min Ratio\":-0.5,\"Middle Ratio\":0,\"Span Max Ratio\":1.5");
            Load(configuration);
            m_Column.SetRange(-1, 1, false);
            Capture(configuration);
            AssertRatioOnly(Write(configuration), -.5f, 0, 1.5f);
            m_Column.SetRange(-10, 10);
            AssertSpan(-20, -10, 20);
            m_Column.SetRange(0, 0);
            Capture(configuration);
            AssertRatioOnly(Write(configuration), -.5f, 0, 1.5f);
            m_Column.SetRange(-10, 10);
            Parameters.SetSpanValues(-5, 0, 5);
            Capture(configuration);
            AssertRatioOnly(Write(configuration), .25f, .5f, .75f);
            Parameters.ApplySynchronizedSpanValues(0, 0, 0);
            Capture(configuration);
            AssertRatioOnly(Write(configuration), .5f, .5f, .5f);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HistoricalCCEPPropertyAlias_PreservesNewRatioFields(bool explicitType)
        {
            string type = explicitType ? "\"$type\":\"HBP.Core.Data.DynamicConfiguration\"," : string.Empty;
            CCEPColumn column = ClassLoaderSaver.LoadFromJsonString<CCEPColumn>("{\"DynamicConfiguration\":{" + type + Ratios + "}}");
            Load(column.CCEPConfiguration);
            m_Column.SetRange(-10, 10);
            AssertSpan(-5, 0, 5);
            AssertRatioOnly(Write(column.CCEPConfiguration), .25f, .5f, .75f);
        }

        [Test]
        public void IEEGColumn_RecomputesThresholdsFromRatiosAfterSignalRangeChanges()
        {
            var column = AddColumn<Column3DIEEG>();
            var site = CreateSite("A1", 0);
            var timeline = CreateTimeline(out _);
            var data = new IEEGColumn { DynamicConfiguration = (DynamicConfiguration)Read(typeof(DynamicConfiguration), Legacy) };
            data.Data.Timeline = data.Data.ProjectionTimeline = timeline;
            data.Data.ProcessedValuesByChannel[site.Information.FullID] = Signal(timeline.Length, 80, 120);
            Bind(column, data, site);
            column.ComputeActivityData();
            column.LoadConfiguration();
            column.SaveConfiguration();
            AssertRatioOnly(Write(data.DynamicConfiguration), .25f, .5f, .75f);

            data.Data.ProcessedValuesByChannel[site.Information.FullID] = Signal(timeline.Length, -10, 10);
            column.ComputeActivityData();
            Assert.That(column.DynamicParameters.SpanMin, Is.EqualTo(-5).Within(.0001f));
            Assert.That(column.DynamicParameters.Middle, Is.Zero.Within(.0001f));
            Assert.That(column.DynamicParameters.SpanMax, Is.EqualTo(5).Within(.0001f));
        }

        [Test]
        public void CCEPColumn_SourceSelectionPreservesRatiosAndMigratesSavedThresholds()
        {
            var column = AddColumn<Column3DCCEP>();
            var first = CreateSite("A1", 0);
            var second = CreateSite("A2", 1);
            var timeline = CreateTimeline(out Bloc bloc);
            var configuration = (CCEPConfiguration)Read(typeof(CCEPConfiguration), Legacy);
            configuration.SiteID = first.Information.FullID;
            var data = new CCEPColumn { Bloc = bloc, CCEPConfiguration = configuration };
            data.Data.Timeline = data.Data.ProjectionTimeline = timeline;
            foreach (var source in new[] { first, second })
            {
                string id = source.Information.FullID;
                float[] signal = source == first ? Signal(timeline.Length, 80, 120) : Signal(timeline.Length, -10, 10);
                data.Data.ProcessedValuesByChannelIDByStimulatedChannelID[id] = new Dictionary<string, float[]>
                {
                    { first.Information.FullID, signal }, { second.Information.FullID, signal }
                };
                data.Data.UnityByChannelIDByStimulatedChannelID[id] = new Dictionary<string, string>();
                data.Data.DataByChannelIDByStimulatedChannelID[id] = new Dictionary<string, BlocChannelData>();
                data.Data.StatisticsByChannelIDByStimulatedChannelID[id] = new Dictionary<string, BlocChannelStatistics>();
            }
            Bind(column, data, first, second);
            column.Sources.AddRange(new[] { first, second });
            column.LoadConfiguration();
            column.SaveConfiguration();
            AssertRatioOnly(Write(configuration), .25f, .5f, .75f);
            column.SelectedSourceSite = null;
            column.SaveConfiguration();
            AssertRatioOnly(Write(configuration), .25f, .5f, .75f);
            column.SelectedSourceSite = second;
            Assert.That(column.DynamicParameters.SpanMin, Is.EqualTo(-5).Within(.0001f));
            Assert.That(column.DynamicParameters.Middle, Is.Zero.Within(.0001f));
            Assert.That(column.DynamicParameters.SpanMax, Is.EqualTo(5).Within(.0001f));
        }

        [Test]
        public void StaticColumn_UsesActualBoundsAndCapturesOnlyRatios()
        {
            var column = AddColumn<Column3DStatic>();
            var site = CreateSite("A1", 0);
            var data = new StaticColumn { StaticConfiguration = (StaticConfiguration)Read(typeof(StaticConfiguration), Legacy) };
            data.Data.ValueByChannelIDByLabel["Low"] = new Dictionary<string, float> { { site.Information.FullID, 80 } };
            data.Data.ValueByChannelIDByLabel["High"] = new Dictionary<string, float> { { site.Information.FullID, 120 } };
            Bind(column, data, site);
            column.ComputeActivityData();
            column.LoadConfiguration();
            column.SaveConfiguration();
            Assert.That(column.StaticParameters.MinimumAmplitude, Is.EqualTo(80));
            AssertRatioOnly(Write(data.StaticConfiguration), .25f, .5f, .75f);
            data.Data.ValueByChannelIDByLabel["Low"][site.Information.FullID] = -10;
            data.Data.ValueByChannelIDByLabel["High"][site.Information.FullID] = 10;
            column.ComputeActivityData();
            Assert.That(column.StaticParameters.SpanMin, Is.EqualTo(-5).Within(.0001f));
            Assert.That(column.StaticParameters.Middle, Is.Zero.Within(.0001f));
            Assert.That(column.StaticParameters.SpanMax, Is.EqualTo(5).Within(.0001f));
        }

        private T AddColumn<T>() where T : Component
        {
            var child = new GameObject(typeof(T).Name);
            child.transform.SetParent(m_Object.transform);
            return child.AddComponent<T>();
        }

        private HBP.Core.Object3D.Site CreateSite(string name, int index)
        {
            var site = AddColumn<HBP.Core.Object3D.Site>();
            site.Information = new SiteInformation { Patient = new Patient(), Name = name, Index = index };
            site.State = new SiteState();
            site.Configuration = new SiteConfiguration();
            return site;
        }

        private static void Bind(Column3D column, Column data, params HBP.Core.Object3D.Site[] sites)
        {
            typeof(Column3D).GetProperty(nameof(Column3D.ColumnData)).SetValue(column, data);
            typeof(Column3D).GetProperty(nameof(Column3D.Sites)).SetValue(column, sites.ToList());
        }

        private static Timeline CreateTimeline(out Bloc bloc)
        {
            bloc = SyntheticProjectFactory.CreateProtocol().Blocs[0];
            SubBloc subBloc = bloc.SubBlocs[0];
            EventInformation information = new(new[] { new EventInformation.EventOccurence(11, 0, 0, 0, 0, -subBloc.Window.Start, 0) });
            var statistics = new SubBlocEventsStatistics(new Dictionary<HBP.Core.Data.Event, EventInformation[]> { { subBloc.MainEvent, new[] { information } } }, AveragingType.Mean);
            return new Timeline(bloc, new Dictionary<SubBloc, List<SubBlocEventsStatistics>> { { subBloc, new List<SubBlocEventsStatistics> { statistics } } }, new Dictionary<SubBloc, int> { { subBloc, 0 } }, new Frequency(100));
        }

        private static float[] Signal(int length, float min, float max) => Enumerable.Range(0, length).Select(index => index == 0 ? min : max).ToArray();

        private static BaseData Read(Type type, string fields)
        {
            if (fields.TrimStart().StartsWith("{"))
            {
                JObject saved = JObject.Parse(fields);
                if (saved["$type"] == null) saved.AddFirst(new JProperty("$type", type.FullName));
                return ClassLoaderSaver.LoadFromJsonString<BaseData>(saved.ToString());
            }
            return ClassLoaderSaver.LoadFromJsonString<BaseData>("{\"$type\":\"" + type.FullName + "\",\"ID\":\"threshold-config\"," + fields + "}");
        }

        private static JObject Write(BaseData configuration)
        {
            using TempDirectoryScope temp = new();
            string path = temp.GetPath("thresholds.json");
            Assert.That(ClassLoaderSaver.SaveToJSon(configuration, path, true), Is.True);
            return JObject.Parse(System.IO.File.ReadAllText(path));
        }

        private void Load(BaseData configuration)
        {
            if (configuration is StaticConfiguration stat) Parameters.LoadConfiguration(stat);
            else Parameters.LoadConfiguration((DynamicConfiguration)configuration);
        }

        private void Capture(BaseData configuration)
        {
            if (configuration is StaticConfiguration stat) Parameters.CaptureConfiguration(stat);
            else Parameters.CaptureConfiguration((DynamicConfiguration)configuration);
        }

        private void AssertSpan(float min, float middle, float max)
        {
            Assert.That(Parameters.SpanMin, Is.EqualTo(min).Within(.0001f));
            Assert.That(Parameters.Middle, Is.EqualTo(middle).Within(.0001f));
            Assert.That(Parameters.SpanMax, Is.EqualTo(max).Within(.0001f));
        }

        private static void AssertRatioOnly(JObject saved, float min, float middle, float max)
        {
            Assert.That(saved["Span Min Ratio"].Value<float>(), Is.EqualTo(min).Within(.0001f));
            Assert.That(saved["Middle Ratio"].Value<float>(), Is.EqualTo(middle).Within(.0001f));
            Assert.That(saved["Span Max Ratio"].Value<float>(), Is.EqualTo(max).Within(.0001f));
            Assert.That(saved["Span Min"], Is.Null);
            Assert.That(saved["Middle"], Is.Null);
            Assert.That(saved["Span Max"], Is.Null);
        }
    }

    public class ThresholdMigrationStaticColumn : Column3DStatic
    {
        public void SetValues(params float[] values)
        {
            ActivityValuesOfUnmaskedSites = values;
            StaticParameters.MinimumAmplitude = values.Min();
            StaticParameters.MaximumAmplitude = values.Max();
            StaticParameters.RefreshSpanValues(this);
        }

        public void SetRange(float min, float max, bool hasData = true)
        {
            ActivityValuesOfUnmaskedSites = hasData ? new[] { min, max } : Array.Empty<float>();
            StaticParameters.MinimumAmplitude = min;
            StaticParameters.MaximumAmplitude = max;
            StaticParameters.RefreshSpanValues(this);
        }
    }
}
