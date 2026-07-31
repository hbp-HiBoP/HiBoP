using System.Linq;
using UnityEngine;

namespace HBP.UI.Main
{
    public class ResponseCodeSelectionPanel : BasicBlocImporterPanel
    {
        #region Properties

        [SerializeField] private Transform m_CodesContainer;
        [SerializeField] private GameObject m_CodeItemPrefab;

        #endregion

        #region Public Methods

        public override bool CanProceed()
        {
            return true;
        }

        public override void OnProceed()
        {
        }

        public override void Refresh()
        {
            foreach (Transform child in m_CodesContainer)
            {
                Destroy(child.gameObject);
            }

            var availableCodes = m_Data.OccurencesByCode.Keys.Except(m_Data.SelectedMainCodes).OrderBy(x => x);

            foreach (var code in availableCodes)
            {
                GameObject itemObject = Instantiate(m_CodeItemPrefab, m_CodesContainer);
                CodeSelectionItem item = itemObject.GetComponent<CodeSelectionItem>();
                item.SetData(code, m_Data.OccurencesByCode[code], m_Data.SelectedResponseCodes.Contains(code));
                item.OnSelectionChanged.AddListener(OnSelectionChanged);
            }
        }

        #endregion

        #region Private Methods

        private void OnSelectionChanged(bool value)
        {
            m_Data.SelectedResponseCodes.Clear();

            var codeItems = m_CodesContainer.GetComponentsInChildren<CodeSelectionItem>();
            foreach (var item in codeItems)
            {
                if (item.IsSelected)
                {
                    m_Data.SelectedResponseCodes.Add(item.Code);
                }
            }

            OnUpdateNavigation.Invoke();
        }

        #endregion
    }
}
