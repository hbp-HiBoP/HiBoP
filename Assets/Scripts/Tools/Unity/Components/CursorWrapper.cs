using UnityEngine;

namespace HBP.UI.Tools
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