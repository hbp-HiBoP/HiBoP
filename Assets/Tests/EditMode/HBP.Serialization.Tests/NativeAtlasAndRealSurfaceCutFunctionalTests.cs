using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HBP.Core.DLL;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Serialization
{
    public class NativeAtlasAndRealSurfaceCutFunctionalTests
    {
        [Test]
        [Category("NativeMigration")]
        [Category("MigrationFunctional")]
        public void MarsAtlas_AllKnownMetadataAndSpatialBoundariesMatchSourceFiles()
        {
            NativeParityAssert.RequireHbpCore();
            string atlasDirectory = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Data", "Atlases", "MarsAtlas");
            string indexPath = Path.Combine(atlasDirectory, "mars_atlas_index.csv");
            string brodmannPath = Path.Combine(atlasDirectory, "brodmann_areas.txt");
            string niftiPath = Path.Combine(atlasDirectory, "colin27_MNI_MarsAtlas.nii");
            string[] brodmannNames = File.ReadAllLines(brodmannPath);
            MarsRecord[] records = File.ReadAllLines(indexPath)
                .Skip(1)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(ParseMarsRecord)
                .ToArray();

            using MarsAtlas atlas = NativeParityAssert.WithBackend(
                NativeBackend.HbpCore,
                () =>
                {
                    MarsAtlas value = new();
                    Assert.That(value.Load(indexPath, brodmannPath, niftiPath), Is.True);
                    return value;
                });

            Assert.That(atlas.Labels(), Is.EquivalentTo(records.Select(record => record.Label)));
            Assert.That(atlas.AreaNames, Is.EquivalentTo(records.Where(record => record.Label != 0).Select(record => record.FullName.Trim()).Distinct()));
            foreach (MarsRecord record in records)
            {
                string expectedBrodmann = string.Join(
                    "/",
                    record.BrodmannIds.Select(id => id >= 0 && id < brodmannNames.Length ? brodmannNames[id] : id.ToString(CultureInfo.InvariantCulture)));
                Assert.That(atlas.Label($"{record.Hemisphere}_{record.Name}"), Is.EqualTo(record.Label), record.Name);
                Assert.That(atlas.Hemisphere(record.Label), Is.EqualTo(record.Hemisphere), record.Label.ToString());
                Assert.That(atlas.Lobe(record.Label), Is.EqualTo(record.Lobe), record.Label.ToString());
                Assert.That(atlas.NameFS(record.Label), Is.EqualTo(record.NameFS), record.Label.ToString());
                Assert.That(atlas.Name(record.Label), Is.EqualTo(record.Name), record.Label.ToString());
                Assert.That(atlas.FullName(record.Label), Is.EqualTo($"{record.Hemisphere} {record.FullName}"), record.Label.ToString());
                Assert.That(atlas.GetAreaName(record.Label), Is.EqualTo($"{record.Hemisphere} {record.FullName}"), record.Label.ToString());
                Assert.That(atlas.BrodmannArea(record.Label), Is.EqualTo(expectedBrodmann), record.Label.ToString());
                Assert.That(
                    atlas.GetInformation(record.Label),
                    Is.EqualTo(new[] { $"{record.Hemisphere}_{record.Name}", record.Hemisphere, record.Lobe, record.NameFS, $"{record.Hemisphere} {record.FullName}" }),
                    record.Label.ToString());

                Color actualColor = atlas.ConvertIndicesToColors(new[] { record.Label }, selectedArea: -1)[0];
                AssertColor(actualColor, record.Color, $"Mars label {record.Label}");
            }

            Assert.That(atlas.Label("not-a-mars-area"), Is.EqualTo(-1));
            Assert.That(atlas.Hemisphere(-1), Is.EqualTo("not found"));
            Assert.That(atlas.Lobe(-1), Is.EqualTo("not found"));
            Assert.That(atlas.Name(-1), Is.EqualTo("not found"));
            AssertColor(atlas.ConvertIndicesToColors(new[] { 999999 }, selectedArea: -1)[0], Color.clear, "unknown Mars label");

            int spatialLabel = records.First().Label;
            Vector3[] coordinates = atlas.GetAreaCoordinates(spatialLabel);
            Assert.That(coordinates, Is.Not.Empty, "real atlas must expose coordinates for its first label");
            Assert.That(atlas.GetClosestAreaIndex(coordinates.First(), 0), Is.EqualTo(spatialLabel), "first boundary voxel");
            Assert.That(atlas.GetClosestAreaIndex(coordinates.Last(), 0), Is.EqualTo(spatialLabel), "last boundary voxel");
            Assert.That(atlas.GetClosestAreaIndex(new Vector3(100000, -100000, 100000), 0), Is.EqualTo(-1), "outside atlas");
        }

        [Test]
        [Category("NativeMigration")]
        [Category("MigrationFunctional")]
        public void JuBrainAtlas_AllKnownNamesColorsAndSpatialBoundariesMatchJson()
        {
            NativeParityAssert.RequireHbpCore();
            string atlasDirectory = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Data", "Atlases", "JuBrain");
            string jsonPath = Path.Combine(atlasDirectory, "jubrain_labels_3.1.json");
            JuBrainRecord[] records = ParseJuBrainRecords(File.ReadAllText(jsonPath));

            using JuBrainAtlas atlas = NativeParityAssert.WithBackend(
                NativeBackend.HbpCore,
                () =>
                {
                    JuBrainAtlas value = new();
                    value.Load();
                    Assert.That(value.Loaded, Is.True);
                    return value;
                });

            Assert.That(atlas.AreaNames, Is.EquivalentTo(records.Select(record => record.Name).Distinct()));
            foreach (JuBrainRecord record in records)
            {
                Assert.That(atlas.GetAreaName(record.Label), Is.EqualTo(record.Name), record.Label.ToString());
                Assert.That(atlas.GetInformation(record.Label), Is.EqualTo(new[] { record.Name }), record.Label.ToString());
                AssertColor(atlas.ConvertIndicesToColors(new[] { record.Label }, selectedArea: -1)[0], record.Color, $"JuBrain label {record.Label}");
                Color expectedHighlighted = new(
                    Mathf.Min(1, record.Color.r + 30.0f / 255.0f),
                    Mathf.Min(1, record.Color.g + 30.0f / 255.0f),
                    Mathf.Min(1, record.Color.b + 30.0f / 255.0f),
                    1);
                AssertColor(atlas.ConvertIndicesToColors(new[] { record.Label }, record.Label)[0], expectedHighlighted, $"highlighted JuBrain label {record.Label}");
            }

            Assert.That(atlas.GetAreaName(999999), Is.Empty);
            Assert.That(atlas.GetInformation(999999), Is.EqualTo(new[] { string.Empty }));
            AssertColor(atlas.ConvertIndicesToColors(new[] { 999999 }, selectedArea: -1)[0], Color.clear, "unknown JuBrain label");

            int spatialLabel = records.First().Label;
            Vector3[] coordinates = atlas.GetAreaCoordinates(spatialLabel);
            Assert.That(coordinates, Is.Not.Empty, "real atlas must expose coordinates for its first label");
            Assert.That(atlas.GetClosestAreaIndex(coordinates.First(), 0), Is.EqualTo(spatialLabel), "first boundary voxel");
            Assert.That(atlas.GetClosestAreaIndex(coordinates.Last(), 0), Is.EqualTo(spatialLabel), "last boundary voxel");
            Assert.That(atlas.GetClosestAreaIndex(new Vector3(100000, -100000, 100000), 0), Is.EqualTo(0), "outside JuBrain volume");
        }

        [Test]
        [Category("NativeMigration")]
        [Category("MigrationFunctional")]
        public void RealBrainSurfaceCut_PreservesTopologyPositionsNormalsIndicesAndColors()
        {
            NativeParityAssert.RequireHbpCore();
            string path = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Data", "Meshes", "MNI_single_hight_Lwhite.obj");
            Surface[] outputs = NativeParityAssert.WithBackend(
                NativeBackend.HbpCore,
                () =>
                {
                    using Surface source = new();
                    Assert.That(source.LoadOBJFile(path), Is.True);
                    using Surface simplified = source.Simplify(numberOfTriangles: 4000, agressiveness: 7);
                    Mesh simplifiedMesh = new() { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                    try
                    {
                        simplified.UpdateMeshFromDLL(simplifiedMesh);
                        if (simplifiedMesh.normals.Length != simplifiedMesh.vertexCount)
                        {
                            simplifiedMesh.RecalculateNormals();
                        }
                        Color[] colors = simplifiedMesh.colors.Length == simplifiedMesh.vertexCount
                            ? simplifiedMesh.colors
                            : Enumerable.Repeat(Color.white, simplifiedMesh.vertexCount).ToArray();
                        simplified.SetBuffers(
                            simplifiedMesh.vertices,
                            simplifiedMesh.triangles,
                            simplifiedMesh.normals,
                            simplifiedMesh.uv.Length == simplifiedMesh.vertexCount ? simplifiedMesh.uv : null,
                            colors);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(simplifiedMesh);
                    }

                    using BBox bbox = simplified.BoundingBox;
                    Vector3 center = (bbox.Min + bbox.Max) * 0.5f;
                    using HBP.Core.Object3D.Cut cut = new(center, Vector3.right);
                    return simplified.Cut(new[] { cut }, noHoles: false, strongCuts: true);
                });

            try
            {
                Assert.That(outputs, Has.Length.EqualTo(2), "one body and one cap");
                using BBox bodyBounds = outputs[0].BoundingBox;
                using BBox capBounds = outputs[1].BoundingBox;
                float planeX = capBounds.Min.x;
                Assert.That(capBounds.Max.x, Is.EqualTo(planeX).Within(0.001f), "cap must lie on its plane");
                Assert.That(bodyBounds.Max.x, Is.LessThanOrEqualTo(planeX + 0.001f), "body must remain on the retained side");
                AssertFullSurfaceBuffers(outputs[0], requireColors: true, "real brain cut body");
                AssertFullSurfaceBuffers(outputs[1], requireColors: false, "real brain cut cap");
            }
            finally
            {
                foreach (Surface output in outputs)
                {
                    output?.Dispose();
                }
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("MigrationFunctional")]
        public void CutGeometryGenerator_MniMergedBoundsEndpointsRemainValid()
        {
            NativeParityAssert.RequireHbpCore();
            NativeParityAssert.WithBackend(
                NativeBackend.HbpCore,
                () =>
                {
                    string meshDirectory = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Data", "Meshes");
                    using Surface left = new();
                    using Surface right = new();
                    using Volume volume = new();
                    Assert.That(left.LoadGIIFile(Path.Combine(meshDirectory, "MNI_Lhemi.gii"), Path.Combine(meshDirectory, "MNI.trm")), Is.True);
                    Assert.That(right.LoadGIIFile(Path.Combine(meshDirectory, "MNI_Rhemi.gii"), Path.Combine(meshDirectory, "MNI.trm")), Is.True);
                    Assert.That(volume.LoadNIFTIFile(Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Data", "IRM", "MNI.nii")), Is.True);
                    left.FlipTriangles();
                    right.FlipTriangles();
                    left.Append(right);

                    using BBox volumeBounds = volume.BoundingBox;
                    using BBox surfaceBounds = left.BoundingBox;
                    using BBox mergedBounds = BBox.Merge(volumeBounds, surfaceBounds);
                    foreach (HBP.Core.Enums.CutOrientation orientation in new[]
                    {
                        HBP.Core.Enums.CutOrientation.Axial,
                        HBP.Core.Enums.CutOrientation.Coronal,
                        HBP.Core.Enums.CutOrientation.Sagittal
                    })
                    {
                        using HBP.Core.DLL.Plane orientationPlane = new(Vector3.zero, Vector3.right);
                        volume.SetPlaneWithOrientation(orientationPlane, orientation, false);
                        float offset = mergedBounds.SizeOffsetCutPlane(orientationPlane, 500);
                        foreach (float position in new[] { 0.0f, 1.0f })
                        {
                            using HBP.Core.Object3D.Cut cut = new(
                                mergedBounds.Center + orientationPlane.Normal.normalized * (position - 0.5f) * offset * 500,
                                orientationPlane.Normal)
                            {
                                Orientation = orientation,
                                Position = position,
                                NumberOfCuts = 500
                            };
                            using CutGeometryGenerator geometry = new();
                            Assert.DoesNotThrow(
                                () => geometry.Initialize(volume, cut, -1),
                                $"{orientation} endpoint {position}");
                            Assert.That(geometry.TextureSize.x, Is.GreaterThan(0), $"{orientation} endpoint {position} width");
                            Assert.That(geometry.TextureSize.y, Is.GreaterThan(0), $"{orientation} endpoint {position} height");
                        }
                    }
                    return true;
                });
        }

        private static void AssertFullSurfaceBuffers(Surface surface, bool requireColors, string context)
        {
            Assert.That(surface.NumberOfVertices, Is.GreaterThan(0), context);
            Assert.That(surface.NumberOfTriangles, Is.GreaterThan(0), context);
            Mesh mesh = new() { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            try
            {
                surface.UpdateMeshFromDLL(mesh);
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                Assert.That(vertices, Has.Length.EqualTo(surface.NumberOfVertices), context);
                Assert.That(triangles, Has.Length.EqualTo(surface.NumberOfTriangles * 3), context);
                Assert.That(mesh.normals, Has.Length.EqualTo(vertices.Length), context + " normals");
                if (requireColors)
                {
                    Assert.That(mesh.colors, Has.Length.EqualTo(vertices.Length), context + " colors");
                }

                int firstInvalidTriangle = -1;
                int firstDegenerateTriangle = -1;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i];
                    int b = triangles[i + 1];
                    int c = triangles[i + 2];
                    if (a < 0 || a >= vertices.Length || b < 0 || b >= vertices.Length || c < 0 || c >= vertices.Length)
                    {
                        firstInvalidTriangle = i / 3;
                        break;
                    }
                    if (Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).sqrMagnitude <= 1e-12f)
                    {
                        firstDegenerateTriangle = i / 3;
                        break;
                    }
                }
                Assert.That(firstInvalidTriangle, Is.EqualTo(-1), $"{context}: invalid indices at triangle {firstInvalidTriangle}");
                Assert.That(firstDegenerateTriangle, Is.EqualTo(-1), $"{context}: degenerate triangle {firstDegenerateTriangle}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private static MarsRecord ParseMarsRecord(string line)
        {
            string[] fields = line.Split(',');
            Assert.That(fields, Has.Length.EqualTo(8), line);
            int[] rgb = fields[7].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
            return new MarsRecord
            {
                Label = int.Parse(fields[0], CultureInfo.InvariantCulture),
                Hemisphere = fields[1],
                Lobe = fields[2],
                NameFS = fields[3],
                Name = fields[4],
                FullName = fields[5],
                BrodmannIds = fields[6].Split('/').Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray(),
                Color = new Color(rgb[0] / 255.0f, rgb[1] / 255.0f, rgb[2] / 255.0f, 1)
            };
        }

        private static JuBrainRecord[] ParseJuBrainRecords(string json)
        {
            MatchCollection matches = Regex.Matches(
                json,
                "\\\"grayvalue\\\"\\s*:\\s*\\\"(?<label>\\d+)\\\"[\\s\\S]*?\\\"color\\\"\\s*:\\s*\\\"rgb\\((?<r>\\d+),(?<g>\\d+),(?<b>\\d+)\\)\\\"[\\s\\S]*?\\\"name\\\"\\s*:\\s*\\\"(?<name>[^\\\"]*)\\\"");
            Assert.That(matches.Count, Is.GreaterThan(0), "JuBrain JSON structures");
            return matches.Cast<Match>().Select(match => new JuBrainRecord
            {
                Label = int.Parse(match.Groups["label"].Value, CultureInfo.InvariantCulture),
                Name = Regex.Unescape(match.Groups["name"].Value),
                Color = new Color(
                    int.Parse(match.Groups["r"].Value, CultureInfo.InvariantCulture) / 255.0f,
                    int.Parse(match.Groups["g"].Value, CultureInfo.InvariantCulture) / 255.0f,
                    int.Parse(match.Groups["b"].Value, CultureInfo.InvariantCulture) / 255.0f,
                    1)
            }).ToArray();
        }

        private static void AssertColor(Color actual, Color expected, string context)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.00001f), context + " r");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.00001f), context + " g");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.00001f), context + " b");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.00001f), context + " a");
        }

        private sealed class MarsRecord
        {
            public int Label;
            public string Hemisphere;
            public string Lobe;
            public string NameFS;
            public string Name;
            public string FullName;
            public int[] BrodmannIds;
            public Color Color;
        }

        private sealed class JuBrainRecord
        {
            public int Label;
            public string Name;
            public Color Color;
        }
    }
}
