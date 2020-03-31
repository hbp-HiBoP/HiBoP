using UnityEngine;
using UnityEngine.UI;
using Tools.Unity.Lists;
using NewTheme.Components;

namespace HBP.UI
{
    /// <summary>
    /// Component to display mesh in list.
    /// </summary>
    public class MeshItem : ActionnableItem<Data.BaseMesh>
    {
        #region Properties
        [SerializeField] Text m_NameInputField;
        [SerializeField] ThemeElement m_MeshThemeElement;
        [SerializeField] ThemeElement m_MarsAtlasThemeElement;
        [SerializeField] ThemeElement m_TransformationThemeElement;

        [SerializeField] State m_ErrorState;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override Data.BaseMesh Object
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