using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.UI.Lists
{
    [RequireComponent(typeof(Button))]
	public class ContainerItem : MonoBehaviour
	{
		#region Attributs
        protected GenericEvent<int,int> m_OnAction = new GenericEvent<int,int>();
        public GenericEvent<int,int> OnAction { get { return m_OnAction; } }

        public bool insteractable
        {
            get { return GetComponent<Button>().interactable; }
            set { GetComponent<Button>().interactable = value; }
        }
        #endregion

        #region Public Methods
        public void OnClick()
        {     
            OnAction.Invoke(transform.GetSiblingIndex(),0);
        }
		#endregion
	}
}