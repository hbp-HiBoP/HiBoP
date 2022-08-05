using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    public class BlockerButton : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            GetComponent<Button>().onClick.Invoke();
        }
    }
}