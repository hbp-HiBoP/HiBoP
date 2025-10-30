using HBP.Core.Tools;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tools
{
    public class ToggleNavigator : MonoBehaviour
    {
        #region Properties
        [SerializeField] private NamedToggle[] m_Toggles;
        #endregion

        #region Public Methods
        public void Navigate(string name)
        {
            m_Toggles.FirstOrDefault(t => t.Name == name)?.Toggle.SetValue(true);
        }
        #endregion
    }

    [System.Serializable]
    public class NamedToggle
    {
        public string Name;
        public Toggle Toggle;
    }
}