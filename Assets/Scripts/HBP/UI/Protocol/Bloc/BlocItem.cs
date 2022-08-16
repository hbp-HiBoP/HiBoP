using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.UI.Lists;

namespace HBP.UI.Experience.Protocol
{
    /// <summary>
    /// Component to display bloc in list.
    /// </summary>
	public class BlocItem : ActionnableItem<Core.Data.Bloc>
	{
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Image m_Image;
        [SerializeField] Tooltip m_ImageTooltip;
        [SerializeField] Text m_SubBlocsText;
        [SerializeField] Text m_OrderText;
        [SerializeField] Theme.State m_ErrorState;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override Core.Data.Bloc Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;
                m_OrderText.text = value.Order.ToString();
                m_SubBlocsText.SetIEnumerableFieldInItem("SubBlocs", from subBloc in m_Object.SubBlocs select subBloc.Name, m_ErrorState);
                m_Image.overrideSprite = value.Image;
                m_ImageTooltip.Image = value.Image;
            }
        }
        #endregion
    }
}