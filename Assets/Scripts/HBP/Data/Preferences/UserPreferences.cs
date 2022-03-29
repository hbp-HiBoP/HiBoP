using UnityEngine;
using System.IO;
using System.Runtime.Serialization;
using System;
using Tools.Unity;
using UnityEngine.Events;

namespace HBP.Data.Preferences
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
    [DataContract]
    public class UserPreferences : BaseData
    {
        #region Properties
        public static string PATH = Path.Combine(Application.persistentDataPath, "Preferences.txt");
        [DataMember] public GeneralPreferences General { get; set; }
        [DataMember] public DataPreferences Data { get; set; }
        [DataMember] public VisualizationPreferences Visualization { get; set; }
        #endregion

        #region Events
        public UnityEvent OnSavePreferences = new UnityEvent();
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