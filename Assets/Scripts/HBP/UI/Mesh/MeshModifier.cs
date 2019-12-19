using UnityEngine.UI;
using Tools.Unity;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace HBP.UI
{
    public class MeshModifier : ObjectModifier<Data.BaseMesh>
{
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] FileSelector m_TransformationFileSelector;

        [SerializeField] SingleMeshSubModifier m_SingleMeshSubModifier;
        [SerializeField] LeftRightMeshSubModifier m_LeftRightMeshSubModifier;

        Type[] m_Types;
        List<BaseSubModifier> m_SubModifiers;
        List<Data.BaseMesh> m_MeshesTemp;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_NameInputField.interactable = value;
                m_TypeDropdown.interactable = value;
                m_TransformationFileSelector.interactable = value;

                m_SingleMeshSubModifier.Interactable = value;
                m_LeftRightMeshSubModifier.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void OK()
        {
            item = ItemTemp;
            item.RecalculateUsable();
            base.OK();
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(OnChangeName);
            m_TransformationFileSelector.onValueChanged.AddListener(OnChangeTransformation);

            m_TypeDropdown.onValueChanged.AddListener(OnChangeType);
            m_Types = m_TypeDropdown.Set(typeof(Data.BaseMesh));

            m_SingleMeshSubModifier.Initialize();
            m_LeftRightMeshSubModifier.Initialize();

            m_SubModifiers = new List<BaseSubModifier>
            {
                m_SingleMeshSubModifier,
                m_LeftRightMeshSubModifier
            };

            m_MeshesTemp = new List<Data.BaseMesh>
            {
                new Data.SingleMesh(),
                new Data.LeftRightMesh()
            };

        }
        protected override void SetFields(Data.BaseMesh objectToDisplay)
        {
            int index = m_MeshesTemp.FindIndex(t => t.GetType() == ItemTemp.GetType());
            m_MeshesTemp[index] = ItemTemp;

            m_NameInputField.text = objectToDisplay.Name;
            m_TypeDropdown.SetValue(Array.IndexOf(m_Types, objectToDisplay.GetType()));
            m_TransformationFileSelector.File = objectToDisplay.SavedTransformation;
        }

        protected void OnChangeName(string value)
        {
            if(value != "")
            {
                ItemTemp.Name = value;
            }
            else
            {
                m_NameInputField.text = ItemTemp.Name;
            }
        }
        protected void OnChangeType(int value)
        {
            Type type = m_Types[value];

            // Close old subModifier
            m_SubModifiers.Find(subModifier => subModifier.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(itemTemp.GetType()))).IsActive = false;

            Data.BaseMesh mesh = m_MeshesTemp.Find(t => t.GetType() == type);
            mesh.Copy(itemTemp);
            itemTemp = mesh;

            // Open new subModifier;
            BaseSubModifier newSubModifier = m_SubModifiers.Find(subModifier => subModifier.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(type)));
            newSubModifier.IsActive = true;
            newSubModifier.Object = itemTemp;
        }
        protected void OnChangeTransformation(string value)
        {
            ItemTemp.Transformation = value;
        }
        #endregion
    }
}