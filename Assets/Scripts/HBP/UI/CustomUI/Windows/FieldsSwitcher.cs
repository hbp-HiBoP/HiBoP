using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Window
{
    public class FieldsSwitcher : MonoBehaviour
    {
        #region Properties
        [SerializeField] private InputField[] m_InputFields;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                SelectNext();
            }
        }
        private void SelectNext()
        {
            for (int i = 0; i < m_InputFields.Length; ++i)
            {
                if (m_InputFields[i].isFocused)
                {
                    int index = (i + 1) % m_InputFields.Length;
                    m_InputFields[index].Select();
                    break;
                }
            }
        }
        #endregion
    }
}