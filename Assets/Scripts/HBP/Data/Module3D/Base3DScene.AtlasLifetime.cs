using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using HBP.Core.Object3D;
using HBP.Core.Tools;

namespace HBP.Data.Module3D
{
    public partial class Base3DScene
    {
        public System.Collections.Generic.IEnumerable<string> RequiredAtlasIds()
        {
            // MarsAtlas also supplies implantation tags even when its overlay is hidden.
            yield return "mars";
            var state = Visualization?.Configuration?.AtlasConfiguration;
            if (state == null) yield break;
            if (state.JuBrain) yield return "jubrain";
            if (state.IBC) yield return "ibc";
            if (!string.IsNullOrEmpty(state.DiFuMoAtlas)) yield return "difumo:" + state.DiFuMoAtlas;
            if (!string.IsNullOrEmpty(state.LocalizerProtocol)) yield return "localizer:" + state.LocalizerProtocol;
        }

        public event Action<string> AtlasUseAdmission;

        public void EnsureAtlasCanBeUsed(string id)
        {
            ResourceRetention.EnsureCanUse(id);
            AtlasUseAdmission?.Invoke(id);
        }

        private static void RegisterAtlasUsage()
        {
            ResourceRetention.ReleaseBlockReason -= AtlasReleaseBlockReason;
            ResourceRetention.ReleaseBlockReason += AtlasReleaseBlockReason;
        }

        private static string AtlasReleaseBlockReason(string id)
        {
            foreach (var scene in s_LiveScenes)
            {
                if (!scene) continue;
                // Cleanup removes a scene only after its workers have actually completed.
                bool worker = scene.m_UpdatingGenerators || !scene.m_InitializationWork.Status.IsCompleted() || !scene.m_AnatomyWork.Status.IsCompleted() || !scene.m_CorrelationWork.Status.IsCompleted();
                bool retained = worker && AtlasResources.IsLoaded(id);
                if (id == "mars") retained |= scene.AtlasManager != null && scene.AtlasManager.DisplayMarsAtlas || scene.Columns.Any(column => column is Column3DCCEP ccep && ccep.Mode == Column3DCCEP.CCEPMode.MarsAtlas && ccep.SelectedSourceMarsAtlasLabel >= 0);
                else if (id == "jubrain") retained |= scene.AtlasManager != null && scene.AtlasManager.DisplayJuBrainAtlas;
                else if (scene.FMRIManager != null)
                {
                    var fmri = scene.FMRIManager;
                    if (id == "ibc") retained |= fmri.DisplayIBCContrasts;
                    else if (id.StartsWith("difumo:")) retained |= fmri.DisplayDiFuMo && "difumo:" + fmri.SelectedDiFuMoAtlas == id;
                    else if (id.StartsWith("localizer:")) retained |= fmri.DisplayLocalizers && "localizer:" + fmri.SelectedLocalizersProtocol == id;
                }

                if (retained) return "The visualization '" + scene.Name + "' is using " + id + ". Disable its dependent display or close the visualization first.";
            }

            return null;
        }
    }
}
