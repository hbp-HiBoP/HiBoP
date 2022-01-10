using System;
using System.Collections;
using System.Collections.Generic;

public interface ILoadableFromDatabase<T>
{
    IEnumerator LoadFromDatabase(string path, Action<float, float, LoadingText> OnChangeProgress, Action<IEnumerable<T>> result);
}
