using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GravityManager;

public class TutorialScene : MonoBehaviour
{
    public enum ClearCondition {ConsumeAll,ConsumeTillNum};
    public float Num;
    public ClearCondition clearCondition;
    bool isCleared;
    bool isSuperCleared;
    private void FixedUpdate()
    {
        if (isCleared)
        {
            if (transform.childCount == 0&&!isSuperCleared)
            {
                isSuperCleared = true;
                StartCoroutine(DelayPauseGame());
            }
            return;
        }
        if (clearCondition == ClearCondition.ConsumeTillNum)
        {
            if (transform.childCount <= Num)
            {
                Clear();
            }
        }
        //ConsumeAll
        if (transform.childCount == 0)
        {
            isSuperCleared = true;
            StartCoroutine(DelayPauseGame());
            Clear();
        }
    }
    public IEnumerator DelayPauseGame()
    {
        float delayTime = Time.timeSinceLevelLoad + 6f;

        while (Time.timeSinceLevelLoad < delayTime)
        {
            if (PauseMenu.isPaused)
            {
                break;
            }
            yield return new WaitForFixedUpdate();
        }

        if (!PauseMenu.isPaused)
        {
            PauseMenu.Instance.ForcePause();
        }
    }
    public void Clear()
    {
        TutorialManager.instance.TutorialCleared = true;
        TutorialManager.instance.LoadToast(0f,TutorialManager.instance.Toast_Death);
    }
    private void OnEnable()
    {
        if(Time.timeSinceLevelLoad>0.1f)
        {
            GravityManager.Instance.PreRegister_Block(transform);
        }
    }
}
