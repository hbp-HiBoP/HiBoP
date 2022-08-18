using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.UI.Tools.Lists;

namespace HBP.UI.Main
{
    /// <summary>
    /// Component to display site in list.
    /// </summary>
    public class SiteItem : ActionnableItem<Core.Data.Site>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_CoordinatesText;
        [SerializeField] Text m_TagsText;
        [SerializeField] Theme.State m_ErrorState;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override Core.Data.Site Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;

                m_NameText.text = value.Name;

                m_CoordinatesText.SetIEnumerableFieldInItem("Coordinates", m_Object.Coordinates.Select(c => c.ReferenceSystem), m_ErrorState);
                m_TagsText.SetIEnumerableFieldInItem("Tags", m_Object.Tags.Select(t => t.Tag.Name), m_ErrorState);
            }
        }
        #endregion
    }
}