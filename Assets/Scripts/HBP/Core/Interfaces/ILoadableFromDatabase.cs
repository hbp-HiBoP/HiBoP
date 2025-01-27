using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using HBP.Core.Tools;

namespace HBP.Core.Interfaces
{
    public interface ILoadableFromDatabase<T>
    {
        Task<IEnumerable<T>> LoadFromDatabase(Action<float, float, LoadingText> updateProgress);
    }
}