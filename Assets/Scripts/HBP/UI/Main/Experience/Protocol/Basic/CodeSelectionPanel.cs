using System.Linq;
using UnityEngine;

namespace HBP.UI.Main
{
    public class CodeSelectionPanel : BasicBlocImporterPanel
    {
        #region Properties
        [SerializeField] private Transform m_CodesContainer;
        [SerializeField] private GameObject m_CodeItemPrefab;
        #endregion

        #region Public Methods
        public override bool CanProceed()
        {
            return m_Data.SelectedMainCodes.Count > 0;
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

            foreach (var kvp in m_Data.OccurencesByCode.OrderBy(x => x.Key))
            {
                GameObject itemObject = Instantiate(m_CodeItemPrefab, m_CodesContainer);
                CodeSelectionItem item = itemObject.GetComponent<CodeSelectionItem>();
                item.SetData(kvp.Key, kvp.Value, m_Data.SelectedMainCodes.Contains(kvp.Key));
                item.OnSelectionChanged.AddListener(OnSelectionChanged);
            }
        }
        #endregion

        #region Private Methods
        private void OnSelectionChanged(bool value)
        {
            m_Data.SelectedMainCodes.Clear();

            var codeItems = m_CodesContainer.GetComponentsInChildren<CodeSelectionItem>();
            foreach (var item in codeItems)
            {
                if (item.IsSelected)
                {
                    m_Data.SelectedMainCodes.Add(item.Code);
                }
            }

            OnUpdateNavigation.Invoke();
        }
        #endregion
    }
}