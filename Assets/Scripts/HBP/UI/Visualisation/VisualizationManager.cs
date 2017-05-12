using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using data = HBP.Data.Visualization;
using HBP.Module3D;

namespace HBP.UI.Visualization
{
    public class VisualizationManager : MonoBehaviour
    {
        #region Properties
        HiBoP_3DModule_API m_Module3D = new HiBoP_3DModule_API();
        List<data.Visualization> m_Vizualizations = new List<data.Visualization>();
        ReadOnlyCollection<data.Visualization> Visualizations { get { return new ReadOnlyCollection<data.Visualization>(m_Vizualizations); } }
        #endregion

        #region Public Methods
        public void Add(data.Visualization visualization)
        {
            visualization.GetPatients().Where((p) => !p.Brain.ImplantationIsLoaded).ToList().ForEach((p) => p.Brain.LoadImplantation(true));

            m_Vizualizations.Add(visualization);
            visualization.Load();
            //m_Module3D.Add(visualization);
        }
        public void Remove(data.Visualization visualization)
        {
            m_Vizualizations.Remove(visualization);
            //m_Module3D.Remove(visualization);
            visualization.Unload();

            visualization.GetPatients().Where((p) => !m_Vizualizations.Any((v) => v.GetPatients().Contains(p))).ToList().ForEach((p) => p.Brain.UnloadImplantation());
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Module3D = FindObjectOfType<HiBoP_3DModule_API>();
        }
        #endregion
    }
}