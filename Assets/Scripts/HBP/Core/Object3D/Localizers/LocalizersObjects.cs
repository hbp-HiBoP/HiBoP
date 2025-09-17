using HBP.Core.Tools;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using HBP.UI.Tools;

namespace HBP.Core.Object3D
{
    public class LocalizersObjects
    {
        #region Properties
        public List<LocalizerProtocol> Protocols { get; private set; } = new List<LocalizerProtocol>();
        public bool Loaded => Protocols.Count > 0 && Protocols.All(p => p.Loaded);
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
            string protocolDirectory = Path.Combine(ApplicationState.DataPath, "Atlases", "Localizers", protocol);
            return Directory.Exists(protocolDirectory);
        }
        public void Load(string protocol, bool displayErrors = true)
        {
            string protocolDirectory = Path.Combine(ApplicationState.DataPath, "Atlases", "Localizers", protocol);
            
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
        public FMRI GetCurrentFMRI(string protocolName, string dataName, string blocName)
        {
            if (string.IsNullOrEmpty(protocolName) || string.IsNullOrEmpty(dataName) || string.IsNullOrEmpty(blocName))
                return null;

            var protocol = Object3DManager.Localizers.Protocols.FirstOrDefault(p => p.Name == protocolName);
            var data = protocol?.Datas.FirstOrDefault(d => d.Name == dataName);
            var bloc = data?.Blocs.FirstOrDefault(b => b.Name == blocName);
            return bloc?.FMRI;
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
                if (data.Blocs.Count > 0) // Seulement ajouter si des blocs ont été trouvés
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

            // Extensions supportées pour les fichiers NIFTI
            string[] niftiExtensions = { ".nii", ".nii.gz", ".img" };
            
            // Chercher tous les fichiers NIFTI qui ne sont pas des masks
            var niftiFiles = Directory.GetFiles(directory)
                .Where(file => niftiExtensions.Any(ext => file.EndsWith(ext, System.StringComparison.OrdinalIgnoreCase)))
                .Where(file => !IsMaskFile(file))
                .ToList();

            // Créer un bloc pour chaque fichier NIFTI trouvé
            foreach (string niftiFile in niftiFiles)
            {
                string blocName = GetBlocNameFromFile(niftiFile);
                
                // Chercher le fichier mask correspondant à ce bloc
                string maskFile = FindMaskFileForBloc(directory, blocName, niftiExtensions);
                
                LocalizerBloc bloc = new LocalizerBloc(blocName, niftiFile, maskFile);
                Blocs.Add(bloc);
            }
        }
        private string FindMaskFileForBloc(string directory, string blocName, string[] extensions)
        {
            // Pattern: {BlocName}_mask avec différentes extensions
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
        private bool IsMaskFile(string filePath)
        {
            // Extraire le nom de fichier complet sans le chemin
            string fileName = Path.GetFileName(filePath);
            
            // Vérifier si le fichier se termine par _mask suivi d'une extension supportée
            return fileName.ToUpper().Contains("_MASK.NII") || 
                   fileName.ToUpper().Contains("_MASK.IMG") ||
                   fileName.ToUpper().EndsWith("_MASK.NII.GZ");
        }
        private string GetBlocNameFromFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            
            // Retirer toutes les extensions possibles
            if (fileName.EndsWith(".nii.gz", System.StringComparison.OrdinalIgnoreCase))
            {
                return fileName[..^7]; // Enlever .nii.gz
            }
            else if (fileName.EndsWith(".nii", System.StringComparison.OrdinalIgnoreCase))
            {
                return fileName[..^4]; // Enlever .nii
            }
            else if (fileName.EndsWith(".img", System.StringComparison.OrdinalIgnoreCase))
            {
                return fileName[..^4]; // Enlever .img
            }
            
            return Path.GetFileNameWithoutExtension(fileName);
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