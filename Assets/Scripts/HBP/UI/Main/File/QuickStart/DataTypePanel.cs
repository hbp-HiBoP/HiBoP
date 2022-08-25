using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main.QuickStart
{
    public class DataTypePanel : QuickStartPanel
    {
        #region Properties
        [SerializeField] private Toggle m_Anatomical;
        [SerializeField] private Toggle m_AnatomicalAndEEG;
        public bool OnlyAnatomical { get { return m_Anatomical.isOn; } }
        #endregion
    }
}