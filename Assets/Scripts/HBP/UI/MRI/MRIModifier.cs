using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    public class MRIModifier : ObjectModifier<Data.MRI>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] FileSelector m_FileSelector;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_NameInputField.interactable = value;
                m_FileSelector.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(OnChangeName);
            m_FileSelector.onValueChanged.AddListener(OnChangeFile);
        }
        protected override void SetFields(Data.MRI objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_FileSelector.File = objectToDisplay.SavedFile;
        }

        protected void OnChangeName(string value)
        {
            if(value != "")
            {
                ItemTemp.Name = value;
            }
            else
            {
                m_NameInputField.text = ItemTemp.Name;
            }
        }
        protected void OnChangeFile(string value)
        {
            ItemTemp.File = value;
        }
        #endregion
    }
}