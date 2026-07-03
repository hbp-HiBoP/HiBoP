using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HBP.Core.DLL;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class NativeMigrationBaselineTests
    {
        private static readonly Regex DllImportRegex = new(
            "\\[DllImport\\(\"(?<dll>[^\"]+)\"\\s*,\\s*EntryPoint\\s*=\\s*\"(?<entry>[^\"]+)\"",
            RegexOptions.Compiled);

        [Test]
        [Category("NativeMigration")]
        public void NativeBackendConstants_DeclareHistoricalAndCoreDllNames()
        {
            Assert.That(NativeDll.HbpExport, Is.EqualTo("hbp_export"));
            Assert.That(NativeDll.HbpCore, Is.EqualTo("hbp_core"));
            Assert.That(NativeBackend.HbpExport.ToString(), Is.EqualTo("HbpExport"));
            Assert.That(NativeBackend.HbpCore.ToString(), Is.EqualTo("HbpCore"));
        }

        [Test]
        [Category("NativeMigration")]
        public void CurrentDllImportInventory_MatchesStepZeroBaseline()
        {
            List<DllImportSignature> imports = ReadCurrentDllImports();

            Assert.That(imports, Has.Count.EqualTo(273));
            Assert.That(imports.Count(imported => imported.Dll == NativeDll.HbpExport), Is.EqualTo(219));
            Assert.That(imports.Count(imported => imported.Dll == "EEGFormat"), Is.EqualTo(37));
            Assert.That(imports.Count(imported => imported.Dll == "hbp_math"), Is.EqualTo(17));
            Assert.That(imports.Any(imported => imported.Dll == NativeDll.HbpCore), Is.False);
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
                yield return new DllImportSignature(
                    match.Groups["dll"].Value,
                    match.Groups["entry"].Value,
                    relativeFile);
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
