using HBP.Theme.Components;
using HBP.UI.Tools.Lists;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.UI.Main.QuickStart
{
    public class FunctionalDataItem : Item<FunctionalData>
    {
        #region Properties
        [SerializeField] private Text m_PatientName;

        [SerializeField] private RectTransform m_BrainVisionRectTransform;
        [SerializeField] private FileSelector m_BrainVisionHeader;

        [SerializeField] private RectTransform m_MicromedRectTransform;
        [SerializeField] private FileSelector m_MicromedFile;

        [SerializeField] private RectTransform m_ELANRectTransform;
        [SerializeField] private FileSelector m_ELANEEGFile;
        [SerializeField] private FileSelector m_ELANPOSFile;

        [SerializeField] private RectTransform m_EDFRectTransform;
        [SerializeField] private FileSelector m_EDFFile;

        [SerializeField] private RectTransform m_FIFRectTransform;
        [SerializeField] private FileSelector m_FIFFile;

        [SerializeField] ThemeElement m_StateThemeElement;
        [SerializeField] Tooltip m_ErrorText;
        [SerializeField] Theme.State m_OKState;
        [SerializeField] Theme.State m_ErrorState;

        public override FunctionalData Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_PatientName.text = value.DataInfo.Patient.Name;

                if (value.DataInfo.DataContainer.GetType() == typeof(Core.Data.Container.BrainVision))
                {
                    m_BrainVisionRectTransform.gameObject.SetActive(true);
                    m_MicromedRectTransform.gameObject.SetActive(false);
                    m_ELANRectTransform.gameObject.SetActive(false);
                    m_EDFRectTransform.gameObject.SetActive(false);
                    m_FIFRectTransform.gameObject.SetActive(false);
                }
                else if (value.DataInfo.DataContainer.GetType() == typeof(Core.Data.Container.Micromed))
                {
                    m_BrainVisionRectTransform.gameObject.SetActive(false);
                    m_MicromedRectTransform.gameObject.SetActive(true);
                    m_ELANRectTransform.gameObject.SetActive(false);
                    m_EDFRectTransform.gameObject.SetActive(false);
                    m_FIFRectTransform.gameObject.SetActive(false);
                }
                else if (value.DataInfo.DataContainer.GetType() == typeof(Core.Data.Container.Elan))
                {
                    m_BrainVisionRectTransform.gameObject.SetActive(false);
                    m_MicromedRectTransform.gameObject.SetActive(false);
                    m_ELANRectTransform.gameObject.SetActive(true);
                    m_EDFRectTransform.gameObject.SetActive(false);
                    m_FIFRectTransform.gameObject.SetActive(false);
                }
                else if (value.DataInfo.DataContainer.GetType() == typeof(Core.Data.Container.EDF))
                {
                    m_BrainVisionRectTransform.gameObject.SetActive(false);
                    m_MicromedRectTransform.gameObject.SetActive(false);
                    m_ELANRectTransform.gameObject.SetActive(false);
                    m_EDFRectTransform.gameObject.SetActive(true);
                    m_FIFRectTransform.gameObject.SetActive(false);
                }
                else if (value.DataInfo.DataContainer.GetType() == typeof(Core.Data.Container.FIF))
                {
                    m_BrainVisionRectTransform.gameObject.SetActive(false);
                    m_MicromedRectTransform.gameObject.SetActive(false);
                    m_ELANRectTransform.gameObject.SetActive(false);
                    m_EDFRectTransform.gameObject.SetActive(false);
                    m_FIFRectTransform.gameObject.SetActive(true);
                }
                else
                {
                    m_BrainVisionRectTransform.gameObject.SetActive(false);
                    m_MicromedRectTransform.gameObject.SetActive(false);
                    m_ELANRectTransform.gameObject.SetActive(false);
                    m_EDFRectTransform.gameObject.SetActive(false);
                    m_FIFRectTransform.gameObject.SetActive(false);
                }
                m_BrainVisionHeader.File = value.BrainVisionDataContainer.Header;
                m_MicromedFile.File = value.MicromedDataContainer.Path;
                m_ELANEEGFile.File = value.ElanDataContainer.EEG;
                m_ELANPOSFile.File = value.ElanDataContainer.POS;
                m_EDFFile.File = value.EDFDataContainer.File;
                m_FIFFile.File = value.FIFDataContainer.File;

                m_BrainVisionHeader.onValueChanged.RemoveAllListeners();
                m_BrainVisionHeader.onValueChanged.AddListener(t =>
                {
                    value.BrainVisionDataContainer.Header = t;
                    value.DataInfo.GetErrors(ApplicationState.ProjectLoaded.Protocols[0]);
                });
                m_MicromedFile.onValueChanged.RemoveAllListeners();
                m_MicromedFile.onValueChanged.AddListener(t =>
                {
                    value.MicromedDataContainer.Path = t;
                    value.DataInfo.GetErrors(ApplicationState.ProjectLoaded.Protocols[0]);
                });
                m_ELANEEGFile.onValueChanged.RemoveAllListeners();
                m_ELANEEGFile.onValueChanged.AddListener(t =>
                {
                    value.ElanDataContainer.EEG = t;
                    value.DataInfo.GetErrors(ApplicationState.ProjectLoaded.Protocols[0]);
                });
                m_ELANPOSFile.onValueChanged.RemoveAllListeners();
                m_ELANPOSFile.onValueChanged.AddListener(t =>
                {
                    value.ElanDataContainer.POS = t;
                    value.DataInfo.GetErrors(ApplicationState.ProjectLoaded.Protocols[0]);
                });
                m_EDFFile.onValueChanged.RemoveAllListeners();
                m_EDFFile.onValueChanged.AddListener(t =>
                {
                    value.EDFDataContainer.File = t;
                    value.DataInfo.GetErrors(ApplicationState.ProjectLoaded.Protocols[0]);
                });
                m_FIFFile.onValueChanged.RemoveAllListeners();
                m_FIFFile.onValueChanged.AddListener(t =>
                {
                    value.FIFDataContainer.File = t;
                    value.DataInfo.GetErrors(ApplicationState.ProjectLoaded.Protocols[0]);
                });

                m_ErrorText.Text = value.DataInfo.GetErrorsMessage();
                m_StateThemeElement.Set(value.DataInfo.IsOk ? m_OKState : m_ErrorState);
            }
        }
        #endregion
    }
}