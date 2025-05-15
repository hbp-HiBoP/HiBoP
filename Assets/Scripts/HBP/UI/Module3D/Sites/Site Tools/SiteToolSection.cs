using Cysharp.Threading.Tasks;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.UI.Module3D
{
    public abstract class SiteToolSection : MonoBehaviour
    {
        #region Properties
        public Base3DScene Scene { get; set; }
        public ApplyFor ApplyFor { get; set; }
        protected virtual List<Site> Sites
        {
            get
            {
                return ApplyFor switch
                {
                    ApplyFor.FilteredSites => Scene.SelectedColumn.Sites.FindAll(s => s.State.IsFiltered && !s.State.IsMasked),
                    ApplyFor.AllSites => Scene.SelectedColumn.Sites.FindAll(s => !s.State.IsMasked),
                    _ => throw new ArgumentOutOfRangeException(nameof(ApplyFor), ApplyFor, null),
                };
            }
        }
        #endregion

        #region Public Methods
        public abstract void Initialize();
        public abstract UniTask ApplyAsync();
        #endregion
    }
}