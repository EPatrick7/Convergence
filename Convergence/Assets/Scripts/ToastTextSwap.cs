using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ToastTextSwap : MonoBehaviour
{
    public string ToSwapIfGamePad;
    private void Start()
    {
        StartCoroutine(DelayedCheck());
    }
    public IEnumerator DelayedCheck()
    {
        yield return new WaitForSeconds(1);
        
        if (InputManager.GamePadDetected)
        {
            GetComponent<TextMeshProUGUI>().text = ToSwapIfGamePad;
        }
    }
}
