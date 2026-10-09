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
        private static readonly HashSet<string> s_Ready = new(StringComparer.Ordinal);
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

        public static void SetReady(string id)
        {
            if (AtlasResources.Status(id).State != AtlasLoadState.Loaded) throw new InvalidDataException("Atlas is not ready: " + id);
            s_Ready.Add(id);
            Changed?.Invoke();
        }

        public static void Remove(string id)
        {
            s_Ready.Remove(id);
            Changed?.Invoke();
        }

        public static bool CanUse(string id) => AtlasResources.IsLoaded(id) && (!Active || s_Ready.Contains(id));

        public static void Require(string id)
        {
            ResourceRetention.EnsureCanUse(id);
            if (!CanUse(id)) throw new InvalidOperationException("The atlas is not ready on both devices: " + id);
        }

        public static string Reference(string id)
        {
            Require(id);
            return id;
        }

        public static string Resolve(string reference)
        {
            if (string.IsNullOrEmpty(reference)) throw new InvalidDataException("Missing atlas reference.");
            AtlasResources.Find(reference);
            Require(reference);
            return reference;
        }
    }
}
