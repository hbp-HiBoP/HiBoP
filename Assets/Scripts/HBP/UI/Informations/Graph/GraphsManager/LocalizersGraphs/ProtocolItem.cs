using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HBP.Core.Object3D;
using UnityEngine.UI;

namespace HBP.UI.Informations
{
    public class ProtocolItem : MonoBehaviour
    {
        #region Properties
        [SerializeField] private Text m_ProtocolNameText;
        [SerializeField] private GameObject m_BlocItemPrefab;
        [SerializeField] private Transform m_BlocsContainer;
        
        public string Name => m_ProtocolNameText.text;
        private List<BlocItem> m_Blocs = new List<BlocItem>();
        public List<BlocItem> SelectedBlocs => m_Blocs.Where(b => b.IsSelected).ToList();
        public bool IsSelected => SelectedBlocs.Count > 0;
        #endregion

        #region Public Methods
        public void Initialize(string protocolName)
        {
            foreach (var bloc in m_Blocs)
            {
                Destroy(bloc.gameObject);
            }
            m_Blocs.Clear();

            m_ProtocolNameText.text = protocolName;
            foreach (string blocName in Object3DManager.Localizers.GetAvailableBlocNames(protocolName))
            {
                GameObject blocItemObj = Instantiate(m_BlocItemPrefab, m_BlocsContainer);
                BlocItem blocItem = blocItemObj.GetComponent<BlocItem>();
                blocItem.Initialize(blocName);
                m_Blocs.Add(blocItem);
            }
        }
        #endregion
    }
}