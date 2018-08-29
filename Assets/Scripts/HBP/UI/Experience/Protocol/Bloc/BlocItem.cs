using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
	public class BlocItem : Tools.Unity.Lists.ActionnableItem<Bloc>
	{
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_SubBlocsText;
        [SerializeField] Image m_Image;
        [SerializeField] Text m_OrderText;

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
                m_SubBlocsText.text = value.SubBlocs.Count.ToString();
                //m_Image.sprite = value.IllustrationPath TO DO illustration
            }
        }
        #endregion
    }
}