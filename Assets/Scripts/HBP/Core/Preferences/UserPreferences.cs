using HBP.Core.Data;
using HBP.Core.Tools;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;

namespace HBP.Core.Preferences
{
    /**
    * \class UserPreferences
    * \author Adrien Gannerie
    * \version 1.0
    * \date 16 janvier 2017
    * \brief User preferences.
    * 
    * \details Class which contains the user preferences:
    *     - General preferences.
    *     - Data preferences.
    *     - Visualization preferences.
    */
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class UserPreferences : BaseData
    {
        #region Properties
        public static string PATH = Path.Combine(Application.persistentDataPath, "Preferences.txt");
        [JsonProperty] public GeneralPreferences General { get; set; }
        [JsonProperty] public DataPreferences Data { get; set; }
        [JsonProperty] public VisualizationPreferences Visualization { get; set; }
        #endregion

        #region Events
        public UnityEvent OnSavePreferences = new();
        #endregion

        #region Constructors
        public UserPreferences(GeneralPreferences generalPreferences, DataPreferences dataPreferences, VisualizationPreferences visualizationPreferences, string ID) : base(ID)
        {
            General = generalPreferences;
            Data = dataPreferences;
            Visualization = visualizationPreferences;
        }
        public UserPreferences(GeneralPreferences generalPreferences, DataPreferences dataPreferences, VisualizationPreferences visualizationPreferences) : base()
        {
            General = generalPreferences;
            Data = dataPreferences;
            Visualization = visualizationPreferences;
        }
        public UserPreferences() : this(new GeneralPreferences(), new DataPreferences(), new VisualizationPreferences())
        {
        }
        #endregion

        #region Public Methods
        public static UserPreferences Initialize()
        {
            UserPreferences userPreferences = new();
            if (new FileInfo(PATH).Exists)
            {
                try
                {
                    var loadedPreferences = ClassLoaderSaver.LoadFromJson<UserPreferences>(PATH);
                    if (loadedPreferences != null)
                    {
                        userPreferences = loadedPreferences;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            userPreferences.Save();
            return userPreferences;
        }
        public void Save()
        {
            ClassLoaderSaver.SaveToJSon(this, PATH, true);
            OnSavePreferences.Invoke();
        }
        public override object Clone()
        {
            return new UserPreferences(General.Clone() as GeneralPreferences, Data.Clone() as DataPreferences, Visualization.Clone() as VisualizationPreferences, ID);
        }
        public override void Copy(object copy)
        {
            if (copy is UserPreferences preferences)
            {
                General = preferences.General;
                Data = preferences.Data;
                Visualization = preferences.Visualization;
            }
        }
        #endregion
    }
}