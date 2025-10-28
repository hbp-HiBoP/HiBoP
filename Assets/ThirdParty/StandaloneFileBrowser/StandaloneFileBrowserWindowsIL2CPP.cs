#if UNITY_STANDALONE_WIN && !UNITY_EDITOR && ENABLE_IL2CPP
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ThirdParty.SFB
{
    // Flags Win32 OPENFILENAME
    [Flags]
    enum OFN : uint
    {
        READONLY = 0x00000001,
        OVERWRITEPROMPT = 0x00000002,
        HIDEREADONLY = 0x00000004,
        NOCHANGEDIR = 0x00000008,
        ALLOWMULTISELECT = 0x00000200,
        EXPLORER = 0x00080000,
        FILEMUSTEXIST = 0x00001000,
        PATHMUSTEXIST = 0x00000800,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct OPENFILENAME
    {
        public int lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public string lpstrFilter;
        public string lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public IntPtr lpstrFile;       // buffer pointer
        public int nMaxFile;
        public IntPtr lpstrFileTitle;
        public int nMaxFileTitle;
        public string lpstrInitialDir;
        public string lpstrTitle;
        public OFN Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public string lpTemplateName;
        public IntPtr pvReserved;
        public int dwReserved;
        public int FlagsEx;
    }

    static class Native
    {
        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetOpenFileName(ref OPENFILENAME ofn);

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetSaveFileName(ref OPENFILENAME ofn);

        [DllImport("user32.dll")]
        public static extern IntPtr GetActiveWindow();
    }

    public class StandaloneFileBrowserWindows : IStandaloneFileBrowser
    {
        // ==== Public API ====

        public string[] OpenFilePanel(string title, string directory, ExtensionFilter[] extensions, bool multiselect)
        {
            var filter = BuildFilter(extensions);
            var buffer = Marshal.AllocHGlobal(65536 * sizeof(char)); // 64K chars
            try
            {
                ZeroBuffer(buffer, 65536);

                var ofn = NewOfn(title, directory, filter, buffer, 65536,
                    (OFN.EXPLORER | OFN.FILEMUSTEXIST | OFN.PATHMUSTEXIST) |
                    (multiselect ? OFN.ALLOWMULTISELECT : 0));

                if (!Native.GetOpenFileName(ref ofn))
                    return Array.Empty<string>();

                return ParseSelectedFiles(buffer, ofn.nMaxFile, multiselect);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public void OpenFilePanelAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<string[]> cb)
            => cb?.Invoke(OpenFilePanel(title, directory, extensions, multiselect));

        public string[] OpenFolderPanel(string title, string directory, bool multiselect)
        {
            // On reste sur OpenFile dialog avec filtre dossiers via PATHMUSTEXIST
            // Pour un vrai folder picker natif, il faut IFileDialog (COM). Version simple :
            return OpenFilePanel(title, directory, null, multiselect);
        }

        public void OpenFolderPanelAsync(string title, string directory, bool multiselect, Action<string[]> cb)
            => cb?.Invoke(OpenFolderPanel(title, directory, multiselect));

        public string SaveFilePanel(string title, string directory, string defaultName, ExtensionFilter[] extensions)
        {
            var filter = BuildFilter(extensions);
            var buffer = Marshal.AllocHGlobal(65536 * sizeof(char));
            try
            {
                ZeroBuffer(buffer, 65536);
                // Préremplir nom par défaut
                if (!string.IsNullOrEmpty(defaultName))
                {
                    var preset = Path.Combine(string.IsNullOrEmpty(directory) ? "" : directory, defaultName);
                    WriteStringToBuffer(buffer, preset);
                }

                var ofn = NewOfn(title, directory, filter, buffer, 65536,
                    OFN.EXPLORER | OFN.OVERWRITEPROMPT | OFN.PATHMUSTEXIST | OFN.NOCHANGEDIR);

                // Défaut d’extension
                ofn.lpstrDefExt = FirstExtensionOrEmpty(extensions);

                if (!Native.GetSaveFileName(ref ofn))
                    return string.Empty;

                return Marshal.PtrToStringUni(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public void SaveFilePanelAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb)
            => cb?.Invoke(SaveFilePanel(title, directory, defaultName, extensions));

        // ==== Helpers ====

        static OPENFILENAME NewOfn(string title, string directory, string filter, IntPtr buffer, int maxChars, OFN flags)
        {
            return new OPENFILENAME
            {
                lStructSize = Marshal.SizeOf(typeof(OPENFILENAME)),
                hwndOwner = Native.GetActiveWindow(),
                lpstrTitle = title ?? string.Empty,
                lpstrInitialDir = string.IsNullOrEmpty(directory) ? null : GetDirectoryPath(directory),
                lpstrFilter = string.IsNullOrEmpty(filter) ? "All Files\0*.*\0\0" : filter,
                nFilterIndex = 1,
                lpstrFile = buffer,
                nMaxFile = maxChars,
                Flags = flags | OFN.HIDEREADONLY | OFN.NOCHANGEDIR
            };
        }

        static string[] ParseSelectedFiles(IntPtr buffer, int maxChars, bool multiselect)
        {
            var s = Marshal.PtrToStringUni(buffer, maxChars).TrimEnd('\0');
            if (string.IsNullOrEmpty(s)) return Array.Empty<string>();

            var parts = s.Split('\0', StringSplitOptions.RemoveEmptyEntries);
            if (!multiselect || parts.Length == 1)
                return new[] { parts[0] };

            // Format multi : [dir]\0[file1]\0[file2]\0...\0\0
            var dir = parts[0];
            var files = new string[parts.Length - 1];
            for (int i = 1; i < parts.Length; ++i)
                files[i - 1] = Path.Combine(dir, parts[i]);
            return files;
        }

        static string BuildFilter(ExtensionFilter[] exts)
        {
            if (exts == null || exts.Length == 0)
                return "All Files\0*.*\0\0";

            // Format Win32 : "Text files (*.txt)\0*.txt\0PNG (*.png)\0*.png;*.apng\0\0"
            var writer = new System.Text.StringBuilder();
            foreach (var f in exts)
            {
                writer.Append(f.Name);
                writer.Append(" (");
                for (int i = 0; i < f.Extensions.Length; i++)
                {
                    writer.Append("*.");
                    writer.Append(f.Extensions[i]);
                    if (i < f.Extensions.Length - 1) writer.Append(", ");
                }
                writer.Append(")");
                writer.Append('\0');

                for (int i = 0; i < f.Extensions.Length; i++)
                {
                    writer.Append("*.");
                    writer.Append(f.Extensions[i]);
                    if (i < f.Extensions.Length - 1) writer.Append(';');
                }
                writer.Append('\0');
            }
            writer.Append('\0'); // fin double NUL
            return writer.ToString();
        }

        static string FirstExtensionOrEmpty(ExtensionFilter[] exts)
            => (exts != null && exts.Length > 0 && exts[0].Extensions != null && exts[0].Extensions.Length > 0)
                ? exts[0].Extensions[0] : string.Empty;

        static string GetDirectoryPath(string directory)
        {
            try
            {
                var p = Path.GetFullPath(string.IsNullOrEmpty(directory) ? "." : directory);
                if (Directory.Exists(p)) return p;
                var d = Path.GetDirectoryName(p);
                return string.IsNullOrEmpty(d) ? null : d;
            }
            catch { return null; }
        }

        static unsafe void ZeroBuffer(IntPtr ptr, int charCount)
        {
            Span<char> span = new Span<char>(ptr.ToPointer(), charCount);
            span.Clear();
        }

        static unsafe void WriteStringToBuffer(IntPtr ptr, string s)
        {
            var chars = s.AsSpan();
            Span<char> span = new Span<char>(ptr.ToPointer(), chars.Length + 2);
            chars.CopyTo(span);
            span[chars.Length] = '\0';
        }
    }
}
#endif
