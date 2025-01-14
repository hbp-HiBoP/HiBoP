using HBP.Core.Data;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ThirdParty.CielaSpike;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Data.Database
{
    public class GlobalDatabase
    {
        #region Properties
        private GlobalDatabaseSettings m_Settings = new GlobalDatabaseSettings();
        public GlobalDatabaseSettings Settings => m_Settings;

        private List<Protocol> m_Protocols = new List<Protocol>();
        public ReadOnlyCollection<Protocol> Protocols => new ReadOnlyCollection<Protocol>(m_Protocols);
        #endregion

        #region Getters/Setters
        // Protocols.
        public void SetProtocols(IEnumerable<Protocol> protocols)
        {
            m_Protocols = new List<Protocol>();
            AddProtocol(protocols);
        }
        public void AddProtocol(Protocol protocol)
        {
            m_Protocols.Add(protocol);
        }
        public void AddProtocol(IEnumerable<Protocol> protocols)
        {
            foreach (Protocol protocol in protocols)
                AddProtocol(protocol);
        }
        public void RemoveProtocol(Protocol protocol)
        {
            m_Protocols.Remove(protocol);
        }
        public void RemoveProtocol(IEnumerable<Protocol> protocols)
        {
            foreach (Protocol protocol in protocols)
                RemoveProtocol(protocol);
        }
        #endregion

        #region Public Methods
        public static GlobalDatabase Initialize()
        {
            GlobalDatabase database = new GlobalDatabase();
            if (!new DirectoryInfo(ApplicationState.DatabasePath).Exists) Directory.CreateDirectory(ApplicationState.DatabasePath);
            database.LoadSettings();
            if (!database.Settings.Initialized)
            {
                CopyDefaultDatabase();
                database.SaveSettings();
            }
            database.LoadProtocols(ApplicationState.DatabasePath);
            return database;
        }
        public void SaveProtocols()
        {
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(c_SaveProtocols(new DirectoryInfo(ApplicationState.DatabasePath), onChangeProgress.Invoke), onChangeProgress);
        } 
        #endregion

        #region Private Methods
        private static void CopyDefaultDatabase()
        {
            DirectoryInfo defaultDatabaseDirectory = new DirectoryInfo(Path.Combine(ApplicationState.DataPath, "DefaultDatabase"));
            defaultDatabaseDirectory.CopyFilesRecursively(new DirectoryInfo(ApplicationState.DatabasePath));
        }
        private void LoadSettings()
        {
            if (new FileInfo(GlobalDatabaseSettings.PATH).Exists)
            {
                try
                {
                    m_Settings = ClassLoaderSaver.LoadFromJson<GlobalDatabaseSettings>(GlobalDatabaseSettings.PATH);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw e;
                }
            }
        }
        private void SaveSettings()
        {
            m_Settings.Initialized = true;
            ClassLoaderSaver.SaveToJSon(m_Settings, GlobalDatabaseSettings.PATH, true);
        }
        private void LoadProtocols(string rootDirectory)
        {
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(c_LoadProtocols(new DirectoryInfo(rootDirectory), onChangeProgress), onChangeProgress);
        }
        IEnumerator c_LoadProtocols(DirectoryInfo rootDirectory, GenericEvent<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            // Load Protocols
            List<Protocol> protocols = new List<Protocol>();
            DirectoryInfo protocolDirectory = new DirectoryInfo(Path.Combine(rootDirectory.FullName, "Protocols"));
            if (!protocolDirectory.Exists) protocolDirectory.Create();
            FileInfo[] protocolFiles = protocolDirectory.GetFiles("*" + Protocol.EXTENSION, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < protocolFiles.Length; ++i)
            {
                FileInfo protocolFile = protocolFiles[i];
                onChangeProgress.Invoke((float)(i + 1) / protocolFiles.Length, 0, new LoadingText("Loading protocol ", Path.GetFileNameWithoutExtension(protocolFile.Name), " [" + (i + 1).ToString() + "/" + protocolFiles.Length + "]"));
                try
                {
                    protocols.Add(ClassLoaderSaver.LoadFromJson<Protocol>(protocolFile.FullName));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotReadProtocolFileException(Path.GetFileNameWithoutExtension(protocolFile.Name));
                }
            }
            SetProtocols(protocols.ToArray());
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Protocols loaded successfully"));
        }
        IEnumerator c_SaveProtocols(DirectoryInfo projectDirectory, Action<float, float, LoadingText> onChangeProgress)
        {
            yield return Ninja.JumpBack;
            // Save protocols
            DirectoryInfo protocolDirectory = Directory.CreateDirectory(Path.Combine(projectDirectory.FullName, "Protocols"));
            int count = 0;
            int length = m_Protocols.Count();
            foreach (Protocol protocol in m_Protocols)
            {
                onChangeProgress.Invoke((float)count / length, 0, new LoadingText("Saving protocol ", protocol.Name, " [" + (count + 1).ToString() + "/" + length + "]"));
                try
                {
                    ClassLoaderSaver.SaveToJSon(protocol, Path.Combine(protocolDirectory.FullName, protocol.Name + Protocol.EXTENSION), true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    throw new CanNotSaveSettingsException();
                }
                count++;
            }
            onChangeProgress.Invoke(1.0f, 0, new LoadingText("Protocols saved successfully"));
        }
        #endregion
    }
}