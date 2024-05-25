using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ToastTextSwap : MonoBehaviour
{
    public string ToSwapIfGamePad;
    public string ToSwapIfXBox;
    public bool UseAltXBox;
    [HideInInspector]
    public string DefaultText;
    private void Start()
    {
        DefaultText = GetComponent<TextMeshProUGUI>().text;
        StartCoroutine(DelayedCheck());
    }
    public IEnumerator DelayedCheck()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.25f);

            if (InputManager.inputManagers.Count > 0 && InputManager.inputManagers[0].playerInput.devices.Count>0&& InputManager.inputManagers[0].HasGamepad())
            {
                if(UseAltXBox&& InputManager.inputManagers[0].HasXBox())
                {
                    GetComponent<TextMeshProUGUI>().text = ToSwapIfXBox;

                }
                else
                    GetComponent<TextMeshProUGUI>().text = ToSwapIfGamePad;
            }
            else
            {
                GetComponent<TextMeshProUGUI>().text = DefaultText;
            }
        }
    }
}
