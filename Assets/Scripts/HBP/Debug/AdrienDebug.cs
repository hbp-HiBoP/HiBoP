using CielaSpike;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class AdrienDebug : MonoBehaviour
{
    public int Steps = 100;
    public float WaitingTimeInMilliSeconds = 100;

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyUp(KeyCode.Space))
        //{
        //    Load();
        //}
    }

    void Load()
    {
        Debug.Log("Load");
        GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
        ApplicationState.LoadingManager.Load(BigLoading(onChangeProgress), onChangeProgress, OnEnd);
        Debug.Log("trtzt");
    }

    IEnumerator HeavyTask1(int duration, float waitingTimeInMilliSeconds, float progress, GenericEvent<float, float, LoadingText> onChangeProgress, Action<float> output)
    {
        Debug.Log("HeavyTask1 begin");
        float waitingTime = waitingTimeInMilliSeconds / 1000;
        for (int i = 1; i <= duration; i++)
        {
            progress += 0.5f * ((float)1 / duration);
            onChangeProgress.Invoke(progress, 0, new LoadingText(string.Format("[{0}/{1}] Loading", i , duration)));
            yield return new WaitForSeconds(waitingTime);
        }
        output(progress);
        Debug.Log("HeavyTask1 end");
    }

    IEnumerator HeavyTask2(int duration, float waitingTimeInMilliSeconds, float progress, GenericEvent<float, float, LoadingText> onChangeProgress)
    {
        Debug.Log("HeavyTask2 begin");
        float waitingTime = waitingTimeInMilliSeconds / 1000;
        for (int i = 1; i <= duration; i++)
        {
            onChangeProgress.Invoke(progress + 0.5f * ((float)i / duration), 0, new LoadingText(string.Format("[{0}/{1}] Loading", i, duration)));
            yield return new WaitForSeconds(waitingTime);
        }
        Debug.Log("HeavyTask2 end");
    }

    IEnumerator BigLoading(GenericEvent<float, float, LoadingText> onChangeProgress)
    {
        float progress = 0;
        yield return Ninja.JumpToUnity;
        yield return this.StartCoroutineAsync(HeavyTask1(77, 100, progress, onChangeProgress,value => progress = value));
        yield return this.StartCoroutineAsync(HeavyTask2(666, 10, progress, onChangeProgress));
    }

    void OnEnd(TaskState taskState)
    {
        Debug.Log("End");
        Debug.Log(taskState.ToString());
    }

}
