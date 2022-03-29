using UnityEngine;

namespace Theme
{
    public abstract class Settings : ScriptableObject
    {
        public abstract void Set(GameObject gameObject);
    }
}