using System.Collections.Generic;
using System.IO;
using System.Linq;
using HBP.Core.Tools;

namespace HBP.Core.Object3D
{
    public class DiFuMoObjects
    {
        #region Properties
        public Dictionary<string, DiFuMoInformation> Information { get; private set; } = new Dictionary<string, DiFuMoInformation>();
        public Dictionary<string, FMRI> FMRIs { get; private set; } = new Dictionary<string, FMRI>();
        public bool Loaded { get { return FMRIs.Count > 0; } }
        private Dictionary<string, bool> m_Loading = new Dictionary<string, bool>() { { "64", false }, { "128", false }, { "256", false }, { "512", false }, { "1024", false } };
        #endregion

        #region Public Methods
        public void Clean()
        {
            foreach (var fmri in FMRIs.Values)
            {
                fmri?.Clean();
            }
        }
        public void Load(string atlas)
        {
            m_Loading[atlas] = true;
            string csvFile = Path.Combine(ApplicationState.DataPath, "Atlases", "DiFuMo", atlas, string.Format("labels_{0}_dictionary.csv", atlas));
            string file = Path.Combine(ApplicationState.DataPath, "Atlases", "DiFuMo", atlas, "3mm", "maps.nii.gz");
            FMRIs.Add(atlas, new FMRI(atlas, file));
            Information.Add(atlas, new DiFuMoInformation(csvFile));
            m_Loading[atlas] = false;
        }
        public bool IsLoaded(string atlas)
        {
            return FMRIs.Keys.Any(a => a == atlas);
        }
        public bool IsLoading(string atlas)
        {
            return m_Loading[atlas];
        }
        #endregion
    }
}