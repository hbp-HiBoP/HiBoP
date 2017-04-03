using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(Button))]
	public class ContainerItem : MonoBehaviour
	{
		#region Attributs
        protected ActionEvent<int> m_action = new ActionEvent<int>();
        public ActionEvent<int> ActionEvent { get { return m_action; } }

        public bool isInteractable
        {
            get { return GetComponent<Button>().interactable; }
            set { GetComponent<Button>().interactable = value; }
        }
        #endregion

        #region Public Methods
        public void OnClick()
        {
            ActionEvent.Invoke(transform.GetSiblingIndex(),0);
        }
		#endregion
	}
}