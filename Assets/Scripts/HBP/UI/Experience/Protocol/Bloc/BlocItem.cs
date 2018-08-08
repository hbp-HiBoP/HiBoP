using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Experience.Protocol;

namespace HBP.UI.Experience.Protocol
{
	public class BlocItem : Tools.Unity.Lists.ActionnableItem<Bloc>
	{
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_PositionText;
        [SerializeField] Text m_SubBlocsText;
        [SerializeField] Image m_Image;

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
                m_PositionText.text = value.Position.ToString();
                m_SubBlocsText.text = value.SubBlocs.ToString();
                //m_Image.sprite = value.IllustrationPath TO DO illustration.
            }
        }
        #endregion
    }
}