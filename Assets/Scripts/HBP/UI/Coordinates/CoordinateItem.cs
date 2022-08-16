using HBP.UI.Lists;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    /// <summary>
    /// Component to display coordinate in list.
    /// </summary>
    public class CoordinateItem : ActionnableItem<Core.Data.Coordinate>
    {
        #region Properties
        [SerializeField] Text m_ReferenceSystemText;
        [SerializeField] Text m_XText;
        [SerializeField] Text m_YText;
        [SerializeField] Text m_ZText;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override Core.Data.Coordinate Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;

                m_ReferenceSystemText.text = value.ReferenceSystem;
                m_XText.text = value.Position.x.ToString("0.##", CultureInfo.InvariantCulture);
                m_YText.text = value.Position.y.ToString("0.##", CultureInfo.InvariantCulture);
                m_ZText.text = value.Position.z.ToString("0.##", CultureInfo.InvariantCulture);
            }
        }
        #endregion
    }
}