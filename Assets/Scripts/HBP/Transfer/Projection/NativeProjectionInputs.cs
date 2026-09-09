using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using HBP.Core.DLL;
using HBP.Transfer.Anatomy;
using UnityEngine;

namespace HBP.Transfer.Projection
{
    /// <summary>Session-owned native inputs, borrowed until replacement/close. No density work or global shutdown.</summary>
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
            if (disposed) return;
            disposed = true;

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
