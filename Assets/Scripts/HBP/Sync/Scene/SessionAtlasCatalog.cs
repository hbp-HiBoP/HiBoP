using System;
using System.Collections.Generic;
using System.IO;
using HBP.Core.Object3D;
using HBP.Core.Tools;

namespace HBP.Sync.Scene
{
    /// <summary>Availability agreed by both peers; it does not change a scene's delivered resource identities.</summary>
    public static class SessionAtlasCatalog
    {
        private static readonly Dictionary<string, string> s_Ready = new(StringComparer.Ordinal);
        public static bool Active { get; private set; }
        public static event Action Changed;

        public static void Begin()
        {
            s_Ready.Clear();
            Active = true;
            Changed?.Invoke();
        }

        public static void End()
        {
            s_Ready.Clear();
            Active = false;
            Changed?.Invoke();
        }

        public static void SetReady(string id, string fingerprint)
        {
            if (fingerprint == null || fingerprint.Length != 64 || !AtlasResources.IsLoaded(id)) throw new InvalidDataException("Atlas is not ready: " + id);
            s_Ready[id] = fingerprint;
            Changed?.Invoke();
        }

        public static void Remove(string id)
        {
            s_Ready.Remove(id);
            Changed?.Invoke();
        }

        public static bool CanUse(string id) => AtlasResources.IsLoaded(id) && (!Active || s_Ready.ContainsKey(id));

        public static void Require(string id)
        {
            ResourceRetention.EnsureCanUse(id);
            if (!CanUse(id)) throw new InvalidOperationException("The atlas is not ready on both devices: " + id);
        }

        public static string Reference(string id)
        {
            Require(id);
            string fingerprint = Active ? s_Ready[id] : AtlasResources.Status(id).Fingerprint;
            // Legacy/local tests and resources prepared before a session have no coordinator result yet.
            if (fingerprint == null) fingerprint = AtlasResources.Fingerprint(id);
            return id + "@" + fingerprint;
        }

        public static string Resolve(string reference)
        {
            int separator = reference?.LastIndexOf('@') ?? -1;
            if (separator < 1) throw new InvalidDataException("Missing atlas content identity.");
            string id = reference.Substring(0, separator);
            if (Reference(id) != reference) throw new InvalidDataException("Atlas content identity differs between devices.");
            return id;
        }
    }
}
