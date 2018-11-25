using UnityEngine;
using HBP.Module3D;
using System.Linq;
using System.Collections.Generic;

namespace HBP.UI.Informations
{
    public class SiteInformations : MonoBehaviour
    {
        #region Properties
        Base3DScene m_Scene;
        public Base3DScene Scene
        {
            get { return m_Scene; }
            set
            {
                if(m_Scene != null) m_Scene.OnRequestSiteInformation.RemoveListener(Display);
                m_Scene = value;
                m_Scene.OnRequestSiteInformation.AddListener(Display);
                //m_TrialMatrixZone.Scene = value;
                //m_GraphZone.Scene = value;
            }
        }

        List<string> m_Channels = new List<string>();
        public string[] Channels
        {
            get
            {
                return m_Channels.ToArray();
            }
            set
            {
                m_Channels = value.ToList();
                // TODO
            }
        }

        [SerializeField] GraphZone m_GraphZone;
        [SerializeField] TrialMatrixZone m_TrialMatrixZone;
        #endregion

        #region Public Methods
        public void Display(IEnumerable<Site> sites)
        {
            //m_ChannelsSites = sites.ToArray();
            //m_TrialMatrixZone.Display(sites);
            //GraphZone.Display(sites);
        }
        #endregion
    }
}