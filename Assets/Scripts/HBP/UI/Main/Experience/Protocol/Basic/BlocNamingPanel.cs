using System.Linq;
using UnityEngine;

namespace HBP.UI.Main
{
    public class BlocNamingPanel : BasicBlocImporterPanel
    {
        #region Properties

        [SerializeField] private Transform m_BlocsContainer;
        [SerializeField] private GameObject m_BlocNamingItemPrefab;

        #endregion

        #region Public Methods

        public override bool CanProceed()
        {
            var namingItems = m_BlocsContainer.GetComponentsInChildren<BlocNamingItem>();
            return namingItems.All(item => !string.IsNullOrEmpty(item.BlocName));
        }

        public override void OnProceed()
        {
            m_Data.BlocNamesByCode.Clear();

            var namingItems = m_BlocsContainer.GetComponentsInChildren<BlocNamingItem>();
            foreach (var item in namingItems)
            {
                m_Data.BlocNamesByCode[item.Code] = item.BlocName;
            }
        }

        public override void Refresh()
        {
            foreach (Transform child in m_BlocsContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var code in m_Data.SelectedMainCodes.OrderBy(x => x))
            {
                GameObject itemObject = Instantiate(m_BlocNamingItemPrefab, m_BlocsContainer);
                BlocNamingItem item = itemObject.GetComponent<BlocNamingItem>();

                string existingName = m_Data.BlocNamesByCode.ContainsKey(code) ? m_Data.BlocNamesByCode[code] : "";
                int occurrences = m_Data.OccurencesByCode[code];

                item.SetData(code, occurrences, existingName);
                item.OnNameChanged.AddListener(OnUpdateNavigation.Invoke);
            }
        }

        #endregion
    }
}
