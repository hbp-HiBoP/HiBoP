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
                TrialMatrixZone.Scene = value;
                GraphZone.Scene = value;
            }
        }

        public GraphZone GraphZone;
        public TrialMatrixZone TrialMatrixZone;

        Site[] m_Sites;
        #endregion

        #region Public Methods
        public void Display(IEnumerable<Site> sites)
        {
            Debug.Log("Display " + sites.First().Information.DisplayedName);
            m_Sites = sites.ToArray();
            TrialMatrixZone.Display(sites);
            //GraphZone.Display(sites);
        }
        #endregion

        #region Private Methods
        void OnMinimizeColumns()
        {
            //DisplayCurves();
        }
        void OnUpdateROI()
        {
            //GenerateCurves();
            //DisplayCurves();
        }
        #endregion
    }
}