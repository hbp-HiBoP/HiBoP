using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;

namespace HBP.Quest
{
#if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(UiName = "HiBoP Quest Controller Render Models", Desc = "Loads controller geometry from the Quest OpenXR runtime.", Company = "HiBoP", Version = "1.0.0", BuildTargetGroups = new[] { UnityEditor.BuildTargetGroup.Android }, OpenxrExtensionStrings = Extension, FeatureId = "fr.crnl.hibop.quest.controller-render-models")]
#endif
    public sealed class QuestControllerRenderModels : OpenXRFeature
    {
        private const string Extension = "XR_FB_render_model";
        private const int MaxModelBytes = 32 * 1024 * 1024;
        private ulong instance, session;
        private ulong[] paths;
        private EnumeratePaths enumeratePaths;
        private StringToPath stringToPath;
        private GetProperties getProperties;
        private LoadModel loadModel;
        private bool reportedFailure;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult GetProcAddr(ulong instance, [MarshalAs(UnmanagedType.LPStr)] string name, out IntPtr function);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult EnumeratePaths(ulong session, uint capacity, out uint count, IntPtr paths);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult StringToPath(ulong instance, [MarshalAs(UnmanagedType.LPStr)] string text, out ulong path);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult GetProperties(ulong session, ulong path, IntPtr properties);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult LoadModel(ulong session, ref ModelLoadInfo info, ref ModelBuffer buffer);

        [StructLayout(LayoutKind.Sequential)]
        private struct ModelPath
        {
            public XrStructureType Type;
            public IntPtr Next;
            public ulong Path;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ModelCapabilities
        {
            public XrStructureType Type;
            public IntPtr Next;
            public ulong Flags;
        }

        // Uses an inline 64-byte name with the same ARM64 alignment as the native char array.
        [StructLayout(LayoutKind.Sequential, Size = 64)]
        private struct ModelName
        {
            private uint alignment;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ModelProperties
        {
            public XrStructureType Type;
            public IntPtr Next;
            public uint Vendor;
            public ModelName Name;
            public ulong Key;
            public uint Version;
            public ulong Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ModelLoadInfo
        {
            public XrStructureType Type;
            public IntPtr Next;
            public ulong Key;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ModelBuffer
        {
            public XrStructureType Type;
            public IntPtr Next;
            public uint Capacity;
            public uint Count;
            public IntPtr Data;
        }

        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            if (!OpenXRRuntime.IsExtensionEnabled(Extension) || xrGetInstanceProcAddr == IntPtr.Zero)
            {
                Debug.LogWarning($"[Quest controller models] {Extension} is unavailable.");
                return false;
            }

            instance = xrInstance;
            var resolve = Marshal.GetDelegateForFunctionPointer<GetProcAddr>(xrGetInstanceProcAddr);
            enumeratePaths = Resolve<EnumeratePaths>(resolve, "xrEnumerateRenderModelPathsFB");
            stringToPath = Resolve<StringToPath>(resolve, "xrStringToPath");
            getProperties = Resolve<GetProperties>(resolve, "xrGetRenderModelPropertiesFB");
            loadModel = Resolve<LoadModel>(resolve, "xrLoadRenderModelFB");
            bool ready = enumeratePaths != null && stringToPath != null && getProperties != null && loadModel != null;
            if (ready) Debug.Log($"[Quest controller models] {Extension} enabled.");
            return ready;
        }

        protected override void OnSessionBegin(ulong xrSession)
        {
            session = xrSession;
            paths = null;
            reportedFailure = false;
        }

        protected override void OnSessionEnd(ulong xrSession) => session = 0;
        protected override void OnSessionDestroy(ulong xrSession) => session = 0;

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            instance = session = 0;
            paths = null;
        }

        private T Resolve<T>(GetProcAddr resolve, string name) where T : Delegate
        {
            var result = resolve(instance, name, out var function);
            return Check(result, name) && function != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<T>(function) : null;
        }

        internal bool TryLoad(bool left, out byte[] bytes, out string identity)
        {
            bytes = null;
            identity = null;
            if (session == 0) return false;
            paths ??= EnumerateControllerPaths();
            ulong path = paths?[left ? 0 : 1] ?? 0;
            if (path == 0) return false;
            if (!TryGetProperties(path, out ulong key, out identity)) return false;

            var info = new ModelLoadInfo { Type = XrStructureType.RenderModelLoadInfoFB, Key = key };
            var buffer = new ModelBuffer { Type = XrStructureType.RenderModelBufferFB };
            if (!Check(loadModel(session, ref info, ref buffer), "count model bytes") || buffer.Count == 0 || buffer.Count > MaxModelBytes)
                return false;

            var data = new byte[buffer.Count];
            var pin = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                buffer.Capacity = buffer.Count;
                buffer.Data = pin.AddrOfPinnedObject();
                if (!Check(loadModel(session, ref info, ref buffer), "load model GLB") || buffer.Count != data.Length)
                    return false;
            }
            finally
            {
                pin.Free();
            }

            bytes = data;
            Debug.Log($"[Quest controller models] Loaded {(left ? "left" : "right")} {identity}, {data.Length} bytes.");
            return true;
        }

        private ulong[] EnumerateControllerPaths()
        {
            if (!Check(enumeratePaths(session, 0, out uint count, IntPtr.Zero), "count model paths") || count == 0 || count > 128)
                return null;
            var available = new ModelPath[count];
            for (int index = 0; index < available.Length; index++) available[index].Type = XrStructureType.RenderModelPathInfoFB;
            var pin = GCHandle.Alloc(available, GCHandleType.Pinned);
            try
            {
                if (!Check(enumeratePaths(session, count, out count, pin.AddrOfPinnedObject()), "enumerate model paths") || count > available.Length)
                    return null;
            }
            finally
            {
                pin.Free();
            }

            var result = new ulong[2];
            for (int hand = 0; hand < result.Length; hand++)
            {
                string name = hand == 0 ? "/model_fb/controller/left" : "/model_fb/controller/right";
                if (!Check(stringToPath(instance, name, out ulong candidate), name)) continue;
                for (int i = 0; i < count; i++)
                    if (available[i].Path == candidate)
                        result[hand] = candidate;
            }

            Debug.Log($"[Quest controller models] Controller paths available. Left: {result[0] != 0}; right: {result[1] != 0}.");
            return result;
        }

        private bool TryGetProperties(ulong path, out ulong key, out string identity)
        {
            key = 0;
            identity = null;
            var capabilities = new ModelCapabilities
            {
                Type = XrStructureType.RenderModelCapabilitiesRequestFB,
                Flags = 3 // glTF subsets 1 and 2. Only geometry is read.
            };
            IntPtr capabilitiesPtr = Marshal.AllocHGlobal(Marshal.SizeOf<ModelCapabilities>());
            IntPtr propertiesPtr = IntPtr.Zero;
            try
            {
                propertiesPtr = Marshal.AllocHGlobal(Marshal.SizeOf<ModelProperties>());
                Marshal.StructureToPtr(capabilities, capabilitiesPtr, false);
                var input = new ModelProperties { Type = XrStructureType.RenderModelPropertiesFB, Next = capabilitiesPtr };
                Marshal.StructureToPtr(input, propertiesPtr, false);
                var result = getProperties(session, path, propertiesPtr);
                if (result == XrResult.RenderModelUnavailableFB || !Check(result, "get model properties")) return false;
                var properties = Marshal.PtrToStructure<ModelProperties>(propertiesPtr);
                if (properties.Key == 0) return false;
                int nameOffset = Marshal.OffsetOf<ModelProperties>(nameof(ModelProperties.Name)).ToInt32();
                var name = new byte[64];
                Marshal.Copy(IntPtr.Add(propertiesPtr, nameOffset), name, 0, name.Length);
                int length = Array.IndexOf(name, (byte)0);
                identity = $"{System.Text.Encoding.UTF8.GetString(name, 0, length < 0 ? name.Length : length)} ({properties.Vendor}, version {properties.Version})";
                key = properties.Key;
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(propertiesPtr);
                Marshal.FreeHGlobal(capabilitiesPtr);
            }
        }

        private bool Check(XrResult result, string operation)
        {
            if (result == XrResult.Success) return true;
            if (!reportedFailure) Debug.LogWarning($"[Quest controller models] Could not {operation}: {result}.");
            reportedFailure = true;
            return false;
        }
    }
}
