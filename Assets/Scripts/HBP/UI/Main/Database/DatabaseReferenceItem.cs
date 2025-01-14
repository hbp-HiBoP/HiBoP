using HBP.Data.Database;
using HBP.UI.Tools.Lists;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class DatabaseReferenceItem : ActionnableItem<DatabaseReference>
    {
        #region Properties
        [SerializeField] Text m_NameText;
        [SerializeField] Text m_TypeText;
        [SerializeField] Text m_LastUpdatedText;
        [SerializeField] Theme.State m_ErrorState;

        public override DatabaseReference Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;
                m_TypeText.text = value.Type.ToString();
                m_LastUpdatedText.text = value.LastUpdated.ToString();
            }
        }
        #endregion
    }
}