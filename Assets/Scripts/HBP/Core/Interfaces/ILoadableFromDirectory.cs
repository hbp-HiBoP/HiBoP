using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;

namespace HBP.Core.Interfaces
{
    public interface ILoadableFromDirectory<T>
    {
        UniTask<IEnumerable<T>> LoadFromDirectory(string[] paths, Action<float, float, LoadingText> updateProgress);
    }
}
