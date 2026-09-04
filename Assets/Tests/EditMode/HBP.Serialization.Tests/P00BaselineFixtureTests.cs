using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using HBP.Tests.Serialization.Helpers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class P00BaselineFixtureTests
    {
        [Test]
        [Category("XR.P00")]
        public void Manifest_ContainsOnlyApprovedPortableDatasets()
        {
            JObject manifest = LoadManifest();
            Assert.That(manifest["decisionStatus"].Value<string>(), Is.EqualTo("GO"));
            Assert.That(manifest["realExternalDataAllowed"].Value<bool>(), Is.False);

            JArray datasets = (JArray)manifest["datasets"];
            Assert.That(datasets.Select(dataset => dataset["id"].Value<string>()), Is.EqualTo(new[] { "D0", "D1", "D2", "D3", "D4", "D5", "D6" }));
            foreach (JObject dataset in datasets.Children<JObject>())
            {
                Assert.That(dataset["testsAllowed"].Value<bool>(), Is.True, dataset["id"].Value<string>());
                Assert.That(dataset["redactedArtifactsAllowed"].Value<bool>(), Is.True, dataset["id"].Value<string>());
                Assert.That(dataset["versioningAllowed"].Value<bool>(), Is.True, dataset["id"].Value<string>());
                if (dataset["fixture"] != null)
                {
                    string fixture = dataset["fixture"].Value<string>();
                    Assert.That(Path.IsPathRooted(fixture), Is.False, dataset["id"].Value<string>());
                    Assert.That(File.Exists(ProjectPath(fixture)), Is.True, fixture);
                }
            }
        }

        [Test]
        [Category("XR.P00")]
        public void MniAssets_MatchApprovedD1HashesAndCounts()
        {
            JObject d1 = LoadManifest()["datasets"].Children<JObject>().Single(dataset => dataset["id"].Value<string>() == "D1");
            Assert.That(d1["copyAllowed"].Value<bool>(), Is.False);
            foreach (JObject asset in d1["assets"].Children<JObject>())
            {
                string path = ProjectPath(asset["path"].Value<string>());
                Assert.That(File.Exists(path), Is.True, path);
                Assert.That(new FileInfo(path).Length, Is.EqualTo(asset["bytes"].Value<long>()), path);
                Assert.That(Sha256(path), Is.EqualTo(asset["sha256"].Value<string>()), path);

                if (Path.GetExtension(path).Equals(".obj", StringComparison.OrdinalIgnoreCase))
                {
                    (int vertices, int faces) = CountObjGeometry(path);
                    Assert.That(vertices, Is.EqualTo(asset["vertices"].Value<int>()), path);
                    Assert.That(faces, Is.EqualTo(asset["faces"].Value<int>()), path);
                }
            }
        }

        [Test]
        [Category("XR.P00")]
        public void D0_HasExactSurfaceSiteAndCutBuffers()
        {
            JObject d0 = LoadFixture("D0");
            JArray vertices = (JArray)d0["vertices"];
            JArray triangles = (JArray)d0["triangles"];
            JArray sites = (JArray)d0["sites"];
            Assert.That(vertices.Count, Is.EqualTo(4));
            Assert.That(triangles.Count, Is.EqualTo(4));
            Assert.That(d0["surfaceValues"].Count(), Is.EqualTo(vertices.Count));
            Assert.That(sites.Select(site => site["id"].Value<string>()).Distinct().Count(), Is.EqualTo(sites.Count));
            Assert.That(sites.Any(site => site["id"].Value<string>() == d0["expectedPickedSiteId"].Value<string>()), Is.True);

            foreach (int index in triangles.SelectMany(triangle => triangle.Values<int>()))
            {
                Assert.That(index, Is.InRange(0, vertices.Count - 1));
            }

            JObject cut = (JObject)d0["cut"];
            Assert.That(cut["scalarValues"].Count(), Is.EqualTo(cut["width"].Value<int>() * cut["height"].Value<int>()));
            Assert.That(cut["scalarValues"].Values<int>(), Is.EqualTo(Enumerable.Range(0, 16)));
        }

        [Test]
        [Category("XR.P00")]
        public void D2D3D4_DescriptorsCoverRepresentativeAndStressCases()
        {
            JObject d2 = LoadFixture("D2");
            Assert.That(d2["siteCount"].Value<int>(), Is.EqualTo(150));
            Assert.That(d2["columnCount"].Value<int>(), Is.EqualTo(3));
            Assert.That(d2["cutOrientations"].Values<string>(), Is.EquivalentTo(new[] { "Axial", "Coronal", "Sagittal" }));

            JObject d3 = LoadFixture("D3");
            int generatedSiteCount = d3["groupCount"].Value<int>() * d3["sitesPerGroup"].Value<int>();
            Assert.That(generatedSiteCount, Is.EqualTo(37500));
            Assert.That(d3["expectedSiteCount"].Value<int>(), Is.EqualTo(generatedSiteCount));
            Assert.That(d3["columnCount"].Value<int>(), Is.EqualTo(8));

            JObject d4 = LoadFixture("D4");
            long voxelCount = Product(d4["volumeDimensions"].Values<int>());
            long projectionPointCount = Product(d4["projectionGridDimensions"].Values<int>());
            long logicalBytes = voxelCount * d4["bytesPerValue"].Value<int>() * (d4["overlayCount"].Value<int>() + 1L);
            Assert.That(voxelCount, Is.EqualTo(d4["expectedVolumeVoxelCount"].Value<long>()));
            Assert.That(projectionPointCount, Is.EqualTo(d4["expectedProjectionPointCount"].Value<long>()));
            Assert.That(logicalBytes, Is.EqualTo(d4["expectedLogicalBytesWithBaseVolume"].Value<long>()));
        }

        [Test]
        [Category("XR.P00")]
        public void GoldenBuffers_MatchDeterministicGeneratorAndD5Expectations()
        {
            JObject expected = JObject.Parse(File.ReadAllText(P00BaselineGoldenCli.DefaultOutputPath));
            JObject generated = P00BaselineGoldenCli.Generate();
            Assert.That(JToken.DeepEquals(generated, expected), Is.True);

            JObject d5 = LoadFixture("D5");
            JArray generatedSamples = (JArray)generated["d5Temporal"]["samples"];
            JArray definitions = (JArray)d5["samples"];
            Assert.That(generatedSamples.Count, Is.EqualTo(definitions.Count));
            for (int index = 0; index < definitions.Count; ++index)
            {
                Assert.That(generatedSamples[index]["values"].Values<float>(), Is.EqualTo(definitions[index]["expected"].Values<float>()).Within(0.000001f));
            }
        }

        [Test]
        [Category("XR.P00")]
        public void D6_SentinelsAreRemovedFromRedactedArtifact()
        {
            JObject d6 = LoadFixture("D6");
            string[] sentinels = d6["sentinels"].Values<string>().ToArray();
            string artifact = string.Join(";", sentinels.Select((sentinel, index) => $"field-{index}={sentinel}"));
            foreach (string sentinel in sentinels)
            {
                artifact = artifact.Replace(sentinel, d6["redactedToken"].Value<string>());
            }

            foreach (string sentinel in sentinels)
            {
                Assert.That(artifact, Does.Not.Contain(sentinel));
            }
        }

        private static JObject LoadManifest()
        {
            return JObject.Parse(File.ReadAllText(TestPathUtility.FixturePath("XR", "Baselines", "manifest.json")));
        }

        private static JObject LoadFixture(string id)
        {
            return JObject.Parse(File.ReadAllText(TestPathUtility.FixturePath("XR", "Baselines", id, "fixture.json")));
        }

        private static string ProjectPath(string relativePath)
        {
            return Path.Combine(TestPathUtility.ProjectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string Sha256(string path)
        {
            using SHA256 sha256 = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return string.Concat(sha256.ComputeHash(stream).Select(value => value.ToString("x2")));
        }

        private static (int vertices, int faces) CountObjGeometry(string path)
        {
            int vertices = 0;
            int faces = 0;
            foreach (string line in File.ReadLines(path))
            {
                if (line.StartsWith("v ", StringComparison.Ordinal)) ++vertices;
                else if (line.StartsWith("f ", StringComparison.Ordinal)) ++faces;
            }

            return (vertices, faces);
        }

        private static long Product(IEnumerable<int> values)
        {
            return values.Aggregate(1L, (product, value) => product * value);
        }
    }
}
