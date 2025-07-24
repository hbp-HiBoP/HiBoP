using System.Linq;
using UnityEngine;

namespace HBP.UI.Main
{
    public class ResponseAssignmentPanel : BasicBlocImporterPanel
    {
        #region Properties
        [SerializeField] private Transform m_ResponseCodesContainer;
        [SerializeField] private GameObject m_ResponseAssignmentItemPrefab;
        #endregion

        #region Public Methods
        public override bool CanProceed()
        {
            if (m_Data.SelectedResponseCodes.Count == 0)
                return true;

            var assignmentItems = m_ResponseCodesContainer.GetComponentsInChildren<ResponseAssignmentItem>();
            return assignmentItems.All(item => item.HasAssignedBlocs());
        }
        public override void OnProceed()
        {
            m_Data.ResponseCodesByMainCode.Clear();
            
            // First, process bloc names to get the created blocs
            m_Data.ProcessBlocNames();
            
            var assignmentItems = m_ResponseCodesContainer.GetComponentsInChildren<ResponseAssignmentItem>();
            foreach (var item in assignmentItems)
            {
                var assignedBlocNames = item.GetAssignedBlocNames();
                foreach (var blocName in assignedBlocNames)
                {
                    // Find the bloc data for this name
                    var blocData = m_Data.CreatedBlocs.FirstOrDefault(b => b.Name == blocName);
                    if (blocData != null)
                    {
                        // Add this response code to all main codes in this bloc
                        foreach (var mainCode in blocData.MainCodes)
                        {
                            if (!m_Data.ResponseCodesByMainCode.ContainsKey(mainCode))
                                m_Data.ResponseCodesByMainCode[mainCode] = new System.Collections.Generic.List<int>();
                            
                            m_Data.ResponseCodesByMainCode[mainCode].Add(item.ResponseCode);
                        }
                    }
                }
            }
        }
        public override void Refresh()
        {
            foreach (Transform child in m_ResponseCodesContainer)
            {
                Destroy(child.gameObject);
            }

            // Process bloc names first to get the created blocs
            m_Data.ProcessBlocNames();

            foreach (var responseCode in m_Data.SelectedResponseCodes.OrderBy(x => x))
            {
                GameObject itemObject = Instantiate(m_ResponseAssignmentItemPrefab, m_ResponseCodesContainer);
                ResponseAssignmentItem item = itemObject.GetComponent<ResponseAssignmentItem>();
                
                int occurrences = m_Data.OccurencesByCode[responseCode];
                item.OnAssignmentChanged.AddListener(OnUpdateNavigation.Invoke);
                item.SetDataWithBlocs(responseCode, occurrences, m_Data.CreatedBlocs);
            }
        }
        #endregion
    }
}