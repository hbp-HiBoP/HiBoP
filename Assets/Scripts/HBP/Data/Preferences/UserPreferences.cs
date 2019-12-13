using UnityEngine;
using System.IO;
using System.Runtime.Serialization;
using HBP.UI.Theme;
using System;

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
    public class UserPreferences: ICopiable, ICloneable
    {
        #region Properties
        public static string PATH = Path.Combine(Application.persistentDataPath, "Preferences.txt");
        public Theme Theme;
        [DataMember] public GeneralPreferences General { get; set; }
        [DataMember] public DataPreferences Data { get; set; }
        [DataMember] public VisualizationPreferences Visualization { get; set; }
        #endregion

        #region Constructors
        public UserPreferences() : this(new GeneralPreferences(), new DataPreferences(), new VisualizationPreferences())
        {
        }
        public UserPreferences(GeneralPreferences generalPreferences, DataPreferences dataPreferences, VisualizationPreferences visualizationPreferences)
        {
            General = generalPreferences;
            Data = dataPreferences;
            Visualization = visualizationPreferences;
        }
        #endregion

        #region Public Methods
        public object Clone()
        {
            return new UserPreferences(General.Clone() as GeneralPreferences, Data.Clone() as DataPreferences, Visualization.Clone() as VisualizationPreferences);
        }
        public void Copy(object copy)
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