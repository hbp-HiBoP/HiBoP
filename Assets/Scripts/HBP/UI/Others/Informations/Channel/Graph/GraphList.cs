using System.Collections;
using Tools.Unity.Graph;
using UnityEngine;
using CielaSpike;
using UnityEngine.Profiling;

public class GraphList : MonoBehaviour
{
    public GameObject SimplifiedGraph;
    public RectTransform Content;

    public void Display(GraphData[] graphs)
    {
        this.StartCoroutineAsync(c_Display(graphs));
    }

    IEnumerator c_AddGraph(GraphData graph)
    {
        Profiler.BeginSample("Instantiate graph");
        SimplifiedGraph simplifiedGraph = Instantiate(SimplifiedGraph, Content).GetComponent<SimplifiedGraph>();
        Profiler.EndSample();
        yield return new WaitForEndOfFrame();
        Profiler.BeginSample("Plot graph");
        simplifiedGraph.Plot(graph);
        Profiler.EndSample();
    }
     IEnumerator c_Display(GraphData[] graphs)
    {
        foreach (var graph in graphs)
        {
            yield return Ninja.JumpToUnity;
            StartCoroutine(c_AddGraph(graph));
            yield return Ninja.JumpBack;
        }
    }
}
