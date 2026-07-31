using HBP.Core.Tools;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using Cysharp.Threading.Tasks;

namespace HBP.Core.Object3D
{
    #region Shared Helpers

    internal static class LocalizersHelpers
    {
        public static readonly string[] NiftiExtensions = { ".nii", ".nii.gz", ".img" };

        public static bool IsMaskFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            string fileName = Path.GetFileName(filePath).ToUpperInvariant();
            return fileName.EndsWith("_MASK.NII") || fileName.EndsWith("_MASK.NII.GZ") || fileName.EndsWith("_MASK.IMG");
        }

        public static string GetBlocNameFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return string.Empty;
            string fileName = Path.GetFileName(filePath);
            if (fileName.EndsWith(".nii.gz", StringComparison.OrdinalIgnoreCase))
            {
                return fileName[..^7];
            }
            else if (fileName.EndsWith(".nii", StringComparison.OrdinalIgnoreCase))
            {
                return fileName[..^4];
            }
            else if (fileName.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
            {
                return fileName[..^4];
            }

            return Path.GetFileNameWithoutExtension(fileName);
        }
    }

    #endregion

    public class LocalizersObjects
    {
        #region Properties

        public List<LocalizerProtocol> Protocols { get; private set; } = new List<LocalizerProtocol>();
        public bool Loaded => Protocols.Count > 0 && Protocols.All(p => p.Loaded);
        private static string LocalizersPath => Path.Combine(ApplicationState.DataPath, "Atlases", "Localizers");

        public List<string> AvailableProtocolNames
        {
            get
            {
                string localizersPath = LocalizersPath;
                if (Directory.Exists(localizersPath))
                {
                    return Directory.GetDirectories(localizersPath).Select(Path.GetFileName).OrderBy(n => n).ToList();
                }

                return new List<string>();
            }
        }

        public List<string> AvailableDataNames
        {
            get
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string localizersPath = LocalizersPath;
                if (!Directory.Exists(localizersPath)) return new List<string>();
                foreach (var protocolDirectory in Directory.GetDirectories(localizersPath))
                {
                    foreach (var dataDirectory in Directory.GetDirectories(protocolDirectory))
                    {
                        names.Add(Path.GetFileName(dataDirectory));
                    }
                }

                return names.OrderBy(n => n).ToList();
            }
        }

        #endregion

        #region Public Methods

        public void Clean()
        {
            foreach (var protocol in Protocols)
            {
                protocol?.Clean();
            }
        }

        public bool IsAvailable(string protocol)
        {
            string protocolDirectory = Path.Combine(LocalizersPath, protocol);
            return Directory.Exists(protocolDirectory);
        }

        public bool TryLoad(string protocol)
        {
            string protocolDirectory = Path.Combine(LocalizersPath, protocol);

            if (Directory.Exists(protocolDirectory))
            {
                LocalizerProtocol localizerProtocol = new(protocol, protocolDirectory);
                Protocols.Add(localizerProtocol);
                return true;
            }

            return false;
        }

        public void Unload(string protocolName)
        {
            var protocol = Protocols.FirstOrDefault(p => p.Name == protocolName);
            if (protocol != null)
            {
                protocol.Clean();
                Protocols.Remove(protocol);
            }
        }

        /// <summary>
        /// Load specific blocs for a protocol and data type. Creates the protocol structure if it doesn't exist.
        /// </summary>
        /// <param name="protocolName">Name of the protocol</param>
        /// <param name="dataName">Name of the data type</param>
        /// <param name="blocNames">List of bloc names to load</param>
        /// <returns>List of loaded bloc names that weren't previously loaded</returns>
        public async UniTask<List<string>> LoadSpecificBlocsAsync(string protocolName, string dataName, IEnumerable<string> blocNames)
        {
            var loadedBlocs = new List<string>();
            if (string.IsNullOrEmpty(protocolName) || string.IsNullOrEmpty(dataName) || blocNames == null)
                return loadedBlocs;

            string protocolDirectory = Path.Combine(LocalizersPath, protocolName);
            if (!Directory.Exists(protocolDirectory))
                return loadedBlocs;

            string dataDirectory = Path.Combine(protocolDirectory, dataName);
            if (!Directory.Exists(dataDirectory))
                return loadedBlocs;

            // Get or create protocol
            var protocol = Protocols.FirstOrDefault(p => p.Name == protocolName);
            if (protocol == null)
            {
                protocol = new LocalizerProtocol(protocolName, protocolDirectory, loadBlocs: false);
                Protocols.Add(protocol);
            }

            // Get or create data
            var data = protocol.Datas.FirstOrDefault(d => d.Name == dataName);
            if (data == null)
            {
                data = new LocalizerData(dataName, dataDirectory, loadBlocs: false);
                protocol.Datas.Add(data);
            }

            // Load specific blocs
            foreach (string blocName in blocNames)
            {
                var existingBloc = data.Blocs.FirstOrDefault(b => b.Name == blocName);
                if (existingBloc == null)
                {
                    // Find the bloc file
                    var niftiFiles = Directory.GetFiles(dataDirectory).Where(file => LocalizersHelpers.NiftiExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))).Where(file => !LocalizersHelpers.IsMaskFile(file)).Where(file => LocalizersHelpers.GetBlocNameFromFile(file).Equals(blocName, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (niftiFiles.Count > 0)
                    {
                        string niftiFile = niftiFiles[0];
                        string maskFile = data.FindMaskFileForBloc(dataDirectory, blocName, LocalizersHelpers.NiftiExtensions);
                        var bloc = new LocalizerBloc(blocName, niftiFile, maskFile);
                        data.Blocs.Add(bloc);

                        // Wait for the bloc to load
                        await UniTask.WaitUntil(() => bloc.Loaded);
                        loadedBlocs.Add(blocName);
                    }
                }
                else if (!existingBloc.Loaded)
                {
                    // Load the existing bloc if not already loaded
                    await existingBloc.FMRI.LoadAsync();
                    loadedBlocs.Add(blocName);
                }
            }

            return loadedBlocs;
        }

        /// <summary>
        /// Unload specific blocs from a protocol and data type
        /// </summary>
        /// <param name="protocolName">Name of the protocol</param>
        /// <param name="dataName">Name of the data type</param>
        /// <param name="blocNames">List of bloc names to unload</param>
        public void UnloadSpecificBlocs(string protocolName, string dataName, IEnumerable<string> blocNames)
        {
            if (string.IsNullOrEmpty(protocolName) || string.IsNullOrEmpty(dataName) || blocNames == null)
                return;

            var protocol = Protocols.FirstOrDefault(p => p.Name == protocolName);
            if (protocol == null)
                return;

            var data = protocol.Datas.FirstOrDefault(d => d.Name == dataName);
            if (data == null)
                return;

            foreach (string blocName in blocNames)
            {
                var bloc = data.Blocs.FirstOrDefault(b => b.Name == blocName);
                if (bloc != null)
                {
                    bloc.Clean();
                    data.Blocs.Remove(bloc);
                }
            }

            // Clean up empty data/protocol structures if needed
            if (data.Blocs.Count == 0)
            {
                protocol.Datas.Remove(data);
            }

            if (protocol.Datas.Count == 0)
            {
                Protocols.Remove(protocol);
            }
        }

        public FMRI GetCurrentFMRI(string protocolName, string dataName, string blocName)
        {
            if (string.IsNullOrEmpty(protocolName) || string.IsNullOrEmpty(dataName) || string.IsNullOrEmpty(blocName))
                return null;

            var protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolName);
            var data = protocol?.Datas.FirstOrDefault(d => d.Name == dataName);
            var bloc = data?.Blocs.FirstOrDefault(b => b.Name == blocName);
            return bloc?.FMRI;
        }

        /// <summary>
        /// Return all available bloc names for a protocol by parsing directories (does not use loaded data).
        /// </summary>
        public List<string> GetAvailableBlocNames(string protocol)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(protocol)) return new List<string>();
            string protocolDirectory = Path.Combine(LocalizersPath, protocol);
            if (!Directory.Exists(protocolDirectory)) return new List<string>();

            foreach (var dataDirectory in Directory.GetDirectories(protocolDirectory))
            {
                var files = Directory.GetFiles(dataDirectory).Where(f => LocalizersHelpers.NiftiExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase))).Where(f => !LocalizersHelpers.IsMaskFile(f));
                foreach (var file in files)
                {
                    result.Add(LocalizersHelpers.GetBlocNameFromFile(file));
                }
            }

            return result.OrderBy(n => n).ToList();
        }

        #endregion
    }

    public class LocalizerProtocol
    {
        #region Properties

        public string Name { get; private set; }
        public List<LocalizerData> Datas { get; private set; } = new List<LocalizerData>();
        public bool Loaded => Datas.All(d => d.Loaded);

        #endregion

        #region Constructors

        public LocalizerProtocol(string name, string protocolDirectory, bool loadBlocs = true)
        {
            Name = name;
            if (loadBlocs)
            {
                LoadDatasFromDirectory(protocolDirectory);
            }
        }

        #endregion

        #region Private Methods

        private void LoadDatasFromDirectory(string directory)
        {
            if (!Directory.Exists(directory))
                return;

            var dataDirectories = Directory.GetDirectories(directory);
            foreach (string dataDirectory in dataDirectories)
            {
                string dataName = Path.GetFileName(dataDirectory);
                LocalizerData data = new(dataName, dataDirectory);
                if (data.Blocs.Count > 0)
                {
                    Datas.Add(data);
                }
            }
        }

        #endregion

        #region Public Methods

        public void Clean()
        {
            foreach (var data in Datas)
            {
                data?.Clean();
            }
        }

        #endregion
    }

    public class LocalizerData
    {
        #region Properties

        public string Name { get; private set; }
        public List<LocalizerBloc> Blocs { get; private set; } = new List<LocalizerBloc>();
        public bool Loaded => Blocs.All(b => b.Loaded);

        #endregion

        #region Constructors

        public LocalizerData(string name, string dataDirectory, bool loadBlocs = true)
        {
            Name = name;
            if (loadBlocs)
            {
                LoadBlocsFromDirectory(dataDirectory);
            }
        }

        #endregion

        #region Private Methods

        private void LoadBlocsFromDirectory(string directory)
        {
            if (!Directory.Exists(directory))
                return;

            var niftiExtensions = LocalizersHelpers.NiftiExtensions;
            var niftiFiles = Directory.GetFiles(directory).Where(file => niftiExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))).Where(file => !LocalizersHelpers.IsMaskFile(file)).ToList();

            foreach (string niftiFile in niftiFiles)
            {
                string blocName = LocalizersHelpers.GetBlocNameFromFile(niftiFile);
                string maskFile = FindMaskFileForBloc(directory, blocName, niftiExtensions);
                LocalizerBloc bloc = new(blocName, niftiFile, maskFile);
                Blocs.Add(bloc);
            }
        }

        #endregion

        #region Public Methods

        public string FindMaskFileForBloc(string directory, string blocName, string[] extensions)
        {
            foreach (string extension in extensions)
            {
                string maskPattern = $"{blocName}_mask{extension}";
                var maskFiles = Directory.GetFiles(directory, maskPattern, SearchOption.TopDirectoryOnly);
                if (maskFiles.Length > 0)
                {
                    return maskFiles[0];
                }
            }

            return string.Empty;
        }

        public void Clean()
        {
            foreach (var bloc in Blocs)
            {
                bloc?.Clean();
            }
        }

        #endregion
    }

    public class LocalizerBloc
    {
        #region Properties

        public string Name { get; private set; }
        public FMRI FMRI { get; private set; }
        public bool Loaded => FMRI?.Loaded ?? false;

        #endregion

        #region Constructors

        public LocalizerBloc(string name, string fmriFile, string maskFile = "")
        {
            Name = name;
            FMRI = new FMRI(name, fmriFile, maskFile);
        }

        #endregion

        #region Public Methods

        public void Clean()
        {
            FMRI?.Clean();
        }

        #endregion
    }
}
