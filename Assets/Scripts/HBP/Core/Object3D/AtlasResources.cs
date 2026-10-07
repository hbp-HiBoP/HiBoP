using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.DLL;
using HBP.Core.Preferences;
using HBP.Core.Tools;

namespace HBP.Core.Object3D
{
    public enum AtlasLoadState
    {
        Unloaded,
        Loading,
        Loaded,
        Failed,
        Cancelled
    }

    public sealed class AtlasLoadResult
    {
        public string Id { get; }
        public AtlasLoadState State { get; }
        public string Fingerprint { get; }
        public string Error { get; }
        public bool Succeeded => State == AtlasLoadState.Loaded || State == AtlasLoadState.Unloaded;

        public AtlasLoadResult(string id, AtlasLoadState state, string fingerprint = null, string error = null)
        {
            Id = id;
            State = state;
            Fingerprint = fingerprint;
            Error = error;
        }
    }

    /// <summary>The same installed-resource adapters serve startup and explicit loads on every platform.</summary>
    public static class AtlasResources
    {
        public sealed class Definition
        {
            public string Id { get; }
            public string Directory { get; }
            public string[] RequiredFiles { get; }
            public Func<AtlasesPreferences, bool> Preload { get; }

            internal Definition(string id, string directory, Func<AtlasesPreferences, bool> preload, params string[] required)
            {
                Id = id;
                Directory = directory;
                Preload = preload;
                RequiredFiles = required;
            }
        }

        private static readonly Definition[] s_Definitions = BuildDefinitions();

        private sealed class LoadOperation
        {
            public AsyncLazy<AtlasLoadResult> Work;
            public readonly List<CancellationToken> Consumers = new();

            public void CheckCancellation()
            {
                if (Consumers.All(token => token.IsCancellationRequested)) throw new OperationCanceledException("All atlas consumers cancelled.");
            }
        }

        private static readonly Dictionary<string, LoadOperation> s_Loads = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, AtlasLoadResult> s_Results = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, (object Identity, string Fingerprint)> s_LoadedIdentities = new(StringComparer.Ordinal);
        public static IReadOnlyList<Definition> Definitions => s_Definitions;
        public static event Action<AtlasLoadResult> Changed;

        private static Definition[] BuildDefinitions()
        {
            var definitions = new List<Definition>
            {
                new("mars", "MarsAtlas", p => p.PreloadMarsAtlas, "mars_atlas_index.csv", "brodmann_areas.txt", "colin27_MNI_MarsAtlas.nii"),
                new("jubrain", "JuBrain", p => p.PreloadJuBrain, "JulichBrainAtlas_3.1_207areas_MPM_lh_Colin27.nii.gz", "JulichBrainAtlas_3.1_207areas_MPM_rh_Colin27.nii.gz", "jubrain_labels_3.1.json"),
                new("ibc", "IBC", p => p.PreloadIBC, "map_labels.csv", "all_maps.nii.gz")
            };
            foreach (string size in new[] { "64", "128", "256", "512", "1024" })
                definitions.Add(new Definition("difumo:" + size, "DiFuMo/" + size, p => size switch { "64" => p.PreloadDiFuMo64, "128" => p.PreloadDiFuMo128, "256" => p.PreloadDiFuMo256, "512" => p.PreloadDiFuMo512, _ => p.PreloadDiFuMo1024 }, "labels_" + size + "_dictionary.csv", "3mm/maps.nii.gz"));
            foreach (string name in new[] { "AUDI", "LEC1", "LEC2", "MCSE", "MOTO", "MVEB", "MVIS", "VISU" })
                definitions.Add(new Definition("localizer:" + name, "Localizers/" + name, p => name switch { "AUDI" => p.PreloadLocalizerAUDI, "LEC1" => p.PreloadLocalizerLEC1, "LEC2" => p.PreloadLocalizerLEC2, "MCSE" => p.PreloadLocalizerMCSE, "MOTO" => p.PreloadLocalizerMOTO, "MVEB" => p.PreloadLocalizerMVEB, "MVIS" => p.PreloadLocalizerMVIS, _ => p.PreloadLocalizerVISU }));
            return definitions.ToArray();
        }

        public static Definition Find(string id) => s_Definitions.FirstOrDefault(d => d.Id == id) ?? throw new ArgumentException("Unknown atlas: " + id);

        public static bool IsLoaded(string id) =>
            id switch
            {
                "mars" => Object3DManager.MarsAtlas.Loaded,
                "jubrain" => Object3DManager.JuBrain.Loaded,
                "ibc" => Object3DManager.IBC.Loaded,
                _ => id.StartsWith("difumo:", StringComparison.Ordinal) ? Object3DManager.DiFuMo.IsLoaded(id.Substring(7)) : Object3DManager.Localizers.Protocols.Any(p => "localizer:" + p.Name == id && p.CompleteInstallation && p.Datas.Count > 0 && p.Loaded)
            };

        private static object LoadedIdentity(string id) => id switch
        {
            "mars" => Object3DManager.MarsAtlas,
            "jubrain" => Object3DManager.JuBrain,
            "ibc" => Object3DManager.IBC,
            _ => id.StartsWith("difumo:", StringComparison.Ordinal) ? Object3DManager.DiFuMo.FMRIs.GetValueOrDefault(id.Substring(7)) : Object3DManager.Localizers.Protocols.FirstOrDefault(p => "localizer:" + p.Name == id)
        };

        public static AtlasLoadResult Status(string id) => s_Loads.ContainsKey(id) ? new(id, AtlasLoadState.Loading) : IsLoaded(id) ? new(id, AtlasLoadState.Loaded, s_Results.TryGetValue(id, out var loaded) && loaded.State == AtlasLoadState.Loaded && s_LoadedIdentities.TryGetValue(id, out var identity) && ReferenceEquals(identity.Identity, LoadedIdentity(id)) ? identity.Fingerprint : null) : s_Results.TryGetValue(id, out var result) && !result.Succeeded ? result : new(id, AtlasLoadState.Unloaded);

        public static string Fingerprint(string id, string root = null)
        {
            var definition = Find(id);
            root ??= ApplicationState.DataPath;
            string folder = StandardData.Resolve(root, "Atlases/" + definition.Directory);
            if (!System.IO.Directory.Exists(folder)) throw new FileNotFoundException("Atlas is not installed: " + id);
            string[] files = definition.RequiredFiles.Length > 0 ? definition.RequiredFiles.Select(file => StandardData.Resolve(root, "Atlases/" + definition.Directory + "/" + file)).ToArray() : System.IO.Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Where(file => !file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)).OrderBy(file => file, StringComparer.Ordinal).ToArray();
            if (files.Length == 0 || definition.RequiredFiles.Length == 0 && !files.Any(file => LocalizersHelpers.NiftiExtensions.Any(extension => file.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))) throw new FileNotFoundException("Atlas has no volumes: " + id);
            var content = new StringBuilder(id).Append('\n');
            foreach (string file in files)
            {
                content.Append(file.Substring(folder.Length).Replace('\\', '/')).Append(':').Append(StandardData.HashFile(file)).Append('\n');
                if (StandardData.CompanionFile(file) is string companion) content.Append(Path.GetFileName(companion)).Append(':').Append(StandardData.HashFile(companion)).Append('\n');
            }

            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(content.ToString()))).Replace("-", "").ToLowerInvariant();
        }

        public static async UniTask<AtlasLoadResult> LoadAsync(string id, CancellationToken token = default)
        {
            await UniTask.SwitchToMainThread();
            Find(id);
            if (!s_Loads.TryGetValue(id, out var work))
            {
                work = new LoadOperation();
                var operation = work;
                work.Work = UniTask.Lazy(() => LoadCoreAsync(id, operation));
                s_Loads.Add(id, work);
            }

            work.Consumers.Add(token);
            // A cancelled caller must still observe native completion; it never frees in-flight input.
            var result = await work.Work.Task;
            await UniTask.SwitchToMainThread();
            work.Consumers.Remove(token);
            return token.IsCancellationRequested ? new(id, AtlasLoadState.Cancelled, error: "Atlas request cancelled.") : result;
        }

        private static async UniTask<AtlasLoadResult> LoadCoreAsync(string id, LoadOperation operation)
        {
            AtlasLoadResult result;
            IDisposable usage = null;
            try
            {
                usage = ResourceRetention.Retain(id);
                operation.CheckCancellation();
                Changed?.Invoke(new(id, AtlasLoadState.Loading));
                string root = ApplicationState.DataPath;
                if (IsLoaded(id) && (!s_LoadedIdentities.TryGetValue(id, out var identity) || !ReferenceEquals(identity.Identity, LoadedIdentity(id)))) throw new IOException("Loaded atlas has no verified resource identity. Unload and reload it: " + id);
                string fingerprint = await UniTask.RunOnThreadPool(() => Fingerprint(id, root));
                await UniTask.SwitchToMainThread();
                if (IsLoaded(id) && s_LoadedIdentities[id].Fingerprint != fingerprint) throw new IOException("Installed atlas files differ from the loaded resource: " + id);
                if (!IsLoaded(id)) await PrepareAsync(id, root, fingerprint, operation);
                if (!IsLoaded(id)) throw new InvalidDataException("Atlas could not be loaded: " + id);
                result = new(id, AtlasLoadState.Loaded, fingerprint);
                s_LoadedIdentities[id] = (LoadedIdentity(id), fingerprint);
            }
            catch (Exception exception)
            {
                await UniTask.SwitchToMainThread();
                result = new(id, exception is OperationCanceledException ? AtlasLoadState.Cancelled : AtlasLoadState.Failed, error: exception.Message);
            }
            finally
            {
                usage?.Dispose();
            }

            await UniTask.SwitchToMainThread();
            s_Loads.Remove(id);
            s_Results[id] = result;
            Changed?.Invoke(result);
            return result;
        }

        private static async UniTask VerifyBeforePublishAsync(string id, string root, string fingerprint, LoadOperation operation)
        {
            string after = await UniTask.RunOnThreadPool(() => Fingerprint(id, root));
            await UniTask.SwitchToMainThread();
            operation.CheckCancellation();
            if (after != fingerprint) throw new IOException("Atlas files changed during loading: " + id);
        }

        private static async UniTask PrepareAsync(string id, string root, string fingerprint, LoadOperation operation)
        {
            if (id == "mars")
            {
                var candidate = new MarsAtlas();
                try
                {
                    await UniTask.RunOnThreadPool(candidate.Load);
                    await VerifyBeforePublishAsync(id, root, fingerprint, operation);
                    if (!candidate.Loaded) throw new InvalidDataException("MarsAtlas loading failed.");
                    Object3DManager.MarsAtlas.Dispose();
                    Object3DManager.MarsAtlas = candidate;
                }
                catch
                {
                    candidate.Dispose();
                    throw;
                }
            }
            else if (id == "jubrain")
            {
                var candidate = new JuBrainAtlas();
                try
                {
                    await UniTask.RunOnThreadPool(candidate.Load);
                    await VerifyBeforePublishAsync(id, root, fingerprint, operation);
                    if (!candidate.Loaded) throw new InvalidDataException("JuBrain loading failed.");
                    Object3DManager.JuBrain.Dispose();
                    Object3DManager.JuBrain = candidate;
                }
                catch
                {
                    candidate.Dispose();
                    throw;
                }
            }
            else if (id == "ibc")
            {
                var candidate = new IBCObjects();
                try
                {
                    candidate.Load(false);
                    await AwaitBothAsync(candidate.FMRI.LoadAsync(), candidate.Information.LoadCompletion);
                    await VerifyBeforePublishAsync(id, root, fingerprint, operation);
                    Object3DManager.IBC.Clean();
                    Object3DManager.IBC = candidate;
                }
                catch
                {
                    candidate.Clean();
                    throw;
                }
            }
            else if (id.StartsWith("difumo:", StringComparison.Ordinal))
            {
                string size = id.Substring(7);
                var candidate = new DiFuMoObjects();
                try
                {
                    candidate.Load(size, false);
                    await AwaitBothAsync(candidate.FMRIs[size].LoadAsync(), candidate.Information[size].LoadCompletion);
                    await VerifyBeforePublishAsync(id, root, fingerprint, operation);
                    Object3DManager.DiFuMo.FMRIs.Add(size, candidate.FMRIs[size]);
                    Object3DManager.DiFuMo.Information.Add(size, candidate.Information[size]);
                }
                catch
                {
                    candidate.Clean();
                    throw;
                }
            }
            else
            {
                string name = id.Substring(10);
                if (Object3DManager.Localizers.Protocols.Any(protocol => protocol.Name == name)) throw new InvalidOperationException("A graph is using a partial localizer installation. Finish the job and unload the protocol before loading its complete atlas.");
                var candidate = new LocalizerProtocol(name, Path.Combine(ApplicationState.DataPath, "Atlases", "Localizers", name), loadInBackground: false);
                try
                {
                    if (candidate.Datas.Count == 0) throw new InvalidDataException("Localizer has no data: " + name);
                    foreach (var data in candidate.Datas)
                    foreach (var bloc in data.Blocs)
                        await bloc.FMRI.LoadAsync();
                    await VerifyBeforePublishAsync(id, root, fingerprint, operation);
                    if (Object3DManager.Localizers.Protocols.Any(protocol => protocol.Name == name)) throw new InvalidOperationException("A localizer graph started during loading. Finish the job and retry the atlas load.");
                    Object3DManager.Localizers.Protocols.Add(candidate);
                }
                catch
                {
                    candidate.Clean();
                    throw;
                }
            }
        }

        private static async UniTask AwaitBothAsync(UniTask first, UniTask second)
        {
            Exception error = null;
            try
            {
                await first;
            }
            catch (Exception exception)
            {
                error = exception;
            }

            try
            {
                await second;
            }
            catch (Exception exception)
            {
                error ??= exception;
            }

            if (error != null) throw error;
        }

        public static AtlasLoadResult Unload(string id, ResourceRetention.Reservation reservation = null)
        {
            Find(id);

            void Release()
            {
                switch (id)
                {
                    case "mars": Object3DManager.UnloadMarsAtlas(); break;
                    case "jubrain": Object3DManager.UnloadJuBrain(); break;
                    case "ibc": Object3DManager.UnloadIBC(); break;
                    default:
                        if (id.StartsWith("difumo:", StringComparison.Ordinal)) Object3DManager.UnloadDiFuMo(id.Substring(7));
                        else Object3DManager.UnloadLocalizer(id.Substring(10));
                        break;
                }
            }

            if (reservation != null) reservation.Release(Release);
            else
            {
                using var owned = ResourceRetention.ReserveRelease(id);
                owned.Release(Release);
            }

            var result = new AtlasLoadResult(id, AtlasLoadState.Unloaded);
            s_LoadedIdentities.Remove(id);
            s_Results[id] = result;
            Changed?.Invoke(result);
            return result;
        }

        public static async UniTask PreloadAsync(AtlasesPreferences preferences, CancellationToken token = default)
        {
            foreach (var definition in s_Definitions)
            {
                token.ThrowIfCancellationRequested();
                if (!definition.Preload(preferences)) continue;
                var result = await LoadAsync(definition.Id, token);
                if (!result.Succeeded) UnityEngine.Debug.LogWarning("Atlas " + definition.Id + ": " + result.Error);
            }
        }
    }
}
