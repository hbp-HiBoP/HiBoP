using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text.RegularExpressions;
using HBP.Core.DLL;
using HBP.Core.DLL.HbpCore;
using HBP.Core.Enums;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using HbpPlane = HBP.Core.DLL.Plane;
using HbpSegment3 = HBP.Core.DLL.Segment3;
using LegacyBBox = HBP.Tests.Serialization.LegacyNative.BBox;
using LegacySurface = HBP.Tests.Serialization.LegacyNative.Surface;
using LegacyVolume = HBP.Tests.Serialization.LegacyNative.Volume;

namespace HBP.Tests.Serialization
{
    public class NativeMigrationBaselineTests
    {
        private static readonly Regex DllImportRegex = new("\\[DllImport\\((?:\"(?<dll>[^\"]+)\"|HbpCoreLibrary\\.Name)\\s*,\\s*EntryPoint\\s*=\\s*\"(?<entry>[^\"]+)\"", RegexOptions.Compiled);

        [Test]
        [Category("NativeMigration")]
        public void RuntimeExposesNoBackendSelectorOrLegacyCompatibilityProperties()
        {
            Assembly runtime = typeof(Volume).Assembly;
            Assert.That(runtime.GetType("HBP.Core.DLL.NativeBackend"), Is.Null);
            Assert.That(runtime.GetType("HBP.Core.DLL.NativeBackendOptions"), Is.Null);
            Assert.That(runtime.GetType("HBP.Core.DLL.NativeDll"), Is.Null);
            Assert.That(typeof(Volume).GetProperty("UsesHbpCore"), Is.Null);
            Assert.That(typeof(Surface).GetProperty("UsesHbpCore"), Is.Null);
            Assert.That(typeof(ActivityGenerator).GetProperty("UsesHbpCore"), Is.Null);
        }

        [Test]
        [Category("NativeMigration")]
        public void RemovedStep4LegacyApis_AreAbsentFromManagedRuntime()
        {
            const BindingFlags publicInstance = BindingFlags.Public | BindingFlags.Instance;

            Assert.That(typeof(Volume).GetMethod("GetCubeBoundingBox", publicInstance), Is.Null);
            Assert.That(typeof(Surface).GetMethod("GetCubeBoundingBox", publicInstance), Is.Null);
            Assert.That(typeof(MarsAtlas).GetMethod("GenerateAtlasRawSiteList", publicInstance), Is.Null);
            Assert.That(typeof(RawSiteList).GetMethod("SaveToObj", publicInstance), Is.Null);
            Assert.That(typeof(Transformation3).GetMethod("Inverse", publicInstance), Is.Null);
        }

        [Test]
        [Category("NativeMigration")]
        [Category("HbpCoreOnly")]
        public void HbpCoreOnlyRun_DoesNotRequireLegacyDllOnDisk()
        {
            if (Environment.GetEnvironmentVariable("HBP_EXPECT_NO_LEGACY_DLL") == "1")
            {
                string legacyDllPath = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Plugins", "x86_64", "Windows", "hbp_export.dll");
                Assert.That(File.Exists(legacyDllPath), Is.False, "The no-legacy validation must physically remove hbp_export.dll from the project.");
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void CurrentDllImportInventory_ContainsNoLegacyRuntimeImports()
        {
            List<DllImportSignature> imports = ReadCurrentDllImports();

            Assert.That(imports, Has.Count.EqualTo(246));
            Assert.That(imports.Count(imported => imported.Dll == "hbp_export"), Is.Zero);
            Assert.That(imports.Count(imported => imported.Dll == "EEGFormat"), Is.EqualTo(37));
            Assert.That(imports.Count(imported => imported.Dll == "hbp_math"), Is.EqualTo(17));
            string[] hbpCoreImportFiles = imports.Where(imported => imported.Dll == "hbp_core").Select(imported => imported.RelativeFile).Distinct().ToArray();
            Assert.That(hbpCoreImportFiles, Is.EquivalentTo(new[] { "BBox.cs", "BrainAtlas.cs", "Electrodes.cs", "Generators/ActivityGenerator.cs", "Generators/CutGenerator.cs", "Generators/CutGeometryGenerator.cs", "Generators/DensityGenerator.cs", "Generators/FMRIGenerator.cs", "Generators/GeneratorSurface.cs", "Generators/IEEGGenerator.cs", "Generators/MEGGenerator.cs", "Generators/SurfaceGenerator.cs", "HbpCore/HbpCoreRuntime.cs", "JuBrainAtlas.cs", "MarsAtlas.cs", "NIFTI.cs", "Plane.cs", "Segment3.cs", "Surface.cs", "SurfaceList.cs", "Transformation3.cs", "Volume.cs" }));
            Assert.That(imports.Count(imported => imported.Dll == "hbp_core"), Is.EqualTo(192));
            Assert.That(imports.Where(imported => imported.RelativeFile == "VideoStream.cs"), Is.Empty);
            Assert.That(imports.Any(imported => imported.Entry.Contains("PatientElectrodesList")), Is.False);
            Assert.That(imports.Any(imported => imported.RelativeFile == "ROI.cs"), Is.False);
            Assert.That(imports.Any(imported => imported.Entry.EndsWith("_ROI", StringComparison.Ordinal)), Is.False);
            Assert.That(imports.Any(imported => imported.RelativeFile == "Texture.cs"), Is.False);
            Assert.That(imports.Any(imported => imported.Entry.EndsWith("_Texture", StringComparison.Ordinal)), Is.False);
        }

        [Test]
        [Category("NativeMigration")]
        public void RuntimeAssemblyReflectionContainsNoLegacyDllImports()
        {
            MethodInfo[] offenders = typeof(Volume).Assembly.GetTypes().Where(type => type.Namespace != null && type.Namespace.StartsWith("HBP.Core", StringComparison.Ordinal)).SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)).Where(method => method.GetCustomAttribute<DllImportAttribute>()?.Value == "hbp_export").ToArray();
            Assert.That(offenders, Is.Empty);
        }

        [Test]
        [Category("NativeMigration")]
        public void LegacyImportsAreConfinedToAssetsTests()
        {
            string scriptsFolder = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Scripts");
            string[] unexpectedFiles = Directory.GetFiles(scriptsFolder, "*.cs", SearchOption.AllDirectories).Where(file => Regex.IsMatch(File.ReadAllText(file), "DllImport\\s*\\(\\s*\"hbp_export\"")).Select(file => file.Substring(scriptsFolder.Length).TrimStart('\\', '/').Replace('\\', '/')).ToArray();

            Assert.That(unexpectedFiles, Is.Empty, "hbp_export imports are allowed only under Assets/Tests.");

            string testsFolder = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Tests");
            Assert.That(Directory.GetFiles(testsFolder, "*.cs", SearchOption.AllDirectories).Any(file => File.ReadAllText(file).Contains("hbp_export")), Is.True, "The Editor-only oracle adapters must remain available to parity tests.");
        }

        [Test]
        [Category("NativeMigration")]
        public void ProductionSourcesContainNoLegacyBackendSymbols()
        {
            string scriptsFolder = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Scripts");
            string[] forbiddenSymbols =
            {
                "NativeBackend",
                "NativeBackendOptions",
                "NativeDll",
                "UsesHbpCore",
                "HbpExport",
                "hbp_export"
            };

            foreach (string file in Directory.GetFiles(scriptsFolder, "*.cs", SearchOption.AllDirectories))
            {
                string contents = File.ReadAllText(file);
                foreach (string forbiddenSymbol in forbiddenSymbols)
                {
                    Assert.That(contents, Does.Not.Contain(forbiddenSymbol), $"{file} still contains the legacy runtime symbol {forbiddenSymbol}.");
                }
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void HbpExportPlugins_AreEditorOnlyAndExcludedFromPlayerBuilds()
        {
            if (Environment.GetEnvironmentVariable("HBP_EXPECT_NO_LEGACY_DLL") == "1")
            {
                Assert.Ignore("The no-legacy validation temporarily removes the oracle plugin and its metadata.");
            }

            string[] metadataPaths =
            {
                Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Plugins", "x86_64", "Windows", "hbp_export.dll.meta"),
                Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Plugins", "x86_64", "Linux", "libhbp_export.so.meta"),
                Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Plugins", "x86_64", "MacOS", "hbp_export.bundle.meta")
            };

            foreach (string metadataPath in metadataPaths)
            {
                string metadata = File.ReadAllText(metadataPath);
                Assert.That(Regex.IsMatch(metadata, @"Editor(?s:.{0,120})enabled:\s*1"), Is.True, metadataPath);
                Assert.That(Regex.IsMatch(metadata, @"Standalone(?s:.{0,120})enabled:\s*1"), Is.False, metadataPath);
                Assert.That(Regex.IsMatch(metadata, @"(?m)^\s+(Linux64|OSXUniversal|Win|Win64):\s*\r?$\n\s+enabled:\s*1"), Is.False, metadataPath);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void Vec3Conversions_FlipReferenceSystemByDefault_AndAllowExplicitNativeValues()
        {
            Type vec3Type = typeof(Volume).Assembly.GetType("HBP.Core.DLL.Vec3", throwOnError: true);
            MethodInfo fromVector3 = vec3Type.GetMethod("FromVector3", BindingFlags.Public | BindingFlags.Static);
            MethodInfo toVector3 = vec3Type.GetMethod("ToVector3", BindingFlags.Public | BindingFlags.Instance);

            object converted = fromVector3.Invoke(null, new object[] { new Vector3(1, 2, 3), true });
            Assert.That(ReadVec3Field(converted, "x"), Is.EqualTo(-1).Within(0.0001f));
            Assert.That(ReadVec3Field(converted, "y"), Is.EqualTo(2).Within(0.0001f));
            Assert.That(ReadVec3Field(converted, "z"), Is.EqualTo(3).Within(0.0001f));
            AssertVector((Vector3)toVector3.Invoke(converted, new object[] { true }), new Vector3(1, 2, 3));

            object native = fromVector3.Invoke(null, new object[] { new Vector3(1, 2, 3), false });
            Assert.That(ReadVec3Field(native, "x"), Is.EqualTo(1).Within(0.0001f));
            AssertVector((Vector3)toVector3.Invoke(native, new object[] { false }), new Vector3(1, 2, 3));
            AssertVector((Vector3)toVector3.Invoke(native, new object[] { true }), new Vector3(-1, 2, 3));
        }

        [Test]
        [Category("NativeMigration")]
        public void ReferenceSystemConversion_CouplesXReflectionAndTriangleWinding()
        {
            Type conversionType = typeof(Volume).Assembly.GetType("HBP.Core.DLL.ReferenceSystemConversion", throwOnError: true);
            const BindingFlags staticMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            FieldInfo invertXField = conversionType.GetField("InvertX", staticMembers);
            FieldInfo flipsHandednessField = conversionType.GetField("FlipsHandedness", staticMembers);
            MethodInfo convertTriangleWinding = conversionType.GetMethod("ConvertTriangleWinding", staticMembers);

            Assert.That(invertXField, Is.Not.Null);
            Assert.That(flipsHandednessField, Is.Not.Null);
            Assert.That(convertTriangleWinding, Is.Not.Null);
            bool invertX = (bool)invertXField.GetRawConstantValue();
            bool flipsHandedness = (bool)flipsHandednessField.GetRawConstantValue();
            Assert.That(flipsHandedness, Is.EqualTo(invertX));

            int[] triangles = { 0, 1, 2, 3, 4, 5 };
            int[] converted = (int[])convertTriangleWinding.Invoke(null, new object[] { triangles, true });
            int[] unconverted = (int[])convertTriangleWinding.Invoke(null, new object[] { triangles, false });
            Assert.That(converted, Is.EqualTo(flipsHandedness ? new[] { 0, 2, 1, 3, 5, 4 } : triangles));
            Assert.That(unconverted, Is.EqualTo(triangles));
        }

        [Test]
        [Category("NativeMigration")]
        public void AnatomicalMeshPrefabs_UseIdentityScale()
        {
            string[] prefabPaths =
            {
                "Assets/Prefabs/3D/Objects/Brain.prefab",
                "Assets/Prefabs/3D/Objects/SimplifiedBrain.prefab",
                "Assets/Prefabs/3D/Objects/Cut.prefab"
            };

            foreach (string prefabPath in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.That(prefab, Is.Not.Null, prefabPath);
                AssertVector(prefab.transform.localScale, Vector3.one);
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void Camera3DCutMarker_DoesNotApplyManualReferenceSystemFlip()
        {
            string camera3DPath = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Scripts", "HBP", "Data", "Module3D", "Camera3D.cs");
            string contents = File.ReadAllText(camera3DPath);

            Assert.That(Regex.IsMatch(contents, @"point\.x\s*\*=\s*-1"), Is.False);
            Assert.That(Regex.IsMatch(contents, @"normal\.x\s*\*=\s*-1"), Is.False);
        }

        [Test]
        [Category("NativeMigration")]
        public void SiteInfo_ExplicitNativeVector3ExposesReflectedUnityPosition()
        {
            HBP.Core.Object3D.Implantation3D.SiteInfo site = new()
            {
                NativePosition = new Vector3(4, -5, 6)
            };

            AssertVector(site.NativePosition, new Vector3(4, -5, 6));
            AssertVector(site.UnityPosition, new Vector3(-4, -5, 6));
        }

        [Test]
        [Category("NativeMigration")]
        public void Vec3ReferenceSystemExceptions_RemainExplicitAndAllowlisted()
        {
            string dllFolder = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Scripts", "HBP", "Core", "DLL");
            Dictionary<string, int> allowedFalseConversions = new()
            {
                ["Electrodes.cs"] = 1,
                ["Volume.cs"] = 1
            };
            Dictionary<string, int> actualFalseConversions = Directory.GetFiles(dllFolder, "*.cs", SearchOption.AllDirectories).Select(file => new
            {
                RelativeFile = file.Substring(dllFolder.Length).TrimStart('\\', '/').Replace('\\', '/'),
                Count = Regex.Matches(File.ReadAllText(file), "convertReferenceSystem:\\s*false").Count
            }).Where(item => item.Count > 0).ToDictionary(item => item.RelativeFile, item => item.Count);

            Assert.That(actualFalseConversions, Is.EquivalentTo(allowedFalseConversions));

            string[] forbiddenManualFlipPatterns =
            {
                @"Vec3\.FromVector3\s*\(\s*new\s+Vector3\s*\(\s*-",
                @"new\s*\(\s*\)\s*\{\s*x\s*=\s*-"
            };
            foreach (string file in Directory.GetFiles(dllFolder, "*.cs", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file) == "HbpCoreValueTypes.cs")
                {
                    continue;
                }

                string contents = File.ReadAllText(file);
                foreach (string pattern in forbiddenManualFlipPatterns)
                {
                    Assert.That(Regex.IsMatch(contents, pattern), Is.False, $"{file} still performs a manual hbp_core Vec3 X flip matching {pattern}");
                }
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("NativeParity")]
        [LegacyParityOnly]
        public void HistoricalWrapper_LoadsThroughHbpExportWithoutHbpCoreMigration()
        {
            LegacyBBox bbox = ExecuteNativeOrIgnore(() => NativeParityAssert.WithBackend(BenchmarkBackend.HbpExport, () => new LegacyBBox()), "historical BBox wrapper");
            try
            {
                Assert.That(bbox.getHandle().Handle, Is.Not.EqualTo(IntPtr.Zero));
            }
            finally
            {
                bbox.Dispose();
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void HbpCoreRuntime_CreatesBBoxThroughHbpCore()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            BBox bbox = ExecuteNativeOrIgnore(() => new BBox(), "hbp_core BBox wrapper");
            try
            {
                Assert.That(bbox.getHandle().Handle, Is.Not.EqualTo(IntPtr.Zero));
                Assert.DoesNotThrow(() => _ = bbox.Min);
            }
            finally
            {
                bbox.Dispose();
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("HbpCoreOnly")]
        public void HbpCoreWrappers_RepeatedCreateUseDisposeCycles_ReleaseAllHandles()
        {
            const int cycleCount = 100;

            for (int cycle = 0; cycle < cycleCount; ++cycle)
            {
                Assert.That(HbpCoreRuntime.Init(), Is.EqualTo(HbpCoreStatus.Ok), $"init cycle {cycle}");
                CppDLLImportBase[] objects =
                {
                    BBox.FromMinMax(new Vector3(-4, -3, -2), new Vector3(5, 6, 7)),
                    new HbpPlane(new Vector3(1, 2, 3), Vector3.forward),
                    new HbpSegment3(new Vector3(-1, 0, 1), new Vector3(2, 4, 5)),
                    new Surface(),
                    new Volume()
                };

                try
                {
                    Assert.That(objects.All(item => item.getHandle().Handle != IntPtr.Zero), Is.True, $"live handles in cycle {cycle}");
                    Assert.That(((HbpSegment3)objects[2]).Length, Is.GreaterThan(0));
                    Assert.That(((Surface)objects[3]).NumberOfVertices, Is.Zero);
                    Assert.That(((Volume)objects[4]).IsLoaded, Is.False);
                }
                finally
                {
                    foreach (CppDLLImportBase item in objects.Reverse()) item.Dispose();
                }

                Assert.That(objects.All(item => item.getHandle().Handle == IntPtr.Zero), Is.True, $"released handles in cycle {cycle}");
                Assert.DoesNotThrow(objects[0].Dispose, $"Dispose must remain idempotent in cycle {cycle}");
                Assert.That(HbpCoreRuntime.Shutdown(), Is.EqualTo(HbpCoreStatus.Ok), $"shutdown cycle {cycle}");
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HbpCoreSmoke_LoadsVersion_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out string version, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            Assert.That(version, Is.Not.Empty);
            Assert.That(HbpCoreRuntime.Init(), Is.EqualTo(HbpCoreStatus.Ok));
            Assert.That(HbpCoreRuntime.LastError, Is.Empty);
            Assert.That(HbpCoreRuntime.Shutdown(), Is.EqualTo(HbpCoreStatus.Ok));
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void DLLDebugManager_ReceivesHbpCoreDebugMessage_WhenLibraryIsPresent()
        {
            if (!DLLDebugManager.TryAttachHbpCoreLogger(out string attachError))
            {
                Assert.Ignore($"hbp_core debug callback is not available yet: {attachError}");
            }

            const string message = "hbp_core unity callback";
            try
            {
                LogAssert.Expect(LogType.Warning, message);
                Assert.That(HbpCoreRuntime.DebugMessage(message, HbpCoreLogType.Warning), Is.EqualTo(HbpCoreStatus.Ok));
            }
            finally
            {
                DLLDebugManager.TryResetHbpCoreLogger(out _);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HbpCoreTransformation_LoadsTrmFileNatively_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            string transformPath = Path.Combine(Path.GetTempPath(), "hbp_core_transformation_from_file_test.trm");
            File.WriteAllText(transformPath, string.Join(Environment.NewLine, "10 20 30", "0 -1 0", "1 0 0", "0 0 1"));
            try
            {
                using Transformation3 transformation = Transformation3.FromFile(transformPath);
                AssertVector(transformation.ApplyPoint(new Vector3(-1, 2, 3)), new Vector3(-8, 21, 33));
            }
            finally
            {
                if (File.Exists(transformPath))
                {
                    File.Delete(transformPath);
                }
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HbpCoreBBox_ReturnsBoundsAndPlaneIntersections_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            using BBox bbox = BBox.FromMinMax(new Vector3(-1, -2, -3), new Vector3(3, 4, 5));

            AssertVector(bbox.Min, new Vector3(-1, -2, -3));
            AssertVector(bbox.Max, new Vector3(3, 4, 5));
            AssertVector(bbox.Center, new Vector3(1, 1, 1));
            Assert.That(bbox.Points, Has.Count.EqualTo(8));
            List<HbpSegment3> bboxSegments = bbox.Segments;
            Assert.That(bboxSegments, Has.Count.EqualTo(12));
            DisposeSegments(bboxSegments);

            using HbpPlane plane = new(new Vector3(0, 0, 1), new Vector3(0, 0, 2));
            Assert.That(plane.PointSide(new Vector3(0, 0, 3)), Is.EqualTo(1));
            AssertVector(plane.ProjectPoint(new Vector3(2, 3, 4)), new Vector3(2, 3, 1));
            Assert.That(plane.IntersectSegment(new Vector3(0, 0, -1), new Vector3(0, 0, 3), out Vector3 planeSegmentPoint), Is.True);
            AssertVector(planeSegmentPoint, new Vector3(0, 0, 1));
            plane.Point = new Vector3(0, 0, 2);
            AssertVector(plane.ProjectPoint(new Vector3(2, 3, 4)), new Vector3(2, 3, 2));
            plane.Point = new Vector3(0, 0, 1);

            List<Vector3> intersections = bbox.IntersectionPointsWithPlane(plane);
            Assert.That(intersections, Has.Count.EqualTo(4));
            Assert.That(intersections.All(point => Mathf.Abs(point.z - 1) < 0.0001f), Is.True);

            List<HbpSegment3> segments = bbox.IntersectionLinesWithPlane(plane);
            Assert.That(segments, Has.Count.EqualTo(4));
            Assert.That(segments.All(segment => Mathf.Abs(segment.End1.z - 1) < 0.0001f && Mathf.Abs(segment.End2.z - 1) < 0.0001f), Is.True);
            DisposeSegments(segments);

            using HbpPlane planeA = new(new Vector3(1, 0, 0), Vector3.right);
            using HbpPlane planeB = new(new Vector3(0, 1, 0), Vector3.up);
            HbpSegment3 segment = bbox.IntersectionSegmentBetweenTwoPlanes(planeA, planeB);

            Assert.That(segment, Is.Not.Null);
            AssertSameVectorSet(new[] { segment.End1, segment.End2 }, new[] { new Vector3(1, 1, -3), new Vector3(1, 1, 5) });
            Assert.That(segment.Length, Is.EqualTo(8.0f).Within(0.0001f));
            segment.Dispose();
            Assert.That(bbox.SizeOffsetCutPlane(planeA, 4), Is.InRange(1.0f, 1.01f));

            string transformPath = Path.Combine(Path.GetTempPath(), "hbp_core_bbox_transform_test.trm");
            File.WriteAllText(transformPath, string.Join(Environment.NewLine, "10 20 30", "1 0 0", "0 1 0", "0 0 1"));
            try
            {
                using Transformation3 transformation = Transformation3.FromFile(transformPath);
                AssertVector(transformation.ApplyPoint(new Vector3(1, 2, 3)), new Vector3(-9, 22, 33));

                using BBox transformed = BBox.FromMinMax(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
                transformed.Transform(transformation);
                AssertVector(transformed.Min, new Vector3(-11, 19, 29));
                AssertVector(transformed.Max, new Vector3(-9, 21, 31));
            }
            finally
            {
                if (File.Exists(transformPath))
                {
                    File.Delete(transformPath);
                }
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("NativeParity")]
        [LegacyParityOnly]
        public void HbpCoreBBox_MatchesHbpExportBoundingBox_WhenUsingSameVolumeBounds()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            using LegacyVolume volume = ExecuteNativeOrIgnore(() => NativeParityAssert.WithBackend(BenchmarkBackend.HbpExport, () => new LegacyVolume()), "historical Volume wrapper");
            Assert.That(volume.LoadNIFTIFile(NativePath("Nifti", "fmri_3d.nii")), Is.True);

            using LegacyBBox hbpExportBBox = volume.BoundingBox;
            NativeBBoxToUnityMinMax(hbpExportBBox, out Vector3 expectedMin, out Vector3 expectedMax);
            using BBox hbpCoreBBox = BBox.FromMinMax(expectedMin, expectedMax);

            AssertVector(hbpCoreBBox.Min, expectedMin);
            AssertVector(hbpCoreBBox.Max, expectedMax);
            AssertVector(hbpCoreBBox.Center, NativeToUnity(hbpExportBBox.Center));
            AssertSameVectorSet(hbpCoreBBox.Points, NativeToUnity(hbpExportBBox.Points));
            List<HbpSegment3> hbpCoreSegments = hbpCoreBBox.Segments;
            List<HbpSegment3> hbpExportSegments = hbpExportBBox.Segments;
            Assert.That(hbpCoreSegments, Has.Count.EqualTo(hbpExportSegments.Count));
            DisposeSegments(hbpCoreSegments);
            DisposeSegments(hbpExportSegments);

            using HbpPlane plane = new(NativeToUnity(hbpExportBBox.Center), Vector3.forward);
            AssertSameVectorSet(hbpCoreBBox.IntersectionPointsWithPlane(plane), NativeToUnity(hbpExportBBox.IntersectionPointsWithPlane(plane)));

            using HbpPlane planeA = new(NativeToUnity(hbpExportBBox.Center), Vector3.right);
            using HbpPlane planeB = new(NativeToUnity(hbpExportBBox.Center), Vector3.up);
            HbpSegment3 hbpCoreSegment = hbpCoreBBox.IntersectionSegmentBetweenTwoPlanes(planeA, planeB);
            HbpSegment3 hbpExportSegment = hbpExportBBox.IntersectionSegmentBetweenTwoPlanes(planeA, planeB);

            Assert.That(hbpCoreSegment, Is.Not.Null);
            Assert.That(hbpExportSegment, Is.Not.Null);
            AssertSameVectorSet(new[] { hbpCoreSegment.End1, hbpCoreSegment.End2 }, NativeToUnity(new[] { hbpExportSegment.End1, hbpExportSegment.End2 }));
            hbpCoreSegment.Dispose();
            hbpExportSegment.Dispose();
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("HbpCoreOnly")]
        public void HbpCoreVolumeAndNifti_LoadReadOnlyFixtures_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            try
            {
                using Volume hbpCoreVolume = ExecuteNativeOrIgnore(() => new Volume(), "hbp_core Volume wrapper");
                Assert.That(hbpCoreVolume.LoadNIFTIFile(NativePath("Nifti", "fmri_3d.nii")), Is.True);
                Assert.That(hbpCoreVolume.IsLoaded, Is.True);
                AssertVector(hbpCoreVolume.Center, new Vector3(-2, 2, 2));
                AssertVector(hbpCoreVolume.Spacing, Vector3.one);
                Assert.That(hbpCoreVolume.GetValueFromPosition(new Vector3(-2, 3, 4)), Is.EqualTo(69.0f).Within(0.0001f));

                using BBox hbpCoreBBox = hbpCoreVolume.BoundingBox;
                AssertVector(hbpCoreBBox.Min, new Vector3(-4, 0, 0));
                AssertVector(hbpCoreBBox.Max, new Vector3(0, 4, 4));

                using HbpPlane cutPlane = new(hbpCoreVolume.Center, Vector3.forward);
                Assert.That(hbpCoreVolume.SizeOffsetCutPlane(cutPlane, 10), Is.GreaterThan(0.0f));

                using NIFTI nifti = ExecuteNativeOrIgnore(() => new NIFTI(), "hbp_core NIFTI wrapper");
                Assert.That(nifti.Load(NativePath("Nifti", "fmri_4d.nii.gz")), Is.True);
                Assert.That(nifti.NumberOfVolumes, Is.GreaterThan(1));
                Assert.That(nifti.TimeStep, Is.GreaterThan(0.0f));
                Assert.That(nifti.TimeUnit, Is.Not.Null);

                using Volume extractedVolume = nifti.ExtractVolume(1);
                Assert.That(extractedVolume.IsLoaded, Is.True);
                AssertVector(extractedVolume.Spacing, hbpCoreVolume.Spacing);
            }
            finally
            {
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("HbpCoreOnly")]
        public void HbpCoreSurface_CreatesUnityMeshFromBuffers_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            Mesh mesh = new();
            try
            {
                using Surface surface = ExecuteNativeOrIgnore(() => new Surface(), "hbp_core Surface wrapper");
                surface.SetBuffers(new[]
                {
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(1, 1, 0),
                    new Vector3(0, 1, 0)
                }, new[] { 0, 1, 2, 0, 2, 3 }, uv: new[]
                {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(1, 1),
                    new Vector2(0, 1)
                }, colors: new[]
                {
                    Color.red,
                    Color.green,
                    Color.blue,
                    Color.white
                });
                surface.ComputeNormals();
                surface.UpdateMeshFromDLL(mesh);

                Assert.That(surface.NumberOfVertices, Is.EqualTo(4));
                Assert.That(surface.NumberOfTriangles, Is.EqualTo(2));
                Assert.That(surface.NumberOfVisibleTriangles, Is.EqualTo(2));
                Assert.That(surface.VisibilityMask, Is.EqualTo(new[] { 1, 1 }));
                Assert.That(mesh.vertexCount, Is.EqualTo(4));
                Assert.That(mesh.triangles, Is.EqualTo(new[] { 0, 1, 2, 0, 2, 3 }));
                AssertVector(mesh.vertices[2], new Vector3(1, 1, 0));
                AssertVector(mesh.normals[0], Vector3.forward);
                Assert.That(mesh.uv, Has.Length.EqualTo(4));
                Assert.That(mesh.colors, Has.Length.EqualTo(4));

                using BBox bbox = surface.BoundingBox;
                AssertVector(bbox.Min, Vector3.zero);
                AssertVector(bbox.Max, new Vector3(1, 1, 0));

                using Surface invisibleSurface = surface.UpdateVisibilityMask(new[] { 1, 0 });
                Assert.That(surface.NumberOfVisibleTriangles, Is.EqualTo(1));
                Assert.That(surface.VisibilityMask, Is.EqualTo(new[] { 1, 0 }));
                Assert.That(invisibleSurface.NumberOfTriangles, Is.EqualTo(1));
                surface.UpdateMeshFromDLL(mesh);
                Assert.That(mesh.triangles, Is.EqualTo(new[] { 0, 1, 2 }));

                using Surface rayInvisibleSurface = surface.UpdateVisibilityMask(Vector3.forward, new Vector3(0.7f, 0.2f, 0), TriEraserMode.OneTri, 0.0f);
                Assert.That(surface.NumberOfVisibleTriangles, Is.EqualTo(0));
                Assert.That(rayInvisibleSurface.NumberOfTriangles, Is.EqualTo(2));
                using Surface resetInvisibleSurface = surface.UpdateVisibilityMask(new[] { 1, 1 });
                Assert.That(surface.NumberOfVisibleTriangles, Is.EqualTo(2));

                using Surface clone = (Surface)surface.Clone();
                clone.Append(surface);
                Assert.That(clone.NumberOfVertices, Is.EqualTo(8));
                Assert.That(clone.NumberOfTriangles, Is.EqualTo(4));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HbpCoreSurface_CutsCubeAndGeneratesSurfaceLists_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            Surface[] cutSurfaces = Array.Empty<Surface>();
            List<Surface> generatedCuts = new();
            List<Surface> rawCuts = new();
            try
            {
                using Surface cube = CreateHbpCoreCubeSurface();
                using HBP.Core.Object3D.Cut cut = new(new Vector3(0.5f, 0, 0), Vector3.right);

                cutSurfaces = cube.Cut(new[] { cut }, noHoles: false, strongCuts: true);
                Assert.That(cutSurfaces, Has.Length.EqualTo(2));
                Assert.That(cutSurfaces[0].NumberOfVertices, Is.GreaterThan(0));
                Assert.That(cutSurfaces[0].NumberOfTriangles, Is.GreaterThan(0));
                AssertCutSurfaceLiesOnPlane(cutSurfaces[1], 0.5f);

                generatedCuts = cube.GenerateCutSurfaces(new List<HBP.Core.Object3D.Cut> { cut }, noHoles: false, strongCuts: false);
                Assert.That(generatedCuts, Has.Count.EqualTo(1));
                AssertCutSurfaceLiesOnPlane(generatedCuts[0], 0.5f);

                rawCuts = cube.GenerateRawCutSurfaces(new List<HBP.Core.Object3D.Cut> { cut });
                Assert.That(rawCuts, Has.Count.EqualTo(1));
                Assert.That(rawCuts[0].NumberOfVertices, Is.EqualTo(5));
                Assert.That(rawCuts[0].NumberOfTriangles, Is.EqualTo(16));

                using Surface simplified = cube.Simplify(6, 7);
                Assert.That(simplified.NumberOfVertices, Is.GreaterThan(0));
                Assert.That(simplified.NumberOfTriangles, Is.GreaterThan(0));
                Assert.That(simplified.NumberOfTriangles, Is.LessThanOrEqualTo(cube.NumberOfTriangles));
            }
            finally
            {
                DisposeSurfaces(cutSurfaces);
                DisposeSurfaces(generatedCuts);
                DisposeSurfaces(rawCuts);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("HbpCoreOnly")]
        public void HbpCoreSurface_LoadsGiftiFixture_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            Mesh mesh = new();
            try
            {
                using Surface surface = ExecuteNativeOrIgnore(() => new Surface(), "hbp_core Surface wrapper");
                Assert.That(surface.LoadGIIFile(NativePath("Meshes", "single_surface.gii"), NativePath("Meshes", "MNI.trm")), Is.True);
                surface.UpdateMeshFromDLL(mesh);

                Assert.That(surface.NumberOfVertices, Is.EqualTo(4));
                Assert.That(surface.NumberOfTriangles, Is.EqualTo(4));
                Assert.That(mesh.vertexCount, Is.EqualTo(4));
                Assert.That(mesh.triangles, Has.Length.EqualTo(12));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("HbpCoreOnly")]
        public void HbpCoreSurface_LoadsTriFixtureAndAppliesTransformation_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            using TempDirectoryScope temp = new();
            string triPath = temp.GetPath("transformed_surface.tri");
            string transformationPath = temp.GetPath("transformed_surface.trm");
            File.WriteAllText(triPath, string.Join(Environment.NewLine, "4 2", "0 0 0 0 0 1 1 0 0", "1 0 0 0 0 1 0 1 0", "1 1 0 0 0 1 0 0 1", "0 1 0 0 0 1 1 1 1", "- 2 0 0", "0 1 2", "0 2 3", string.Empty));
            File.WriteAllText(transformationPath, string.Join(Environment.NewLine, "10 20 30", "1 0 0", "0 1 0", "0 0 1"));
            try
            {
                using Surface surface = ExecuteNativeOrIgnore(() => new Surface(), "hbp_core transformed TRI Surface wrapper");
                Assert.That(surface.LoadTRIFile(triPath, transformationPath), Is.True);

                using BBox bbox = surface.BoundingBox;
                AssertVector(bbox.Min, new Vector3(-11, 20, 30));
                AssertVector(bbox.Max, new Vector3(-10, 21, 30));
            }
            finally
            {
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HbpCoreCutGenerators_CreateVolumeAndOverlayPixelBuffers_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            Mesh cutMesh = new();
            try
            {
                using Volume volume = ExecuteNativeOrIgnore(() => new Volume(), "hbp_core Volume wrapper");
                Assert.That(volume.LoadNIFTIFile(NativePath("Nifti", "fmri_3d.nii")), Is.True);

                using HBP.Core.Object3D.Cut cut = new(volume.Center, Vector3.forward)
                {
                    Orientation = HBP.Core.Enums.CutOrientation.Axial
                };
                using CutGeometryGenerator geometryGenerator = ExecuteNativeOrIgnore(() => new CutGeometryGenerator(), "hbp_core CutGeometryGenerator wrapper");
                geometryGenerator.Initialize(volume, cut, 8);

                Vector2Int textureSize = geometryGenerator.TextureSize;
                Assert.That(textureSize.x, Is.GreaterThan(0));
                Assert.That(textureSize.y, Is.GreaterThan(0));
                Assert.That(textureSize.x, Is.LessThanOrEqualTo(8));
                Assert.That(textureSize.y, Is.LessThanOrEqualTo(8));

                Vector2 ratio = geometryGenerator.GetPositionRatioOnTexture(volume.Center);
                Assert.That(ratio.x, Is.InRange(0.45f, 0.55f));
                Assert.That(ratio.y, Is.InRange(0.45f, 0.55f));

                Color32[] colorScheme = HBP.Core.Tools.UnityTextureFactory.Generate1DColorPixels(HBP.Core.Enums.ColorType.BrainColor);
                using CutGenerator volumeOnlyCutGenerator = ExecuteNativeOrIgnore(() => new CutGenerator(), "hbp_core CutGenerator wrapper");
                volumeOnlyCutGenerator.Initialize(null, geometryGenerator, 0);
                volumeOnlyCutGenerator.FillTextureWithVolume(colorScheme, 0.0f, 1.0f);
                Color32[] volumeOnlyPixels = volumeOnlyCutGenerator.CopyBasePixels();
                Assert.That(volumeOnlyPixels, Has.Length.EqualTo(textureSize.x * textureSize.y));
                Assert.That(volumeOnlyPixels.Any(pixel => pixel.r != 0 || pixel.g != 0 || pixel.b != 0), Is.True);

                using Surface cutSurface = ExecuteNativeOrIgnore(() => new Surface(), "hbp_core Surface wrapper");
                cutSurface.SetBuffers(new[] { new Vector3(1, 1, 2), new Vector3(3, 1, 2), new Vector3(1, 3, 2) }, new[] { 0, 1, 2 });
                geometryGenerator.UpdateSurfaceUV(cutSurface);
                cutSurface.UpdateMeshFromDLL(cutMesh);
                Assert.That(cutMesh.uv, Has.Length.EqualTo(3));

                using GeneratorSurface generatorSurface = ExecuteNativeOrIgnore(() => new GeneratorSurface(), "hbp_core GeneratorSurface wrapper");
                generatorSurface.Initialize(cutSurface, volume, 8);
                using DensityGenerator densityGenerator = ExecuteNativeOrIgnore(() => new DensityGenerator(), "hbp_core DensityGenerator wrapper");
                densityGenerator.Initialize(generatorSurface);
                using RawSiteList rawSites = new();
                rawSites.AddSite("S1", new Vector3(-1, 1, 2), 0, 0);
                rawSites.UpdateMask(0, false);
                densityGenerator.ComputeActivity(rawSites, 10.0f, HBP.Core.Enums.SiteInfluenceByDistanceType.Constant);

                using SurfaceGenerator surfaceGenerator = ExecuteNativeOrIgnore(() => new SurfaceGenerator(), "hbp_core SurfaceGenerator wrapper");
                surfaceGenerator.Initialize(densityGenerator);
                surfaceGenerator.ComputeActivityUV(0, 0.4f);
                Assert.That(surfaceGenerator.ActivityUV, Has.Length.EqualTo(cutSurface.NumberOfVertices));
                Assert.That(surfaceGenerator.AlphaUV, Has.Length.EqualTo(cutSurface.NumberOfVertices));

                using CutGenerator cutGenerator = ExecuteNativeOrIgnore(() => new CutGenerator(), "hbp_core CutGenerator wrapper");
                cutGenerator.Initialize(densityGenerator, geometryGenerator, 0);
                cutGenerator.FillTextureWithVolume(colorScheme, 0.0f, 1.0f);
                Color32[] basePixels = cutGenerator.CopyBasePixels();
                Assert.That(basePixels, Has.Length.EqualTo(textureSize.x * textureSize.y));
                Assert.That(basePixels.Any(pixel => pixel.r != 0 || pixel.g != 0 || pixel.b != 0), Is.True);

                cutGenerator.FillTextureWithActivity(HBP.Core.Tools.UnityTextureFactory.Generate1DColorPixels(HBP.Core.Enums.ColorType.MatLab), 0, 0.4f);
                Color32[] activityPixels = cutGenerator.CopyOverlayPixels();
                Assert.That(activityPixels, Has.Length.EqualTo(basePixels.Length));

                cutGenerator.FillTextureWithFMRI(volume, 0.25f, 1.0f, 0.25f, 1.0f, 0.5f);
                Color32[] fmriPixels = cutGenerator.CopyOverlayPixels();
                Assert.That(fmriPixels, Has.Length.EqualTo(basePixels.Length));

                cutGenerator.FillTextureWithLocalizer(volume, 0.0f, 62.0f, 124.0f, null, HBP.Core.Tools.UnityTextureFactory.Generate1DColorPixels(HBP.Core.Enums.ColorType.MatLab));
                Color32[] localizerPixels = cutGenerator.CopyOverlayPixels();
                Assert.That(localizerPixels, Has.Length.EqualTo(basePixels.Length));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cutMesh);
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HbpCoreCutTextures_FilterNativeSiteAgainstCutInUnitySpace_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            Texture2D texture = null;
            try
            {
                using Volume volume = ExecuteNativeOrIgnore(() => new Volume(), "hbp_core Volume wrapper");
                Assert.That(volume.LoadNIFTIFile(NativePath("Nifti", "fmri_3d.nii")), Is.True);

                Vector3 unityCenter = volume.Center;
                using HBP.Core.Object3D.Cut cut = new(unityCenter, Vector3.right)
                {
                    ID = 0,
                    Orientation = CutOrientation.Sagittal
                };
                using CutGeometryGenerator geometryGenerator = ExecuteNativeOrIgnore(() => new CutGeometryGenerator(), "hbp_core CutGeometryGenerator wrapper");
                geometryGenerator.Initialize(volume, cut, 16);
                using CutGenerator cutGenerator = ExecuteNativeOrIgnore(() => new CutGenerator(), "hbp_core CutGenerator wrapper");
                cutGenerator.Initialize(null, geometryGenerator, 0);

                Vector2Int textureSize = geometryGenerator.TextureSize;
                Assert.That(textureSize.x, Is.GreaterThan(0));
                Assert.That(textureSize.y, Is.GreaterThan(0));
                texture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.RGBA32, false);
                texture.SetPixels32(Enumerable.Repeat(new Color32(0, 0, 0, 255), textureSize.x * textureSize.y).ToArray());
                texture.Apply(false, false);

                HBP.Data.Module3D.CutTexturesUtility utility = new();
                utility.BaseBrainCutTextures.Add(texture);
                utility.CutGenerators.Add(cutGenerator);
                Assert.That(Mathf.Abs(unityCenter.x), Is.GreaterThan(0.01f));
                Vector3 nativeCenter = new(-unityCenter.x, unityCenter.y, unityCenter.z);
                HBP.Core.Object3D.Implantation3D.SiteInfo site = new()
                {
                    NativePosition = nativeCenter
                };
                AssertVector(site.UnityPosition, unityCenter);

                utility.DrawSitesOnMRITextures(new List<HBP.Core.Object3D.Cut> { cut }, new[] { site }, precision: 0.01f);

                Assert.That(texture.GetPixels32().Any(pixel => pixel.r == 255 && pixel.g == 0 && pixel.b == 0), Is.True);
            }
            finally
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("HbpCoreOnly")]
        public void HbpCoreLogger_WritesAndClosesConfiguredFile_WhenLibraryIsPresent()
        {
            using TempDirectoryScope temp = new();
            string logPath = temp.GetPath("hbp_core.log");
            if (!HbpCoreRuntime.TrySetLogFile(logPath, out string attachError))
            {
                Assert.Ignore($"hbp_core file logger is not available yet: {attachError}");
            }

            const string message = "hbp_core unity file logger";
            try
            {
                Assert.That(HbpCoreRuntime.DebugMessage(message, HbpCoreLogType.Warning), Is.EqualTo(HbpCoreStatus.Ok));
            }
            finally
            {
                Assert.That(HbpCoreRuntime.TryResetLogFile(out string resetError), Is.True, resetError);
            }

            Assert.That(File.ReadAllText(logPath), Does.Contain($"[WARNING] {message}"));
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("HbpCoreOnly")]
        public void HbpCoreRawSiteList_StoresSitesAndQueriesPlanes_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            try
            {
                using RawSiteList rawSites = ExecuteNativeOrIgnore(() => new RawSiteList(), "hbp_core RawSiteList wrapper");
                rawSites.AddSite("S1", new Vector3(1, 2, 3), patientIndex: 0, index: 0);

                Assert.That(rawSites.NumberOfSites, Is.EqualTo(1));
                Assert.That(rawSites.GetMarsAtlasLabelOfSite(0), Is.EqualTo(-1));

                using HbpPlane plane = new(new Vector3(0, 0, 3), Vector3.forward);
                rawSites.GetSitesOnPlane(plane, 0.01f, out int[] sitesOnPlane);
                Assert.That(sitesOnPlane, Is.EqualTo(new[] { 1 }));

                rawSites.UpdateMask(0, false);
                using RawSiteList clone = new(rawSites);
                Assert.That(clone.NumberOfSites, Is.EqualTo(1));
                clone.GetSitesOnPlane(plane, 0.01f, out int[] cloneSitesOnPlane);
                Assert.That(cloneSitesOnPlane, Is.EqualTo(new[] { 1 }));
            }
            finally
            {
            }
        }

        [Test]
        [Category("NativeMigration")]
        public void PatientSiteLoader_LoadsPtsSitesWithoutPatientElectrodesList()
        {
            string ptsPath = Path.Combine(Path.GetTempPath(), "hbp_core_patient_sites_loader.pts");
            File.WriteAllText(ptsPath, string.Join(Environment.NewLine, "ptsfile", "2", "A1 1 2 3", "B2 4 5 6"));

            try
            {
                List<HBP.Core.Data.Site> sites = HBP.Core.Data.Site.LoadSitesFromPTSFile("Patient", ptsPath);

                Assert.That(sites, Has.Count.EqualTo(2));
                Assert.That(sites[0].Name, Is.EqualTo("A1"));
                Assert.That(sites[0].Coordinates, Has.Count.EqualTo(1));
                AssertVector(sites[0].Coordinates[0].Position.ToVector3(), new Vector3(1, 2, 3));
                Assert.That(sites[1].Name, Is.EqualTo("B2"));
                AssertVector(sites[1].Coordinates[0].Position.ToVector3(), new Vector3(4, 5, 6));
            }
            finally
            {
                if (File.Exists(ptsPath))
                {
                    File.Delete(ptsPath);
                }
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("HbpCoreOnly")]
        public void HbpCoreImplantation3D_BuildsRawSiteListFromManagedSites_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            HBP.Core.Object3D.Implantation3D implantation = null;
            try
            {
                HBP.Core.Data.Patient patient = new("Patient A", Array.Empty<HBP.Core.Data.BaseMesh>(), Array.Empty<HBP.Core.Data.MRI>(), Array.Empty<HBP.Core.Data.Site>(), Array.Empty<HBP.Core.Data.BaseTagValue>(), "", "patient-a");

                List<HBP.Core.Object3D.Implantation3D.SiteInfo> siteInfos = new()
                {
                    new()
                    {
                        Name = "A1",
                        NativePosition = new Vector3(1, 2, 3),
                        PatientIndex = 0,
                        Patient = patient,
                        Electrode = "A",
                        Index = 0
                    },
                    new()
                    {
                        Name = "B2",
                        NativePosition = new Vector3(4, 5, 6),
                        PatientIndex = 0,
                        Patient = patient,
                        Electrode = "B",
                        Index = 1
                    }
                };

                implantation = new HBP.Core.Object3D.Implantation3D("Patient", siteInfos, new[] { patient });

                Assert.That(implantation.IsLoaded, Is.True);
                Assert.That(implantation.SiteInfos, Has.Count.EqualTo(2));
                Assert.That(implantation.SiteInfos[0].Electrode, Is.EqualTo("A"));
                Assert.That(implantation.RawSiteList.NumberOfSites, Is.EqualTo(2));

                using HbpPlane secondSitePlane = new(new Vector3(0, 0, 6), Vector3.forward);
                implantation.RawSiteList.GetSitesOnPlane(secondSitePlane, 0.01f, out int[] sitesOnPlane);
                Assert.That(sitesOnPlane, Is.EqualTo(new[] { 0, 1 }));
            }
            finally
            {
                implantation?.Clean();
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HbpCoreActivityGenerators_ComputeSurfaceActivityUVs_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            try
            {
                using Volume volume = ExecuteNativeOrIgnore(() => new Volume(), "hbp_core Volume wrapper");
                Assert.That(volume.LoadNIFTIFile(NativePath("Nifti", "fmri_3d.nii")), Is.True);

                using Surface surface = ExecuteNativeOrIgnore(() => new Surface(), "hbp_core Surface wrapper");
                surface.SetBuffers(new[] { new Vector3(1, 1, 2), new Vector3(3, 1, 2), new Vector3(1, 3, 2) }, new[] { 0, 1, 2 });

                using GeneratorSurface generatorSurface = ExecuteNativeOrIgnore(() => new GeneratorSurface(), "hbp_core GeneratorSurface wrapper");
                generatorSurface.Initialize(surface, volume, 8);

                using RawSiteList rawSites = new();
                rawSites.AddSite("S1", new Vector3(1, 1, 2), 0, 0);
                rawSites.UpdateMask(0, false);

                using IEEGGenerator ieegGenerator = ExecuteNativeOrIgnore(() => new IEEGGenerator(), "hbp_core IEEGGenerator wrapper");
                ieegGenerator.Initialize(generatorSurface);
                ieegGenerator.ComputeActivity(rawSites, 10.0f, new[] { 1.0f, -0.5f }, 2, rawSites.NumberOfSites, HBP.Core.Enums.SiteInfluenceByDistanceType.Constant);
                ieegGenerator.AdjustValues(0.0f, -1.0f, 1.0f);
                AssertActivityUVs(surface, ieegGenerator);

                using FMRIGenerator fmriGenerator = ExecuteNativeOrIgnore(() => new FMRIGenerator(), "hbp_core FMRIGenerator wrapper");
                fmriGenerator.Initialize(generatorSurface);
                fmriGenerator.ComputeActivity(new[] { (volume, (Volume)null) });
                fmriGenerator.AdjustValues(0.25f, 1.0f, 0.25f, 1.0f);
                fmriGenerator.HideExtremeValues(false, false, false);
                AssertActivityUVs(surface, fmriGenerator);

                using MEGGenerator megGenerator = ExecuteNativeOrIgnore(() => new MEGGenerator(), "hbp_core MEGGenerator wrapper");
                megGenerator.Initialize(generatorSurface);
                megGenerator.ComputeActivity(new[] { (volume, (Volume)null) });
                megGenerator.AdjustValues(0.25f, 1.0f, 0.25f, 1.0f);
                megGenerator.HideExtremeValues(false, false, false);
                AssertActivityUVs(surface, megGenerator);
            }
            finally
            {
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("HbpCoreOnly")]
        public void HbpCoreMarsAtlas_UsesBrainAtlasMethodsAndColorsSurface_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            Mesh mesh = new();
            string parcelsPath = Path.Combine(Path.GetTempPath(), "hbp_core_unity_mars_parcels.gii");
            try
            {
                File.WriteAllText(parcelsPath, MarsParcelsGiftiFixture());
                using MarsAtlas atlas = ExecuteNativeOrIgnore(() => new MarsAtlas(), "hbp_core MarsAtlas wrapper");
                Assert.That(atlas.Load(AtlasPath("mars_atlas_index.csv"), AtlasPath("brodmann_areas.txt"), AtlasPath("colin27_MNI_MarsAtlas.nii")), Is.True);

                Assert.That(atlas.Label("L_VCcm"), Is.EqualTo(1));
                Assert.That(atlas.Hemisphere(1), Is.EqualTo("L"));
                Assert.That(atlas.FullName(1), Does.Contain("Caudal Medial Visual Cortex"));
                Assert.That(atlas.GetInformation(1), Has.Length.EqualTo(5));
                Assert.That(atlas.GetAreaName(1), Does.Contain("Caudal Medial Visual Cortex"));

                Vector3[] coordinates = atlas.GetAreaCoordinates(1);
                Assert.That(coordinates, Is.Not.Empty);
                Assert.That(atlas.GetClosestAreaIndex(coordinates[0], 0), Is.EqualTo(1));

                Color[] colors = atlas.ConvertIndicesToColors(new[] { 1, -1 }, 1);
                Assert.That(colors[0].r, Is.GreaterThan(0.9f));
                Assert.That(colors[1].a, Is.EqualTo(0.0f).Within(0.0001f));

                Assert.That(atlas.Load(AtlasPath("mars_atlas_index.csv"), AtlasPath("brodmann_areas.txt"), AtlasPath("colin27_MNI_MarsAtlas.nii")), Is.True);
                Assert.That(atlas.FullName(1), Does.Contain("Caudal Medial Visual Cortex"));
                Assert.That(atlas.ConvertIndicesToColors(new[] { 1 }, 1)[0].r, Is.GreaterThan(0.9f));

                using Surface surface = ExecuteNativeOrIgnore(() => new Surface(), "hbp_core Surface wrapper");
                Assert.That(surface.LoadGIIFile(NativePath("Meshes", "single_surface.gii")), Is.True);
                int[] labels = atlas.GetSurfaceAreaLabels(surface);
                Assert.That(labels, Has.Length.EqualTo(surface.NumberOfVertices));

                Assert.That(surface.SearchMarsParcelFileAndUpdateColors(atlas, parcelsPath), Is.True);
                surface.UpdateMeshFromDLL(mesh);
                Assert.That(mesh.colors, Has.Length.EqualTo(surface.NumberOfVertices));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
                if (File.Exists(parcelsPath))
                {
                    File.Delete(parcelsPath);
                }
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("HbpCoreOnly")]
        public void HbpCoreJuBrainAtlas_UsesBrainAtlasMethods_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            try
            {
                using JuBrainAtlas atlas = ExecuteNativeOrIgnore(() => new JuBrainAtlas(), "hbp_core JuBrainAtlas wrapper");
                atlas.Load();

                Assert.That(atlas.Loaded, Is.True);
                Assert.That(atlas.AreaNames, Does.Contain("Area 3b (PostCG)"));
                Assert.That(atlas.GetAreaName(1), Is.EqualTo("Area 3b (PostCG)"));
                Assert.That(atlas.GetInformation(1), Is.EqualTo(new[] { "Area 3b (PostCG)" }));

                Vector3[] coordinates = atlas.GetAreaCoordinates(1);
                Assert.That(coordinates, Is.Not.Empty);
                Assert.That(atlas.GetClosestAreaIndex(coordinates[0], 0), Is.EqualTo(1));

                Color[] colors = atlas.ConvertIndicesToColors(new[] { 1, 3, 0 }, 1);
                Assert.That(colors[0].r, Is.GreaterThan(0.9f));
                Assert.That(colors[0].g, Is.GreaterThan(0.9f));
                Assert.That(colors[0].b, Is.GreaterThan(0.7f));
                Assert.That(colors[1].r, Is.GreaterThan(0.8f));
                Assert.That(colors[2].a, Is.EqualTo(0.0f).Within(0.0001f));

                Color normal = atlas.ConvertIndicesToColors(new[] { 1 }, -1)[0];
                Color highlighted = atlas.ConvertIndicesToColors(new[] { 1 }, 1)[0];
                Assert.That(highlighted.r, Is.GreaterThanOrEqualTo(normal.r));
                Assert.That(highlighted.g, Is.GreaterThanOrEqualTo(normal.g));
                Assert.That(highlighted.b, Is.GreaterThanOrEqualTo(normal.b));

                atlas.Load();
                Assert.That(atlas.Loaded, Is.True);
                Assert.That(atlas.AreaNames, Does.Contain("Area 3b (PostCG)"));
                Assert.That(atlas.GetAreaName(1), Is.EqualTo("Area 3b (PostCG)"));
                Assert.That(atlas.GetInformation(1), Is.EqualTo(new[] { "Area 3b (PostCG)" }));
            }
            finally
            {
            }
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        [Category("NativeParity")]
        [LegacyParityOnly]
        public void HbpCoreSurface_MatchesHbpExportObjCube_WhenLibraryIsPresent()
        {
            if (!HbpCoreRuntime.TryGetVersion(out _, out string error))
            {
                Assert.Ignore($"hbp_core is not installed next to hbp_export yet: {error}");
            }

            string objPath = Path.Combine(Path.GetTempPath(), "hbp_core_surface_cube_compare.obj");
            File.WriteAllText(objPath, CubeObjFixture());

            Mesh hbpExportMesh = new();
            Mesh hbpCoreMesh = new();
            try
            {
                using LegacySurface hbpExportSurface = ExecuteNativeOrIgnore(() => NativeParityAssert.WithBackend(BenchmarkBackend.HbpExport, () => new LegacySurface()), "hbp_export Surface wrapper");
                Assert.That(hbpExportSurface.LoadOBJFile(objPath), Is.True);
                hbpExportSurface.UpdateMeshFromDLL(hbpExportMesh);

                using Surface hbpCoreSurface = ExecuteNativeOrIgnore(() => new Surface(), "hbp_core Surface wrapper");
                Assert.That(hbpCoreSurface.LoadOBJFile(objPath), Is.True);
                hbpCoreSurface.UpdateMeshFromDLL(hbpCoreMesh);

                Assert.That(hbpCoreSurface.NumberOfVertices, Is.EqualTo(hbpExportSurface.NumberOfVertices));
                Assert.That(hbpCoreSurface.NumberOfTriangles, Is.EqualTo(hbpExportSurface.NumberOfTriangles));
                using LegacyBBox hbpExportBBox = hbpExportSurface.BoundingBox;
                using BBox hbpCoreBBox = hbpCoreSurface.BoundingBox;
                NativeBBoxToUnityMinMax(hbpExportBBox, out Vector3 expectedMin, out Vector3 expectedMax);
                AssertVector(hbpCoreBBox.Min, expectedMin);
                AssertVector(hbpCoreBBox.Max, expectedMax);
                Assert.That(hbpCoreMesh.vertexCount, Is.EqualTo(hbpExportMesh.vertexCount));
                Assert.That(hbpCoreMesh.triangles, Is.EqualTo(ReverseTriangleWinding(hbpExportMesh.triangles)));
                AssertVector(hbpCoreMesh.vertices[6], NativeToUnity(hbpExportMesh.vertices[6]));
                AssertVector(hbpCoreMesh.normals[0], NativeToUnity(hbpExportMesh.normals[0]));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hbpExportMesh);
                UnityEngine.Object.DestroyImmediate(hbpCoreMesh);
                if (File.Exists(objPath))
                {
                    File.Delete(objPath);
                }
            }
        }

        private static List<DllImportSignature> ReadCurrentDllImports()
        {
            string dllFolder = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Scripts", "HBP", "Core", "DLL");
            return Directory.GetFiles(dllFolder, "*.cs", SearchOption.AllDirectories).SelectMany(ReadDllImportsFromFile).OrderBy(imported => imported.RelativeFile, StringComparer.Ordinal).ThenBy(imported => imported.Entry, StringComparer.Ordinal).ToList();
        }

        private static IEnumerable<DllImportSignature> ReadDllImportsFromFile(string file)
        {
            string dllFolder = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Scripts", "HBP", "Core", "DLL");
            string relativeFile = file.Substring(dllFolder.Length).TrimStart('\\', '/').Replace('\\', '/');

            foreach (Match match in DllImportRegex.Matches(File.ReadAllText(file)))
            {
                string dll = match.Groups["dll"].Success ? match.Groups["dll"].Value : "hbp_core";

                yield return new DllImportSignature(dll, match.Groups["entry"].Value, relativeFile);
            }
        }

        private static T ExecuteNativeOrIgnore<T>(Func<T> action, string context)
        {
            try
            {
                return action();
            }
            catch (Exception exception) when (IsMissingNativeDependency(exception))
            {
                Assert.Ignore($"Native dependency unavailable for {context}: {exception.Message}");
                throw;
            }
        }

        private static bool IsMissingNativeDependency(Exception exception)
        {
            for (Exception current = exception; current != null; current = current.InnerException)
            {
                if (current is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
                {
                    return true;
                }
            }

            return false;
        }

        private static string NativePath(params string[] parts)
        {
            string path = TestPathUtility.FixturePath("Native");
            foreach (string part in parts)
            {
                path = Path.Combine(path, part);
            }

            return path;
        }

        private static string AtlasPath(string fileName)
        {
            return Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Data", "Atlases", "MarsAtlas", fileName);
        }

        private static string CubeObjFixture()
        {
            return string.Join(Environment.NewLine, "v 0 0 0 1 0 0", "v 1 0 0 0 1 0", "v 1 1 0 0 0 1", "v 0 1 0 1 1 0", "v 0 0 1 1 0 1", "v 1 0 1 0 1 1", "v 1 1 1 1 1 1", "v 0 1 1 0.5 0.5 0.5", "vn 0 0 1", "vn 0 0 1", "vn 0 0 1", "vn 0 0 1", "vn 0 0 1", "vn 0 0 1", "vn 0 0 1", "vn 0 0 1", "vt 0 0", "vt 1 0", "vt 1 1", "vt 0 1", "vt 0 0", "vt 1 0", "vt 1 1", "vt 0 1", "f 1/1/1 2/2/2 3/3/3", "f 1/1/1 3/3/3 4/4/4", "f 5/5/5 7/7/7 6/6/6", "f 5/5/5 8/8/8 7/7/7", "f 1/1/1 5/5/5 6/6/6", "f 1/1/1 6/6/6 2/2/2", "f 2/2/2 6/6/6 7/7/7", "f 2/2/2 7/7/7 3/3/3", "f 3/3/3 7/7/7 8/8/8", "f 3/3/3 8/8/8 4/4/4", "f 4/4/4 8/8/8 5/5/5", "f 4/4/4 5/5/5 1/1/1", string.Empty);
        }

        private static string MarsParcelsGiftiFixture()
        {
            return string.Join(Environment.NewLine, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>", "<GIFTI Version=\"1.0\" NumberOfDataArrays=\"1\"><MetaData /><LabelTable />", "<DataArray Intent=\"NIFTI_INTENT_NONE\" DataType=\"NIFTI_TYPE_INT32\" ArrayIndexingOrder=\"RowMajorOrder\" Dimensionality=\"1\" Encoding=\"ASCII\" Endian=\"LittleEndian\" ExternalFileName=\"\" ExternalFileOffset=\"0\" Dim0=\"4\">", "<MetaData /><Data>1 2 1 2</Data></DataArray></GIFTI>", string.Empty);
        }

        private static Surface CreateHbpCoreCubeSurface()
        {
            Surface surface = ExecuteNativeOrIgnore(() => new Surface(), "hbp_core Surface wrapper");
            surface.SetBuffers(new[]
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 1, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, 0, 1),
                new Vector3(1, 0, 1),
                new Vector3(1, 1, 1),
                new Vector3(0, 1, 1)
            }, new[]
            {
                0, 1, 2, 0, 2, 3,
                4, 6, 5, 4, 7, 6,
                0, 4, 5, 0, 5, 1,
                3, 2, 6, 3, 6, 7,
                0, 3, 7, 0, 7, 4,
                1, 5, 6, 1, 6, 2
            });
            surface.ComputeNormals();
            return surface;
        }

        private static void AssertCutSurfaceLiesOnPlane(Surface surface, float x)
        {
            Assert.That(surface.NumberOfVertices, Is.GreaterThanOrEqualTo(4));
            Assert.That(surface.NumberOfTriangles, Is.GreaterThanOrEqualTo(2));
            using BBox bbox = surface.BoundingBox;
            Assert.That(bbox.Min.x, Is.EqualTo(x).Within(0.0001f));
            Assert.That(bbox.Max.x, Is.EqualTo(x).Within(0.0001f));
        }

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
        }

        private static Vector3 NativeToUnity(Vector3 value)
        {
            return new Vector3(-value.x, value.y, value.z);
        }

        private static List<Vector3> NativeToUnity(IEnumerable<Vector3> values)
        {
            return values.Select(NativeToUnity).ToList();
        }

        private static void NativeBBoxToUnityMinMax(LegacyBBox bbox, out Vector3 min, out Vector3 max)
        {
            Vector3 first = NativeToUnity(bbox.Min);
            Vector3 second = NativeToUnity(bbox.Max);
            min = Vector3.Min(first, second);
            max = Vector3.Max(first, second);
        }

        private static int[] ReverseTriangleWinding(int[] triangles)
        {
            int[] result = new int[triangles.Length];
            triangles.CopyTo(result, 0);
            for (int i = 0; i + 2 < result.Length; i += 3)
            {
                (result[i + 1], result[i + 2]) = (result[i + 2], result[i + 1]);
            }

            return result;
        }

        private static float ReadVec3Field(object vec3, string fieldName)
        {
            FieldInfo field = vec3.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Vec3.{fieldName} field is required for native marshalling.");
            return (float)field.GetValue(vec3);
        }

        private static void AssertSameVectorSet(IReadOnlyCollection<Vector3> actual, IReadOnlyCollection<Vector3> expected)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));

            List<Vector3> remaining = new(actual);
            foreach (Vector3 expectedPoint in expected)
            {
                int foundIndex = remaining.FindIndex(actualPoint => VectorsEqual(actualPoint, expectedPoint));
                Assert.That(foundIndex, Is.GreaterThanOrEqualTo(0), $"Missing point {expectedPoint}");
                remaining.RemoveAt(foundIndex);
            }
        }

        private static void DisposeSegments(IEnumerable<HbpSegment3> segments)
        {
            foreach (HbpSegment3 segment in segments)
            {
                segment.Dispose();
            }
        }

        private static void DisposeSurfaces(IEnumerable<Surface> surfaces)
        {
            foreach (Surface surface in surfaces)
            {
                surface?.Dispose();
            }
        }

        private static void AssertActivityUVs(Surface surface, ActivityGenerator activityGenerator)
        {
            using SurfaceGenerator surfaceGenerator = ExecuteNativeOrIgnore(() => new SurfaceGenerator(), "hbp_core SurfaceGenerator wrapper");
            surfaceGenerator.Initialize(activityGenerator);
            surfaceGenerator.ComputeActivityUV(0, 0.4f);
            Assert.That(surfaceGenerator.ActivityUV, Has.Length.EqualTo(surface.NumberOfVertices));
            Assert.That(surfaceGenerator.AlphaUV, Has.Length.EqualTo(surface.NumberOfVertices));
            Assert.That(surfaceGenerator.AlphaUV.Any(uv => uv.x > 0.01f), Is.True);
        }

        private static bool VectorsEqual(Vector3 actual, Vector3 expected)
        {
            return Mathf.Abs(actual.x - expected.x) <= 0.0001f && Mathf.Abs(actual.y - expected.y) <= 0.0001f && Mathf.Abs(actual.z - expected.z) <= 0.0001f;
        }

        private readonly struct DllImportSignature
        {
            public DllImportSignature(string dll, string entry, string relativeFile)
            {
                Dll = dll;
                Entry = entry;
                RelativeFile = relativeFile;
            }

            public string Dll { get; }
            public string Entry { get; }
            public string RelativeFile { get; }
        }
    }
}
