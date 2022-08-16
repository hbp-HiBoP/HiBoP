using UnityEngine;

namespace HBP.UI.Components
{
    public class CursorWrapper : MonoBehaviour
    {
        public bool Visible
        {
            get
            {
                return Cursor.visible;
            }
            set
            {
                Cursor.visible = value;
            }
        }
    }
}