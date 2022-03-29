using UnityEngine;

namespace NewTheme
{
    public abstract class Settings : ScriptableObject
    {
        public abstract void Set(GameObject gameObject);
    }
}