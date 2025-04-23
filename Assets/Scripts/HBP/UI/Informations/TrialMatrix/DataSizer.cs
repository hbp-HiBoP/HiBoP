using HBP.Core.Enums;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Informations.TrialMatrix
{
    [RequireComponent(typeof(LayoutElement))]
    public class DataSizer : MonoBehaviour
    {
        #region Properties
        private LayoutElement m_LayoutElement;
        #endregion

        #region Private Methods
        void Awake()
        {
            m_LayoutElement = GetComponent<LayoutElement>();
        }
        void OnRectTransformDimensionsChange()
        {
            SetSize();
        }
        void SetSize()
        {
            CanvasScalerHandler canvasScalerHandler = GetComponentInParent<CanvasScalerHandler>();
            float scale = canvasScalerHandler ? canvasScalerHandler.Scale : 1;
            switch (PersistentDataManager.UserPreferences.Visualization.TrialMatrix.SubBlocFormat)
            {
                case BlocFormatType.ProtocolRatio:
                    m_LayoutElement.enabled = true;
                    var height = GetComponent<RectTransform>().rect.width * PersistentDataManager.UserPreferences.Visualization.TrialMatrix.ProtocolRatio * scale;
                    m_LayoutElement.minHeight = height;
                    m_LayoutElement.preferredHeight = height;
                    break;
                default:
                    m_LayoutElement.enabled = false;
                    break;
            }
        }
        #endregion
    }
}