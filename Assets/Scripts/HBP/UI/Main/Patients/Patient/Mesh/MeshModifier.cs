using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System;
using HBP.Core.Tools;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to modify a mesh.
    /// </summary>
    public class MeshModifier : ObjectModifier<Core.Data.BaseMesh>
{
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] FileSelector m_TransformationFileSelector;

        [SerializeField] SingleMeshSubModifier m_SingleMeshSubModifier;
        [SerializeField] LeftRightMeshSubModifier m_LeftRightMeshSubModifier;

        Type[] m_Types;
        List<BaseSubModifier> m_SubModifiers;
        List<Core.Data.BaseMesh> m_MeshesTemp;

        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
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
        /// <summary>
        /// Save the modifications.
        /// </summary>
        public override void OK()
        {
            m_Object = ObjectTemp;
            m_Object.RecalculateUsable();
            base.OK();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onEndEdit.AddListener(ChangeName);
            m_TransformationFileSelector.onValueChanged.AddListener(ChangeTransformation);

            m_TypeDropdown.onValueChanged.AddListener(ChangeType);
            m_Types = m_TypeDropdown.Set(typeof(Core.Data.BaseMesh));

            m_SingleMeshSubModifier.Initialize();
            m_LeftRightMeshSubModifier.Initialize();

            m_SubModifiers = new List<BaseSubModifier>
            {
                m_SingleMeshSubModifier,
                m_LeftRightMeshSubModifier
            };

            m_MeshesTemp = new List<Core.Data.BaseMesh>
            {
                new Core.Data.SingleMesh(),
                new Core.Data.LeftRightMesh()
            };

        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">Mesh to modify</param>
        protected override void SetFields(Core.Data.BaseMesh objectToDisplay)
        {
            int index = m_MeshesTemp.FindIndex(t => t.GetType() == ObjectTemp.GetType());
            m_MeshesTemp[index] = ObjectTemp;

            m_NameInputField.text = objectToDisplay.Name;
            m_TypeDropdown.SetValue(Array.IndexOf(m_Types, objectToDisplay.GetType()));
            m_TransformationFileSelector.File = objectToDisplay.SavedTransformation;
        }
        /// <summary>
        /// Change the name of the mesh.
        /// </summary>
        /// <param name="name">Name</param>
        protected void ChangeName(string name)
        {
            if(name != "")
            {
                ObjectTemp.Name = name;
            }
            else
            {
                m_NameInputField.text = ObjectTemp.Name;
            }
        }
        /// <summary>
        /// Change the type of the mesh.
        /// </summary>
        /// <param name="index">Index of the type</param>
        protected void ChangeType(int index)
        {
            Type type = m_Types[index];

            // Close old subModifier
            m_SubModifiers.Find(subModifier => subModifier.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(m_ObjectTemp.GetType()))).IsActive = false;

            Core.Data.BaseMesh mesh = m_MeshesTemp.Find(t => t.GetType() == type);
            mesh.Copy(m_ObjectTemp);
            m_ObjectTemp = mesh;

            // Open new subModifier;
            BaseSubModifier newSubModifier = m_SubModifiers.Find(subModifier => subModifier.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(type)));
            newSubModifier.IsActive = true;
            newSubModifier.Object = m_ObjectTemp;
        }
        /// <summary>
        /// Change the path to the transformation file of the mesh.
        /// </summary>
        /// <param name="path">Path to the transformation file</param>
        protected void ChangeTransformation(string path)
        {
            ObjectTemp.Transformation = path;
        }
        #endregion
    }
}