using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using System.Linq;

namespace HBP.UI
{
    public class SiteItem : ActionnableItem<Data.Site>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_CoordinatesText;
        [SerializeField] Text m_TagsText;
        [SerializeField] State m_ErrorState;

        public override Data.Site Object
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