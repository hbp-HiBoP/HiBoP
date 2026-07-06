using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HBP.Core.DLL;
using HBP.Core.DLL.HbpCore;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HBP.Tests.Serialization
{
    public class NativeMigrationBaselineTests
    {
        private static readonly Regex DllImportRegex = new(
            "\\[DllImport\\((?:\"(?<dll>[^\"]+)\"|NativeDll\\.(?<nativeDll>HbpExport|HbpCore))\\s*,\\s*EntryPoint\\s*=\\s*\"(?<entry>[^\"]+)\"",
            RegexOptions.Compiled);

        [Test]
        [Category("NativeMigration")]
        public void NativeBackendConstants_DeclareHistoricalAndCoreDllNames()
        {
            Assert.That(NativeDll.HbpExport, Is.EqualTo("hbp_export"));
            Assert.That(NativeDll.HbpCore, Is.EqualTo("hbp_core"));
            Assert.That(NativeBackend.HbpExport.ToString(), Is.EqualTo("HbpExport"));
            Assert.That(NativeBackend.HbpCore.ToString(), Is.EqualTo("HbpCore"));
            Assert.That(NativeBackendOptions.ExperimentalBackend, Is.EqualTo(NativeBackend.HbpExport));
            Assert.That(NativeBackendOptions.UsesHbpCore, Is.False);
        }

        [Test]
        [Category("NativeMigration")]
        public void CurrentDllImportInventory_KeepsHistoricalImportsAndAddsOnlyHbpCoreSmokeWrapper()
        {
            List<DllImportSignature> imports = ReadCurrentDllImports();

            Assert.That(imports, Has.Count.EqualTo(280));
            Assert.That(imports.Count(imported => imported.Dll == NativeDll.HbpExport), Is.EqualTo(219));
            Assert.That(imports.Count(imported => imported.Dll == "EEGFormat"), Is.EqualTo(37));
            Assert.That(imports.Count(imported => imported.Dll == "hbp_math"), Is.EqualTo(17));
            string[] hbpCoreImportFiles = imports
                .Where(imported => imported.Dll == NativeDll.HbpCore)
                .Select(imported => imported.RelativeFile)
                .Distinct()
                .ToArray();
            Assert.That(hbpCoreImportFiles, Is.EquivalentTo(new[] { "HbpCore/HbpCoreRuntime.cs" }));
            Assert.That(imports.Count(imported => imported.Dll == NativeDll.HbpCore), Is.EqualTo(7));
        }

        [Test]
        [Category("NativeMigration")]
        [Category("NativeDll")]
        public void HistoricalWrapper_LoadsThroughHbpExportWithoutHbpCoreMigration()
        {
            BBox bbox = ExecuteNativeOrIgnore(() => new BBox(), "historical BBox wrapper");
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
        public void ExperimentalBackendOption_DoesNotMoveHistoricalWrappers()
        {
            NativeBackendOptions.ExperimentalBackend = NativeBackend.HbpCore;
            try
            {
                BBox bbox = ExecuteNativeOrIgnore(() => new BBox(), "historical BBox wrapper");
                try
                {
                    Assert.That(NativeBackendOptions.UsesHbpCore, Is.True);
                    Assert.That(bbox.getHandle().Handle, Is.Not.EqualTo(IntPtr.Zero));
                }
                finally
                {
                    bbox.Dispose();
                }
            }
            finally
            {
                NativeBackendOptions.Reset();
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

        private static List<DllImportSignature> ReadCurrentDllImports()
        {
            string dllFolder = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Scripts", "HBP", "Core", "DLL");
            return Directory
                .GetFiles(dllFolder, "*.cs", SearchOption.AllDirectories)
                .SelectMany(ReadDllImportsFromFile)
                .OrderBy(imported => imported.RelativeFile, StringComparer.Ordinal)
                .ThenBy(imported => imported.Entry, StringComparer.Ordinal)
                .ToList();
        }

        private static IEnumerable<DllImportSignature> ReadDllImportsFromFile(string file)
        {
            string dllFolder = Path.Combine(TestPathUtility.ProjectRoot, "Assets", "Scripts", "HBP", "Core", "DLL");
            string relativeFile = file.Substring(dllFolder.Length).TrimStart('\\', '/').Replace('\\', '/');

            foreach (Match match in DllImportRegex.Matches(File.ReadAllText(file)))
            {
                string dll = match.Groups["dll"].Success
                    ? match.Groups["dll"].Value
                    : NativeDllName(match.Groups["nativeDll"].Value);

                yield return new DllImportSignature(
                    dll,
                    match.Groups["entry"].Value,
                    relativeFile);
            }
        }

        private static string NativeDllName(string nativeDllConstant)
        {
            return nativeDllConstant switch
            {
                nameof(NativeBackend.HbpExport) => NativeDll.HbpExport,
                nameof(NativeBackend.HbpCore) => NativeDll.HbpCore,
                _ => throw new InvalidOperationException($"Unknown NativeDll constant: {nativeDllConstant}")
            };
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
