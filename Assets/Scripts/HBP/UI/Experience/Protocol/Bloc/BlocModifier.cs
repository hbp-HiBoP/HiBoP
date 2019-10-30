using UnityEngine.UI;
using d = HBP.Data.Experience.Protocol;
using Tools.Unity;
using UnityEngine;
using NewTheme.Components;
using System.Linq;

namespace HBP.UI.Experience.Protocol
{
	public class BlocModifier : ItemModifier<d.Bloc> 
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

        #region Public Methods
        public override void Save()
        {
            itemTemp.SubBlocs = m_SubBlocListGestion.List.Objects.ToList();
            base.Save();
        }
        #endregion

        #region Private Methods
        protected override void SetFields(d.Bloc objectToDisplay)
        {
            m_NameInputField.text = objectToDisplay.Name;
            m_NameInputField.onEndEdit.AddListener((value) => ItemTemp.Name = value);

            m_ImageFileSelector.Path = objectToDisplay.IllustrationPath;
            m_ImageFileSelector.onValueChanged.AddListener((value) => ItemTemp.IllustrationPath = value);

            m_SortInputField.text = objectToDisplay.Sort;
            m_SortInputField.onEndEdit.AddListener((value) =>
            {
                ItemTemp.Sort = value;
                SetError();
            });

            m_OrderInputField.text = objectToDisplay.Order.ToString();
            m_OrderInputField.onEndEdit.AddListener((value) => objectToDisplay.Order = int.Parse(value));

            m_SubBlocListGestion.List.Set(objectToDisplay.SubBlocs);

            SetError();

            base.SetFields();
        }
        protected override void Initialize()
        {
            base.Initialize();
            m_SubBlocListGestion.SubWindowsManager.OnOpenSubWindow.AddListener(window => SubWindowsManager.Add(window));
        }
        protected void SetError()
        {
            d.Bloc.SortingMethodError error = ItemTemp.GetSortingMethodError();
            m_SortErrorText.Text = ItemTemp.GetSortingMethodErrorMessage(error);
            m_SortStateThemeElement.Set(error == d.Bloc.SortingMethodError.NoError ? m_OKState : m_ErrorState);
        }
        #endregion
    }
}