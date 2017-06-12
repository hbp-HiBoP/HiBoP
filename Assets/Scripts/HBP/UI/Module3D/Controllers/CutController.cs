using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.UI.Module3D {
    public class CutController : MonoBehaviour {
        #region Properties
        /// <summary>
        /// Associated scene
        /// </summary>
        private Base3DScene m_Scene;
        /// <summary>
        /// Content of the ScrollView
        /// </summary>
        [SerializeField]
        private RectTransform m_Content;
        /// <summary>
        /// Prefab to control the parameters of a cut
        /// </summary>
        [SerializeField]
        private GameObject m_CutControlPrefab;
        /// <summary>
        /// Reference to the transform of the button used to add a cut
        /// </summary>
        [SerializeField]
        private RectTransform m_AddCutButton;
        #endregion

        #region Public Methods
        public void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
        }
        public void AddNewCut()
        {
            Cut cut = m_Scene.AddCutPlane();
            CutParametersController controller = Instantiate(m_CutControlPrefab, m_Content).GetComponent<CutParametersController>();
            controller.Initialize(m_Scene, cut);
            m_AddCutButton.SetAsLastSibling();
        }
        #endregion
    }
}