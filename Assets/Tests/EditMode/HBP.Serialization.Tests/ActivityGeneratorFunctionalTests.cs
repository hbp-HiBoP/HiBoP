using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HBP.Core.DLL;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class ActivityGeneratorFunctionalTests
    {
        [SetUp]
        public void SetUp()
        {
            NativeParityAssert.RequireHbpCore();
            NativeBackendOptions.ExperimentalBackend = NativeBackend.HbpCore;
        }

        [TearDown]
        public void TearDown()
        {
            NativeBackendOptions.Reset();
        }

        [Test]
        [Category("NativeMigration")]
        public void DensityGenerator_CoversNoSitesSingleSiteAllMaskedAndRepeatedCalls()
        {
            using Surface surface = LoadSurface();
            using Volume volume = LoadVolume("fmri_3d.nii");
            using GeneratorSurface generatorSurface = InitializeGeneratorSurface(surface, volume);
            using DensityGenerator density = new();
            density.Initialize(generatorSurface);

            using RawSiteList sites = new();
            Assert.That(density.Progress, Is.EqualTo(0.0f));
            density.ComputeActivity(sites, 10.0f, SiteInfluenceByDistanceType.Constant);
            Assert.That(density.Progress, Is.EqualTo(1.0f));
            Assert.That(density.MaxDensity, Is.EqualTo(0.0f));

            (Vector3 first, _, _, _) = FindTwoPositiveSurfaceVertices(surface, volume);
            sites.AddSite("S1", ToNative(first), 0, 0);
            density.ComputeActivity(sites, 1000.0f, SiteInfluenceByDistanceType.Constant);
            Assert.That(density.MaxDensity, Is.EqualTo(0.0f), "Sites are masked by default.");

            sites.UpdateMask(0, false);
            density.ComputeActivity(sites, 1000.0f, SiteInfluenceByDistanceType.Constant);
            Assert.That(density.MaxDensity, Is.EqualTo(1.0f));

            sites.AddSite("S2", ToNative(first), 0, 1);
            sites.UpdateMask(1, false);
            density.ComputeActivity(sites, 1000.0f, SiteInfluenceByDistanceType.Constant);
            Assert.That(density.MaxDensity, Is.EqualTo(2.0f));

            sites.UpdateMask(0, true);
            sites.UpdateMask(1, true);
            density.ComputeActivity(sites, 1000.0f, SiteInfluenceByDistanceType.Constant);
            Assert.That(density.MaxDensity, Is.EqualTo(0.0f));

            sites.UpdateMask(1, false);
            density.ComputeActivity(sites, 1000.0f, SiteInfluenceByDistanceType.Constant);
            Assert.That(density.MaxDensity, Is.EqualTo(1.0f));
            Assert.That(density.Progress, Is.EqualTo(1.0f));
        }

        [TestCase(SiteInfluenceByDistanceType.Constant, 1.0f)]
        [TestCase(SiteInfluenceByDistanceType.Linear, 0.5f)]
        [TestCase(SiteInfluenceByDistanceType.Quadratic, 0.25f)]
        [Category("NativeMigration")]
        public void DensityAndIeegGenerators_ApplyEveryDistanceMode(SiteInfluenceByDistanceType mode, float expectedWeight)
        {
            using Surface surface = LoadSurface();
            using Volume volume = LoadVolume("fmri_3d.nii");
            using GeneratorSurface generatorSurface = InitializeGeneratorSurface(surface, volume);
            (Vector3 first, Vector3 second, int firstIndex, int secondIndex) = FindTwoPositiveSurfaceVertices(surface, volume);
            float influenceDistance = Vector3.Distance(first, second) * 2.0f;

            using RawSiteList sites = new();
            sites.AddSite("S1", ToNative(first), 0, 0);
            sites.UpdateMask(0, false);

            using DensityGenerator density = new();
            density.Initialize(generatorSurface);
            density.ComputeActivity(sites, influenceDistance, mode);
            using SurfaceGenerator densitySurface = InitializeSurfaceGenerator(density);
            densitySurface.ComputeActivityUV(0, 0.25f);
            Assert.That(densitySurface.ActivityUV[firstIndex].x, Is.EqualTo(1.0f).Within(0.0005f));
            Assert.That(densitySurface.ActivityUV[secondIndex].x, Is.EqualTo(expectedWeight).Within(0.0005f));

            using IEEGGenerator ieeg = new();
            ieeg.Initialize(generatorSurface);
            ieeg.ComputeActivity(sites, influenceDistance, new[] { -0.75f }, 1, 1, mode);
            ieeg.AdjustValues(0.0f, -1.0f, 1.0f);
            using SurfaceGenerator ieegSurface = InitializeSurfaceGenerator(ieeg);
            const float alpha = 0.25f;
            ieegSurface.ComputeActivityUV(0, alpha);
            Assert.That(ieegSurface.ActivityUV[secondIndex].x, Is.EqualTo(0.125f).Within(0.0005f));
            Assert.That(ieegSurface.AlphaUV[firstIndex].x, Is.EqualTo(1.0f).Within(0.0005f));
            Assert.That(ieegSurface.AlphaUV[secondIndex].x, Is.EqualTo(expectedWeight * (1.0f - alpha) + alpha).Within(0.0005f));
        }

        [Test]
        [Category("NativeMigration")]
        public void IeegGenerator_HandlesEmptyIdenticalExtremeNegativeAndNonFiniteInputs()
        {
            using Surface surface = LoadSurface();
            using Volume volume = LoadVolume("fmri_3d.nii");
            using GeneratorSurface generatorSurface = InitializeGeneratorSurface(surface, volume);
            (Vector3 first, _, int firstIndex, _) = FindTwoPositiveSurfaceVertices(surface, volume);

            using IEEGGenerator ieeg = new();
            ieeg.Initialize(generatorSurface);
            using RawSiteList noSites = new();
            ieeg.ComputeActivity(noSites, 10.0f, Array.Empty<float>(), 1, 0, SiteInfluenceByDistanceType.Constant);
            using SurfaceGenerator surfaceGenerator = InitializeSurfaceGenerator(ieeg);
            surfaceGenerator.ComputeActivityUV();
            Assert.That(surfaceGenerator.ActivityUV, Has.All.EqualTo(new Vector2(0.5f, 1.0f)));
            Assert.That(ieeg.Progress, Is.EqualTo(1.0f));

            using RawSiteList oneSite = new();
            oneSite.AddSite("S1", ToNative(first), 0, 0);
            oneSite.UpdateMask(0, false);
            ieeg.ComputeActivity(oneSite, 1000.0f, new[] { 2.0f }, 1, 1, SiteInfluenceByDistanceType.Constant);
            ieeg.AdjustValues(2.0f, 2.0f, 2.0f);
            surfaceGenerator.ComputeActivityUV();
            Assert.That(surfaceGenerator.ActivityUV[firstIndex].x, Is.EqualTo(0.5f).Within(0.0005f));

            ieeg.ComputeActivity(oneSite, 1000.0f, new[] { -1.0e20f }, 1, 1, SiteInfluenceByDistanceType.Constant);
            ieeg.AdjustValues(0.0f, -1.0e20f, 1.0e20f);
            surfaceGenerator.ComputeActivityUV();
            Assert.That(surfaceGenerator.ActivityUV[firstIndex].x, Is.EqualTo(0.0f).Within(0.0005f));
            Assert.That(float.IsFinite(surfaceGenerator.ActivityUV[firstIndex].x), Is.True);

            Assert.Throws<InvalidOperationException>(() =>
                ieeg.ComputeActivity(oneSite, 1000.0f, new[] { float.NaN }, 1, 1, SiteInfluenceByDistanceType.Constant));
            Assert.Throws<ArgumentException>(() =>
                ieeg.ComputeActivity(oneSite, 1000.0f, Array.Empty<float>(), 1, 1, SiteInfluenceByDistanceType.Constant));
            Assert.Throws<ArgumentException>(() =>
                ieeg.ComputeActivity(oneSite, 1000.0f, new[] { 1.0f }, 1, 0, SiteInfluenceByDistanceType.Constant));
            Assert.Throws<InvalidOperationException>(() => ieeg.AdjustValues(0.0f, float.NegativeInfinity, 1.0f));
        }

        [Test]
        [Category("NativeMigration")]
        public void SurfaceGenerator_ComputeMainUvMatchesVolumeOracleAndRejectsInvalidInputs()
        {
            using Surface surface = LoadSurface();
            using Volume volume = LoadVolume("fmri_3d.nii");
            using GeneratorSurface generatorSurface = InitializeGeneratorSurface(surface, volume);
            using DensityGenerator density = new();
            density.Initialize(generatorSurface);
            using SurfaceGenerator surfaceGenerator = InitializeSurfaceGenerator(density);

            const float calMin = 0.25f;
            const float calMax = 0.75f;
            surfaceGenerator.ComputeMainUV(calMin, calMax);
            Mesh mesh = new();
            try
            {
                surface.UpdateMeshFromDLL(mesh);
                float[] values = volume.GetVerticesValues(surface);
                HBP.Core.Tools.MRICalValues extrema = volume.ExtremeValues;
                float diff = extrema.ComputedCalMax - extrema.ComputedCalMin;
                float minValue = extrema.ComputedCalMin + calMin * diff;
                float maxValue = extrema.ComputedCalMin + calMax * diff;
                Assert.That(mesh.uv, Has.Length.EqualTo(values.Length));
                for (int i = 0; i < values.Length; ++i)
                {
                    Vector2 expected = Vector2.zero;
                    if (values[i] > 0.0f)
                    {
                        float clamped = Mathf.Clamp(values[i], minValue, maxValue);
                        expected = new Vector2((clamped - minValue) / diff, 1.0f);
                    }
                    Assert.That(mesh.uv[i].x, Is.EqualTo(expected.x).Within(0.0005f), $"uv[{i}].x");
                    Assert.That(mesh.uv[i].y, Is.EqualTo(expected.y).Within(0.0005f), $"uv[{i}].y");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }

            Assert.Throws<InvalidOperationException>(() => surfaceGenerator.ComputeMainUV(float.NaN, 1.0f));
            using Surface emptySurface = new();
            using GeneratorSurface emptySurfaceGenerator = new();
            Assert.Throws<InvalidOperationException>(() => emptySurfaceGenerator.Initialize(emptySurface, volume, 8));
            using Volume emptyVolume = new();
            using GeneratorSurface emptyVolumeGenerator = new();
            Assert.Throws<InvalidOperationException>(() => emptyVolumeGenerator.Initialize(surface, emptyVolume, 8));
        }

        [Test]
        [Category("NativeMigration")]
        public void IeegGenerator_ComputeActivityAtlasCoversTimelinesMasksAndRepeatedCalls()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                string indexPath = WriteMarsIndex(tempDirectory);
                string brodmannPath = Path.Combine(tempDirectory, "brodmann.txt");
                File.WriteAllText(brodmannPath, "BA0\n");
                using MarsAtlas atlas = new();
                Assert.That(atlas.Load(indexPath, brodmannPath, NativeParityAssert.NativePath("Nifti", "fmri_3d.nii")), Is.True);

                using Surface surface = LoadSurface();
                using Volume volume = LoadVolume("fmri_3d.nii");
                using GeneratorSurface generatorSurface = InitializeGeneratorSurface(surface, volume);
                using IEEGGenerator ieeg = new();
                ieeg.Initialize(generatorSurface);
                using SurfaceGenerator surfaceGenerator = InitializeSurfaceGenerator(ieeg);

                int areaCount = atlas.Labels().Max() + 1;
                const int timelineLength = 2;
                float[] activity = new float[areaCount * timelineLength];
                int[] mask = new int[areaCount];
                for (int label = 1; label < areaCount; ++label)
                {
                    activity[label * timelineLength] = (float)label / (areaCount - 1);
                    activity[label * timelineLength + 1] = -(float)label / (areaCount - 1);
                }

                ieeg.ComputeActivityAtlas(activity, timelineLength, mask, atlas);
                ieeg.AdjustValues(0.0f, -1.0f, 1.0f);
                Assert.That(ieeg.Progress, Is.EqualTo(1.0f));
                surfaceGenerator.ComputeActivityUV(0, 0.2f);

                Mesh mesh = new();
                try
                {
                    surface.UpdateMeshFromDLL(mesh);
                    float[] referenceValues = volume.GetVerticesValues(surface);
                    int influenced = 0;
                    for (int i = 0; i < mesh.vertexCount; ++i)
                    {
                        int label = atlas.GetClosestAreaIndex(mesh.vertices[i], 0);
                        if (label <= 0 || label >= areaCount || referenceValues[i] <= 0.0f) continue;
                        ++influenced;
                        float expected = (activity[label * timelineLength] + 1.0f) * 0.5f;
                        Assert.That(surfaceGenerator.ActivityUV[i].x, Is.EqualTo(expected).Within(0.0005f));
                        Assert.That(surfaceGenerator.ActivityUV[i].y, Is.EqualTo(0.0f).Within(0.0005f));
                        Assert.That(surfaceGenerator.AlphaUV[i].x, Is.EqualTo(1.0f).Within(0.0005f));
                        Assert.That(surfaceGenerator.AlphaUV[i].y, Is.EqualTo(0.0f).Within(0.0005f));
                    }
                    Assert.That(influenced, Is.GreaterThan(0));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }

                Array.Fill(mask, 1);
                ieeg.ComputeActivityAtlas(activity, timelineLength, mask, atlas);
                surfaceGenerator.ComputeActivityUV(1, 0.2f);
                Assert.That(surfaceGenerator.ActivityUV, Has.All.EqualTo(new Vector2(0.5f, 1.0f)));
                Assert.That(surfaceGenerator.AlphaUV, Has.All.EqualTo(new Vector2(0.01f, 1.0f)));
                Assert.That(ieeg.Progress, Is.EqualTo(1.0f));

                activity[2] = float.PositiveInfinity;
                Assert.Throws<InvalidOperationException>(() => ieeg.ComputeActivityAtlas(activity, timelineLength, mask, atlas));
                Assert.Throws<ArgumentException>(() => ieeg.ComputeActivityAtlas(new float[activity.Length - 1], timelineLength, mask, atlas));
            }
            finally
            {
                Directory.Delete(tempDirectory, true);
            }
        }

        [TestCase(GeneratorKind.Fmri)]
        [TestCase(GeneratorKind.Meg)]
        [Category("NativeMigration")]
        public void VolumeActivityGenerator_CoversMultiVolumeEveryHideModeAndRepeatedCalls(GeneratorKind kind)
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                string firstPath = WriteNiftiValues(tempDirectory, "signed-a.nii", i => -2.0f + 4.0f * i / 124.0f);
                string secondPath = WriteNiftiValues(tempDirectory, "signed-b.nii", i => 2.0f - 4.0f * i / 124.0f);
                string nonFinitePath = WriteNiftiValues(tempDirectory, "non-finite.nii", _ => float.NaN);
                using Surface surface = LoadSurface();
                using Volume reference = LoadVolume("fmri_3d.nii");
                using Volume first = LoadVolumePath(firstPath);
                using Volume second = LoadVolumePath(secondPath);
                using Volume nonFinite = LoadVolumePath(nonFinitePath);
                using Volume emptyMaskA = new();
                using Volume emptyMaskB = new();
                using GeneratorSurface generatorSurface = InitializeGeneratorSurface(surface, reference);
                using ActivityGenerator generator = CreateVolumeGenerator(kind);
                generator.Initialize(generatorSurface);
                ComputeVolumeActivity(generator, new[] { (first, emptyMaskA), (second, emptyMaskB) });
                AdjustVolumeValues(generator, 0.25f, 0.75f, 0.25f, 0.75f);
                using SurfaceGenerator surfaceGenerator = InitializeSurfaceGenerator(generator);

                float[] referenceValues = reference.GetVerticesValues(surface);
                float[] rawValues = first.GetVerticesValues(surface);
                float globalMin = Mathf.Min(first.ExtremeValues.Min, second.ExtremeValues.Min);
                float globalMax = Mathf.Max(first.ExtremeValues.Max, second.ExtremeValues.Max);
                const float alpha = 0.2f;
                for (int flags = 0; flags < 8; ++flags)
                {
                    bool hideLower = (flags & 1) != 0;
                    bool hideMiddle = (flags & 2) != 0;
                    bool hideHigher = (flags & 4) != 0;
                    SetHideValues(generator, hideLower, hideMiddle, hideHigher);
                    surfaceGenerator.ComputeActivityUV(0, alpha);
                    for (int i = 0; i < rawValues.Length; ++i)
                    {
                        float normalized = (rawValues[i] - globalMin) / (globalMax - globalMin) * 2.0f - 1.0f;
                        bool hidden = hideLower && normalized < -0.75f
                            || hideMiddle && normalized > -0.25f && normalized < 0.25f
                            || hideHigher && normalized > 0.75f;
                        bool visible = referenceValues[i] > 0.0f && !hidden;
                        Vector2 expected = visible
                            ? new Vector2(0.1f * (1.0f - alpha) + alpha, 0.0f)
                            : new Vector2(0.01f, 1.0f);
                        Assert.That(surfaceGenerator.AlphaUV[i].x, Is.EqualTo(expected.x).Within(0.0005f), $"flags={flags}, alpha[{i}].x");
                        Assert.That(surfaceGenerator.AlphaUV[i].y, Is.EqualTo(expected.y).Within(0.0005f), $"flags={flags}, alpha[{i}].y");
                    }
                }

                SetHideValues(generator, false, false, false);
                surfaceGenerator.ComputeActivityUV(0, alpha);
                Vector2[] firstTimeline = (Vector2[])surfaceGenerator.ActivityUV.Clone();
                surfaceGenerator.ComputeActivityUV(1, alpha);
                Assert.That(surfaceGenerator.ActivityUV.Where((value, index) => value != firstTimeline[index]), Is.Not.Empty);

                ComputeVolumeActivity(generator, new[] { (second, emptyMaskB), (first, emptyMaskA) });
                surfaceGenerator.ComputeActivityUV(1, alpha);
                AssertVectorArrays(surfaceGenerator.ActivityUV, firstTimeline);
                Assert.That(generator.Progress, Is.EqualTo(1.0f));

                using Volume emptyMask = new();
                ComputeVolumeActivity(generator, new[] { (nonFinite, emptyMask) });
                surfaceGenerator.ComputeActivityUV(0, alpha);
                Assert.That(surfaceGenerator.ActivityUV, Has.All.EqualTo(new Vector2(0.0f, 0.0f)),
                    "niftilib normalizes non-finite NIfTI samples to visible zero activity before generation");
                Assert.That(surfaceGenerator.AlphaUV, Has.All.EqualTo(new Vector2(0.28f, 0.0f)));
                Assert.That(generator.Progress, Is.EqualTo(1.0f));
                Assert.Throws<InvalidOperationException>(() => ComputeVolumeActivity(generator, Array.Empty<(Volume, Volume)>()));
            }
            finally
            {
                Directory.Delete(tempDirectory, true);
            }
        }

        private static Surface LoadSurface()
        {
            Surface surface = new();
            try
            {
                Assert.That(surface.LoadGIIFile(NativeParityAssert.NativePath("Meshes", "single_surface.gii")), Is.True);
                return surface;
            }
            catch
            {
                surface.Dispose();
                throw;
            }
        }

        private static Volume LoadVolume(string fileName) => LoadVolumePath(NativeParityAssert.NativePath("Nifti", fileName));

        private static Volume LoadVolumePath(string path)
        {
            Volume volume = new();
            try
            {
                Assert.That(volume.LoadNIFTIFile(path), Is.True, path);
                return volume;
            }
            catch
            {
                volume.Dispose();
                throw;
            }
        }

        private static GeneratorSurface InitializeGeneratorSurface(Surface surface, Volume volume)
        {
            GeneratorSurface generator = new();
            try
            {
                generator.Initialize(surface, volume, 8);
                return generator;
            }
            catch
            {
                generator.Dispose();
                throw;
            }
        }

        private static SurfaceGenerator InitializeSurfaceGenerator(ActivityGenerator activity)
        {
            SurfaceGenerator generator = new();
            try
            {
                generator.Initialize(activity);
                return generator;
            }
            catch
            {
                generator.Dispose();
                throw;
            }
        }

        private static (Vector3 First, Vector3 Second, int FirstIndex, int SecondIndex) FindTwoPositiveSurfaceVertices(Surface surface, Volume volume)
        {
            Mesh mesh = new();
            try
            {
                surface.UpdateMeshFromDLL(mesh);
                float[] values = volume.GetVerticesValues(surface);
                int first = Array.FindIndex(values, value => value > 0.0f);
                Assert.That(first, Is.GreaterThanOrEqualTo(0));
                int second = -1;
                for (int i = first + 1; i < values.Length; ++i)
                {
                    if (values[i] > 0.0f && Vector3.Distance(mesh.vertices[first], mesh.vertices[i]) > 0.0001f)
                    {
                        second = i;
                        break;
                    }
                }
                Assert.That(second, Is.GreaterThanOrEqualTo(0));
                return (mesh.vertices[first], mesh.vertices[second], first, second);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private static Vector3 ToNative(Vector3 unity) => new(-unity.x, unity.y, unity.z);

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), $"hibop_activity_generator_{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return path;
        }

        private static string WriteMarsIndex(string directory)
        {
            string path = Path.Combine(directory, "mars-index.csv");
            List<string> lines = new() { "label,hemisphere,lobe,nameFS,name,fullName,BA,color" };
            for (int label = 1; label <= 124; ++label)
            {
                lines.Add($"{label},L,Frontal,fs_{label},Area{label},Area {label},0,255 0 0");
            }
            File.WriteAllLines(path, lines);
            return path;
        }

        private static string WriteNiftiValues(string directory, string fileName, Func<int, float> valueFactory)
        {
            byte[] bytes = File.ReadAllBytes(NativeParityAssert.NativePath("Nifti", "fmri_3d.nii"));
            int offset = (int)Math.Round(BitConverter.ToSingle(bytes, 108));
            float min = float.PositiveInfinity;
            float max = float.NegativeInfinity;
            for (int i = 0; i < 125; ++i)
            {
                float value = valueFactory(i);
                byte[] encoded = BitConverter.GetBytes(value);
                Buffer.BlockCopy(encoded, 0, bytes, offset + i * sizeof(float), encoded.Length);
                if (float.IsFinite(value))
                {
                    min = Mathf.Min(min, value);
                    max = Mathf.Max(max, value);
                }
            }
            if (float.IsFinite(min)) Buffer.BlockCopy(BitConverter.GetBytes(min), 0, bytes, 128, sizeof(float));
            if (float.IsFinite(max)) Buffer.BlockCopy(BitConverter.GetBytes(max), 0, bytes, 124, sizeof(float));
            string path = Path.Combine(directory, fileName);
            File.WriteAllBytes(path, bytes);
            return path;
        }

        private static ActivityGenerator CreateVolumeGenerator(GeneratorKind kind)
        {
            return kind == GeneratorKind.Fmri ? new FMRIGenerator() : new MEGGenerator();
        }

        private static void ComputeVolumeActivity(ActivityGenerator generator, IEnumerable<(Volume, Volume)> volumes)
        {
            if (generator is FMRIGenerator fmri) fmri.ComputeActivity(volumes);
            else if (generator is MEGGenerator meg) meg.ComputeActivity(volumes);
            else throw new ArgumentOutOfRangeException(nameof(generator));
        }

        private static void AdjustVolumeValues(ActivityGenerator generator, float negativeMin, float negativeMax, float positiveMin, float positiveMax)
        {
            if (generator is FMRIGenerator fmri) fmri.AdjustValues(negativeMin, negativeMax, positiveMin, positiveMax);
            else if (generator is MEGGenerator meg) meg.AdjustValues(negativeMin, negativeMax, positiveMin, positiveMax);
            else throw new ArgumentOutOfRangeException(nameof(generator));
        }

        private static void SetHideValues(ActivityGenerator generator, bool lower, bool middle, bool higher)
        {
            if (generator is FMRIGenerator fmri) fmri.HideExtremeValues(lower, middle, higher);
            else if (generator is MEGGenerator meg) meg.HideExtremeValues(lower, middle, higher);
            else throw new ArgumentOutOfRangeException(nameof(generator));
        }

        private static void AssertVectorArrays(IReadOnlyList<Vector2> actual, IReadOnlyList<Vector2> expected)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            for (int i = 0; i < expected.Count; ++i)
            {
                Assert.That(actual[i].x, Is.EqualTo(expected[i].x).Within(0.0005f), $"value[{i}].x");
                Assert.That(actual[i].y, Is.EqualTo(expected[i].y).Within(0.0005f), $"value[{i}].y");
            }
        }

        public enum GeneratorKind
        {
            Fmri,
            Meg
        }
    }
}
