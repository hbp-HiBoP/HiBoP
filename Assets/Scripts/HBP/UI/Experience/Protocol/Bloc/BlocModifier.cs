using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;
using Tools.Unity;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
	public class BlocModifier : ItemModifier<d.Bloc> 
	{
		#region Properties
		[SerializeField] InputField m_NameInputField, m_SortInputField, m_OrderInputField;
        [SerializeField] SubBlocListGestion m_SubBlocListGestion;
        [SerializeField] ImageSelector m_ImageFileSelector;
        [SerializeField] Button m_AddSubBloc, m_RemoveSubBloc;

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
                m_SortInputField.interactable = value;
                m_OrderInputField.interactable = value;

                m_SubBlocListGestion.Interactable = value;
                m_ImageFileSelector.interactable = value;

                m_AddSubBloc.interactable = value;
                m_RemoveSubBloc.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        protected override void SetFields(d.Bloc objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_NameInputField.onEndEdit.AddListener((value) => ItemTemp.Name = value);

            m_ImageFileSelector.Path = objectToDisplay.IllustrationPath;
            m_ImageFileSelector.onValueChanged.AddListener(() => ItemTemp.IllustrationPath = m_ImageFileSelector.Path);

            m_SortInputField.text = objectToDisplay.Sort;
            m_SortInputField.onEndEdit.AddListener((value) => ItemTemp.Sort = value);

            m_OrderInputField.text = objectToDisplay.Order.ToString();
            m_OrderInputField.onEndEdit.AddListener((value) => objectToDisplay.Order = int.Parse(value));

            m_SubBlocListGestion.Initialize(m_SubWindows);
            m_SubBlocListGestion.Items = objectToDisplay.SubBlocs;

            base.SetFields();
        }
        #endregion
    }
}