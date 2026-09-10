using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public abstract class BaseSiteConditions : MonoBehaviour
    {
        protected Base3DScene m_Scene;
        protected SiteConditions Conditions;
        public GenericEvent<float> OnFilter = new();
        public GenericEvent<bool> OnEndFilter = new();

        public virtual void Initialize(Base3DScene scene)
        {
            m_Scene = scene;
            Conditions = new SiteConditions(scene);
        }

        protected abstract Func<Core.Object3D.Site, bool> CreateFilter();

        public async UniTaskVoid FilterSitesWithConditions(List<Core.Object3D.Site> sites, CancellationToken token, CancellationToken progressToken)
        {
            bool completed = false;
            try
            {
                using var stop = CancellationTokenSource.CreateLinkedTokenSource(token, progressToken);
                await m_Scene.FilterSitesAsync(sites, CreateFilter(), value => OnFilter.Invoke(value), stop.Token);
                completed = true;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                DialogBoxManager.OpenScrollable(Core.Enums.DialogBoxType.Error, "Unknown error", exception.ToString()).Forget();
            }

            if (this && !token.IsCancellationRequested && !progressToken.IsCancellationRequested) OnEndFilter.Invoke(completed);
        }
    }
}
