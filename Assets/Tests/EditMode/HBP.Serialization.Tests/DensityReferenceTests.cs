using System;
using System.IO;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class DensityReferenceTests
    {
        [Test]
        public void RatiosPersistAndResolveAgainstCurrentProjectionWithoutChangingReference()
        {
            var parameters = new AnatomyDataParameters();
            parameters.SetSpanRatios(.1f, .4f, .8f);
            var configuration = new AnatomicConfiguration(23f, .2f, .5f, .7f, "density-config");
            Assert.That(configuration.SpanMinRatio, Is.EqualTo(.2f));
            Assert.That(new AnatomicConfiguration(15, .1f, .3f, .8f).SpanMaxRatio, Is.EqualTo(.8f));
            parameters.CaptureConfiguration(configuration);
            var json = JsonConvert.SerializeObject(configuration);
            Assert.That(json, Does.Contain("Span Min Ratio").And.Not.Contain("\"Span Min\":"));
            var copy = new AnatomicConfiguration();
            copy.Copy(configuration);
            foreach (var candidate in new[] { copy, (AnatomicConfiguration)configuration.Clone(), JsonConvert.DeserializeObject<AnatomicConfiguration>(json) })
            {
                var restored = new AnatomyDataParameters();
                restored.LoadConfiguration(candidate);
                Assert.That(restored.SpanMinRatio, Is.EqualTo(.1f));
                Assert.That(restored.SpanMin, Is.Zero);
                restored.PublishReference(20f);
                Assert.That(new[] { restored.SpanMin, restored.Middle, restored.SpanMax }, Is.EqualTo(new[] { 2f, 8f, 16f }));
                restored.InfluenceDistance = 40;
                Assert.That(restored.SpanMin, Is.EqualTo(2f));
                restored.PublishProjectionMaximum(10f);
                Assert.That(restored.SpanMin, Is.EqualTo(1f));
                Assert.That(restored.ReferenceMaximum, Is.EqualTo(20f));
                restored.PublishReference(30f);
                Assert.That(restored.SpanMin, Is.EqualTo(1f), "Reference refresh must not replace the current projection scale.");
                restored.PublishProjectionMaximum(0f);
                Assert.That(restored.SpanMin, Is.Zero, "An empty projection must not fall back to the nonempty reference.");
                Assert.That(restored.SpanMinRatio, Is.EqualTo(.1f));
                restored.PublishProjectionMaximum(null);
                Assert.That(restored.SpanMin, Is.EqualTo(3f));
                Assert.That(candidate.ID, Is.EqualTo("density-config"));
            }
        }

        [Test]
        public void LegacyConfigurationAndAutoUseLowerDensityInterval()
        {
            var old = JsonConvert.DeserializeObject<AnatomicConfiguration>("{\"MaximumInfluence\":27}");
            var parameters = new AnatomyDataParameters();
            parameters.SetSpanRatios(.1f, .2f, .3f);
            parameters.LoadConfiguration(old);
            parameters.PublishReference(4f);
            Assert.That(new[] { parameters.SpanMin, parameters.Middle, parameters.SpanMax }, Is.EqualTo(new[] { .2f, 1.4f, 2.6f }).Within(1e-6f));
            parameters.SetSpanValues(.5f, 1f, 2f);
            parameters.PublishReference(0f);
            Assert.That(parameters.SpanMinRatio, Is.EqualTo(.125f), "An empty reference must preserve pending ratios.");
            parameters.ResetSpanValues();
            Assert.That(parameters.SpanMinRatio, Is.EqualTo(.05f));
            Assert.That(parameters.MiddleRatio, Is.EqualTo(.35f));
            Assert.That(parameters.SpanMaxRatio, Is.EqualTo(.65f));
            Assert.Throws<ArgumentOutOfRangeException>(() => parameters.SetSpanRatios(float.NaN, .5f, .75f));
            Assert.Throws<ArgumentOutOfRangeException>(() => parameters.SetSpanRatios(-.1f, .5f, .75f));
            var partial = JsonConvert.DeserializeObject<AnatomicConfiguration>("{\"Span Min Ratio\":0.25}");
            Assert.That(new[] { partial.SpanMinRatio, partial.MiddleRatio, partial.SpanMaxRatio }, Is.EqualTo(new[] { .25f, .35f, .65f }));
            var invalid = Assert.Catch(() => JsonConvert.DeserializeObject<AnatomicConfiguration>("{\"Span Min Ratio\":0.9}"));
            Assert.That(invalid.GetBaseException(), Is.TypeOf<JsonSerializationException>());
            Assert.That(new AnatomicConfiguration("defaults-id").ID, Is.EqualTo("defaults-id"));
            Assert.That(old.MaximumInfluence, Is.EqualTo(27f));
        }

        [Test]
        [Category("NativeMigration")]
        public void ReferenceIgnoresMasksAndDisplayedRadiusWhileCalibrationOnlyChangesColors()
        {
            using var volume = new Volume();
            Assert.That(volume.LoadNIFTIFile(Path.Combine(Application.dataPath, "Data/IRM/MNI.nii")), Is.True);
            using var grid = ActivityProjectionGrid.Create(volume, 24, VolumeInterpolation.Nearest);
            using var sites = new RawSiteList();
            var dimensions = grid.Dimensions;
            var points = grid.Points;
            var p = points[(dimensions.x / 2 * dimensions.y + dimensions.y / 2) * dimensions.z + dimensions.z / 2];
            sites.AddSite("A", p, 0, 0);
            sites.AddSite("B", p, 0, 1);
            using var density = new DensityGenerator();
            density.Initialize(grid);
            var reference = density.PrepareReference(sites, 15f, SiteInfluenceByDistanceType.Constant, UnityTextureFactory.HistogramBinCount);
            Assert.That(reference.Maximum, Is.EqualTo(2f));
            Assert.That(reference.Bins.Last(), Is.GreaterThan(0));
            Assert.That(reference.Bins.Sum(), Is.LessThan(points.Length), "Zero-density space is excluded.");
            foreach (bool masked in new[] { false, true })
            foreach (float radius in new[] { 0f, 3f, 40f })
            {
                sites.UpdateMasks(new[] { masked, masked });
                density.ComputeActivity(sites, radius, SiteInfluenceByDistanceType.Constant);
                float actualMaximum = density.MaxDensity;
                var next = density.PrepareReference(sites, 15f, SiteInfluenceByDistanceType.Constant, UnityTextureFactory.HistogramBinCount);
                Assert.That(next.Maximum, Is.EqualTo(reference.Maximum));
                Assert.That(next.Bins, Is.EqualTo(reference.Bins));
                Assert.That(density.MaxDensity, Is.EqualTo(actualMaximum));
            }

            sites.UpdateMasks(new[] { false, false });
            density.ComputeActivity(sites, 15, SiteInfluenceByDistanceType.Constant);
            using var surface = new Surface();
            Vector3 v = new(-p.x, p.y, p.z);
            surface.SetBuffers(new[] { v, v + Vector3.right, v + Vector3.up }, new[] { 0, 1, 2 });
            using var projection = new SurfaceGenerator();
            projection.Initialize(density, surface, 0, 1);
            density.AdjustValues(2, 0, 4);
            projection.ComputeActivityUV(0, .8f);
            Assert.That(projection.ActivityUV[0].x, Is.EqualTo(.5f).Within(1e-5f));
            var alpha = (Vector2[])projection.AlphaUV.Clone();
            density.AdjustValues(1, .5f, 1.5f);
            projection.ComputeActivityUV(0, .8f);
            Assert.That(projection.ActivityUV[0].x, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(projection.AlphaUV, Is.EqualTo(alpha));
            Assert.That(density.MaxDensity, Is.EqualTo(2f));
        }
    }
}
