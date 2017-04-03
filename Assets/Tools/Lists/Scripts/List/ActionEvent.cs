using UnityEngine.Events;

namespace Tools.Unity.Lists
{
    public class ActionEvent<T> : UnityEvent<T,int> {}
    public class SelectEvent : UnityEvent {};
}