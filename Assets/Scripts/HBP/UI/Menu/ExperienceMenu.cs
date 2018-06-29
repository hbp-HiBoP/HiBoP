using UnityEngine;

namespace HBP.UI.Experience
{
    public class ExperienceMenu : MonoBehaviour
    {
        [SerializeField]
        GameObject protocolGestionPrefab;

        [SerializeField]
        GameObject datasetGestionPrefab;

        public void OpenProtocolGestion()
        {
            Protocol.ProtocolGestion.Open(true);
        }

        public void OpenDatasetGestion()
        {
            Dataset.DatasetModifier.Open(true);
        }
    }
}