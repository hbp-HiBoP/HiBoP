using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HBP.Core.Tools;

namespace HBP.Core.Interfaces
{
    public interface ILoadableFromDirectory<T>
    {
        Task<IEnumerable<T>> LoadFromDirectory(string[] paths, Action<float, float, LoadingText> updateProgress);
    }
}