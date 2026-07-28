using System.Linq;
using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Data;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Core.Database;

namespace HBP.UI.Main
{
    public class AliasCollectionModifier : ObjectModifier<AliasCollection>
    {
        #region Properties
        [SerializeField] AliasListGestion m_AliasListGestion;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;

                m_AliasListGestion.Interactable = value;
                m_AliasListGestion.Modifiable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void OK()
        {
            Project project = ApplicationState.LoadedProject;
            GlobalDatabase database = DatabaseManager.Database;
            ValidationRequest projectRequest =
                ValidationImpactAnalyzer.ForAliases(
                    Object.Aliases,
                    ObjectTemp.Aliases,
                    project?.Datasets.SelectMany(dataset => dataset.Data),
                    project?.Patients);
            ValidationRequest databaseRequest =
                ValidationImpactAnalyzer.ForAliases(
                    Object.Aliases,
                    ObjectTemp.Aliases,
                    database.DataInfos,
                    database.Patients);
            base.OK();
            PersistentDataManager.Aliases.Save();
            MarkAliasDependentStatesStale(
                projectRequest,
                project?.Datasets.SelectMany(dataset => dataset.Data),
                project?.Patients);
            MarkAliasDependentStatesStale(
                databaseRequest,
                database.DataInfos,
                database.Patients);
            if (projectRequest.Aspects != ValidationAspect.None)
            {
                project?.InvalidateValidation(projectRequest);
            }
            if (databaseRequest.Aspects != ValidationAspect.None)
            {
                database.InvalidateValidation(databaseRequest);
            }
        }

        private static void MarkAliasDependentStatesStale(
            ValidationRequest request,
            System.Collections.Generic.IEnumerable<DataInfo> dataInfos,
            System.Collections.Generic.IEnumerable<Patient> patients)
        {
            foreach (DataInfo dataInfo in dataInfos ??
                System.Array.Empty<DataInfo>())
            {
                if (request.Matches(
                    dataInfo,
                    ValidationAspect.SourceAvailability))
                {
                    dataInfo.MarkValidationStale(
                        ValidationAspect.SourceAvailability |
                        ValidationAspect.SourceReadability |
                        ValidationAspect.StaticContent |
                        ValidationAspect.Epoching |
                        ValidationAspect.ChannelMapping);
                }
            }
            foreach (Patient patient in patients ??
                System.Array.Empty<Patient>())
            {
                if (request.Matches(patient))
                {
                    patient.MarkAssetValidationStale();
                }
            }
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_AliasListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
        }
        protected override void SetFields(AliasCollection objectToDisplay)
        {
            m_AliasListGestion.List.Set(objectToDisplay.Aliases);
        }
        #endregion
    }
}
