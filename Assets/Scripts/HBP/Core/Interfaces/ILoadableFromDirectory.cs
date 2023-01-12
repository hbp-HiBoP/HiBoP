using System;
using System.Collections;
using System.Collections.Generic;
using HBP.Core.Tools;

namespace HBP.Core.Interfaces
{
    public interface ILoadableFromDirectory<T>
    {
        IEnumerator LoadFromDirectory(string[] paths, Action<float, float, LoadingText> OnChangeProgress, Action<IEnumerable<T>> result);
    }
}