using HBP.Core.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class ExportProtocolItem : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Text m_ProtocolNameText;
        [SerializeField] private Transform m_BlocsContainer;
        [SerializeField] private GameObject m_BlocItemPrefab;
        
        public string Name => m_ProtocolNameText.text;
        private List<ExportBlocItem> m_BlocItems = new();
        public List<ExportBlocItem> SelectedBlocs => m_BlocItems.Where(b => b.IsSelected).ToList();
        public bool IsSelected => SelectedBlocs.Count > 0;
        #endregion
        
        #region Events
        [HideInInspector] public UnityEvent OnToggleChanged = new();
        #endregion
        
        #region Public Methods
        public void Initialize(Protocol protocol)
        {
            // Clear existing blocs
            foreach (var bloc in m_BlocItems)
            {
                if (bloc != null) Destroy(bloc.gameObject);
            }
            m_BlocItems.Clear();
            
            m_ProtocolNameText.text = protocol.Name;
            
            // Create bloc items
            foreach (var bloc in protocol.Blocs.OrderBy(b => b.Name))
            {
                GameObject blocItemObj = Instantiate(m_BlocItemPrefab, m_BlocsContainer);
                ExportBlocItem blocItem = blocItemObj.GetComponent<ExportBlocItem>();
                if (blocItem != null)
                {
                    blocItem.Initialize(bloc.Name);
                    blocItem.OnToggleChanged.AddListener(() => OnToggleChanged.Invoke());
                    m_BlocItems.Add(blocItem);
                }
            }
        }
        #endregion
    }
}