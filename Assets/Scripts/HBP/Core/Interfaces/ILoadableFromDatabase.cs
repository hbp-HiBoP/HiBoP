using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;

namespace HBP.Core.Interfaces
{
    public interface ILoadableFromDatabase<T>
    {
        UniTask<IEnumerable<T>> LoadFromDatabase(Action<float, float, LoadingText> updateProgress);
    }
}