using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Transfer.Transport;

namespace HBP.Sync.Scene
{
    public sealed class SessionAtlasInventory
    {
        public bool Preparing;
        public Dictionary<string, SessionAtlasState> Atlases = new(StringComparer.Ordinal);
    }

    public sealed class SessionAtlasState
    {
        public AtlasLoadState State;
        public string Error;
        public bool Preload;
    }

    /// <summary>One globals context owns its silent preloads, independently of the transferring socket.</summary>
    public sealed class SessionAtlasPreparation
    {
        private readonly CancellationTokenSource stop = new();
        private readonly HashSet<string> requested;
        private readonly Dictionary<string, SessionAtlasState> results = new(StringComparer.Ordinal);
        private readonly Func<Task> installed;
        private readonly Func<string, CancellationToken, Task<AtlasLoadResult>> load;
        private readonly Func<string, AtlasLoadResult> status;
        private Task running = Task.CompletedTask;
        private bool preparing;
        public Task Completion => running;

        public SessionAtlasPreparation(AtlasesPreferences preferences, Func<Task> installed, Func<string, CancellationToken, Task<AtlasLoadResult>> load = null, Func<string, AtlasLoadResult> status = null)
        {
            requested = AtlasResources.Definitions.Where(d => d.Preload(preferences)).Select(d => d.Id).ToHashSet(StringComparer.Ordinal);
            this.installed = installed;
            this.load = load ?? ((id, token) => AtlasResources.LoadAsync(id, token).AsTask());
            this.status = status ?? AtlasResources.Status;
        }

        public void Start()
        {
            if (preparing || stop.IsCancellationRequested || results.Count != 0) return;
            preparing = true;
            running = RunAsync();
        }

        private async Task RunAsync()
        {
            using var timing = PairingTiming.Measure("quest.atlas-preparation");
            try
            {
                // Only this context's wait is cancelled; the application owns and observes installation.
                await installed().AsUniTask().AttachExternalCancellation(stop.Token);
                await UniTask.SwitchToMainThread();
                foreach (string id in requested)
                {
                    stop.Token.ThrowIfCancellationRequested();
                    var result = await load(id, stop.Token);
                    await UniTask.SwitchToMainThread();
                    results[id] = new() { State = result.State, Error = result.Error, Preload = true };
                }
            }
            catch (Exception exception)
            {
                await UniTask.SwitchToMainThread();
                foreach (string id in requested.Where(id => !results.ContainsKey(id)))
                    results[id] = new() { State = stop.IsCancellationRequested ? AtlasLoadState.Cancelled : AtlasLoadState.Failed, Error = exception.Message, Preload = true };
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                preparing = false;
            }
        }

        public SessionAtlasInventory Snapshot()
        {
            var inventory = new SessionAtlasInventory { Preparing = preparing };
            foreach (var definition in AtlasResources.Definitions)
            {
                string id = definition.Id;
                var resource = status(id);
                var state = new SessionAtlasState { State = resource.State, Error = resource.Error, Preload = requested.Contains(id) };
                if (state.Preload && results.TryGetValue(id, out var result) && resource.State == AtlasLoadState.Unloaded && (result.State == AtlasLoadState.Failed || result.State == AtlasLoadState.Cancelled)) state = result;
                else if (state.Preload && preparing && !results.ContainsKey(id) && resource.State == AtlasLoadState.Unloaded) state.State = AtlasLoadState.Loading;
                inventory.Atlases[id] = state;
            }

            return inventory;
        }

        public async Task CloseAsync()
        {
            stop.Cancel();
            await running;
        }
    }
}
