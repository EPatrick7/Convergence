using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ToastTextSwap : MonoBehaviour
{
    public string ToSwapIfGamePad;
    public string ToSwapIfXBox;
    public string ToSwapIfSwitch;
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

            if (InputManager.inputManagers.Count > 0 && InputManager.inputManagers[0].playerInput.devices.Count>0&& InputManager.inputManagers[0].HasGamepad())
            {
                if(UseAltXBox&& InputManager.inputManagers[0].HasXBox())
                {
                    GetComponent<TextMeshProUGUI>().text = ToSwapIfXBox;

                }
                else if (UseAltXBox && InputManager.inputManagers[0].HasSwitch())
                {
                    GetComponent<TextMeshProUGUI>().text = ToSwapIfSwitch;

                }
                else
                    GetComponent<TextMeshProUGUI>().text = ToSwapIfGamePad;
            }
            else
            {
                GetComponent<TextMeshProUGUI>().text = DefaultText;
            }
            yield return new WaitForSeconds(0.25f);
        }
    }
}
