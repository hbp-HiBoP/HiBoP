using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;

namespace HBP.UI.Anatomy
{
    public class MeshItem : ActionnableItem<Data.Anatomy.Mesh>
    {
        #region Properties
        [SerializeField] Text m_NameInputField;
        [SerializeField] Image m_Mesh;
        [SerializeField] Image m_MarsAtlas;
        [SerializeField] Image m_Transformation;
        public override Data.Anatomy.Mesh Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameInputField.text = value.Name;
                //Color normalColor = ApplicationState.GeneralSettings.Theme.Window.Content.Item.Text.Color;
                Color normalColor = Color.white;
                Color notInteractableColor = ApplicationState.UserPreferences.Theme.General.NotInteractable;
                m_Mesh.color = value.HasMesh ? normalColor : notInteractableColor;
                m_MarsAtlas.color = value.HasMarsAtlas ? normalColor : notInteractableColor;
                m_Transformation.color = value.HasTransformation ? normalColor : notInteractableColor;
            }
        }
        #endregion
    }
}