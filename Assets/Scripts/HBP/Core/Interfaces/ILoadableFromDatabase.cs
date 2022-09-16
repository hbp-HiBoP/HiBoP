using System;
using System.Collections;
using System.Collections.Generic;
using HBP.Core.Tools;

namespace HBP.Core.Interfaces
{
    public interface ILoadableFromDatabase<T>
    {
        IEnumerator LoadFromDatabase(string path, Action<float, float, LoadingText> OnChangeProgress, Action<IEnumerable<T>> result);
    }
}