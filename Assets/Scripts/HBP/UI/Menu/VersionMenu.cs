using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    public class VersionMenu : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Text>().text = string.Format("{0} {1}", Application.productName, Application.version);
        }
    }
}