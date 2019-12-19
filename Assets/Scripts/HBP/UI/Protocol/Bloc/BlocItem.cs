using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Protocol;
using Tools.Unity;
using System.Linq;

namespace HBP.UI.Experience.Protocol
{
	public class BlocItem : Tools.Unity.Lists.ActionnableItem<Bloc>
	{
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Image m_Image;
        [SerializeField] Tooltip m_ImageTooltip;
        [SerializeField] Text m_SubBlocsText;
        [SerializeField] Text m_OrderText;

        [SerializeField] State m_ErrorState;

        public override Bloc Object
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