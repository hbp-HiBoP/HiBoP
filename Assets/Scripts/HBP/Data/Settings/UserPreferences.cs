using UnityEngine;
using System.IO;
using System.Runtime.Serialization;
using HBP.UI.Theme;

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
    public class UserPreferences
    {
        #region Properties
        public static string PATH = Directory.GetParent(Application.dataPath).FullName + Path.DirectorySeparatorChar + "Preferences.txt";
        public Theme Theme;
        [DataMember] public GeneralPreferences General { get; set; }
        [DataMember] public DataPreferences Data { get; set; }
        [DataMember] public VisualizationPreferences Visualization { get; set; }
        #endregion

        #region Constructors
        public UserPreferences()
        {
            General = new GeneralPreferences();
            Data = new DataPreferences();
            Visualization = new VisualizationPreferences();
        }
        #endregion
    }
}