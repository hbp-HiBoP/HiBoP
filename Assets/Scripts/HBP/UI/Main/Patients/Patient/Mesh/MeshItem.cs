using UnityEngine;
using HBP.Theme;
using HBP.UI.Tools.Lists;

namespace HBP.UI.Main
{
    /// <summary>
    /// Component to display mesh in list.
    /// </summary>
    public class MeshItem : ActionnableItem<Core.Data.BaseMesh>
    {
        #region Properties
        [SerializeField] UnityEngine.UI.Text m_NameInputField;
        [SerializeField] ThemeElement m_MeshThemeElement;
        [SerializeField] ThemeElement m_MarsAtlasThemeElement;
        [SerializeField] ThemeElement m_TransformationThemeElement;

        [SerializeField] Theme.State m_ErrorState;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override Core.Data.BaseMesh Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameInputField.text = value.Name;
                if (value.HasMesh) m_MeshThemeElement.Set();
                else m_MeshThemeElement.Set(m_ErrorState);
                if (value.HasMarsAtlas) m_MarsAtlasThemeElement.Set();
                else m_MarsAtlasThemeElement.Set(m_ErrorState);
                if (value.HasTransformation) m_TransformationThemeElement.Set();
                else m_TransformationThemeElement.Set(m_ErrorState);
            }
        }
        #endregion
    }
}