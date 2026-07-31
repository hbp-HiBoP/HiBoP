using Cysharp.Threading.Tasks;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public abstract class SiteToolSection : MonoBehaviour
    {
        #region Properties

        protected Base3DScene m_Scene;

        public virtual Base3DScene Scene
        {
            get => m_Scene;
            set => m_Scene = value;
        }

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

        public virtual void Initialize()
        {
            LoadSettings();
        }

        public abstract UniTask ApplyAsync();
        public abstract void StoreSettings();
        public abstract void LoadSettings();

        #endregion
    }
}
