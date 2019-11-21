using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;
using Tools.Unity;
using UnityEngine;
using NewTheme.Components;
using System.Linq;
using Tools.CSharp;

namespace HBP.UI.Experience.Protocol
{
	public class BlocModifier : ObjectModifier<d.Bloc> 
	{
		#region Properties
		[SerializeField] InputField m_NameInputField, m_SortInputField, m_OrderInputField;
        [SerializeField] SubBlocListGestion m_SubBlocListGestion;
        [SerializeField] ImageSelector m_ImageFileSelector;
        [SerializeField] Button m_AddSubBloc, m_RemoveSubBloc;

        [SerializeField] ThemeElement m_SortStateThemeElement;
        [SerializeField] Tooltip m_SortErrorText;
        [SerializeField] State m_OKState;
        [SerializeField] State m_ErrorState;

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
        protected override void Initialize()
        {
            base.Initialize();
            m_NameInputField.onEndEdit.AddListener(OnChangeName);
            m_ImageFileSelector.onValueChanged.AddListener(OnChangeImage);
            m_SortInputField.onEndEdit.AddListener(OnChangeSort);
            m_OrderInputField.onEndEdit.AddListener(OnChangeOrder);

            m_SubBlocListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_SubBlocListGestion.List.OnAddObject.AddListener(OnAddSubBloc);
            m_SubBlocListGestion.List.OnRemoveObject.AddListener(OnRemoveSubBloc);
        }
        protected override void SetFields(d.Bloc objectToDisplay)
        {
            base.SetFields();

            m_NameInputField.text = objectToDisplay.Name;
            m_ImageFileSelector.Path = objectToDisplay.IllustrationPath;
            m_SortInputField.text = objectToDisplay.Sort;
            m_OrderInputField.text = objectToDisplay.Order.ToString();
            m_SubBlocListGestion.List.Set(objectToDisplay.SubBlocs);
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
        protected void OnChangeImage(string value)
        {
            ItemTemp.IllustrationPath = value;
        }
        protected void OnChangeSort(string value)
        {
            ItemTemp.Sort = value;
            d.Bloc.SortingMethodError error = ItemTemp.GetSortingMethodError();
            m_SortErrorText.Text = ItemTemp.GetSortingMethodErrorMessage(error);
            m_SortStateThemeElement.Set(error == d.Bloc.SortingMethodError.NoError ? m_OKState : m_ErrorState);
        }
        protected void OnChangeOrder(string value)
        {
            if(int.TryParse(value, out int order))
            {
                ItemTemp.Order = order;
            }
            else
            {
                m_OrderInputField.text = ItemTemp.Order.ToString();
            }
        }

        protected void OnAddSubBloc(d.SubBloc subBloc)
        {
            ItemTemp.SubBlocs.AddIfAbsent(subBloc);
        }
        protected void OnRemoveSubBloc(d.SubBloc subBloc)
        {
            ItemTemp.SubBlocs.Remove(subBloc);
        }
        #endregion
    }
}