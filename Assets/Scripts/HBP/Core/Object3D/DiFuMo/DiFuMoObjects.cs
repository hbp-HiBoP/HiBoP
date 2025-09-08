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
        public bool Loaded => FMRIs.Values.Any(f => f.Loaded) && Information.Values.Any(i => i.Loaded);
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
            string csvFile = Path.Combine(ApplicationState.DataPath, "Atlases", "DiFuMo", atlas, string.Format("labels_{0}_dictionary.csv", atlas));
            string file = Path.Combine(ApplicationState.DataPath, "Atlases", "DiFuMo", atlas, "3mm", "maps.nii.gz");
            FMRIs.Add(atlas, new FMRI(atlas, file));
            Information.Add(atlas, new DiFuMoInformation(csvFile));
        }
        public bool IsLoaded(string atlas)
        {
            if (FMRIs.TryGetValue(atlas, out FMRI fmri) && Information.TryGetValue(atlas, out DiFuMoInformation information))
            {
                return fmri.Loaded && information.Loaded;
            }
            return false;
        }
        public bool IsLoading(string atlas)
        {
            if (FMRIs.TryGetValue(atlas, out FMRI fmri) && Information.TryGetValue(atlas, out DiFuMoInformation information))
            {
                return fmri.Loading || information.Loading;
            }
            return false;
        }
        #endregion
    }
}