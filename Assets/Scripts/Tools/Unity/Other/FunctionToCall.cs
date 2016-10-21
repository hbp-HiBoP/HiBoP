using UnityEngine;
using System.Reflection;
using System.Linq;

namespace Tools.Unity
{
	[System.Serializable]
	public class FunctionToCall
	{
        [SerializeField]
        GameObject m_gameObject;
        public GameObject GameObject { get { return m_gameObject; } set { m_gameObject = value; } }

        [SerializeField]
        Component m_component;
        public Component Component { get { return m_component; } set { m_component = value; } }
        public int IndexComponent
        {
            get
            {
                Component[] l_components = GameObject.GetComponents<Component>();
                int l_index = 0;
                for (int i = 0; i < l_components.Length; i++)
                {
                    if (Component == l_components[i])
                    {
                        l_index = i;
                        break;
                    }
                }
                return l_index;
            }
            set
            {
                Component = GameObject.GetComponents<Component>()[value];
            }
        }

        [SerializeField]
        string m_method;
        public string Method { get { return m_method; } set { m_method = value; } }
        public int IndexMethod
        {
            get
            {
                MethodInfo[] l_methods = Component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(x => x.GetParameters().Length == 0).ToArray();
                int l_index = 0;
                for (int i = 0; i < l_methods.Length; i++)
                {
                    if (Method == l_methods[i].Name)
                    {
                        l_index = i;
                        break;
                    }
                }
                return l_index;
            }
            set
            {
                Method = Component.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(x => x.GetParameters().Length == 0).ToArray()[value].Name;
            }
        }

        public void Send()
        {
            if(GameObject != null && Component != null && Method != string.Empty)
            {
                Component.SendMessage(Method);
            }
        }
	}
}
