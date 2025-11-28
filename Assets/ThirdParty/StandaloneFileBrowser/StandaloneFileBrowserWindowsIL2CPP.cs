#if UNITY_STANDALONE_WIN && !UNITY_EDITOR && ENABLE_IL2CPP
using System;
using System.Collections.Generic;
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

    // IFileDialog COM interface for folder browser
    [Flags]
    enum FOS : uint
    {
        OVERWRITEPROMPT = 0x00000002,
        STRICTFILETYPES = 0x00000004,
        NOCHANGEDIR = 0x00000008,
        PICKFOLDERS = 0x00000020,
        FORCEFILESYSTEM = 0x00000040,
        ALLNONSTORAGEITEMS = 0x00000080,
        NOVALIDATE = 0x00000100,
        ALLOWMULTISELECT = 0x00000200,
        PATHMUSTEXIST = 0x00000800,
        FILEMUSTEXIST = 0x00001000,
        CREATEPROMPT = 0x00002000,
        SHAREAWARE = 0x00004000,
        NOREADONLYRETURN = 0x00008000,
        NOTESTFILECREATE = 0x00010000,
        HIDEMRUPLACES = 0x00020000,
        HIDEPINNEDPLACES = 0x00040000,
        NODEREFERENCELINKS = 0x00100000,
        DONTADDTORECENT = 0x02000000,
        FORCESHOWHIDDEN = 0x10000000,
        DEFAULTNOMINIMODE = 0x20000000,
        FORCEPREVIEWPANEON = 0x40000000,
    }

    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItem
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
        void GetParent(out IShellItem ppsi);
        void GetDisplayName(uint sigdnName, out IntPtr ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }

    [ComImport, Guid("b63ea76d-1f85-456f-a19c-48159efa858b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IShellItemArray
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppvOut);
        void GetPropertyStore(uint flags, ref Guid riid, out IntPtr ppv);
        void GetPropertyDescriptionList(IntPtr keyType, ref Guid riid, out IntPtr ppv);
        void GetAttributes(uint attribFlags, uint sfgaoMask, out uint psfgaoAttribs);
        void GetCount(out uint pdwNumItems);
        void GetItemAt(uint dwIndex, out IShellItem ppsi);
        void EnumItems(out IntPtr ppenumShellItems);
    }

    [ComImport, Guid("42f85136-db7e-439c-85f1-e4075d135fc8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IFileOpenDialog
    {
        // IModalWindow
        [PreserveSig] int Show(IntPtr hwndOwner);
        // IFileDialog
        void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
        void SetFileTypeIndex(uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise(IntPtr pfde, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOptions(FOS fos);
        void GetOptions(out FOS pfos);
        void SetDefaultFolder(IShellItem psi);
        void SetFolder(IShellItem psi);
        void GetFolder(out IShellItem ppsi);
        void GetCurrentSelection(out IShellItem ppsi);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName(out IntPtr pszName);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult(out IShellItem ppsi);
        void AddPlace(IShellItem psi, int fdap);
        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        void Close(int hr);
        void SetClientGuid(ref Guid guid);
        void ClearClientData();
        void SetFilter(IntPtr pFilter);
        // IFileOpenDialog
        void GetResults(out IShellItemArray ppenum);
        void GetSelectedItems(out IShellItemArray ppsai);
    }

    static class Native
    {
        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetOpenFileName(ref OPENFILENAME ofn);

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetSaveFileName(ref OPENFILENAME ofn);

        [DllImport("user32.dll")]
        public static extern IntPtr GetActiveWindow();

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            ref Guid riid,
            out IShellItem ppv);

        [DllImport("ole32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.Interface)]
        public static extern object CoCreateInstance(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
            IntPtr pUnkOuter,
            int dwClsContext,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid);

        // CLSID for FileOpenDialog
        public static readonly Guid CLSID_FileOpenDialog = new Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7");
        public static readonly Guid IID_IFileOpenDialog = new Guid("42f85136-db7e-439c-85f1-e4075d135fc8");
        public static readonly Guid IID_IShellItem = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");

        public const int CLSCTX_INPROC_SERVER = 1;
        public const int CLSCTX_ALL = 0x17; // CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER
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
            IFileOpenDialog dialog = null;
            try
            {
                // Create FileOpenDialog COM object
                dialog = (IFileOpenDialog)Native.CoCreateInstance(
                    Native.CLSID_FileOpenDialog,
                    IntPtr.Zero,
                    Native.CLSCTX_ALL,
                    Native.IID_IFileOpenDialog);

                // Set options: pick folders
                FOS options = FOS.PICKFOLDERS | FOS.FORCEFILESYSTEM | FOS.PATHMUSTEXIST;
                if (multiselect)
                    options |= FOS.ALLOWMULTISELECT;
                dialog.SetOptions(options);

                // Set title
                if (!string.IsNullOrEmpty(title))
                    dialog.SetTitle(title);

                // Set initial directory
                if (!string.IsNullOrEmpty(directory))
                {
                    var dirPath = GetDirectoryPath(directory);
                    if (!string.IsNullOrEmpty(dirPath))
                    {
                        Guid iidShellItem = Native.IID_IShellItem;
                        if (Native.SHCreateItemFromParsingName(dirPath, IntPtr.Zero, ref iidShellItem, out IShellItem folderItem) == 0)
                        {
                            dialog.SetFolder(folderItem);
                            Marshal.ReleaseComObject(folderItem);
                        }
                    }
                }

                // Show dialog
                int hr = dialog.Show(Native.GetActiveWindow());
                if (hr != 0) // User cancelled or error
                {
                    return Array.Empty<string>();
                }

                // Get results
                var results = new List<string>();
                if (multiselect)
                {
                    dialog.GetResults(out IShellItemArray itemArray);
                    itemArray.GetCount(out uint count);
                    for (uint i = 0; i < count; i++)
                    {
                        itemArray.GetItemAt(i, out IShellItem item);
                        item.GetDisplayName(0x80058000, out IntPtr pszName); // SIGDN_FILESYSPATH
                        if (pszName != IntPtr.Zero)
                        {
                            results.Add(Marshal.PtrToStringUni(pszName));
                            Marshal.FreeCoTaskMem(pszName);
                        }
                        Marshal.ReleaseComObject(item);
                    }
                    Marshal.ReleaseComObject(itemArray);
                }
                else
                {
                    dialog.GetResult(out IShellItem item);
                    item.GetDisplayName(0x80058000, out IntPtr pszName); // SIGDN_FILESYSPATH
                    if (pszName != IntPtr.Zero)
                    {
                        results.Add(Marshal.PtrToStringUni(pszName));
                        Marshal.FreeCoTaskMem(pszName);
                    }
                    Marshal.ReleaseComObject(item);
                }

                return results.ToArray();
            }
            catch (Exception)
            {
                // Fallback: return empty if COM fails
                return Array.Empty<string>();
            }
            finally
            {
                if (dialog != null)
                    Marshal.ReleaseComObject(dialog);
            }
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
                // Pr�remplir nom par d�faut
                if (!string.IsNullOrEmpty(defaultName))
                {
                    var preset = Path.Combine(string.IsNullOrEmpty(directory) ? "" : directory, defaultName);
                    WriteStringToBuffer(buffer, preset);
                }

                var ofn = NewOfn(title, directory, filter, buffer, 65536,
                    OFN.EXPLORER | OFN.OVERWRITEPROMPT | OFN.PATHMUSTEXIST | OFN.NOCHANGEDIR);

                // D�faut d�extension
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
