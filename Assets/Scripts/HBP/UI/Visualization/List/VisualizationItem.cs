using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using System.Linq;

namespace HBP.UI.Visualization
{
    /// <summary>
    /// Component to display a visualization in list.
    /// </summary>
    public class VisualizationItem : ActionnableItem<Core.Data.Visualization>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_PatientsText;
        [SerializeField] Text m_ColumnsText;

        [SerializeField] HBP.Theme.State m_ErrorState;

        /// <summary>
        /// Object to Display.
        /// </summary>
        public override Core.Data.Visualization Object
        {
            get
            {
                return base.Object;
            }

            set
            {
                base.Object = value;

                m_NameText.text = value.Name;
                m_PatientsText.SetIEnumerableFieldInItem("Patients", value.Patients.Select(p => p.Name), m_ErrorState);
                m_ColumnsText.SetIEnumerableFieldInItem("Columns", value.Columns.Select(c => c.Name), m_ErrorState);
            }
        }
        #endregion
    }
}