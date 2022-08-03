using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using ThirdParty.CielaSpike;

public class CoroutineManager : MonoBehaviour
{
    List<Coroutine> m_coroutines = new List<Coroutine>();
    public ReadOnlyCollection<Coroutine> Coroutines { get { return new ReadOnlyCollection<Coroutine>(m_coroutines); } }

    public Coroutine Add(IEnumerator coroutine)
    {
        Coroutine l_coroutine = this.StartCoroutineAsync(coroutine);
        m_coroutines.Add(l_coroutine);
        return l_coroutine;
    }
    public void StopCoroutineAsync(Coroutine coroutine)
    {
        m_coroutines.Remove(coroutine);
        StopCoroutine(coroutine);
    }
}
