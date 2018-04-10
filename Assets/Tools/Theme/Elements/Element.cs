using UnityEngine;

namespace NewTheme
{
    public abstract class Element : ScriptableObject
    {
        public abstract void Set(GameObject gameObject);
    }
}