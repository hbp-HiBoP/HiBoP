using HBP.Core.Tools;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using HBP.UI.Tools;
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
        private readonly string m_LocalizersPath = Path.Combine(ApplicationState.DataPath, "Atlases", "Localizers");
        public List<string> AvailableProtocolNames
        {
            get
            {
                if (Directory.Exists(m_LocalizersPath))
                {
                    return Directory.GetDirectories(m_LocalizersPath).Select(Path.GetFileName).OrderBy(n => n).ToList();
                }
                return new List<string>();
            }
        }
        public List<string> AvailableDataNames
        {
            get
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!Directory.Exists(m_LocalizersPath)) return new List<string>();
                foreach (var protocolDirectory in Directory.GetDirectories(m_LocalizersPath))
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
            string protocolDirectory = Path.Combine(m_LocalizersPath, protocol);
            return Directory.Exists(protocolDirectory);
        }
        public void Load(string protocol, bool displayErrors = true)
        {
            string protocolDirectory = Path.Combine(m_LocalizersPath, protocol);
            
            if (Directory.Exists(protocolDirectory))
            {
                LocalizerProtocol localizerProtocol = new LocalizerProtocol(protocol, protocolDirectory);
                Protocols.Add(localizerProtocol);
            }
            else if (displayErrors)
            {
                DialogBoxManager.Open(Enums.DialogBoxType.Error, "Can not load localizer", $"The localizer {protocol} could not be loaded. Please make sure you downloaded it and put it in the right folder.").Forget();
            }
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
            string protocolDirectory = Path.Combine(m_LocalizersPath, protocol);
            if (!Directory.Exists(protocolDirectory)) return new List<string>();

            foreach (var dataDirectory in Directory.GetDirectories(protocolDirectory))
            {
                var files = Directory.GetFiles(dataDirectory)
                    .Where(f => LocalizersHelpers.NiftiExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                    .Where(f => !LocalizersHelpers.IsMaskFile(f));
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
        public LocalizerProtocol(string name, string protocolDirectory)
        {
            Name = name;
            LoadDatasFromDirectory(protocolDirectory);
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
                LocalizerData data = new LocalizerData(dataName, dataDirectory);
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
        public LocalizerData(string name, string dataDirectory)
        {
            Name = name;
            LoadBlocsFromDirectory(dataDirectory);
        }
        #endregion

        #region Private Methods
        private void LoadBlocsFromDirectory(string directory)
        {
            if (!Directory.Exists(directory))
                return;

            var niftiExtensions = LocalizersHelpers.NiftiExtensions;
            var niftiFiles = Directory.GetFiles(directory)
                .Where(file => niftiExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .Where(file => !LocalizersHelpers.IsMaskFile(file))
                .ToList();

            foreach (string niftiFile in niftiFiles)
            {
                string blocName = LocalizersHelpers.GetBlocNameFromFile(niftiFile);
                string maskFile = FindMaskFileForBloc(directory, blocName, niftiExtensions);
                LocalizerBloc bloc = new LocalizerBloc(blocName, niftiFile, maskFile);
                Blocs.Add(bloc);
            }
        }
        private string FindMaskFileForBloc(string directory, string blocName, string[] extensions)
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
        #endregion

        #region Public Methods
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