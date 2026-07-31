using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class ResponseAssignmentItem : MonoBehaviour
    {
        #region Properties

        [SerializeField] private Text m_ResponseCodeText;
        [SerializeField] private Text m_OccurrencesText;
        [SerializeField] private Transform m_BlocTogglesContainer;
        [SerializeField] private GameObject m_BlocTogglePrefab;

        public int ResponseCode { get; private set; }
        public int Occurrences { get; private set; }

        private List<BlocToggleItem> m_BlocToggles = new();

        #endregion

        #region Events

        [HideInInspector] public UnityEvent OnAssignmentChanged = new();

        #endregion

        #region Public Methods

        public void SetData(int responseCode, int occurrences, List<int> mainCodes, Dictionary<int, string> blocNamesByCode)
        {
            ResponseCode = responseCode;
            Occurrences = occurrences;

            m_ResponseCodeText.text = responseCode.ToString();
            m_OccurrencesText.text = $"({occurrences} occurences)";

            foreach (Transform child in m_BlocTogglesContainer)
            {
                Destroy(child.gameObject);
            }

            m_BlocToggles.Clear();

            foreach (var mainCode in mainCodes.OrderBy(x => x))
            {
                GameObject toggleObject = Instantiate(m_BlocTogglePrefab, m_BlocTogglesContainer);
                BlocToggleItem toggleItem = toggleObject.GetComponent<BlocToggleItem>();

                string blocName = blocNamesByCode.ContainsKey(mainCode) ? blocNamesByCode[mainCode] : $"Code {mainCode}";
                toggleItem.SetData(mainCode, blocName);
                toggleItem.OnToggleChanged.AddListener(() => OnAssignmentChanged.Invoke());

                m_BlocToggles.Add(toggleItem);
            }
        }

        public void SetDataWithBlocs(int responseCode, int occurrences, List<BlocCreationData> createdBlocs)
        {
            ResponseCode = responseCode;
            Occurrences = occurrences;

            m_ResponseCodeText.text = responseCode.ToString();
            m_OccurrencesText.text = $"({occurrences} occurences)";

            foreach (Transform child in m_BlocTogglesContainer)
            {
                Destroy(child.gameObject);
            }

            m_BlocToggles.Clear();

            foreach (var blocData in createdBlocs.OrderBy(x => x.Name))
            {
                GameObject toggleObject = Instantiate(m_BlocTogglePrefab, m_BlocTogglesContainer);
                BlocToggleItem toggleItem = toggleObject.GetComponent<BlocToggleItem>();

                toggleItem.SetDataWithBlocName(blocData.Name);
                toggleItem.OnToggleChanged.AddListener(() => OnAssignmentChanged.Invoke());

                m_BlocToggles.Add(toggleItem);
            }
        }

        public bool HasAssignedBlocs()
        {
            return m_BlocToggles.Any(toggle => toggle.IsSelected);
        }

        public List<int> GetAssignedMainCodes()
        {
            return m_BlocToggles.Where(toggle => toggle.IsSelected).Select(toggle => toggle.MainCode).ToList();
        }

        public List<string> GetAssignedBlocNames()
        {
            return m_BlocToggles.Where(toggle => toggle.IsSelected).Select(toggle => toggle.BlocName).ToList();
        }

        #endregion
    }
}
