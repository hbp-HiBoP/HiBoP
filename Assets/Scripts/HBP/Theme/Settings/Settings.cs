using UnityEngine;

namespace HBP.Theme
{
    public abstract class Settings : ScriptableObject
    {
        public abstract void Set(GameObject gameObject);
    }
}