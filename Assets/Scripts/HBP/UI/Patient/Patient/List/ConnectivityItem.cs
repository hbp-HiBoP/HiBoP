using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Patient;
using Tools.CSharp;

namespace HBP.UI.Patient
{
    public class ConnectivityItem : Tools.Unity.Lists.ListItemWithSave<Connectivity>
    {
        #region Properties
        [SerializeField]
        InputField m_label;

        [SerializeField]
        InputField m_path;

        [SerializeField]
        Button m_button;
        #endregion

        #region Public Methods
        protected override void SetObject(Connectivity connectivity)
        {
            m_label.text = connectivity.Label;
            m_path.text = connectivity.Path;
        }

        public override void Save()
        {
            Object.Label = m_label.text;
            Object.Path = m_path.text;
        }

        /// <summary>
        /// Open the file dialog.
        /// </summary>
        public void OpenPath()
        {
            string l_filePath = m_path.text;
            if (l_filePath == string.Empty) l_filePath = ApplicationState.ProjectLoaded.Settings.PatientDatabase + System.IO.Path.DirectorySeparatorChar;
            string l_resultStandalone = VISU3D.DLL.QtGUI.getOpenFileName(new string[] { "txt" }, "Please select the patient connectivity file", l_filePath);
            l_resultStandalone.StandardizeToPath();
            if (l_resultStandalone != string.Empty)
            {
                m_path.text = l_resultStandalone;
            }
        }
        #endregion
    }
}