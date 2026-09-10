using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using HBP.Core.Enums;
using HBP.Core.DLL;
using HBP.Transfer.Anatomy;
using UnityEngine;

namespace HBP.Transfer.Projection
{
    /// <summary>Session-owned inputs. Retirement defers release until the current native calculation finishes.</summary>
    public sealed class NativeProjectionInputs : IDisposable
    {
        public Volume Volume { get; private set; }
        public Surface Surface { get; private set; }
        public RawSiteList Sites { get; private set; }
        public AnatomyProjection Settings { get; private set; }
        public string VolumePath { get; private set; }
        public string CleanupError { get; private set; }
        private string directory;
        private bool disposed;
        private readonly object lifetime = new object();
        private bool computing;

        public sealed class IEEGResult
        {
            public Vector2[] ActivityUV, AlphaUV;
            public Vector3[] GridPoints;
            public Vector3Int GridDimensions;
            public int[] SiteMasks;
            public SurfaceProjectionCoverage Coverage;
            public double PreparationMs, ComputeMs, ProjectionAndCopyMs;
            public long UvCopyBytes, InputCopyBytes;
        }

        /// <summary>One received sample, calibrated with the full Desktop preparation's range.
        /// Temporal Alpha belongs to SiteValues; the surface keeps the received sample-and-hold value.</summary>
        public Task<IEEGResult> ComputeIEEGAsync(IEEGInstant instant, bool captureGrid = false)
        {
            if (instant == null) throw new ArgumentNullException(nameof(instant));
            lock (lifetime)
            {
                if (disposed) throw new ObjectDisposedException(nameof(NativeProjectionInputs));
                if (computing) throw new InvalidOperationException("Projection is already computing.");
                computing = true;
            }

            try
            {
                return Task.Run(() =>
                {
                    try
                    {
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        using var grid = ActivityProjectionGrid.Create(Volume, Settings.GridDimension, (VolumeInterpolation)Settings.Interpolation);
                        using var generator = new IEEGGenerator();
                        generator.Initialize(grid);
                        generator.SetSmoothActivityBoundaries(true); // HBNA rejects unsupported false at capture.
                        using var projection = new SurfaceGenerator();
                        projection.Initialize(generator, Surface, 0, 1);
                        var values = instant.SurfaceValues.ToArray();
                        var result = new IEEGResult { PreparationMs = watch.Elapsed.TotalMilliseconds, GridDimensions = grid.Dimensions, InputCopyBytes = 4L * values.Length };
                        watch.Restart();
                        generator.ComputeCalibratedActivity(Sites, Settings.InfluenceDistance, values, 1, Sites.NumberOfSites, (SiteInfluenceByDistanceType)Settings.InfluenceByDistance, instant.Middle, instant.SpanMin, instant.SpanMax);
                        result.ComputeMs = watch.Elapsed.TotalMilliseconds;
                        watch.Restart();
                        projection.ComputeActivityUV(0, Settings.ActivityAlpha);
                        result.ActivityUV = projection.ActivityUV;
                        result.AlphaUV = projection.AlphaUV;
                        result.Coverage = projection.ProjectionCoverage;
                        result.ProjectionAndCopyMs = watch.Elapsed.TotalMilliseconds;
                        result.UvCopyBytes = 16L * result.ActivityUV.Length;
                        if (captureGrid) result.GridPoints = grid.Points;
                        result.SiteMasks = Sites.GetMask();
                        return result;
                    }
                    finally
                    {
                        FinishComputation();
                    }
                });
            }
            catch
            {
                FinishComputation();
                throw;
            }
        }

        public sealed class DensityResult
        {
            public Vector2[] ActivityUV, AlphaUV;
            public Vector3[] GridPoints;
            public Vector3Int GridDimensions;
            public int[] SiteMasks;
            public SurfaceProjectionCoverage Coverage;
            public float MaxDensity;
            public double PreparationMs, ComputeMs, ProjectionAndCopyMs;
            public long UvCopyBytes;
        }

        /// <summary>Reserve inputs before scheduling. No cancellation of native work; callers may discard its result.</summary>
        public Task<DensityResult> ComputeDensityAsync(bool captureGrid = false)
        {
            lock (lifetime)
            {
                if (disposed) throw new ObjectDisposedException(nameof(NativeProjectionInputs));
                if (computing) throw new InvalidOperationException("Density is already computing.");
                computing = true;
            }

            try
            {
                return Task.Run(() =>
                {
                    try
                    {
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        // These are the same wrappers used by Base3DScene and Column3DAnatomy.
                        using var grid = ActivityProjectionGrid.Create(Volume, Settings.GridDimension, (VolumeInterpolation)Settings.Interpolation);
                        using var density = new DensityGenerator();
                        density.Initialize(grid);
                        using var projection = new SurfaceGenerator();
                        projection.Initialize(density, Surface, 0, 1);
                        var result = new DensityResult { PreparationMs = watch.Elapsed.TotalMilliseconds, GridDimensions = grid.Dimensions };
                        watch.Restart();
                        density.ComputeActivity(Sites, Settings.InfluenceDistance, (SiteInfluenceByDistanceType)Settings.InfluenceByDistance);
                        result.ComputeMs = watch.Elapsed.TotalMilliseconds;
                        watch.Restart();
                        projection.ComputeActivityUV(0, Settings.ActivityAlpha);
                        result.ActivityUV = projection.ActivityUV;
                        result.AlphaUV = projection.AlphaUV;
                        result.MaxDensity = density.MaxDensity;
                        result.Coverage = projection.ProjectionCoverage;
                        result.ProjectionAndCopyMs = watch.Elapsed.TotalMilliseconds;
                        result.UvCopyBytes = 16L * result.ActivityUV.Length;
                        if (captureGrid) result.GridPoints = grid.Points;
                        result.SiteMasks = Sites.GetMask();
                        return result;
                    }
                    finally
                    {
                        FinishComputation();
                    }
                });
            }
            catch
            {
                FinishComputation();
                throw;
            }
        }

        private void FinishComputation()
        {
            lock (lifetime)
            {
                computing = false;
                if (disposed) ReleaseResources();
            }
        }

        public static Vector3 TransportToNativeSite(AnatomyBuffer<float> position) => new Vector3(ReferenceSystemConversion.ConvertX(position[0]), position[1], position[2]);

        public static NativeProjectionInputs Create(AnatomySnapshot snapshot, string privateRoot)
        {
            if (snapshot?.Projection == null) throw new InvalidDataException("Projection inputs require a reference volume.");
            var result = new NativeProjectionInputs { Settings = snapshot.Projection };
            try
            {
                // Only locally generated identifiers enter paths. Network/session IDs never become filenames.
                string root = Path.GetFullPath(privateRoot);
                for (var parent = new DirectoryInfo(root); parent != null; parent = parent.Parent)
                    if (parent.Exists && (parent.Attributes & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("Projection cache cannot traverse symbolic links/reparse points.");
                Directory.CreateDirectory(root);
                result.directory = Path.GetFullPath(Path.Combine(root, Guid.NewGuid().ToString("N")));
                if (!result.directory.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new IOException("Projection cache escaped its private root.");
                Directory.CreateDirectory(result.directory);
                result.VolumePath = Path.Combine(result.directory, "reference.nii");
                using (var file = new FileStream(result.VolumePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                {
                    byte[] bytes = snapshot.Projection.VolumeBytes.ToArray();
                    file.Write(bytes, 0, bytes.Length);
                    file.Flush();
                    file.Position = 0;
                    using var sha = SHA256.Create();
                    if (!sha.ComputeHash(file).SequenceEqual(snapshot.Projection.VolumeHash.ToArray())) throw new IOException("Private volume file SHA-256 mismatch.");
                }

                result.Volume = new Volume();
                if (!result.Volume.LoadNIFTIFile(result.VolumePath)) throw new InvalidDataException("Native reference volume loading failed: " + Core.DLL.HbpCore.HbpCoreRuntime.LastError);
                int[] dimensions = snapshot.Projection.Dimensions;
                if (result.Volume.Dimensions != new Vector3Int(dimensions[0], dimensions[1], dimensions[2])) throw new InvalidDataException("Native volume dimensions differ from the received header.");
                result.Surface = new Surface();
                // SetBuffers owns the native X reflection and winding conversion for BOTH positions and normals.
                result.Surface.SetBuffers(Vectors(snapshot.Positions), snapshot.Indices.ToArray().Select(index => checked((int)index)).ToArray(), Vectors(snapshot.Normals), Uvs(snapshot.Uvs));
                result.Sites = new RawSiteList();
                result.Sites.SetPatients(snapshot.Contacts.PatientIds.Select(id => new Core.Data.Patient { ID = id }));
                foreach (AnatomySite site in snapshot.Contacts.Sites)
                {
                    result.Sites.AddSite(site.Name, TransportToNativeSite(site.Position), site.PatientIndex, site.SourceIndex);
                    result.Sites.UpdateMask(site.Order, site.EffectiveMasked);
                }

                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }

        private static Vector3[] Vectors(AnatomyBuffer<float> values)
        {
            var result = new Vector3[values.Count / 3];
            for (int i = 0; i < result.Length; i++) result[i] = new Vector3(values[3 * i], values[3 * i + 1], values[3 * i + 2]);
            return result;
        }

        private static Vector2[] Uvs(AnatomyBuffer<float> values)
        {
            if (values.Count == 0) return null;
            var result = new Vector2[values.Count / 2];
            for (int i = 0; i < result.Length; i++) result[i] = new Vector2(values[2 * i], values[2 * i + 1]);
            return result;
        }

        public void Dispose()
        {
            lock (lifetime)
            {
                if (disposed) return;
                disposed = true;
                if (!computing) ReleaseResources();
            }
        }

        private void ReleaseResources()
        {
            // Attempt every release. Cleanup after publication must not turn an effective commit into a rejection.
            void Release(Action action)
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    CleanupError = (CleanupError ?? "") + exception.Message + Environment.NewLine;
                    Debug.LogError("Projection resource cleanup failed: " + exception.Message);
                }
            }

            Release(() => Sites?.Dispose());
            Release(() => Surface?.Dispose());
            Release(() => Volume?.Dispose());
            if (directory != null)
                Release(() =>
                {
                    if (VolumePath != null && File.Exists(VolumePath)) File.Delete(VolumePath);
                    if (Directory.Exists(directory)) Directory.Delete(directory); // Never recursive.
                });
            Settings = null;
        }
    }
}
