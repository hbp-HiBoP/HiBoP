using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using HBP.Core.Object3D;
using HBP.RenderModelAdapters;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class P03RenderModelGoldenTests
    {
        private const float MathematicalTolerance = 0.000001f;

        [Test]
        public void D0D5D6_DesktopCaptureAndIndependentReconstruction_ProduceStableParityArtifacts()
        {
            string artifactRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".artifacts", "xr", "p03"));
            Directory.CreateDirectory(artifactRoot);
            IReadOnlyDictionary<string, BaselineEntry> baseline = LoadBaseline();
            HashSet<string> recordedNames = new();
            List<GoldenEntry> entries = new();

            Mesh surfaceMesh = CreateQuadMesh();
            GameObject firstSiteObject = null;
            GameObject secondSiteObject = null;
            try
            {
                byte[] surfaceSource = SerializeMesh(surfaceMesh);
                SurfaceAsset surfaceAsset = DesktopSurfaceRenderModelAdapter.CaptureAsset(surfaceMesh, Hash(surfaceSource), CRNL.HiBoP.RenderModel.SurfaceRepresentation.Anatomical);
                byte[] surfaceCandidate = SerializeSurfaceAsset(surfaceAsset);
                RecordPair(artifactRoot, baseline, recordedNames, entries, "d0-surface", surfaceSource, surfaceCandidate);

                firstSiteObject = CreateSite("D6_PATIENT_SENTINEL_A", new Vector3(1f, 2f, 3f));
                secondSiteObject = CreateSite("D6_PATIENT_SENTINEL_B", new Vector3(-4f, 5f, 6f));
                Site[] sites = { firstSiteObject.GetComponent<Site>(), secondSiteObject.GetComponent<Site>() };
                ContractId[] siteIds = { Id(101), Id(102) };
                byte[] siteSource = SerializeSites(siteIds, sites.Select(site => site.Information.DefaultPosition).ToArray());
                SiteAsset siteAsset = DesktopSiteRenderModelAdapter.CaptureAsset(sites, siteIds, Hash(siteSource));
                byte[] siteCandidate = SerializeSiteAsset(siteAsset);
                RecordPair(artifactRoot, baseline, recordedNames, entries, "d0-sites", siteSource, siteCandidate);
                sites[0].Information.DefaultPosition = new Vector3(99f, 99f, 99f);
                Assert.That(siteAsset.Positions[0], Is.EqualTo(new Float3(1f, 2f, 3f)), "The adapter must not alias mutable HiBoP site data.");

                CutGeometryAsset cutGeometry = DesktopCutRenderModelAdapter.CaptureGeometry(surfaceMesh, Hash(surfaceSource));
                byte[] cutGeometryCandidate = SerializeCutGeometry(cutGeometry);
                RecordPair(artifactRoot, baseline, recordedNames, entries, "d0-cut-geometry", surfaceSource, cutGeometryCandidate);

                Color32[] cutPixels =
                {
                    new(10, 20, 30, 255), new(40, 50, 60, 192),
                    new(70, 80, 90, 128), new(100, 110, 120, 0),
                };
                byte[] cutSource = SerializeColors(cutPixels);
                CutOverlayFrame cutOverlay = DesktopCutRenderModelAdapter.CaptureOverlay(cutPixels, 2, 2, Id(201), Id(200), new StateRevision(2), new RenderTemporalSample(0, 0.75f), new ScopeRevision(1));
                byte[] cutCandidate = SerializeColors(cutOverlay.Pixels);
                RecordPair(artifactRoot, baseline, recordedNames, entries, "d0-cut-overlay", cutSource, cutCandidate);
                RecordPngPair(artifactRoot, baseline, recordedNames, entries, "d0-cut-image", cutPixels, ToColor32(cutOverlay.Pixels), 2, 2);
                cutPixels[0] = new Color32(255, 255, 255, 255);
                Assert.That(cutOverlay.Pixels[0], Is.EqualTo(new Rgba32(10, 20, 30, 255)), "The adapter must not alias mutable Unity pixel data.");

                Vector2[] desktopActivityUvs = { new(0f, 0f), new(0.5f, 1f) };
                Vector2[] desktopOpacityUvs = { new(0.8f, 0f), new(0.01f, 1f) };
                RenderTemporalSample sample = new(0, 0.75f);
                SurfaceFrame surfaceFrame = DesktopSurfaceRenderModelAdapter.CaptureFrame(desktopActivityUvs, desktopOpacityUvs, surfaceAsset.Hash, new StateRevision(2), sample);
                SurfaceRenderStreams reconstructed = RenderModelReconstructor.ReconstructSurfaceStreams(surfaceFrame);
                byte[] d5SurfaceSource = SerializeVector2(desktopActivityUvs.Concat(desktopOpacityUvs).ToArray());
                byte[] d5SurfaceCandidate = SerializeFloat2(reconstructed.ActivityUvs.ToArray().Concat(reconstructed.OpacityUvs.ToArray()).ToArray());
                RecordPair(artifactRoot, baseline, recordedNames, entries, "d5-surface-sample-and-hold", d5SurfaceSource, d5SurfaceCandidate);
                desktopActivityUvs[0] = new Vector2(99f, 99f);
                Assert.That(surfaceFrame.ActivityValues[0], Is.EqualTo(0f), "The adapter must not alias mutable Unity UV data.");

                HBP.Core.Data.TemporalSample desktopSample = new(0, 0.75f);
                float desktopSiteValue = desktopSample.Evaluate(new[] { 0f, 10f });
                float renderModelSiteValue = sample.EvaluateLinear(0f, 10f);
                Assert.That(Mathf.Abs(renderModelSiteValue - desktopSiteValue), Is.LessThanOrEqualTo(MathematicalTolerance));
                byte[] d5SiteSource = SerializeFloats(new[] { 0f, 10f, desktopSample.Alpha, desktopSiteValue });
                byte[] d5SiteCandidate = SerializeFloats(new[] { 0f, 10f, sample.TemporalAlpha, renderModelSiteValue });
                RecordPair(artifactRoot, baseline, recordedNames, entries, "d5-site-linear", d5SiteSource, d5SiteCandidate);

                Assert.That(surfaceFrame.TemporalApplication, Is.EqualTo(TemporalApplication.SampleAndHold));
                Assert.That(cutOverlay.TemporalApplication, Is.EqualTo(TemporalApplication.SampleAndHold));
                Assert.That(renderModelSiteValue, Is.Not.EqualTo(surfaceFrame.ActivityValues[0]));

                string manifest = BuildManifest(entries);
                File.WriteAllText(Path.Combine(artifactRoot, "manifest.json"), manifest, new UTF8Encoding(false));
                Assert.That(recordedNames, Is.EquivalentTo(baseline.Keys), "Every approved P00 baseline entry must be reconstructed exactly once.");
                AssertNoSentinelInArtifacts(artifactRoot, "D6_PATIENT_SENTINEL_A", "D6_PATIENT_SENTINEL_B");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(surfaceMesh);
                if (firstSiteObject != null) UnityEngine.Object.DestroyImmediate(firstSiteObject);
                if (secondSiteObject != null) UnityEngine.Object.DestroyImmediate(secondSiteObject);
            }
        }

        [Test]
        public void SurfaceCapture_RejectsLossyUvSentinelNormalization()
        {
            AssetHash hash = Hash(new byte[] { 1 });
            RenderTemporalSample sample = new(0, 0.5f);

            Assert.Throws<ArgumentException>(() => DesktopSurfaceRenderModelAdapter.CaptureFrame(new[] { new Vector2(0.25f, 0f) }, new[] { new Vector2(0.8f, 1f) }, hash, new StateRevision(1), sample));
            Assert.Throws<ArgumentException>(() => DesktopSurfaceRenderModelAdapter.CaptureFrame(new[] { new Vector2(0.25f, 0.5f) }, new[] { new Vector2(0.8f, 0.5f) }, hash, new StateRevision(1), sample));
        }

        private static Mesh CreateQuadMesh()
        {
            Mesh mesh = new() { name = "P03 D0 synthetic surface" };
            mesh.vertices = new[] { new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f), new Vector3(1f, 1f, 0f), new Vector3(-1f, 1f, 0f) };
            mesh.normals = Enumerable.Repeat(Vector3.forward, 4).ToArray();
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject CreateSite(string forbiddenHumanName, Vector3 position)
        {
            GameObject gameObject = new(forbiddenHumanName);
            Site site = gameObject.AddComponent<Site>();
            site.Information = new SiteInformation { Name = forbiddenHumanName, DefaultPosition = position };
            site.State = new SiteState();
            gameObject.transform.localPosition = position;
            return gameObject;
        }

        private static void RecordPair(string root, IReadOnlyDictionary<string, BaselineEntry> baseline, ISet<string> recordedNames, ICollection<GoldenEntry> entries, string name, byte[] source, byte[] candidate)
        {
            string sourceHash = Sha256(source);
            string candidateHash = Sha256(candidate);
            Assert.That(baseline.TryGetValue(name, out BaselineEntry expected), Is.True, $"Missing approved P00 baseline for {name}.");
            Assert.That(recordedNames.Add(name), Is.True, $"Duplicate P00 reconstruction for {name}.");
            Assert.That(source.Length, Is.EqualTo(expected.bytes), $"Desktop byte count diverged from approved P00 baseline for {name}.");
            Assert.That(sourceHash, Is.EqualTo(expected.sha256), $"Desktop output diverged from approved P00 baseline for {name}.");
            Assert.That(candidateHash, Is.EqualTo(expected.sha256), $"RenderModel output diverged from approved P00 baseline for {name}.");
            Assert.That(candidate, Is.EqualTo(source), name);
            Assert.That(candidateHash, Is.EqualTo(sourceHash), name);
            File.WriteAllBytes(Path.Combine(root, name + "-desktop.bin"), source);
            File.WriteAllBytes(Path.Combine(root, name + "-render-model.bin"), candidate);
            entries.Add(new GoldenEntry(name, source.Length, sourceHash, candidateHash));
        }

        private static void RecordPngPair(string root, IReadOnlyDictionary<string, BaselineEntry> baseline, ISet<string> recordedNames, ICollection<GoldenEntry> entries, string name, Color32[] desktopPixels, Color32[] renderModelPixels, int width, int height)
        {
            byte[] reference = EncodePng(desktopPixels, width, height);
            byte[] candidate = EncodePng(renderModelPixels, width, height);
            RecordPair(root, baseline, recordedNames, entries, name, reference, candidate);
            File.WriteAllBytes(Path.Combine(root, name + "-desktop.png"), reference);
            File.WriteAllBytes(Path.Combine(root, name + "-render-model.png"), candidate);
        }

        private static Color32[] ToColor32(RenderBuffer<Rgba32> values)
        {
            Color32[] result = new Color32[values.Count];
            for (int index = 0; index < values.Count; index++)
                result[index] = new Color32(values[index].R, values[index].G, values[index].B, values[index].A);
            return result;
        }

        private static byte[] EncodePng(Color32[] pixels, int width, int height)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false, true);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                return texture.EncodeToPNG();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static byte[] SerializeMesh(Mesh mesh)
        {
            return Write(writer =>
            {
                Write(writer, mesh.vertices);
                Write(writer, mesh.normals);
                Write(writer, mesh.triangles);
                Write(writer, mesh.uv);
            });
        }

        private static byte[] SerializeSurfaceAsset(SurfaceAsset asset)
        {
            return Write(writer =>
            {
                Write(writer, asset.Positions);
                Write(writer, asset.Normals);
                Write(writer, asset.Indices);
                Write(writer, asset.StaticUvs);
            });
        }

        private static byte[] SerializeCutGeometry(CutGeometryAsset asset)
        {
            return Write(writer =>
            {
                Write(writer, asset.Positions);
                Write(writer, asset.Normals);
                Write(writer, asset.Indices);
                Write(writer, asset.Uvs);
            });
        }

        private static byte[] SerializeSites(IReadOnlyList<ContractId> ids, IReadOnlyList<Vector3> positions)
        {
            return Write(writer =>
            {
                writer.Write(ids.Count);
                for (int index = 0; index < ids.Count; index++)
                {
                    writer.Write(ids[index].High);
                    writer.Write(ids[index].Low);
                    Write(writer, positions[index]);
                }
            });
        }

        private static byte[] SerializeSiteAsset(SiteAsset asset)
        {
            return Write(writer =>
            {
                writer.Write(asset.SiteIds.Count);
                for (int index = 0; index < asset.SiteIds.Count; index++)
                {
                    writer.Write(asset.SiteIds[index].High);
                    writer.Write(asset.SiteIds[index].Low);
                    Write(writer, asset.Positions[index]);
                }
            });
        }

        private static byte[] SerializeColors(Color32[] values) =>
            Write(writer =>
            {
                foreach (Color32 value in values)
                {
                    writer.Write(value.r);
                    writer.Write(value.g);
                    writer.Write(value.b);
                    writer.Write(value.a);
                }
            });

        private static byte[] SerializeColors(RenderBuffer<Rgba32> values) =>
            Write(writer =>
            {
                for (int index = 0; index < values.Count; index++)
                {
                    writer.Write(values[index].R);
                    writer.Write(values[index].G);
                    writer.Write(values[index].B);
                    writer.Write(values[index].A);
                }
            });

        private static byte[] SerializeVector2(Vector2[] values) =>
            Write(writer =>
            {
                foreach (Vector2 value in values)
                {
                    writer.Write(value.x);
                    writer.Write(value.y);
                }
            });

        private static byte[] SerializeFloat2(Float2[] values) =>
            Write(writer =>
            {
                foreach (Float2 value in values)
                {
                    writer.Write(value.X);
                    writer.Write(value.Y);
                }
            });

        private static byte[] SerializeFloats(float[] values) =>
            Write(writer =>
            {
                foreach (float value in values) writer.Write(value);
            });

        private static byte[] Write(Action<BinaryWriter> action)
        {
            using MemoryStream stream = new();
            using (BinaryWriter writer = new(stream, Encoding.UTF8, true)) action(writer);
            return stream.ToArray();
        }

        private static void Write(BinaryWriter writer, Vector3 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        private static void Write(BinaryWriter writer, Float3 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }

        private static void Write(BinaryWriter writer, Vector3[] values)
        {
            writer.Write(values.Length);
            foreach (Vector3 value in values) Write(writer, value);
        }

        private static void Write(BinaryWriter writer, Vector2[] values)
        {
            writer.Write(values.Length);
            foreach (Vector2 value in values)
            {
                writer.Write(value.x);
                writer.Write(value.y);
            }
        }

        private static void Write(BinaryWriter writer, int[] values)
        {
            writer.Write(values.Length);
            foreach (int value in values) writer.Write(value);
        }

        private static void Write(BinaryWriter writer, RenderBuffer<Float3> values)
        {
            writer.Write(values.Count);
            for (int index = 0; index < values.Count; index++) Write(writer, values[index]);
        }

        private static void Write(BinaryWriter writer, RenderBuffer<Float2> values)
        {
            writer.Write(values.Count);
            for (int index = 0; index < values.Count; index++)
            {
                writer.Write(values[index].X);
                writer.Write(values[index].Y);
            }
        }

        private static void Write(BinaryWriter writer, RenderBuffer<uint> values)
        {
            writer.Write(values.Count);
            for (int index = 0; index < values.Count; index++) writer.Write(checked((int)values[index]));
        }

        private static AssetHash Hash(byte[] bytes) => AssetHash.FromBytes(Sha256Bytes(bytes));
        private static ContractId Id(ulong value) => new(value, value + 1);
        private static string Sha256(byte[] bytes) => string.Concat(Sha256Bytes(bytes).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));

        private static byte[] Sha256Bytes(byte[] bytes)
        {
            using SHA256 sha = SHA256.Create();
            return sha.ComputeHash(bytes);
        }

        private static IReadOnlyDictionary<string, BaselineEntry> LoadBaseline()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "dev", "xr", "fixtures", "P00", "synthetic-render-goldens.json"));
            Assert.That(File.Exists(path), Is.True, $"Approved P00 baseline is missing: {path}");
            BaselineManifest manifest = JsonUtility.FromJson<BaselineManifest>(File.ReadAllText(path));
            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.schema, Is.EqualTo("P00-synthetic-render-v1"));
            Assert.That(manifest.entries, Is.Not.Null.And.Not.Empty);
            return manifest.entries.ToDictionary(entry => entry.name, StringComparer.Ordinal);
        }

        private static void AssertNoSentinelInArtifacts(string root, params string[] sentinels)
        {
            foreach (string path in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                byte[] content = File.ReadAllBytes(path);
                foreach (string sentinel in sentinels)
                    Assert.That(Contains(content, Encoding.UTF8.GetBytes(sentinel)), Is.False, $"D6 sentinel leaked into {path}.");
            }
        }

        private static bool Contains(byte[] content, byte[] pattern)
        {
            if (pattern.Length == 0 || pattern.Length > content.Length)
                return false;
            for (int offset = 0; offset <= content.Length - pattern.Length; offset++)
            {
                int index = 0;
                while (index < pattern.Length && content[offset + index] == pattern[index]) index++;
                if (index == pattern.Length)
                    return true;
            }

            return false;
        }

        private static string BuildManifest(IEnumerable<GoldenEntry> entries)
        {
            StringBuilder builder = new();
            builder.AppendLine("{");
            builder.AppendLine("  \"schema\": \"P00-P03-synthetic-v1\",");
            builder.AppendLine("  \"datasets\": [\"D0\", \"D5\", \"D6\"],");
            builder.AppendLine($"  \"unity\": \"{Application.unityVersion}\",");
            builder.AppendLine("  \"policy\": \"synthetic-only-automatic\",");
            builder.AppendLine("  \"floatMaxAbsoluteError\": 0.000001,");
            builder.AppendLine("  \"integerMaskIndexRgbaTolerance\": 0,");
            builder.AppendLine("  \"entries\": [");
            GoldenEntry[] values = entries.ToArray();
            for (int index = 0; index < values.Length; index++)
            {
                GoldenEntry value = values[index];
                builder.Append($"    {{ \"name\": \"{value.Name}\", \"bytes\": {value.ByteCount}, \"desktopSha256\": \"{value.DesktopHash}\", \"renderModelSha256\": \"{value.RenderModelHash}\", \"equal\": true }}");
                builder.AppendLine(index + 1 == values.Length ? string.Empty : ",");
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private readonly struct GoldenEntry
        {
            public GoldenEntry(string name, int byteCount, string desktopHash, string renderModelHash)
            {
                Name = name;
                ByteCount = byteCount;
                DesktopHash = desktopHash;
                RenderModelHash = renderModelHash;
            }

            public string Name { get; }
            public int ByteCount { get; }
            public string DesktopHash { get; }
            public string RenderModelHash { get; }
        }

        [Serializable]
        private sealed class BaselineManifest
        {
            public string schema;
            public BaselineEntry[] entries;
        }

        [Serializable]
        private sealed class BaselineEntry
        {
            public string name;
            public int bytes;
            public string sha256;
        }
    }
}
