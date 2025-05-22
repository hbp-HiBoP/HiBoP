using HBP.Core.Data;
using HBP.Core.DLL;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class SpecificSiteLocationFilterConditionSubModifier : SubModifier<SpecificSiteLocationFilterCondition>
    {
        #region Properties
        [SerializeField] Dropdown m_LocationTypeDropdown;
        [SerializeField] GameObject m_MeshPart;
        [SerializeField] Dropdown m_MeshPartDropdown;
        [SerializeField] GameObject m_AtlasType;
        [SerializeField] Dropdown m_AtlasTypeDropdown;
        [SerializeField] GameObject m_AtlasArea;
        [SerializeField] Dropdown m_AtlasAreaDropdown;

        private List<string> m_CurrentAtlasAreas = new List<string>();
        private bool m_UpdatingAtlasAreaDropdown = false;

        protected List<object> m_FilteringObjects;
        public List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_LocationTypeDropdown.onValueChanged.AddListener(OnChangeLocationType);
            m_MeshPartDropdown.onValueChanged.AddListener(OnChangeMeshPart);
            m_AtlasTypeDropdown.onValueChanged.AddListener(OnChangeAtlasType);
            m_AtlasAreaDropdown.onValueChanged.AddListener(OnChangeAtlasArea);
        }
        #endregion

        #region Private Methods
        protected override void SetFields(SpecificSiteLocationFilterCondition objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_LocationTypeDropdown.Set(typeof(SpecificSiteLocationFilterCondition.SpecificLocationType), (int)objectToDisplay.LocationType);
            m_MeshPartDropdown.Set(typeof(MeshPart), (int)objectToDisplay.MeshPart);
            m_AtlasTypeDropdown.Set(typeof(SpecificSiteLocationFilterCondition.Atlas), (int)objectToDisplay.AtlasType);

            UpdateAtlasAreaDropdown(objectToDisplay.AtlasType, objectToDisplay.AtlasArea);
            UpdateFieldVisibility(objectToDisplay.LocationType);
        }
        private void OnChangeLocationType(int value)
        {
            Object.LocationType = (SpecificSiteLocationFilterCondition.SpecificLocationType)value;
            UpdateFieldVisibility(Object.LocationType);
        }
        private void OnChangeMeshPart(int value)
        {
            Object.MeshPart = (MeshPart)value;
        }
        private void OnChangeAtlasType(int value)
        {
            Object.AtlasType = (SpecificSiteLocationFilterCondition.Atlas)value;
            UpdateAtlasAreaDropdown(Object.AtlasType, Object.AtlasArea);
        }
        private void OnChangeAtlasArea(int value)
        {
            if (value >= 0 && value < m_CurrentAtlasAreas.Count && !m_UpdatingAtlasAreaDropdown)
            {
                Object.AtlasArea = m_CurrentAtlasAreas[value];
            }
        }
        private void UpdateFieldVisibility(SpecificSiteLocationFilterCondition.SpecificLocationType type)
        {
            m_MeshPart.SetActive(type == SpecificSiteLocationFilterCondition.SpecificLocationType.BrainMesh);
            bool isAtlas = type == SpecificSiteLocationFilterCondition.SpecificLocationType.Atlas;
            m_AtlasType.gameObject.SetActive(isAtlas);
            m_AtlasArea.gameObject.SetActive(isAtlas);
        }
        private void UpdateAtlasAreaDropdown(SpecificSiteLocationFilterCondition.Atlas atlasType, string selectedArea)
        {
            BrainAtlas atlas = null;
            switch (atlasType)
            {
                case SpecificSiteLocationFilterCondition.Atlas.MarsAtlas:
                    atlas = Object3DManager.MarsAtlas;
                    break;
                case SpecificSiteLocationFilterCondition.Atlas.Jubrain:
                    atlas = Object3DManager.JuBrain;
                    break;
            }

            m_UpdatingAtlasAreaDropdown = true;

            m_CurrentAtlasAreas.Clear();
            m_AtlasAreaDropdown.ClearOptions();

            if (atlas != null)
            {
                var areaNames = atlas.AreaNames;
                m_CurrentAtlasAreas.AddRange(areaNames);
                m_AtlasAreaDropdown.AddOptions(new List<string>(areaNames));

                int selectedIndex = 0;
                if (!string.IsNullOrEmpty(selectedArea))
                {
                    selectedIndex = m_CurrentAtlasAreas.IndexOf(selectedArea);
                    if (selectedIndex < 0) selectedIndex = 0;
                }

                m_UpdatingAtlasAreaDropdown = false;

                m_AtlasAreaDropdown.SetValue(selectedIndex);
            }

            m_UpdatingAtlasAreaDropdown = false;
        }
        #endregion
    }
}