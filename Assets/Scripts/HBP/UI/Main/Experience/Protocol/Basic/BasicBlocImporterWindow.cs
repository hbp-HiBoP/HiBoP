using HBP.Core.Data;
using HBP.Core.Data.Container;
using HBP.UI.Tools;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Main
{
    public class BasicBlocImporterWindow : DialogWindow
    {
        #region Properties
        private string m_FilePath;
        public string FilePath
        {
            get => m_FilePath;
            set
            {
                m_FilePath = value;
                SetFilePath(m_FilePath);
            }
        }

        private Dictionary<int, int> m_OccurencesByCode = new();
        #endregion

        #region Events
        public GenericEvent<Bloc[]> OnBlocsImported = new();
        #endregion

        #region Private Methods
        private void SetFilePath(string filePath)
        {
            LoadEvents(filePath);
        }
        private void LoadEvents(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            Core.DLL.EEG.File.FileType type;
            string[] files;
            if (fileInfo.Extension == BrainVision.HEADER_EXTENSION)
            {
                type = Core.DLL.EEG.File.FileType.BrainVision;
                files = new string[] { filePath };
            }
            else if (fileInfo.Extension == EDF.EDF_EXTENSION)
            {
                type = Core.DLL.EEG.File.FileType.EDF;
                files = new string[] { filePath };
            }
            else if (fileInfo.Extension == Elan.POS_EXTENSION)
            {
                type = Core.DLL.EEG.File.FileType.ELAN;
                files = new string[] { "", filePath, "" };
            }
            else if (fileInfo.Extension == Micromed.MICROMED_EXTENSION)
            {
                type = Core.DLL.EEG.File.FileType.Micromed;
                files = new string[] { filePath };
            }
            else if (fileInfo.Extension == FIF.FIF_EXTENSION)
            {
                type = Core.DLL.EEG.File.FileType.FIF;
                files = new string[] { filePath };
            }
            else
            {
                throw new Exception("Invalid data container type");
            }
            Core.DLL.EEG.File file = new Core.DLL.EEG.File(type, false, files);
            List<Core.DLL.EEG.Trigger> triggers = file.Triggers;

            m_OccurencesByCode.Clear();
            foreach (var uniqueCode in triggers.Select(t => t.Code).Distinct())
            {
                m_OccurencesByCode[uniqueCode] = 0;
            }
            foreach (var trigger in triggers)
            {
                m_OccurencesByCode[trigger.Code]++;
            }
        }
        #endregion
    }
}