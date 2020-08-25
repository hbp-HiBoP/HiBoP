using System.Collections;
using System.Collections.Generic;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.QuickStart
{
    public class FinalizationPanel : QuickStartPanel
    {
        #region Properties
        [SerializeField] private InputField m_ProjectName;
        [SerializeField] private FolderSelector m_ProjectLocation;
        #endregion
    }
}