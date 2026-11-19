using System;
using System.IO;
using System.Linq;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Transfer.Anatomy;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Transfer.Anatomy.Desktop
{
    public class IEEGProjectionTests
    {
        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(8, false)]
        [TestCase(8, true)]
        public void CommonProjectionMatchesPreviousDesktopAcrossSamplesAndParameters(int count, bool allMasked)
        {
            using var surface = new Surface();
            surface.SetBuffers(new[] { new Vector3(10, 0, 0), new Vector3(11, 1, 0), new Vector3(9, 0, 1), new Vector3(1000, 0, 0) }, new[] { 0, 1, 2, 1, 2, 3 });
            using var sites = new RawSiteList();
            for (int i = 0; i < count; ++i) sites.AddSite("S" + i, new Vector3(-10 + i, i, 0), 0, i);
            var masks = Enumerable.Range(0, count).Select(i => allMasked || i % 3 == 2).ToArray();
            foreach (VolumeInterpolation interpolation in Enum.GetValues(typeof(VolumeInterpolation)))
            foreach (SiteInfluenceByDistanceType rule in Enum.GetValues(typeof(SiteInfluenceByDistanceType)))
                Compare(surface, sites, masks, 24, interpolation, rule);
        }

        [Test]
        public void FullMniSurfaceMatchesPreviousDesktopAcrossSamples()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-questAnatomyFixture");
            if (index < 0) Assert.Ignore("Supply the QUEST-017 eight-contact MNI HBNA capture with -questAnatomyFixture.");
            var snapshot = AnatomySnapshotCodec.Decode(File.ReadAllBytes(args[index + 1]));
            Assert.That(snapshot.Contacts.Sites.Count, Is.EqualTo(8));
            using var surface = new Surface();
            surface.SetBuffers(Enumerable.Range(0, snapshot.Positions.Count / 3).Select(i => new Vector3(snapshot.Positions[i * 3], snapshot.Positions[i * 3 + 1], snapshot.Positions[i * 3 + 2])).ToArray(), snapshot.Indices.ToArray().Select(i => (int)i).ToArray());
            using var sites = new RawSiteList();
            foreach (var site in snapshot.Contacts.Sites) sites.AddSite(site.Name, new Vector3(-site.Position[0], site.Position[1], site.Position[2]), site.PatientIndex, site.SourceIndex);
            Compare(surface, sites, snapshot.Contacts.Sites.Select(site => site.EffectiveMasked).ToArray(), 80, VolumeInterpolation.Trilinear, SiteInfluenceByDistanceType.Quadratic);
        }

        // Previous Desktop orchestration at 8638b7aad166, retained only as the reference.
        private static void Compare(Surface surface, RawSiteList sites, bool[] masks, int dimension, VolumeInterpolation interpolation, SiteInfluenceByDistanceType rule)
        {
            using var volume = new Volume();
            Assert.That(volume.LoadNIFTIFile(Path.Combine(Application.dataPath, "Data/IRM/MNI.nii")), Is.True);
            using var grid = ActivityProjectionGrid.Create(volume, dimension, interpolation);
            using var generator = new IEEGGenerator();
            using var reference = new IEEGGenerator();
            using var instant = new IEEGGenerator();
            generator.Initialize(grid);
            reference.Initialize(grid);
            instant.Initialize(grid);
            using var projection = new SurfaceGenerator();
            using var referenceProjection = new SurfaceGenerator();
            using var instantProjection = new SurfaceGenerator();
            projection.Initialize(generator, surface, 0, 1);
            referenceProjection.Initialize(reference, surface, 0, 1);
            instantProjection.Initialize(instant, surface, 0, 1);
            int count = sites.NumberOfSites;
            const int length = 5;
            // Time-major samples include negative, zero and positive amplitudes.
            var values = Enumerable.Range(0, length).SelectMany(t => Enumerable.Range(0, count).Select(s => (t - 2f) * (s % 3 - 1) * 3f)).ToArray();
            Vector2[][] initial = null;
            foreach (var parameters in new[] { (15f, 0f, -10f, 10f), (25f, -1f, -8f, 3f), (15f, 0f, -10f, 10f) })
            {
                var (distance, middle, minimum, maximum) = parameters;
                for (int i = 0; i < count; ++i) sites.UpdateMask(i, masks[i]);
                reference.ComputeActivity(sites, distance, values, length, count, rule);
                var referenceMetrics = reference.GetLastComputeMetrics();
                reference.AdjustValues(middle, minimum, maximum);
                sites.UpdateMasks(masks);
                var metrics = generator.ComputeCalibratedActivity(sites, distance, values, length, count, rule, middle, minimum, maximum);
                Assert.That(metrics.storedValueCount, Is.EqualTo(referenceMetrics.storedValueCount));
                Assert.That(metrics.storedWeightCount, Is.EqualTo(referenceMetrics.storedWeightCount));
                var current = new Vector2[length][];
                for (int t = 0; t < length; ++t)
                {
                    // A future single-instant consumer must use the full preparation's calibration.
                    instant.ComputeCalibratedActivity(sites, distance, values.Skip(t * count).Take(count).ToArray(), 1, count, rule, middle, minimum, maximum);
                    foreach (float alpha in new[] { 0f, .8f, 1f })
                    {
                        projection.ComputeActivityUV(t, alpha);
                        referenceProjection.ComputeActivityUV(t, alpha);
                        instantProjection.ComputeActivityUV(0, alpha);
                        Assert.That(projection.ActivityUV, Is.EqualTo(referenceProjection.ActivityUV), $"Activity sample {t}");
                        Assert.That(projection.AlphaUV, Is.EqualTo(referenceProjection.AlphaUV), $"Alpha sample {t}");
                        Assert.That(instantProjection.ActivityUV, Is.EqualTo(projection.ActivityUV), $"Single instant {t}");
                        Assert.That(instantProjection.AlphaUV, Is.EqualTo(projection.AlphaUV));
                        Assert.That(projection.ActivityUV.All(uv => float.IsFinite(uv.x) && float.IsFinite(uv.y)), Is.True);
                        Assert.That(projection.AlphaUV.All(uv => float.IsFinite(uv.x) && float.IsFinite(uv.y)), Is.True);
                        Assert.That(projection.ProjectionCoverage.validVertexCount, Is.EqualTo(referenceProjection.ProjectionCoverage.validVertexCount));
                        Assert.That(projection.ProjectionCoverage.classification, Is.EqualTo(referenceProjection.ProjectionCoverage.classification));
                    }

                    current[t] = (Vector2[])projection.ActivityUV.Clone();
                    if (initial != null && distance == 15) Assert.That(current[t], Is.EqualTo(initial[t]));
                }

                initial ??= current;
                // Recalibrate an already computed field through the same native wrapper.
                generator.AdjustValues(2, -5, 9);
                reference.AdjustValues(2, -5, 9);
                for (int t = 0; t < length; ++t)
                {
                    projection.ComputeActivityUV(t, .8f);
                    referenceProjection.ComputeActivityUV(t, .8f);
                    Assert.That(projection.ActivityUV, Is.EqualTo(referenceProjection.ActivityUV));
                    Assert.That(projection.AlphaUV, Is.EqualTo(referenceProjection.AlphaUV));
                }
            }
        }
    }
}
